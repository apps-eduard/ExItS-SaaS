using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Returns;

/// <summary>Input for one return line before the aggregate is created.</summary>
public sealed record SaleReturnLineDraft(
    SaleLineId SaleLineId,
    decimal QuantityReturned,
    RestockDisposition RestockDisposition,
    string? LineReason = null,
    decimal? SellableQuantity = null,
    decimal? DamagedQuantity = null);

/// <summary>One immutable line on a completed sale return.</summary>
public sealed class SaleReturnLine
{
    public const int NameSnapshotMaxLength = SaleLine.NameSnapshotMaxLength;
    public const int LineReasonMaxLength = 256;

    public SaleReturnLineId Id { get; }
    public SaleReturnId SaleReturnId { get; }
    public PosOrganizationId OrganizationId { get; }
    public SaleLineId SaleLineId { get; }
    public CatalogProductId ProductId { get; }
    public string ProductNameSnapshot { get; }
    public UnitOfMeasure UomSnapshot { get; }
    public decimal QuantityReturned { get; }
    public decimal UnitPriceSnapshot { get; }
    public decimal RefundAmount { get; }
    public RestockDisposition RestockDisposition { get; }
    public decimal SellableQuantity { get; }
    public decimal DamagedQuantity { get; }
    public string? LineReason { get; }
    public Guid? InventoryMovementId { get; private set; }

    private SaleReturnLine(
        SaleReturnLineId id,
        SaleReturnId saleReturnId,
        PosOrganizationId organizationId,
        SaleLineId saleLineId,
        CatalogProductId productId,
        string productNameSnapshot,
        UnitOfMeasure uomSnapshot,
        decimal quantityReturned,
        decimal unitPriceSnapshot,
        decimal refundAmount,
        RestockDisposition restockDisposition,
        decimal sellableQuantity,
        decimal damagedQuantity,
        string? lineReason,
        Guid? inventoryMovementId)
    {
        Id = id;
        SaleReturnId = saleReturnId;
        OrganizationId = organizationId;
        SaleLineId = saleLineId;
        ProductId = productId;
        ProductNameSnapshot = productNameSnapshot;
        UomSnapshot = uomSnapshot;
        QuantityReturned = quantityReturned;
        UnitPriceSnapshot = unitPriceSnapshot;
        RefundAmount = refundAmount;
        RestockDisposition = restockDisposition;
        SellableQuantity = sellableQuantity;
        DamagedQuantity = damagedQuantity;
        LineReason = lineReason;
        InventoryMovementId = inventoryMovementId;
    }

