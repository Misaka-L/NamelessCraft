using System.Text.Json.Serialization;

namespace NamelessCraft.Models.Xbox;

public record XboxDisplayClaims([property: JsonPropertyName("xui")] XboxDisplayClaimItem[] Xui);
public record XboxDisplayClaimItem([property: JsonPropertyName("uhs")] string UserHash);