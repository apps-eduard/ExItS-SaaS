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
/// One immutable line of a customer order. Catalog fields and prices are snapshotted at submit
/// so later catalog edits never rewrite history.
/// </summary>
public sealed class CustomerOrderLine
{
    public const int NameSnapshotMaxLength = 200;
    public const int SkuSnapshotMaxLength = 64;
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
    public decimal Quantity { get; }
    public decimal UnitPrice { get; }
    public decimal Discount { get; }
    public decimal LineTotal { get; }

    private CustomerOrderLine(
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
        decimal lineTotal)
    {
        Id = id;
        OrderId = orderId;
        ProductId = productId;
        LineNumber = lineNumber;
        NameSnapshot = nameSnapshot;
        SkuSnapshot = skuSnapshot;
        UnitSnapshot = unitSnapshot;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Discount = discount;
        LineTotal = lineTotal;
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
            quantity,
            unitPrice,
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
        decimal lineTotal) =>
        new(
            id,
            orderId,
            productId,
            lineNumber,
            nameSnapshot,
            skuSnapshot,
            unitSnapshot,
            quantity,
            unitPrice,
            discount,
            lineTotal);

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
