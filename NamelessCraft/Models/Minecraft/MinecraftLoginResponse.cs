using System.Text.Json.Serialization;

namespace NamelessCraft.Models.Minecraft;

public record MinecraftLoginResponse(
    [property: JsonPropertyName("username")] Guid UserName,
    [property: JsonPropertyName("roles")] object[] Roles,
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] long ExpiresIn
    );