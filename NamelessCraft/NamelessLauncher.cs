using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NamelessCraft.Core;
using NamelessCraft.Core.Models;
using NamelessCraft.Core.Models.Minecraft;
using NamelessCraft.Core.Tools;
using NamelessCraft.Models.Options;
using NamelessCraft.Tools;

namespace NamelessCraft;

public partial class NamelessLauncher : ILauncher
{
    public readonly LauncherOptions Options;
    
    public event DataReceivedEventHandler? GameOutputDataReceived;
    public event EventHandler<EventArgs>? GameExited;

    public NamelessLauncher(LauncherOptions options)
    {
        Options = options;
    }

    public NamelessLauncher(Action<LauncherOptions> action)
    {
        var options = new LauncherOptions();
        action(options);

        Options = options;
    }

    public async Task StartAsync()
    {
        if (Options.MinecraftVersionInfo is not { } info)
            throw new ArgumentNullException(nameof(Options.MinecraftVersionInfo),
                "you must provide a minecraft version info");

        var args = await GetLaunchCommandLineInternal();

        Debug.WriteLine($"{args}");
        Console.WriteLine($"{args}");

        var jvmRuntime = Options.JvmRuntime;
        if (jvmRuntime == null)
        {
            var jvmRuntimes = await JvmTools.LookupJvmRuntimesAsync();
            if (info.RequiredJavaVersion is { } requiredJavaVersion)
            {
                jvmRuntime = jvmRuntimes.Where(jvm => jvm.MajorVersion >= requiredJavaVersion.MajorVersion)
                    .OrderBy(jvm => jvm.MajorVersion)
                    .First();
            }
            else
            {
                jvmRuntime = jvmRuntimes.OrderByDescending(jvm => jvm.MajorVersion)
                    .First();
            }
        }

        var process = Process.Start(new ProcessStartInfo()
        {
            FileName = jvmRuntime.GetJavaExecutable(),
            WorkingDirectory = Options.GameDirectory,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            StandardOutputEncoding = Encoding.UTF8,
            RedirectStandardError = true,
            StandardErrorEncoding = Encoding.UTF8
        });

        if (process == null)
            throw new InvalidOperationException("Can't start the process");

        process.EnableRaisingEvents = true;
        
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.OutputDataReceived += (_, eventArgs) => GameOutputDataReceived?.Invoke(this, eventArgs);
        process.ErrorDataReceived += (_, eventArgs) => GameOutputDataReceived?.Invoke(this, eventArgs);
        process.Exited += (_, eventArgs) => GameExited?.Invoke(this, eventArgs);
    }

    public async Task<string> GetLaunchCommandLineAsync()
    {
        return await GetLaunchCommandLineInternal();
    }

    private async Task<string> GetLaunchCommandLineInternal()
    {
        if (Options.MinecraftVersionInfo is not { } info)
            throw new ArgumentNullException(nameof(Options.MinecraftVersionInfo),
                "you must provide a minecraft version info");

        var result = await Options.Authenticator.AuthenticateAsync();

        var systemName = OSVersionTools.GetOSName();
        var systemArchitecture = OSVersionTools.GetOSArchitecture();
        var systemVersion = OSVersionTools.GetOSVersion();

        var features = Options.LaunchFeatures;
        var arguments = Options.LaunchArguments;

        arguments.Add("auth_player_name", result.UserName);
        arguments.Add("version_name", info.Id);
        arguments.Add("game_directory", "D:/Minecraft/BakaXL/.minecraft/versions/1.20.1/");
        arguments.Add("assets_root", "D:/Minecraft/BakaXL/.minecraft/assets");
        arguments.Add("game_assets", "D:/Minecraft/BakaXL/.minecraft/assets");
        arguments.Add("assets_index_name", info.Assets);
        arguments.Add("auth_uuid", result.Uuid.ToMinecraftUuid());
        arguments.Add("auth_access_token", result.AccessToken);
        arguments.Add("user_type", result.AuthenticationType.ToLaunchArgument());
        arguments.Add("version_type",
            string.IsNullOrWhiteSpace(Options.CustomVersionType) ? info.Type : Options.CustomVersionType);
        arguments.Add("launcher_name", Options.LauncherName);
        arguments.Add("launcher_version", Options.LauncherVersion);
        arguments.Add("natives_directory",
            Path.Join(Path.GetTempPath(), Path.GetRandomFileName() + Path.DirectorySeparatorChar));

        arguments.Add("classpath", GenClassPath(info.Libraries, features, "windows", "x86",
            systemVersion, "D:/Minecraft/BakaXL/.minecraft/libraries",
            "D:/Minecraft/BakaXL/.minecraft/versions/1.20.1/1.20.1.jar"));

        var args = GenLaunchArgs(info, features, arguments, systemName, systemArchitecture, systemVersion);

        return args;
    }

