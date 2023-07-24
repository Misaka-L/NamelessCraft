using System.Diagnostics;
using System.IO.Compression;
using System.Text;
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

        var jvmRuntimePath = Options.JvmRuntimePath;
        if (jvmRuntimePath == null)
        {
            var jvmRuntimes = await JvmTools.LookupJvmRuntimesAsync();
            if (info.MinecraftVersionInfo.RequiredJavaVersion is { } requiredJavaVersion)
            {
                var tempJvmRuntimes = jvmRuntimes.Where(jvm => jvm.MajorVersion >= requiredJavaVersion.MajorVersion)
                    .OrderBy(jvm => jvm.MajorVersion).ToArray();

                if (tempJvmRuntimes.Length == 0)
                    throw new InvalidOperationException(
                        $"No JVM runtime available (required {requiredJavaVersion.MajorVersion})");

                jvmRuntimePath = tempJvmRuntimes.First().GetJavaExecutable();
            }
            else
            {
                var tempJvmRuntimes = jvmRuntimes.OrderByDescending(jvm => jvm.MajorVersion).ToArray();
                if (tempJvmRuntimes.Length == 0)
                    throw new InvalidOperationException("No JVM runtime available");

                jvmRuntimePath = tempJvmRuntimes.First().GetJavaExecutable();
            }
        }

        var process = Process.Start(new ProcessStartInfo()
        {
            FileName = jvmRuntimePath,
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

        var versionJarPath = Options.VersionJarPath ?? Options.MinecraftVersionInfo.VersionJarPath;
        var nativeFolderPath = await DecompressionNativeLibraries(
            Options.MinecraftVersionInfo.MinecraftVersionInfo.Libraries, Options.LibrariesDirectoryPath, systemName);

        var features = Options.LaunchFeatures;
        var arguments = Options.LaunchArguments;

        arguments.Add("auth_player_name", result.UserName);
        arguments.Add("version_name", info.Id);
        arguments.Add("game_directory", Options.GameDirectory);
        arguments.Add("assets_root", Options.AssetsDirectoryPath);
        arguments.Add("game_assets", Options.AssetsDirectoryPath);
        arguments.Add("assets_index_name", info.MinecraftVersionInfo.Assets ?? info.Id);
        arguments.Add("auth_uuid", result.Uuid.ToMinecraftUuid());
        arguments.Add("auth_access_token", result.AccessToken);
        arguments.Add("user_type", result.AuthenticationType.ToLaunchArgument());
        arguments.Add("version_type",
            string.IsNullOrWhiteSpace(Options.CustomVersionType)
                ? info.MinecraftVersionInfo.Type
                : Options.CustomVersionType);
        arguments.Add("launcher_name", Options.LauncherName);
        arguments.Add("launcher_version", Options.LauncherVersion);
        arguments.Add("natives_directory", nativeFolderPath);
        arguments.Add("user_properties", "{}");

        arguments.Add("classpath", GenClassPath(info.MinecraftVersionInfo.Libraries, features, "windows", "x86",
            systemVersion, Options.LibrariesDirectoryPath,
            versionJarPath));

        var args = GenLaunchArgs(info.MinecraftVersionInfo, features, arguments, systemName, systemArchitecture,
            systemVersion);

        return args;
    }

    private static string GenLaunchArgs(MinecraftVersionInfo versionInfo, Dictionary<string, bool> features,
        Dictionary<string, string> arguments, string systemName, string systemArchitecture, string systemVersion)
    {
        if (versionInfo.LegacyLaunchArguments is { } legacyLaunchArguments)
        {
            var jvmArgs = GenJvmArguments(arguments);
            var gameArgs = ReplaceArgumentTemplate(legacyLaunchArguments, arguments);
            return $"{jvmArgs} {versionInfo.MainClass} {gameArgs}";
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

    private static string GenLaunchArgsInternal(MinecraftGameLaunchArgument[] launchArguments,
        Dictionary<string, bool> features,
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

    private static string GenJvmArguments(Dictionary<string, string> arguments)
    {
        return ReplaceArgumentTemplate(
            "-Djava.library.path=${natives_directory} " +
            "-Djna.tmpdir=${natives_directory} " +
            "-Dorg.lwjgl.system.SharedLibraryExtractPath=${natives_directory} " +
            "-Dminecraft.launcher.brand=${launcher_name} " +
            "-Dminecraft.launcher.version=${launcher_version} " +
            "-cp ${classpath}",
            arguments);
    }

    private static string ReplaceArgumentTemplate(string template, Dictionary<string, string> arguments)
    {
        var argBuilder = new StringBuilder(template);
        foreach (Match match in ArgumentTemplateRegex().Matches(template))
        {
            if (!arguments.TryGetValue(match.Groups[1].Value, out var value)) continue;

            argBuilder.Replace(match.Groups[0].Value, value.Contains(' ') ? $"\"{value}\"" : value);
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

            var libraryPath = library.Downloads?.Artifact?.Path;
            if (libraryPath == null)
            {
                var librarySplit = library.Name.Split(":");
                var libraryPackage = librarySplit[0];
                var libraryName = librarySplit[1];
                var libraryVersion = librarySplit[2];

                libraryPath = Path.Combine(libraryPackage.Split("."));
                libraryPath = Path.Combine(libraryPath, libraryName, libraryVersion,
                    $"{libraryName}-{libraryVersion}.jar");
            }

            classPathBuilder.Append(Path.Join(librariesBasePath, libraryPath) + separator);
        }

        classPathBuilder.Append(versionJarPath + separator);
        return classPathBuilder.ToString();
    }

    private static async ValueTask<string> DecompressionNativeLibraries(IEnumerable<Library> libraries,
        string librariesBasePath,
        string systemName)
    {
        var tempDirectory = Directory.CreateTempSubdirectory("nameless_launcher_native_libraries_");
        var tempDirectoryPath = tempDirectory.ToString();
        var nativeLibraries = GetNativeLibraryInfosForSystem(libraries, systemName);

        foreach (var nativeLibrary in nativeLibraries)
        {
            var nativeLibraryPath = Path.Combine(librariesBasePath, nativeLibrary.NativeLibraryInfo.Path);
            if (!File.Exists(nativeLibraryPath))
                throw new InvalidOperationException($"Native library {nativeLibraryPath} not exists");

            try
            {
                await Task.Run(() =>
                {
                    var extraSetting = new NativeLibraryExtractSetting();
                    if (nativeLibrary.NativeLibraryExtractSetting != null)
                    {
                        extraSetting = nativeLibrary.NativeLibraryExtractSetting;
                    }

                    var nativeLibraryZip = ZipFile.OpenRead(nativeLibraryPath);
                    foreach (var zipArchiveEntry in nativeLibraryZip.Entries)
                    {
                        if (extraSetting.Exclude.Contains(zipArchiveEntry.FullName) ||
                            extraSetting.Exclude.Select(exclude =>
                                    Path.EndsInDirectorySeparator(exclude) &&
                                    zipArchiveEntry.FullName.StartsWith(exclude))
                                .Order().First())
                            continue;

                        if (zipArchiveEntry.Name.Length == 0)
                        {
                            Directory.CreateDirectory(Path.Combine(tempDirectoryPath, zipArchiveEntry.FullName));
                        }
                        else
                        {
                            zipArchiveEntry.ExtractToFile(Path.Combine(tempDirectoryPath, zipArchiveEntry.FullName));
                        }
                    }
                });
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Fail to decompression native library {nativeLibraryPath}", e);
            }
        }

        Debug.WriteLine(tempDirectoryPath);
        return tempDirectoryPath;
    }

    private static IEnumerable<NativeLibraryExtraInfo> GetNativeLibraryInfosForSystem(IEnumerable<Library> libraries,
        string systemName)
    {
        var tempNativeLibraries =
            libraries.Where(library => library.NativeLibraryInfo != null);

#pragma warning disable CS8602
        switch (systemName)
        {
            case MinecraftOSIdentify.Windows:
                return tempNativeLibraries.Where(library =>
                        library.NativeLibraryInfo?.NativeLibraryWindowsKey != null &&
                        library.Downloads?.Classifiers?.NativeLibraryInfoWindows != null)
                    .Select(library =>
                        new NativeLibraryExtraInfo(library.Downloads.Classifiers.NativeLibraryInfoWindows,
                            library.NativeLibraryExtractSetting)).ToArray();
            case MinecraftOSIdentify.Linux:
                return tempNativeLibraries.Where(library =>
                        library.NativeLibraryInfo?.NativeLibraryLinuxKey != null &&
                        library.Downloads?.Classifiers?.NativeLibraryInfoLinux != null)
                    .Select(library => new NativeLibraryExtraInfo(library.Downloads.Classifiers.NativeLibraryInfoLinux,
                        library.NativeLibraryExtractSetting)).ToArray();
            case MinecraftOSIdentify.Osx:
                return tempNativeLibraries.Where(library =>
                        library.NativeLibraryInfo?.NativeLibraryOsXKey != null &&
                        library.Downloads?.Classifiers?.NativeLibraryInfoOsx != null)
                    .Select(library => new NativeLibraryExtraInfo(library.Downloads.Classifiers.NativeLibraryInfoOsx,
                        library.NativeLibraryExtractSetting)).ToArray();
        }
#pragma warning restore CS8602

        return Array.Empty<NativeLibraryExtraInfo>();
    }

    [GeneratedRegex(@"\$\{(.+?)\}")]
    private static partial Regex ArgumentTemplateRegex();
}