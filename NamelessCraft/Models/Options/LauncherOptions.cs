using NamelessCraft.Authenticator;
using NamelessCraft.Core;
using NamelessCraft.Core.Models;
using NamelessCraft.Core.Models.Minecraft;

namespace NamelessCraft.Models.Options;

public class LauncherOptions
{
    public IGameAuthenticator Authenticator { get; set; } = new OfflineAuthenticator("nameless");
    public string CustomJvmArguments { get; set; } = "";
    public string CustomGameArguments { get; set; } = "";

    public MinecraftVersionInfo? MinecraftVersionInfo { get; set; }

    public JvmRuntime? JvmRuntime { get; set; }

    public string GameDirectory { get; set; } = "";
    public string AssetsDirectoryPath { get; set; } = "";
    public string LibrariesDirectoryPath { get; set; } = "";
    public string VersionJarPath { get; set; } = "";

    public Dictionary<string, string> LaunchArguments { get; set; } = new();
    public Dictionary<string, bool> LaunchFeatures { get; set; } = new();

    public string LauncherName { get; set; } = "NamelessLauncher";
    public string LauncherVersion { get; set; } = "v0.1.0";
    public string CustomVersionType { get; set; } = "";
}