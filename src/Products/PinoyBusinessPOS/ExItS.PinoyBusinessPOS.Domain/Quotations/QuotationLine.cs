using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Quotations;

/// <summary>
/// One line on a seller quotation. Product identity is catalog-only; name/SKU/UOM/price are
/// snapshotted on Issue. No inventory reservation or stock movement exists on quotations.
/// </summary>
public sealed class QuotationLine
{
    public const int NameSnapshotMaxLength = 200;
    public const int SkuSnapshotMaxLength = 64;
    public const decimal MaxUnitPrice = 9_999_999_999.99m;
    public const decimal MaxQuantity = 999_999.999m;

    public QuotationLineId Id { get; }
    public QuotationId QuotationId { get; }
    public PosOrganizationId OrganizationId { get; }
    public CatalogProductId ProductId { get; private set; }
    public int LineNumber { get; }
    public string? NameSnapshot { get; private set; }
    public string? SkuSnapshot { get; private set; }
    public UnitOfMeasure? UomSnapshot { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal? DiscountAmount { get; private set; }
    public decimal LineTotal { get; private set; }

    private QuotationLine(
        QuotationLineId id,
        QuotationId quotationId,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        int lineNumber,
        string? nameSnapshot,
        string? skuSnapshot,
        UnitOfMeasure? uomSnapshot,
        decimal quantity,
        decimal unitPrice,
        decimal? discountAmount,
        decimal lineTotal)
    {
        Id = id;
        QuotationId = quotationId;
        OrganizationId = organizationId;
        ProductId = productId;
        LineNumber = lineNumber;
        NameSnapshot = nameSnapshot;
        SkuSnapshot = skuSnapshot;
        UomSnapshot = uomSnapshot;
        Quantity = quantity;
        UnitPrice = unitPrice;
        DiscountAmount = discountAmount;
        LineTotal = lineTotal;
    }

    internal static QuotationLine CreateDraft(
        QuotationId quotationId,
        PosOrganizationId organizationId,
        int lineNumber,
        QuotationLineDraft draft,
        QuotationLineId? id = null)
    {
        var qty = NormalizeDraftQuantity(draft.Quantity);
        var price = NormalizeUnitPrice(draft.UnitPrice);
        var discount = NormalizeDiscount(draft.DiscountAmount, price, qty);
        return new QuotationLine(
            id ?? QuotationLineId.New(),
            quotationId,
            organizationId,
            draft.ProductId,
            lineNumber,
            draft.NameSnapshot is null ? null : NormalizeNameSnapshot(draft.NameSnapshot, required: false),
            NormalizeOptionalSku(draft.SkuSnapshot),
            draft.UomSnapshot,
            qty,
            price,
            discount,
            ComputeLineTotal(price, qty, discount));
    }

    internal void FreezeSnapshot(QuotationLineSnapshotInput snapshot)
    {
        if (snapshot.ProductId != ProductId)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidQuotationLine,
                "Product id mismatch when freezing quotation line snapshot.");
        }

        NameSnapshot = NormalizeNameSnapshot(snapshot.NameSnapshot, required: true);
        SkuSnapshot = NormalizeOptionalSku(snapshot.SkuSnapshot);
        UomSnapshot = snapshot.UomSnapshot;
        Quantity = SaleLine.NormalizeQuantity(snapshot.Quantity, snapshot.UomSnapshot);
        UnitPrice = NormalizeUnitPrice(snapshot.UnitPrice);
        DiscountAmount = NormalizeDiscount(snapshot.DiscountAmount, UnitPrice, Quantity);
        LineTotal = ComputeLineTotal(UnitPrice, Quantity, DiscountAmount);
    }

    public static QuotationLine Rehydrate(
        QuotationLineId id,
        QuotationId quotationId,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        int lineNumber,
        string? nameSnapshot,
        string? skuSnapshot,
        UnitOfMeasure? uomSnapshot,
        decimal quantity,
        decimal unitPrice,
        decimal? discountAmount,
        decimal lineTotal) =>
        new(
            id,
            quotationId,
            organizationId,
            productId,
            lineNumber,
            nameSnapshot,
            skuSnapshot,
            uomSnapshot,
            quantity,
            unitPrice,
            discountAmount,
            lineTotal);

    private static decimal ComputeLineTotal(decimal unitPrice, decimal quantity, decimal? discount)
    {
        var gross = SaleMoney.RoundMoney(unitPrice * quantity);
        var net = discount is null ? gross : SaleMoney.RoundMoney(gross - discount.Value);
        if (net < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidQuotationLineDiscount,
                "Line discount cannot exceed line gross total.");
        }

        return net;
    }

    private static decimal NormalizeDraftQuantity(decimal quantity)
    {
        if (quantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidQuotationQuantity,
                "Quotation quantity must be greater than zero.");
        }

        if (quantity > MaxQuantity)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidQuotationQuantity,
                "Quotation quantity is too large.");
        }

        return quantity;
    }

    private static decimal NormalizeUnitPrice(decimal unitPrice)
    {
        if (unitPrice < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidQuotationUnitPrice,
                "Unit price cannot be negative.");
        }

        if (unitPrice > MaxUnitPrice)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidQuotationUnitPrice,
                "Unit price is too large.");
        }

        return SaleMoney.RoundMoney(unitPrice);
    }

    private static decimal? NormalizeDiscount(decimal? discount, decimal unitPrice, decimal quantity)
    {
        if (discount is null)
        {
            return null;
        }

        if (discount.Value < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidQuotationLineDiscount,
                "Line discount cannot be negative.");
        }

        var normalized = SaleMoney.RoundMoney(discount.Value);
        var gross = SaleMoney.RoundMoney(unitPrice * quantity);
        if (normalized > gross)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidQuotationLineDiscount,
                "Line discount cannot exceed line gross total.");
        }

        return normalized == 0m ? null : normalized;
    }

    private static string? NormalizeOptionalSku(string? sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return null;
        }

        var trimmed = sku.Trim();
        return trimmed.Length > SkuSnapshotMaxLength
            ? trimmed[..SkuSnapshotMaxLength]
            : trimmed;
    }

    private static string? NormalizeNameSnapshot(string? name, bool required)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            if (required)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidQuotationLine,
                    "Product name snapshot is required on issue.");
            }

            return null;
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameSnapshotMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidQuotationLine,
                $"Product name snapshot must be at most {NameSnapshotMaxLength} characters.");
        }

        return trimmed;
    }
}
