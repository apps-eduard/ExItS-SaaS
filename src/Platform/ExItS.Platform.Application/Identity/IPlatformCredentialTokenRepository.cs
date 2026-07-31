using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Application.Identity;

public interface IPlatformCredentialTokenRepository
{
    Task<PlatformCredentialToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task AddAsync(PlatformCredentialToken token, CancellationToken cancellationToken = default);

    Task UpdateAsync(PlatformCredentialToken token, CancellationToken cancellationToken = default);

    Task InvalidateActiveForUserAsync(
        PlatformUserId userId,
        PlatformCredentialTokenPurpose purpose,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Outbound auth message boundary (password reset / email verification).
/// Default implementation is a no-op — no email vendor is selected in this WP.
/// </summary>
public interface IPlatformAuthOutboundMessageSink
{
    Task PublishAsync(PlatformAuthOutboundMessage message, CancellationToken cancellationToken = default);
}

public sealed record PlatformAuthOutboundMessage(
    string Kind,
    Guid UserId,
    string Email,
    string OpaqueToken,
    DateTimeOffset ExpiresAtUtc);

public sealed record CredentialWorkflowAckDto(
    string Message,
    string? DebugToken,
    DateTimeOffset? ExpiresAtUtc);

public static class PlatformAuthOutboundMessageKinds
{
    public const string PasswordReset = "password_reset";
    public const string EmailVerification = "email_verification";
}
