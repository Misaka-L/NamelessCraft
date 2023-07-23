namespace NamelessCraft.Core.Models;

public record GameAuthenticationResult(string UserName, string AccessToken, Guid Uuid, AuthenticationType AuthenticationType);

public enum AuthenticationType
{
    Microsoft,
    Mojang
}

public static class AuthenticationTypeExtenstion
{
    public static string ToLaunchArgument(this AuthenticationType type)
    {
        return type switch
        {
            AuthenticationType.Microsoft => "msa",
            AuthenticationType.Mojang => "mojang",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}