using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NamelessCraft.Core.Tools;

namespace NamelessCraft.Core.Models.Minecraft;

public class MinecraftOptionalRule
{
    [JsonPropertyName("action")] public OptionalLaunchArgumentRuleAction Action { get; set; }

    [JsonPropertyName("features")] public Dictionary<string, bool>? OptionalLaunchArgumentFeature { get; set; }

    [JsonPropertyName("os")] public OptionalOsRule? OptionalOsRule { get; set; }

    public bool IsRuleAllow(Dictionary<string, bool> features, string systemName = "",
        string systemArchitecture = "", string systemVersion = "") =>
        IsRuleAllow(this, features, systemName, systemArchitecture, systemVersion);

    public static bool IsRuleAllow(MinecraftOptionalRule rule, Dictionary<string, bool> features, string systemName = "",
        string systemArchitecture = "", string systemVersion = "")
    {
        if (string.IsNullOrWhiteSpace(systemName)) systemName = OSVersionTools.GetOSName();
        if (string.IsNullOrWhiteSpace(systemArchitecture)) systemArchitecture = OSVersionTools.GetOSArchitecture();
        if (string.IsNullOrWhiteSpace(systemVersion)) systemVersion = OSVersionTools.GetOSVersion();

        if (rule.OptionalOsRule is { } osRule)
        {
            if (osRule.Arch is { } arch && arch != systemArchitecture) return IsRuleAllow(rule.Action, false);
            if (osRule.Name is { } name && name != systemName) return IsRuleAllow(rule.Action, false);
            if (osRule.VersionRegex is { } versionRegex && !Regex.IsMatch(systemVersion, versionRegex))
                return IsRuleAllow(rule.Action, false);
        }

        if (rule.OptionalLaunchArgumentFeature is not { } featuresRule)
            return IsRuleAllow(rule.Action, true);

        foreach (var (key, value) in featuresRule)
        {
            if (features.TryGetValue(key, out var featureValue) && featureValue != value)
            {
                return IsRuleAllow(rule.Action, false);
            }

            return IsRuleAllow(rule.Action, false);
        }

        return IsRuleAllow(rule.Action, true);
    }

    private static bool IsRuleAllow(OptionalLaunchArgumentRuleAction action, bool value) =>
        action == OptionalLaunchArgumentRuleAction.Allow && value;
}

public class OptionalOsRule
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("arch")]
    public string? Arch { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("version")]
    public string? VersionRegex { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OptionalLaunchArgumentRuleAction
{
    Disallow,
    Allow
}