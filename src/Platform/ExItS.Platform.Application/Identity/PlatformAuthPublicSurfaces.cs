using ExItS.Platform.Application.Common;

namespace ExItS.Platform.Application.Identity;

/// <summary>
/// Server-selected originating surfaces for public EmailVerification and PasswordReset links.
/// Arbitrary callback URLs are never accepted.
/// </summary>
public static class PlatformAuthPublicSurfaces
{
    public const string PinoyLoanManager = "pinoy-loan-manager";

    public static ApplicationResult<string?> Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ApplicationResult<string?>.Success(null);
        }

        var trimmed = value.Trim();
        if (string.Equals(trimmed, PinoyLoanManager, StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<string?>.Success(PinoyLoanManager);
        }

        return ApplicationResult<string?>.Failure(
            ApplicationErrorCodes.AuthPublicSurfaceInvalid,
            "Unknown auth public surface.");
    }
}
