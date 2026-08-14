using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Organizations;

public static class SalesDocumentEducationVersions
{
    public const string Current = "transaction-summary-v1";
}

/// <summary>
/// Historical proof that one organization Owner reviewed one version of the sales-document education.
/// Acknowledgment records never grant tax-document issuance authority.
/// </summary>
public sealed class OrganizationSalesDocumentAcknowledgment
{
    public Guid Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public PlatformUserId UserId { get; }
    public string Version { get; }
    public DateTimeOffset AcknowledgedAtUtc { get; }
    public string? ContentKey { get; }

    private OrganizationSalesDocumentAcknowledgment(
        Guid id,
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        string version,
        DateTimeOffset acknowledgedAtUtc,
        string? contentKey)
    {
        Id = id;
        OrganizationId = organizationId;
        UserId = userId;
        Version = version;
        AcknowledgedAtUtc = acknowledgedAtUtc;
        ContentKey = contentKey;
    }

    public static OrganizationSalesDocumentAcknowledgment Create(
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        string version,
        DateTimeOffset acknowledgedAtUtc,
        string? contentKey = null,
        Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(userId);
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException("Education version is required.", nameof(version));
        }

        EnsureUtc(acknowledgedAtUtc);
        var normalizedVersion = version.Trim();
        return new(
            id ?? Guid.NewGuid(),
            organizationId,
            userId,
            normalizedVersion,
            acknowledgedAtUtc,
            string.IsNullOrWhiteSpace(contentKey) ? normalizedVersion : contentKey.Trim());
    }

    public static OrganizationSalesDocumentAcknowledgment Rehydrate(
        Guid id,
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        string version,
        DateTimeOffset acknowledgedAtUtc,
        string? contentKey) =>
        new(id, organizationId, userId, version, acknowledgedAtUtc, contentKey);

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", nameof(value));
        }
    }
}
