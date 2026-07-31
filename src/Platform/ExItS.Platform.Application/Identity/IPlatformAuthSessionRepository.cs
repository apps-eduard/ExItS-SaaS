using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Application.Identity;

public interface IPlatformAuthSessionRepository
{
    Task<PlatformAuthSession?> GetByIdAsync(
        PlatformAuthSessionId sessionId,
        CancellationToken cancellationToken = default);

    Task<PlatformAuthSession?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task AddAsync(PlatformAuthSession session, CancellationToken cancellationToken = default);

    Task UpdateAsync(PlatformAuthSession session, CancellationToken cancellationToken = default);

    Task<int> RevokeAllActiveForUserAsync(
        PlatformUserId userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);
}

/// <summary>Creates opaque session tokens and hashes them for persistence (never store raw tokens).</summary>
public interface IPlatformSessionTokenService
{
    string CreateOpaqueToken();

    string HashToken(string opaqueToken);
}

/// <summary>Shared claim / scheme names for Platform browser sessions (Application boundary).</summary>
public static class PlatformSessionClaimTypes
{
    public const string AuthenticationScheme = "PlatformSession";
    public const string SessionId = "exits_session_id";
    public const string RequestTokenItemKey = "PlatformSession:token";
}

public sealed record PlatformAuthSessionInfoDto(
    Guid SessionId,
    Guid UserId,
    string Username,
    string DisplayName,
    string Email,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset AbsoluteExpiresAtUtc,
    DateTimeOffset LastActivityAtUtc);

/// <summary>Returned only from login — includes the opaque session token once.</summary>
public sealed record PlatformLoginResultDto(
    string SessionToken,
    Guid SessionId,
    Guid UserId,
    string Username,
    string DisplayName,
    string Email,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset AbsoluteExpiresAtUtc);
