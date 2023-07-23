using System.Text.Json.Serialization;

namespace NamelessCraft.Models.Minecraft;

public record MinecraftProfileResponse(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name
);