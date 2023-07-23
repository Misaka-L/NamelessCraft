namespace NamelessCraft.Models.Xbox;

public record XboxUserAuthenticateResponse(DateTimeOffset IssueInstant, DateTimeOffset NotAfter, string Token,
    XboxDisplayClaims DisplayClaims, long? XErr);

/// <summary>
/// XErr Error Codes
/// </summary>
/// <see href="https://wiki.vg/Microsoft_Authentication_Scheme"/>
public static class XboxError
{
    /// <summary>
    /// The account doesn't have an Xbox account.
    /// </summary>
    /// <remarks>
    /// Once they sign up for one (or login through minecraft.net to create one) then they can proceed with the login.
    /// This shouldn't happen with accounts that have purchased Minecraft with a Microsoft account,
    /// as they would've already gone through that Xbox signup process.
    /// </remarks>
    public const long TheAccountDontHaveAnXboxAccount = 2148916233;

    /// <summary>
    /// The account is from a country where Xbox Live is not available/banned
    /// </summary>
    public const long TheAccountComeFormACountryXboxIsUnavailable = 2148916235;

    /// <summary>
    /// The account needs adult verification on Xbox page. (South Korea)
    /// </summary>
    public const long TheAccountNeedsAdultVerificationA = 2148916236;
    /// <summary>
    /// The account needs adult verification on Xbox page. (South Korea)
    /// </summary>
    public const long TheAccountNeedsAdultVerificationB = 2148916237;

    /// <summary>
    /// The account is a child (under 18) and cannot proceed unless the account is added to a Family by an adult.
    /// </summary>
    /// <remarks>
    /// This only seems to occur when using a custom Microsoft Azure application. When using the Minecraft launchers client id, this doesn't trigger.
    /// </remarks>
    public const long TheAccountIsAChild = 2148916238;
}