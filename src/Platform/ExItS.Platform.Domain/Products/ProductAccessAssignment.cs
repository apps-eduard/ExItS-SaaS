using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Domain.Products;

/// <summary>
/// Explicit commercial product-access assignment for a Platform user within an organization.
/// Does not assign product-local roles (Cashier, Store Manager, POS Administrator, etc.).
/// Does not deliver entitlements into product applications.
/// </summary>
public sealed class ProductAccessAssignment
{
    public ProductAccessAssignmentId Id { get; }
    public PlatformUserId UserId { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public OrganizationMembershipId MembershipId { get; }
    public ProductCode ProductCode { get; }
    public ProductAccessStatus Status { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; }
    public string GrantedByActor { get; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevokedByActor { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private ProductAccessAssignment(
        ProductAccessAssignmentId id,
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        OrganizationMembershipId membershipId,
        ProductCode productCode,
        ProductAccessStatus status,
        DateTimeOffset grantedAtUtc,
        string grantedByActor,
        DateTimeOffset? revokedAtUtc,
        string? revokedByActor,
        string? reason,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        UserId = userId;
        OrganizationId = organizationId;
        MembershipId = membershipId;
        ProductCode = productCode;
        Status = status;
        GrantedAtUtc = grantedAtUtc;
        GrantedByActor = grantedByActor;
        RevokedAtUtc = revokedAtUtc;
        RevokedByActor = revokedByActor;
        Reason = reason;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static ProductAccessAssignment Grant(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        OrganizationMembershipId membershipId,
        ProductCode productCode,
        string grantedByActor,
        DateTimeOffset utcNow,
        string? reason = null,
        ProductAccessAssignmentId? id = null)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(membershipId);
        ArgumentNullException.ThrowIfNull(productCode);
        EnsureUtc(utcNow);
        var actor = NormalizeActor(grantedByActor);

        return new ProductAccessAssignment(
            id ?? ProductAccessAssignmentId.New(),
            userId,
            organizationId,
            membershipId,
            productCode,
            ProductAccessStatus.Active,
            utcNow,
            actor,
            null,
            null,
            NormalizeOptionalReason(reason),
            utcNow,
            utcNow);
    }

    /// <summary>Rehydrate from persistence. Not for application grant flows.</summary>
    public static ProductAccessAssignment Rehydrate(
        ProductAccessAssignmentId id,
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        OrganizationMembershipId membershipId,
        ProductCode productCode,
        ProductAccessStatus status,
        DateTimeOffset grantedAtUtc,
        string grantedByActor,
        DateTimeOffset? revokedAtUtc,
        string? revokedByActor,
        string? reason,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            userId,
            organizationId,
            membershipId,
            productCode,
            status,
            grantedAtUtc,
            grantedByActor,
            revokedAtUtc,
            revokedByActor,
            reason,
            createdAtUtc,
            updatedAtUtc);

    public void Revoke(string revokedByActor, string? reason, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == ProductAccessStatus.Revoked)
        {
            return;
        }

        Status = ProductAccessStatus.Revoked;
        RevokedAtUtc = utcNow;
        RevokedByActor = NormalizeActor(revokedByActor);
        Reason = NormalizeOptionalReason(reason) ?? Reason;
        UpdatedAtUtc = utcNow;
    }

    private static string NormalizeActor(string actor)
    {
        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new DomainException(
                DomainErrorCodes.ActorReferenceRequired,
                "Actor reference is required.");
        }

        var trimmed = actor.Trim();
        if (trimmed.Length > 128)
        {
            throw new DomainException(
                DomainErrorCodes.ActorReferenceRequired,
                "Actor reference must be at most 128 characters.");
        }

        return trimmed;
    }

    private static string? NormalizeOptionalReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > 512)
        {
            throw new DomainException(
                DomainErrorCodes.OverrideReasonRequired,
                "Reason must be at most 512 characters.");
        }

        return trimmed;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtcTimestamp,
                "Timestamps must be UTC (offset zero).");
        }
    }
}
