using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

/// <summary>Snapshot input for one customer-order line at submit time.</summary>
public sealed record CustomerOrderLineDraft(
    CatalogProductId ProductId,
    string NameSnapshot,
    string? SkuSnapshot,
    UnitOfMeasure UnitSnapshot,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount = 0m);

/// <summary>
/// One line of a customer order. Catalog fields and unit price are snapshotted at submit.
/// Quantity may be reduced by the seller under partial-fulfillment policy; original quantity is preserved.
/// </summary>
public sealed class CustomerOrderLine
{
    public const int NameSnapshotMaxLength = 200;
    public const int SkuSnapshotMaxLength = 64;
    public const int AdjustmentNoteMaxLength = 512;
    public const decimal MaxUnitPrice = 9_999_999_999.99m;
    public const decimal MaxQuantity = 999_999.999m;
    public const decimal MaxDiscount = 999_999_999.99m;

    public CustomerOrderLineId Id { get; }
    public CustomerOrderId OrderId { get; }
    public CatalogProductId ProductId { get; }
    public int LineNumber { get; }
    public string NameSnapshot { get; }
    public string? SkuSnapshot { get; }
    public UnitOfMeasure UnitSnapshot { get; }
    /// <summary>Quantity ordered at submit (immutable snapshot).</summary>
    public decimal OriginalQuantity { get; }
    /// <summary>Current fulfillable quantity (may be reduced; 0 = item cancelled).</summary>
    public decimal Quantity { get; private set; }
    /// <summary>OriginalQuantity − Quantity (cancelled / shortage amount).</summary>
    public decimal CancelledQuantity => OriginalQuantity - Quantity;
    public bool IsCancelled => Quantity <= 0m;
    public decimal UnitPrice { get; }
    /// <summary>Discount at submit (immutable). Current <see cref="Discount"/> is scaled from this.</summary>
    public decimal OriginalDiscount { get; }
    public decimal Discount { get; private set; }
    public decimal LineTotal { get; private set; }
    public CustomerOrderLineAdjustmentReason? AdjustmentReason { get; private set; }
    public string? AdjustmentNote { get; private set; }

    private CustomerOrderLine(
        CustomerOrderLineId id,
        CustomerOrderId orderId,
        CatalogProductId productId,
        int lineNumber,
        string nameSnapshot,
        string? skuSnapshot,
        UnitOfMeasure unitSnapshot,
        decimal originalQuantity,
        decimal quantity,
        decimal unitPrice,
        decimal originalDiscount,
        decimal discount,
        decimal lineTotal,
        CustomerOrderLineAdjustmentReason? adjustmentReason = null,
        string? adjustmentNote = null)
    {
        Id = id;
        OrderId = orderId;
        ProductId = productId;
        LineNumber = lineNumber;
        NameSnapshot = nameSnapshot;
        SkuSnapshot = skuSnapshot;
        UnitSnapshot = unitSnapshot;
        OriginalQuantity = originalQuantity;
        Quantity = quantity;
        UnitPrice = unitPrice;
        OriginalDiscount = originalDiscount;
        Discount = discount;
        LineTotal = lineTotal;
        AdjustmentReason = adjustmentReason;
        AdjustmentNote = adjustmentNote;
    }

