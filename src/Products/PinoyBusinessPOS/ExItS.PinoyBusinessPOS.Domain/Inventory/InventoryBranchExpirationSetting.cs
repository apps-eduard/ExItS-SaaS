using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Branch-scoped expiration tracking for one catalog product.
/// CatalogProduct.TracksExpiration remains a legacy compatibility field — not authoritative for operations.
/// </summary>
public sealed class InventoryBranchExpirationSetting
{
    public PosOrganizationId OrganizationId { get; }
    public PosBranchId BranchId { get; }
    public CatalogProductId ProductId { get; }
    public bool TracksExpiration { get; private set; }
    public int? ExpirationWarningDays { get; private set; }
    public DateTimeOffset EnabledAtUtc { get; private set; }
    public Guid EnabledBy { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public Guid UpdatedBy { get; private set; }

    private InventoryBranchExpirationSetting(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        bool tracksExpiration,
        int? expirationWarningDays,
        DateTimeOffset enabledAtUtc,
        Guid enabledBy,
        DateTimeOffset updatedAtUtc,
        Guid updatedBy)
    {
        OrganizationId = organizationId;
        BranchId = branchId;
        ProductId = productId;
        TracksExpiration = tracksExpiration;
        ExpirationWarningDays = expirationWarningDays;
        EnabledAtUtc = enabledAtUtc;
        EnabledBy = enabledBy;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedBy = updatedBy;
    }

    public static InventoryBranchExpirationSetting CreateEnabled(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        int? expirationWarningDays,
        Guid actorId,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        var warning = InventoryLot.NormalizeWarningDays(expirationWarningDays);
        return new InventoryBranchExpirationSetting(
            organizationId,
            branchId,
            productId,
            tracksExpiration: true,
            expirationWarningDays: warning,
            enabledAtUtc: utcNow,
            enabledBy: actorId,
            updatedAtUtc: utcNow,
            updatedBy: actorId);
    }

    public static InventoryBranchExpirationSetting Rehydrate(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        bool tracksExpiration,
        int? expirationWarningDays,
        DateTimeOffset enabledAtUtc,
        Guid enabledBy,
        DateTimeOffset updatedAtUtc,
        Guid updatedBy) =>
        new(
            organizationId,
            branchId,
            productId,
            tracksExpiration,
            expirationWarningDays,
            enabledAtUtc,
            enabledBy,
            updatedAtUtc,
            updatedBy);

    public void Enable(int? expirationWarningDays, Guid actorId, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        var warning = InventoryLot.NormalizeWarningDays(expirationWarningDays);
        if (!TracksExpiration)
        {
            EnabledAtUtc = utcNow;
            EnabledBy = actorId;
        }

        TracksExpiration = true;
        ExpirationWarningDays = warning;
        UpdatedAtUtc = utcNow;
        UpdatedBy = actorId;
    }

    public void SetWarningDays(int? expirationWarningDays, Guid actorId, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (!TracksExpiration)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryExpirationRequired,
                "Expiration tracking is not enabled for this branch product.");
        }

        ExpirationWarningDays = InventoryLot.NormalizeWarningDays(expirationWarningDays);
        UpdatedAtUtc = utcNow;
        UpdatedBy = actorId;
    }

    public void Disable(Guid actorId, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        TracksExpiration = false;
        ExpirationWarningDays = null;
        UpdatedAtUtc = utcNow;
        UpdatedBy = actorId;
    }

    public int EffectiveWarningDays =>
        TracksExpiration
            ? (ExpirationWarningDays ?? InventoryLot.DefaultWarningDays)
            : InventoryLot.DefaultWarningDays;

    private static void EnsureActor(Guid actorId)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryExpirationActor,
                "Actor id must be a non-empty GUID.");
        }
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }
}
