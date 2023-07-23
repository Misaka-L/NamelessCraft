using NamelessCraft.Core.Models.Minecraft;

namespace NamelessCraft.Core.Models;

public record GameVersion(MinecraftVersionInfo MinecraftVersionInfo, string VersionJarPath)
{
    public string Id => MinecraftVersionInfo.Id;

    public static GameVersion ParseFromFile(string jsonPath, string minecraftVersionFolderPath)
    {
        var minecraftVersionInfo = MinecraftVersionInfo.ParseFromFile(jsonPath, minecraftVersionFolderPath);

        var jarVersionId = minecraftVersionInfo.InheritsFrom ?? minecraftVersionInfo.Id;
        var versionJarPath = Path.Combine(minecraftVersionFolderPath, jarVersionId , $"{jarVersionId}.jar");

        return new GameVersion(minecraftVersionInfo, versionJarPath);
    }
}