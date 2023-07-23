namespace NamelessCraft.Core.Models.Minecraft;

public static class MinecraftOSIdentify
{
    public const string Windows = "windows";
    public const string Linux = "linux";
    public const string Osx = "osx";
    // For Nameless Launcher internal logic
    public const string Other = "other";
}

public static class MinecraftOSArchitectureIdentify
{
    public const string X86 = "x86";
    public const string X64 = "x86_64";
    public const string Arm64 = "arm64";
    // For Nameless Launcher internal logic
    public const string Unknown = "unknown";
}