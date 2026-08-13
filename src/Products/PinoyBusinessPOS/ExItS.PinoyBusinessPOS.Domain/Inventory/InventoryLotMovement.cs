using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Immutable lot-level quantity effect. Complements product-level <see cref="StockMovement"/>
/// without breaking sale/receipt uniqueness that is still one row per product.
/// </summary>
public sealed class InventoryLotMovement
{
    public Guid Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public InventoryLotId LotId { get; }
    public CatalogProductId ProductId { get; }
    public StockMovementType MovementType { get; }
    public decimal QuantityEffect { get; }
    public StockMovementSourceType SourceType { get; }
    public Guid? SourceId { get; }
    public Guid? StockMovementId { get; }
    public DateTimeOffset RecordedAtUtc { get; }
    public Guid RecordedBy { get; }

    private InventoryLotMovement(
        Guid id,
        PosOrganizationId organizationId,
        InventoryLotId lotId,
        CatalogProductId productId,
        StockMovementType movementType,
        decimal quantityEffect,
        StockMovementSourceType sourceType,
        Guid? sourceId,
        Guid? stockMovementId,
        DateTimeOffset recordedAtUtc,
        Guid recordedBy)
    {
        Id = id;
        OrganizationId = organizationId;
        LotId = lotId;
        ProductId = productId;
        MovementType = movementType;
        QuantityEffect = quantityEffect;
        SourceType = sourceType;
        SourceId = sourceId;
        StockMovementId = stockMovementId;
        RecordedAtUtc = recordedAtUtc;
        RecordedBy = recordedBy;
    }

    public static InventoryLotMovement Create(
        PosOrganizationId organizationId,
        InventoryLotId lotId,
        CatalogProductId productId,
        StockMovementType movementType,
        decimal quantityEffect,
        StockMovementSourceType sourceType,
        Guid recordedBy,
        DateTimeOffset utcNow,
        Guid? sourceId = null,
        Guid? stockMovementId = null,
        Guid? id = null)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }

        if (recordedBy == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleActor,
                "A non-empty actor identifier is required.");
        }

        if (quantityEffect == 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryQuantity,
                "Lot movement quantity effect cannot be zero.");
        }

        return new InventoryLotMovement(
            id ?? Guid.NewGuid(),
            organizationId,
            lotId,
            productId,
            movementType,
            quantityEffect,
            sourceType,
            sourceId,
            stockMovementId,
            utcNow,
            recordedBy);
    }

    public static InventoryLotMovement Rehydrate(
        Guid id,
        PosOrganizationId organizationId,
        InventoryLotId lotId,
        CatalogProductId productId,
        StockMovementType movementType,
        decimal quantityEffect,
        StockMovementSourceType sourceType,
        Guid? sourceId,
        Guid? stockMovementId,
        DateTimeOffset recordedAtUtc,
        Guid recordedBy) =>
        new(
            id,
            organizationId,
            lotId,
            productId,
            movementType,
            quantityEffect,
            sourceType,
            sourceId,
            stockMovementId,
            recordedAtUtc,
            recordedBy);
}
