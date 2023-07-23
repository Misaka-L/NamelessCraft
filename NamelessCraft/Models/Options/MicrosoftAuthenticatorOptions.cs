namespace NamelessCraft.Models.Options;

public class MicrosoftAuthenticatorOptions
{
    public string XboxLiveApiBaseUrl = "https://user.auth.xboxlive.com";
    public string MinecraftServiceApiUrl = "https://api.minecraftservices.com";
    public string XSTSApiBaseUrl = "https://xsts.auth.xboxlive.com";

    public string MicrosoftAccountAccessToken = "";

    public HttpClient HttpClient = new();
}