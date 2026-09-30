using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Append-only audit of lot expiration / batch number corrections. Not a stock movement.
/// </summary>
public sealed class InventoryLotIdentityCorrection
{
    public const int ReasonMaxLength = 512;

    public InventoryLotIdentityCorrectionId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public PosBranchId BranchId { get; }
    public CatalogProductId ProductId { get; }
    public InventoryLotId InventoryLotId { get; }
    public DateOnly OldExpirationDate { get; }
    public DateOnly NewExpirationDate { get; }
    public string? OldLotNumber { get; }
    public string? NewLotNumber { get; }
    public string Reason { get; }
    public Guid CorrectedBy { get; }
    public DateTimeOffset CorrectedAtUtc { get; }

    private InventoryLotIdentityCorrection(
        InventoryLotIdentityCorrectionId id,
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        InventoryLotId inventoryLotId,
        DateOnly oldExpirationDate,
        DateOnly newExpirationDate,
        string? oldLotNumber,
        string? newLotNumber,
        string reason,
        Guid correctedBy,
        DateTimeOffset correctedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        BranchId = branchId;
        ProductId = productId;
        InventoryLotId = inventoryLotId;
        OldExpirationDate = oldExpirationDate;
        NewExpirationDate = newExpirationDate;
        OldLotNumber = oldLotNumber;
        NewLotNumber = newLotNumber;
        Reason = reason;
        CorrectedBy = correctedBy;
        CorrectedAtUtc = correctedAtUtc;
    }

    public static InventoryLotIdentityCorrection Create(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        InventoryLotId inventoryLotId,
        DateOnly oldExpirationDate,
        DateOnly newExpirationDate,
        string? oldLotNumber,
        string? newLotNumber,
        string reason,
        Guid correctedBy,
        DateTimeOffset utcNow,
        InventoryLotIdentityCorrectionId? id = null)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }

        if (correctedBy == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryLotIdentityCorrectionActor,
                "Actor id must be a non-empty GUID.");
        }

        return new InventoryLotIdentityCorrection(
            id ?? InventoryLotIdentityCorrectionId.New(),
            organizationId,
            branchId,
            productId,
            inventoryLotId,
            oldExpirationDate,
            newExpirationDate,
            oldLotNumber,
            newLotNumber,
            NormalizeReason(reason),
            correctedBy,
            utcNow);
    }

    public static InventoryLotIdentityCorrection Rehydrate(
        InventoryLotIdentityCorrectionId id,
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        InventoryLotId inventoryLotId,
        DateOnly oldExpirationDate,
        DateOnly newExpirationDate,
        string? oldLotNumber,
        string? newLotNumber,
        string reason,
        Guid correctedBy,
        DateTimeOffset correctedAtUtc) =>
        new(
            id,
            organizationId,
            branchId,
            productId,
            inventoryLotId,
            oldExpirationDate,
            newExpirationDate,
            oldLotNumber,
            newLotNumber,
            reason,
            correctedBy,
            correctedAtUtc);

    public static string NormalizeReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryLotIdentityCorrectionReason,
                "Lot identity correction reason is required.");
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > ReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryLotIdentityCorrectionReason,
                $"Lot identity correction reason must be at most {ReasonMaxLength} characters.");
        }

        return trimmed;
    }
}
