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
    string? LineReason = null);

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

        var quantity = SaleLine.NormalizeQuantity(draft.QuantityReturned, saleLine.UnitOfMeasureSnapshot);
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
            lineReason,
            inventoryMovementId);

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
