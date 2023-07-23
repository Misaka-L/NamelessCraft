using System.Text.RegularExpressions;
using Microsoft.Win32;
using NamelessCraft.Core.Models;

namespace NamelessCraft.Tools;

public static partial class JvmTools
{
    public static readonly string[] JvmRuntimeVendors =
        { "Java", "BellSoft", "AdoptOpenJDK", "Zulu", "Microsoft", "Eclipse Foundation", "Semeru", "Eclipse Adoptium" };

    public static async Task<JvmRuntime[]> LookupJvmRuntimesAsync()
    {
        var jvmRuntimes = new List<JvmRuntime>();

        if (OperatingSystem.IsWindows())
        {
            jvmRuntimes.AddRange(LookupJvmRuntimesInRegistry(@"SOFTWARE\JavaSoft\Java Runtime Environment\"));
            jvmRuntimes.AddRange(LookupJvmRuntimesInRegistry(@"SOFTWARE\JavaSoft\Java Development Kit\"));
            jvmRuntimes.AddRange(LookupJvmRuntimesInRegistry(@"SOFTWARE\JavaSoft\JRE\"));
            jvmRuntimes.AddRange(LookupJvmRuntimesInRegistry(@"SOFTWARE\JavaSoft\JDK\"));

            jvmRuntimes.AddRange(
                await LookupJvmRuntimesInProgramFiles(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)));
            jvmRuntimes.AddRange(
                await LookupJvmRuntimesInProgramFiles(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)));
        }
        else if (OperatingSystem.IsLinux())
        {
            jvmRuntimes.AddRange(
                await LookupJvmRuntimeInDirectories("/usr/java"));
            jvmRuntimes.AddRange(
                await LookupJvmRuntimeInDirectories("/usr/lib/jvm"));
            jvmRuntimes.AddRange(
                await LookupJvmRuntimeInDirectories("/usr/lib32/jvm"));
        }
        else if (OperatingSystem.IsMacOS())
        {
            // TODO
        }

        jvmRuntimes.AddRange(await GetMinecraftLauncherJvmRuntime());

        return jvmRuntimes
            .GroupBy(jvmRuntime => jvmRuntime.Path)
            .Select(group => group.First())
            .ToArray();
    }

    private static async Task<JvmRuntime[]> GetMinecraftLauncherJvmRuntime()
    {
        var minecraftLauncherJvmRuntimePath = GetMinecraftLauncherJvmRuntimePath();
        if (minecraftLauncherJvmRuntimePath == null || !Directory.Exists(minecraftLauncherJvmRuntimePath))
            return Array.Empty<JvmRuntime>();

        var jvmRuntimes = new List<JvmRuntime>();

        foreach (var jvmRuntimePath in Directory.GetDirectories(minecraftLauncherJvmRuntimePath))
        {
            var jvmPlatformPaths = Directory.GetDirectories(jvmRuntimePath);
            foreach (var jvmPlatformPath in jvmPlatformPaths)
            {
                foreach (var realJvmPlatformPath in Directory.GetDirectories(jvmPlatformPath))
                {
                    if (await LookupJvmRuntimeInDirectory(realJvmPlatformPath) is { } runtime)
                        jvmRuntimes.Add(runtime);
                }
            }
        }

        return jvmRuntimes.ToArray();
    }

    private static string? GetMinecraftLauncherJvmRuntimePath()
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Join(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Packages\\Microsoft.4297127D64EC6_8wekyb3d8bbwe\\LocalCache\\Local\\runtime\\");
        }

        if (OperatingSystem.IsLinux())
        {
            return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".minecraft/runtime/");
        }

        if (OperatingSystem.IsMacOS())
        {
            return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library/Application Support/.minecraft/runtime");
        }

        return null;
    }

    private static async Task<JvmRuntime[]> LookupJvmRuntimesInProgramFiles(string path)
    {
        var jvmRuntimes = new List<JvmRuntime>();

        foreach (var jvmRuntimeVendor in JvmRuntimeVendors)
        {
            var jvmVendorPath = Path.Combine(path, jvmRuntimeVendor);

            if (!Directory.Exists(jvmVendorPath)) continue;
            var jvmDirectories = Directory.GetDirectories(jvmVendorPath);

            foreach (var jvmDirectory in jvmDirectories)
            {
                if (await LookupJvmRuntimeInDirectory(jvmDirectory) is { } runtime)
                    jvmRuntimes.Add(runtime);
            }
        }

        return jvmRuntimes.ToArray();
    }

    private static async Task<JvmRuntime?> LookupJvmRuntimeInDirectory(string path)
    {
        var releaseFilePath = Path.Combine(path, "release");
        if (!File.Exists(releaseFilePath)) return null;

        var releaseFile = await File.ReadAllTextAsync(releaseFilePath);
        var jvmFullVersion =
            JvmVersionReleaseFileRegex().Matches(releaseFile).First().Groups[1].Value;
        var jvmMajorVersion = GetJvmMajorVersion(jvmFullVersion);

        return new JvmRuntime(jvmFullVersion, jvmMajorVersion, path);
    }

    private static async Task<JvmRuntime[]> LookupJvmRuntimeInDirectories(string path)
    {
        if (!Directory.Exists(path)) return Array.Empty<JvmRuntime>();
        var jvmRuntimes = new List<JvmRuntime>();

        foreach (var directory in Directory.GetDirectories(path))
        {
            if (await LookupJvmRuntimeInDirectory(directory) is { } runtime)
                jvmRuntimes.Add(runtime);
        }

        return jvmRuntimes.ToArray();
    }

    private static JvmRuntime[] LookupJvmRuntimesInRegistry(string path)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException($"{nameof(LookupJvmRuntimesInRegistry)} only support Windows");

        var jvmHomes = new List<JvmRuntime>();

        using var keys = Registry.LocalMachine.OpenSubKey(path);

        if (keys == null) return Array.Empty<JvmRuntime>();

        var subKeys = keys.GetSubKeyNames();
        foreach (var subKey in subKeys)
        {
            using var javaKey = keys.OpenSubKey(subKey);
            using var javaMsiKey = javaKey?.OpenSubKey("MSI");

            if (javaMsiKey?.GetValue("INSTALLDIR") is not string installDir ||
                javaMsiKey.GetValue("FullVersion") is not string fullVersion)
                continue;

            var jvmMajorVersion = GetJvmMajorVersion(fullVersion);

            installDir = Path.EndsInDirectorySeparator(installDir)
                ? installDir.Remove(installDir.Length - 1)
                : installDir;

            jvmHomes.Add(new JvmRuntime(fullVersion, jvmMajorVersion, installDir));
        }

        return jvmHomes.ToArray();
    }

    private static long GetJvmMajorVersion(string version)
    {
        var jvmVersionMatches = JvmVersionRegex().Matches(version);
        if (jvmVersionMatches.Count != 0)
        {
            var majorVersion = jvmVersionMatches.First().Groups[1].Value;
            if (majorVersion != "1")
                return long.Parse(majorVersion);
        }

        var jvmMajorVersionMatches = JvmMajorVersionRegex().Matches(version);
        if (jvmMajorVersionMatches.Count != 0)
        {
            return long.Parse(jvmMajorVersionMatches.First().Groups[1].Value);
        }

        return -1;
    }

    [GeneratedRegex("1\\.([0-9])+")]
    private static partial Regex JvmMajorVersionRegex();

    [GeneratedRegex("([0-9]+)")]
    private static partial Regex JvmVersionRegex();

    [GeneratedRegex("JAVA_VERSION=\"(.+)\\\"", RegexOptions.Multiline)]
    private static partial Regex JvmVersionReleaseFileRegex();
}