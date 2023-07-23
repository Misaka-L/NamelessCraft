using System.Text.Json.Serialization;

namespace NamelessCraft.Core.Models.Minecraft;

public class MinecraftModernLaunchArgument
{
    [JsonPropertyName("game")] public MinecraftGameLaunchArgument[] GameLaunchArguments { get; set; }

    [JsonPropertyName("jvm")] public MinecraftGameLaunchArgument[] JvmLaunchArguments { get; set; }
}

public struct MinecraftGameLaunchArgument
{
    public MinecraftOptionalLaunchArgument? OptionalLaunchArgument;
    public string String;

    public static implicit operator MinecraftGameLaunchArgument(MinecraftOptionalLaunchArgument optionalLaunchArgument) =>
        new() { OptionalLaunchArgument = optionalLaunchArgument };

    public static implicit operator MinecraftGameLaunchArgument(string String) => new MinecraftGameLaunchArgument { String = String };
}

public class MinecraftOptionalLaunchArgument
{
    [JsonPropertyName("rules")] public MinecraftOptionalRule[] Rules { get; set; }

    [JsonPropertyName("value")] public OptionalLaunchArgumentValue Value { get; set; }
}

public struct OptionalLaunchArgumentValue
{
    public string? String;
    public string[]? StringArray;

    public static implicit operator OptionalLaunchArgumentValue(string String) =>
        new() { String = String };

    public static implicit operator OptionalLaunchArgumentValue(string[] StringArray) =>
        new() { StringArray = StringArray };
}