    internal static SaleReturnLine Create(
        SaleReturnId saleReturnId,
        PosOrganizationId organizationId,
        SaleLine saleLine,
        SaleReturnLineDraft draft,
        decimal previouslyReturnedQuantity,
        decimal previouslyRefundedAmount,
        SaleReturnLineId? id = null)
    {
        if (saleLine.Id != draft.SaleLineId)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleReturnLine,
                "Return line must reference a line from the originating sale.");
        }

        var quantity = SaleLine.NormalizeQuantity(
            draft.QuantityReturned,
            saleLine.UnitOfMeasureSnapshot,
            saleLine.SellingModeSnapshot);
        var refundableQty = SaleReturnRefundable.RefundableQuantity(saleLine, previouslyReturnedQuantity);
        if (quantity > refundableQty)
        {
            throw new DomainException(
                DomainErrorCodes.SaleReturnQuantityExceedsRefundable,
                "Returned quantity exceeds the refundable quantity for this sale line.");
        }

        var refundAmount = SaleReturnRefundable.ComputeRefundAmount(
            saleLine,
            quantity,
            previouslyReturnedQuantity,
            previouslyRefundedAmount);

        if (refundAmount <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleReturnRefundAmount,
                "Refund amount must be greater than zero.");
        }

        var (sellableQuantity, damagedQuantity) = ResolveDispositionQuantities(
            quantity,
            draft,
            saleLine.UnitOfMeasureSnapshot,
            saleLine.SellingModeSnapshot);

        return new SaleReturnLine(
            id ?? SaleReturnLineId.New(),
            saleReturnId,
            organizationId,
            saleLine.Id,
            saleLine.ProductId,
            saleLine.NameSnapshot,
            saleLine.UnitOfMeasureSnapshot,
            quantity,
            saleLine.UnitPrice,
            refundAmount,
            draft.RestockDisposition,
            sellableQuantity,
            damagedQuantity,
            NormalizeLineReason(draft.LineReason),
            inventoryMovementId: null);
    }

    public void AttachInventoryMovement(StockMovementId movementId)
    {
        if (InventoryMovementId is not null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleReturnLine,
                "Inventory movement is already linked to this return line.");
        }

        InventoryMovementId = movementId.Value;
    }

    public static SaleReturnLine Rehydrate(
        SaleReturnLineId id,
        SaleReturnId saleReturnId,
        PosOrganizationId organizationId,
        SaleLineId saleLineId,
        CatalogProductId productId,
        string productNameSnapshot,
        UnitOfMeasure uomSnapshot,
        decimal quantityReturned,
        decimal unitPriceSnapshot,
        decimal refundAmount,
        RestockDisposition restockDisposition,
        decimal sellableQuantity,
        decimal damagedQuantity,
        string? lineReason,
        Guid? inventoryMovementId) =>
        new(
            id,
            saleReturnId,
            organizationId,
            saleLineId,
            productId,
            productNameSnapshot,
            uomSnapshot,
            quantityReturned,
            unitPriceSnapshot,
            refundAmount,
            restockDisposition,
            sellableQuantity,
            damagedQuantity,
            lineReason,
            inventoryMovementId);

    private static (decimal SellableQuantity, decimal DamagedQuantity) ResolveDispositionQuantities(
        decimal quantityReturned,
        SaleReturnLineDraft draft,
        UnitOfMeasure unitOfMeasure,
        SellingMode sellingMode)
    {
        var sellable = draft.SellableQuantity;
        var damaged = draft.DamagedQuantity;
        if (sellable is null && damaged is null)
        {
            return draft.RestockDisposition switch
            {
                RestockDisposition.ReturnToStock => (quantityReturned, 0m),
                RestockDisposition.DoNotRestock => (0m, quantityReturned),
                _ => throw new DomainException(
                    DomainErrorCodes.InvalidSaleReturnRestockDisposition,
                    "Unknown restock disposition.")
            };
        }

        if (sellable is null || damaged is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleReturnLine,
                "Sellable and damaged quantities must both be provided when classifying a split disposition.");
        }

        var normalizedSellable = NormalizeSplitQuantity(sellable.Value, unitOfMeasure, sellingMode);
        var normalizedDamaged = NormalizeSplitQuantity(damaged.Value, unitOfMeasure, sellingMode);

        if (normalizedSellable < 0m || normalizedDamaged < 0m || normalizedSellable + normalizedDamaged != quantityReturned)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleReturnLine,
                "Sellable and damaged quantities must be non-negative and sum to returned quantity.");
        }

        return (normalizedSellable, normalizedDamaged);
    }

    private static decimal NormalizeSplitQuantity(
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        SellingMode sellingMode)
    {
        if (quantity < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleReturnLine,
                "Split disposition quantities cannot be negative.");
        }

        var maxDecimals = SaleMoney.MaxQuantityDecimals(unitOfMeasure, sellingMode);
        if (!SaleMoney.HasAtMostDecimals(quantity, maxDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleReturnLine,
                maxDecimals == 0
                    ? $"{unitOfMeasure} quantities must be whole numbers."
                    : $"{unitOfMeasure} quantities may have at most {maxDecimals} decimal places.");
        }

        return quantity;
    }

    private static string? NormalizeLineReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        var trimmed = reason.Trim();
        return trimmed.Length > LineReasonMaxLength ? trimmed[..LineReasonMaxLength] : trimmed;
    }
}
