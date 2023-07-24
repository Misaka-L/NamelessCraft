namespace NamelessCraft.Models.Options;

public class MicrosoftAuthenticatorOptions
{
    public string XboxLiveApiBaseUrl { get; set; } = "https://user.auth.xboxlive.com";
    public string MinecraftServiceApiUrl { get; set; } = "https://api.minecraftservices.com";
    public string XSTSApiBaseUrl { get; set; } = "https://xsts.auth.xboxlive.com";

    public string MicrosoftAccountAccessToken { get; set; } = "";
    public bool UseMinecraftLauncherClientId { get; set; } = false;

    public HttpClient HttpClient { get; set; } = new();
}