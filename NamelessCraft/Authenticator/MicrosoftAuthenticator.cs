using System.Net.Http.Headers;
using System.Net.Http.Json;
using NamelessCraft.Core;
using NamelessCraft.Core.Models;
using NamelessCraft.Models.Options;
using NamelessCraft.Models.Xbox;
using NamelessCraft.Models.Minecraft;
using NamelessCraft.Tools;

namespace NamelessCraft.Authenticator;

public class MicrosoftAuthenticator : IGameAuthenticator
{
    public MicrosoftAuthenticatorOptions Options = new();

    public MicrosoftAuthenticator()
    {
    }

    public MicrosoftAuthenticator(Action<MicrosoftAuthenticatorOptions> action)
    {
        action(Options);
    }

    public MicrosoftAuthenticator(MicrosoftAuthenticatorOptions options)
    {
        Options = options;
    }

    public async Task<GameAuthenticationResult> AuthenticateAsync()
    {
        if (string.IsNullOrWhiteSpace(Options.MicrosoftAccountAccessToken))
            throw new ArgumentException("Microsoft Account AccessToken can't be empty",
                nameof(MicrosoftAuthenticatorOptions.MicrosoftAccountAccessToken));

        var httpClient = Options.HttpClient;
        var xboxLiveApiBaseUrl = Options.XboxLiveApiBaseUrl;
        var minecraftServiceApiBaseUrl = Options.MinecraftServiceApiUrl;
        var xstsApiBaseUrl = Options.XSTSApiBaseUrl;

        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NamelessCraft", "v0.1.0"));

        try
        {
            // Xbox Live Api
            // Auth with Xbox
            XboxUserAuthenticateResponse? xboxAuthenticateResponse = null;
            try
            {
                var rpsTicket = Options.UseMinecraftLauncherClientId
                    ? "" + Options.MicrosoftAccountAccessToken
                    : "d=" + Options.MicrosoftAccountAccessToken;

                xboxAuthenticateResponse = await httpClient.PostAsJsonAsync<XboxUserAuthenticateResponse>(
                    xboxLiveApiBaseUrl + "/user/authenticate", new
                    {
                        Properties = new
                        {
                            AuthMethod = "RPS",
                            SiteName = "user.auth.xboxlive.com",
                            RpsTicket = rpsTicket
                        },
                        RelyingParty = "http://auth.xboxlive.com",
                        TokenType = "JWT"
                    });
            }
            catch (Exception innerException)
            {
                throw new InvalidOperationException(
                    $"Can't get the response of {xboxLiveApiBaseUrl + "/user/authenticate"}, The Xbox Live Authenticate API may have changed or you provide an invalid token",
                    innerException);
            }

            ThrowAuthenticException(xboxAuthenticateResponse.XErr);

            var xboxLiveToken = xboxAuthenticateResponse.Token;
            var xboxUserHash = xboxAuthenticateResponse.DisplayClaims.Xui.First().UserHash;

            // XSTS API
            XboxUserAuthenticateResponse? xstsAuthorizeResponse = null;
            try
            {
                xstsAuthorizeResponse = await httpClient.PostAsJsonAsync<XboxUserAuthenticateResponse>(
                    xstsApiBaseUrl + "/xsts/authorize", new
                    {
                        Properties = new
                        {
                            SandboxId = "RETAIL",
                            UserTokens = new[] { xboxLiveToken }
                        },
                        RelyingParty = "rp://api.minecraftservices.com/",
                        TokenType = "JWT"
                    });
            }
            catch (Exception innerException)
            {
                throw new InvalidOperationException(
                    $"Can't get the response of {xboxLiveApiBaseUrl + "/xsts/authorize"}, The Xbox Live XSTS Authorize API may have changed",
                    innerException);
            }

            ThrowAuthenticException(xboxAuthenticateResponse.XErr);

            var xstsToken = xstsAuthorizeResponse.Token;

            // Minecraft Service Api
            // Login With Xbox
            MinecraftLoginResponse? minecraftLoginResponse = null;
            try
            {
                minecraftLoginResponse = await httpClient.PostAsJsonAsync<MinecraftLoginResponse>(
                    minecraftServiceApiBaseUrl + "/authentication/login_with_xbox", new
                    {
                        identityToken = $"XBL3.0 x={xboxUserHash};{xstsToken}"
                    });
            }
            catch (Exception innerException)
            {
                throw new InvalidOperationException(
                    $"Can't get the response of {minecraftServiceApiBaseUrl + "/authentication/login_with_xbox"}, The Minecraft service API may have changed or you use an azure application which didn't pass the review?",
                    innerException);
            }

            if (minecraftLoginResponse.AccessToken is not {} minecraftToken)
            {
                throw new InvalidOperationException(
                    $"Can't get the response of {minecraftServiceApiBaseUrl + "/authentication/login_with_xbox"}, The Minecraft service API may have changed or you use an azure application which didn't pass the review?");
            }
            
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", minecraftToken);

            // Get Minecraft Profile
            MinecraftProfileResponse? minecraftProfileResponse = null;
            try
            {
                minecraftProfileResponse =
                    await httpClient.GetAsync<MinecraftProfileResponse>(minecraftServiceApiBaseUrl +
                                                                        "/minecraft/profile");
            }
            catch (Exception innerException)
            {
                throw new InvalidOperationException(
                    $"Can't get the response of {minecraftServiceApiBaseUrl + "/authentication/login_with_xbox"}, The Minecraft service API may have changed",
                    innerException);
            }

            if (minecraftProfileResponse is not { Id: { } playerId, Name: { } playerName })
                throw new InvalidOperationException(
                    $"Can't get uuid and name from {minecraftServiceApiBaseUrl + "/authentication/login_with_xbox"}, Did the account owns Minecraft?");

            return new GameAuthenticationResult(playerName, minecraftToken, Guid.Parse(playerId),
                AuthenticationType.Microsoft);
        }
        catch (Exception e)
        {
            throw new Exception("Authenticate Fail", e);
        }
    }

    private void ThrowAuthenticException(long? xboxError)
    {
        if (xboxError == null) return;

        throw xboxError switch
        {
            XboxError.TheAccountIsAChild => new InvalidOperationException(
                $"({xboxError}) The account is a child (under 18) and cannot proceed unless the account is added to a Family by an adult."),
            XboxError.TheAccountDontHaveAnXboxAccount => new InvalidOperationException(
                $"({xboxError}) The account doesn't have an Xbox account."),
            XboxError.TheAccountNeedsAdultVerificationA | XboxError.TheAccountNeedsAdultVerificationB => new
                InvalidOperationException(
                    $"({xboxError}) The account needs adult verification on Xbox page. (South Korea)"),
            XboxError.TheAccountComeFormACountryXboxIsUnavailable => new InvalidOperationException(
                $"({xboxError}) The account is from a country where Xbox Live is not available/banned."),
            _ => new InvalidOperationException($"Unknown Xbox Error Code ({xboxError})")
        };
    }
}