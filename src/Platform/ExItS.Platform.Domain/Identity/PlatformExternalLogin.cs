using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Identity;

/// <summary>
/// Links an external identity provider subject to a single <see cref="PlatformUser"/>.
/// Does not grant membership, entitlements, Platform roles, or product-local roles.
/// </summary>
public sealed class PlatformExternalLogin
{
    public const string ProviderGoogle = "google";
    public const string ProviderFacebook = "facebook";

    public PlatformExternalLoginId Id { get; }
    public PlatformUserId UserId { get; }
    public string Provider { get; }
    public string ProviderSubject { get; }
    public string? ProviderEmail { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private PlatformExternalLogin(
        PlatformExternalLoginId id,
        PlatformUserId userId,
        string provider,
        string providerSubject,
        string? providerEmail,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        UserId = userId;
        Provider = provider;
        ProviderSubject = providerSubject;
        ProviderEmail = providerEmail;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static PlatformExternalLogin Create(
        PlatformUserId userId,
        string provider,
        string providerSubject,
        string? providerEmail,
        DateTimeOffset utcNow,
        PlatformExternalLoginId? id = null)
    {
        ArgumentNullException.ThrowIfNull(userId);
        EnsureUtc(utcNow);
        var normalizedProvider = NormalizeProvider(provider);
        var subject = NormalizeSubject(providerSubject);
        var email = string.IsNullOrWhiteSpace(providerEmail)
            ? null
            : PlatformUser.NormalizeEmail(providerEmail);

        return new PlatformExternalLogin(
            id ?? PlatformExternalLoginId.New(),
            userId,
            normalizedProvider,
            subject,
            email,
            utcNow,
            utcNow);
    }

    public static PlatformExternalLogin Rehydrate(
        PlatformExternalLoginId id,
        PlatformUserId userId,
        string provider,
        string providerSubject,
        string? providerEmail,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(id, userId, provider, providerSubject, providerEmail, createdAtUtc, updatedAtUtc);

    public void TouchProviderEmail(string? providerEmail, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        ProviderEmail = string.IsNullOrWhiteSpace(providerEmail)
            ? null
            : PlatformUser.NormalizeEmail(providerEmail);
        UpdatedAtUtc = utcNow;
    }

    public static string NormalizeProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "External provider is required.");
        }

        var normalized = provider.Trim().ToLowerInvariant();
        if (normalized is not (ProviderGoogle or ProviderFacebook))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidAccountStatusTransition,
                "External provider must be google or facebook.");
        }

        return normalized;
    }

    private static string NormalizeSubject(string providerSubject)
    {
        if (string.IsNullOrWhiteSpace(providerSubject))
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "External provider subject is required.");
        }

        var trimmed = providerSubject.Trim();
        if (trimmed.Length > 256)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "External provider subject is too long.");
        }

        return trimmed;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "External login timestamps must be UTC.");
        }
    }
}

public sealed class PlatformExternalLoginId : IEquatable<PlatformExternalLoginId>
{
    public Guid Value { get; }

    private PlatformExternalLoginId(Guid value) => Value = value;

    public static PlatformExternalLoginId New() => new(Guid.NewGuid());

    public static PlatformExternalLoginId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "External login id is required.");
        }

        return new PlatformExternalLoginId(value);
    }

    public bool Equals(PlatformExternalLoginId? other) =>
        other is not null && Value == other.Value;

    public override bool Equals(object? obj) =>
        obj is PlatformExternalLoginId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(PlatformExternalLoginId? left, PlatformExternalLoginId? right) =>
        Equals(left, right);

    public static bool operator !=(PlatformExternalLoginId? left, PlatformExternalLoginId? right) =>
        !Equals(left, right);
}
