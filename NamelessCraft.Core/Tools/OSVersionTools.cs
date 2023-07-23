using System.Runtime.InteropServices;
using NamelessCraft.Core.Models.Minecraft;

namespace NamelessCraft.Core.Tools;

public static class OSVersionTools
{
    public static string GetOSName()
    {
        // DO NOT USE PlatformID.MacOSX
        // see https://learn.microsoft.com/en-us/dotnet/api/System.PlatformID?view=net-7.0
        // see also https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Environment.OSVersion.OSX.cs#L12-L14
        
        if (OperatingSystem.IsMacOS()) return MinecraftOSIdentify.Osx;
        if (OperatingSystem.IsWindows()) return MinecraftOSIdentify.Windows;
        if (OperatingSystem.IsLinux()) return MinecraftOSIdentify.Linux;

        return MinecraftOSIdentify.Other;
    }

    public static string GetOSArchitecture()
    {
        return RuntimeInformation.OSArchitecture switch
        {
            Architecture.X86 => MinecraftOSArchitectureIdentify.X86,
            Architecture.X64 => MinecraftOSArchitectureIdentify.X64,
            Architecture.Arm64 => MinecraftOSArchitectureIdentify.Arm64,
            _ => MinecraftOSArchitectureIdentify.Unknown
        };
    }

    public static string GetOSVersion()
    {
        return Environment.OSVersion.VersionString;
    }
}