    internal static CustomerOrderLine Create(
        CustomerOrderId orderId,
        int lineNumber,
        CustomerOrderLineDraft draft,
        CustomerOrderLineId? id = null)
    {
        var unitPrice = NormalizeUnitPrice(draft.UnitPrice);
        var quantity = NormalizeQuantity(draft.Quantity, draft.UnitSnapshot);
        var discount = NormalizeDiscount(draft.Discount);
        var gross = SaleMoney.RoundMoney(unitPrice * quantity);
        if (discount > gross)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderLine,
                "Line discount cannot exceed the line gross amount.");
        }

        var lineTotal = SaleMoney.RoundMoney(gross - discount);

        return new CustomerOrderLine(
            id ?? CustomerOrderLineId.New(),
            orderId,
            draft.ProductId,
            lineNumber,
            NormalizeNameSnapshot(draft.NameSnapshot),
            NormalizeOptionalSku(draft.SkuSnapshot),
            draft.UnitSnapshot,
            originalQuantity: quantity,
            quantity,
            unitPrice,
            originalDiscount: discount,
            discount,
            lineTotal);
    }

    public static CustomerOrderLine Rehydrate(
        CustomerOrderLineId id,
        CustomerOrderId orderId,
        CatalogProductId productId,
        int lineNumber,
        string nameSnapshot,
        string? skuSnapshot,
        UnitOfMeasure unitSnapshot,
        decimal quantity,
        decimal unitPrice,
        decimal discount,
        decimal lineTotal,
        decimal? originalQuantity = null,
        decimal? originalDiscount = null,
        CustomerOrderLineAdjustmentReason? adjustmentReason = null,
        string? adjustmentNote = null) =>
        new(
            id,
            orderId,
            productId,
            lineNumber,
            nameSnapshot,
            skuSnapshot,
            unitSnapshot,
            originalQuantity ?? quantity,
            quantity,
            unitPrice,
            originalDiscount ?? discount,
            discount,
            lineTotal,
            adjustmentReason,
            adjustmentNote);

    /// <summary>
    /// Seller reduces fulfillable quantity (never increases). Zero cancels the item.
    /// Discount is scaled proportionally from the original submit discount.
    /// </summary>
    internal void ApplySellerQuantityAdjustment(
        decimal newQuantity,
        CustomerOrderLineAdjustmentReason reason,
        string? note)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderLineAdjustment,
                "Adjustment reason is invalid.");
        }

        if (reason == CustomerOrderLineAdjustmentReason.Other && string.IsNullOrWhiteSpace(note))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderLineAdjustment,
                "Enter a note when the adjustment reason is Other.");
        }

        if (newQuantity < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderLineQuantity,
                "Quantity cannot be negative.");
        }

        if (newQuantity > Quantity)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderLineQuantity,
                "Sellers cannot increase line quantity through Reduce Quantity.");
        }

        if (newQuantity == Quantity)
        {
            return;
        }

        if (newQuantity > 0m)
        {
            newQuantity = NormalizeQuantity(newQuantity, UnitSnapshot);
        }

        if (newQuantity > OriginalQuantity)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderLineQuantity,
                "Fulfillable quantity cannot exceed the original ordered quantity.");
        }

        var scaledDiscount = newQuantity <= 0m || OriginalQuantity <= 0m
            ? 0m
            : SaleMoney.RoundMoney(OriginalDiscount * (newQuantity / OriginalQuantity));
        var gross = SaleMoney.RoundMoney(UnitPrice * newQuantity);
        if (scaledDiscount > gross)
        {
            scaledDiscount = gross;
        }

        Quantity = newQuantity;
        Discount = scaledDiscount;
        LineTotal = SaleMoney.RoundMoney(gross - scaledDiscount);
        AdjustmentReason = reason;
        AdjustmentNote = NormalizeAdjustmentNote(note);
    }

    private static string? NormalizeAdjustmentNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var trimmed = note.Trim();
        return trimmed.Length > AdjustmentNoteMaxLength
            ? trimmed[..AdjustmentNoteMaxLength]
            : trimmed;
    }

    public static decimal NormalizeQuantity(decimal quantity, UnitOfMeasure unitOfMeasure)
    {
        if (quantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderLineQuantity,
                "Quantity must be greater than zero.");
        }

        if (quantity > MaxQuantity)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderLineQuantity,
                $"Quantity must be at most {MaxQuantity}.");
        }

        var maxDecimals = SaleMoney.MaxQuantityDecimals(unitOfMeasure);
        if (!SaleMoney.HasAtMostDecimals(quantity, maxDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderLineQuantity,
                maxDecimals == 0
                    ? $"{unitOfMeasure} is sold in whole units, so the quantity cannot have decimal places."
                    : $"{unitOfMeasure} quantities may have at most {maxDecimals} decimal places.");
        }

        return quantity;
    }

    public static decimal NormalizeUnitPrice(decimal unitPrice)
    {
        if (unitPrice < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderLineUnitPrice,
                "Unit price cannot be negative.");
        }

        if (unitPrice > MaxUnitPrice)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderLineUnitPrice,
                "Unit price is too large.");
        }

        if (!SaleMoney.HasAtMostDecimals(unitPrice, SaleMoney.MonetaryDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderLineUnitPrice,
                "Unit price must have at most 2 decimal places.");
        }

        return unitPrice;
    }

    public static decimal NormalizeDiscount(decimal discount)
    {
        if (discount < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderLineDiscount,
                "Discount cannot be negative.");
        }

        if (discount > MaxDiscount)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderLineDiscount,
                "Discount is too large.");
        }

        if (!SaleMoney.HasAtMostDecimals(discount, SaleMoney.MonetaryDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderLineDiscount,
                "Discount must have at most 2 decimal places.");
        }

        return discount;
    }

    private static string NormalizeNameSnapshot(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderLine,
                "A product name snapshot is required for every order line.");
        }

        var trimmed = name.Trim();
        return trimmed.Length > NameSnapshotMaxLength
            ? trimmed[..NameSnapshotMaxLength]
            : trimmed;
    }

    private static string? NormalizeOptionalSku(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > SkuSnapshotMaxLength ? trimmed[..SkuSnapshotMaxLength] : trimmed;
    }
}
