using NamelessCraft.Core.Models.Minecraft;

namespace NamelessCraft.Tools;

public static class RuleTools
{
    public static bool IsRulesAllow(IEnumerable<OptionalRule> rules, Dictionary<string, bool> features, string systemName,
        string systemArchitecture, string systemVersion)
    {
        return rules.OrderBy(rule => rule.Action)
            .All(rule => rule.IsRuleAllow(features, systemName, systemArchitecture, systemVersion));
    }
}