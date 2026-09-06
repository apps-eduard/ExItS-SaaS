using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public sealed record StockRequestLineDraft(
    CatalogProductId ProductId,
    decimal RequestedQuantity,
    string NameSnapshot,
    UnitOfMeasure UnitOfMeasure,
    SellingMode SellingMode = SellingMode.PerItem);

public sealed class StockRequestLine
{
    public const int NameSnapshotMaxLength = 200;

    public StockRequestLineId Id { get; }
    public StockRequestId StockRequestId { get; }
    public CatalogProductId ProductId { get; }
    public int LineNumber { get; }
    public decimal RequestedQuantity { get; }
    /// <summary>Null until the warehouse approves the request.</summary>
    public decimal? ApprovedQuantity { get; private set; }
    public string NameSnapshot { get; }
    public UnitOfMeasure UnitOfMeasure { get; }

    private StockRequestLine(
        StockRequestLineId id,
        StockRequestId stockRequestId,
        CatalogProductId productId,
        int lineNumber,
        decimal requestedQuantity,
        decimal? approvedQuantity,
        string nameSnapshot,
        UnitOfMeasure unitOfMeasure)
    {
        Id = id;
        StockRequestId = stockRequestId;
        ProductId = productId;
        LineNumber = lineNumber;
        RequestedQuantity = requestedQuantity;
        ApprovedQuantity = approvedQuantity;
        NameSnapshot = nameSnapshot;
        UnitOfMeasure = unitOfMeasure;
    }

    internal static StockRequestLine Create(
        StockRequestId stockRequestId,
        int lineNumber,
        StockRequestLineDraft draft,
        StockRequestLineId? id = null)
    {
        var name = NormalizeName(draft.NameSnapshot);
        if (draft.RequestedQuantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockRequestQuantity,
                "Stock request quantity must be greater than zero.");
        }

        var normalizedQty = SaleLine.NormalizeQuantity(draft.RequestedQuantity, draft.UnitOfMeasure, draft.SellingMode);
        return new StockRequestLine(
            id ?? StockRequestLineId.New(),
            stockRequestId,
            draft.ProductId,
            lineNumber,
            normalizedQty,
            approvedQuantity: null,
            name,
            draft.UnitOfMeasure);
    }

    /// <summary>Sets approved quantity without changing <see cref="RequestedQuantity"/>.</summary>
    internal void SetApprovedQuantity(decimal approvedQuantity, SellingMode sellingMode = SellingMode.PerItem)
    {
        if (approvedQuantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockRequestQuantity,
                "Approved quantity must be greater than zero.");
        }

        var normalized = SaleLine.NormalizeQuantity(approvedQuantity, UnitOfMeasure, sellingMode);
        if (normalized > RequestedQuantity)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockRequestQuantity,
                $"Approved quantity cannot exceed requested quantity for '{NameSnapshot}'.");
        }

        ApprovedQuantity = normalized;
    }

    /// <summary>Quantity used for fulfillment comparisons (approved when set, else requested).</summary>
    public decimal FulfillmentTargetQuantity => ApprovedQuantity ?? RequestedQuantity;

    public static StockRequestLine Rehydrate(
        StockRequestLineId id,
        StockRequestId stockRequestId,
        CatalogProductId productId,
        int lineNumber,
        decimal requestedQuantity,
        string nameSnapshot,
        UnitOfMeasure unitOfMeasure,
        decimal? approvedQuantity = null) =>
        new(
            id,
            stockRequestId,
            productId,
            lineNumber,
            requestedQuantity,
            approvedQuantity,
            nameSnapshot,
            unitOfMeasure);

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockRequestLine,
                "Stock request line product name is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameSnapshotMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockRequestLine,
                $"Product name must be at most {NameSnapshotMaxLength} characters.");
        }

        return trimmed;
    }
}