    private static string GenLaunchArgs(MinecraftVersionInfo versionInfo, Dictionary<string, bool> features,
        Dictionary<string, string> arguments, string systemName, string systemArchitecture, string systemVersion)
    {
        if (versionInfo.LegacyLaunchArguments is { } legacyLaunchArguments)
        {
            return ReplaceArgumentTemplate(legacyLaunchArguments, arguments);
        }

        if (versionInfo.ModernLaunchArguments is not { } launchArgument)
            throw new ArgumentNullException(nameof(versionInfo.ModernLaunchArguments),
                "you need at least a legacyLaunchArguments or modernLaunchArguments");

        var argsBuilder = new StringBuilder();
        argsBuilder.Append(GenLaunchArgsInternal(launchArgument.JvmLaunchArguments, features, arguments, systemName,
            systemArchitecture, systemVersion));
        argsBuilder.Append($"{versionInfo.MainClass} ");
        argsBuilder.Append(GenLaunchArgsInternal(launchArgument.GameLaunchArguments, features, arguments, systemName,
            systemArchitecture, systemVersion));

        return argsBuilder.ToString();
    }

    private static string GenLaunchArgsInternal(GameLaunchArgument[] launchArguments, Dictionary<string, bool> features,
        Dictionary<string, string> arguments, string systemName, string systemArchitecture, string systemVersion)
    {
        var argsBuilder = new StringBuilder();

        foreach (var argument in launchArguments)
        {
            if (argument.String is { } argumentString)
            {
                argsBuilder.Append($"{ReplaceArgumentTemplate(argumentString, arguments)} ");
            }
            else if (argument.OptionalLaunchArgument is { } optionalLaunchArgument &&
                     RuleTools.IsRulesAllow(optionalLaunchArgument.Rules, features, systemName, systemArchitecture,
                         systemVersion))
            {
                if (optionalLaunchArgument.Value.String is { } valueString)
                {
                    argsBuilder.Append($"{ReplaceArgumentTemplate(valueString, arguments)} ");
                }
                else if (optionalLaunchArgument.Value.StringArray is { } valueStringArray)
                {
                    foreach (var template in valueStringArray)
                    {
                        argsBuilder.Append($"{ReplaceArgumentTemplate(template, arguments)} ");
                    }
                }
            }
        }

        return argsBuilder.ToString();
    }

    private static string ReplaceArgumentTemplate(string template, Dictionary<string, string> arguments)
    {
        var argBuilder = new StringBuilder(template);
        foreach (Match match in ArgumentTemplateRegex().Matches(template))
        {
            if (arguments.TryGetValue(match.Groups[1].Value, out var value))
            {
                argBuilder.Replace(match.Groups[0].Value, value);
            }
        }

        return argBuilder.ToString();
    }

    private static string GenClassPath(Library[] libraries, Dictionary<string, bool> features, string systemName,
        string systemArchitecture, string systemVersion, string librariesBasePath, string versionJarPath)
    {
        var classPathBuilder = new StringBuilder();
        var separator = systemName == MinecraftOSIdentify.Windows ? ";" : ":";

        foreach (var library in libraries)
        {
            if (library.Rules != null &&
                !RuleTools.IsRulesAllow(library.Rules, features, systemName, systemArchitecture, systemVersion))
                continue;

            classPathBuilder.Append(Path.Join(librariesBasePath, library.Downloads.Artifact.Path) + separator);
        }

        classPathBuilder.Append(versionJarPath + separator);
        return classPathBuilder.ToString();
    }

    [GeneratedRegex(@"\$\{(.+)\}")]
    private static partial Regex ArgumentTemplateRegex();
}