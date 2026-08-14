using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class StockCountEntityMapper
{
    public static StockCount ToDomain(StockCountRecord record, IReadOnlyList<StockCountLineRecord> lines) =>
        StockCount.Rehydrate(
            StockCountId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            record.CountNumber,
            StockCountStatuses.Parse(record.Status),
            record.CountDate,
            record.Title,
            record.Notes,
            record.StartedAtUtc,
            record.StartedBy,
            record.CompletedAtUtc,
            record.CompletedBy,
            record.CancelledAtUtc,
            record.CancelledBy,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            lines.Select(ToDomain).ToList());

    public static StockCountLine ToDomain(StockCountLineRecord record) =>
        StockCountLine.Rehydrate(
            StockCountLineId.From(record.Id),
            StockCountId.From(record.StockCountId),
            PosOrganizationId.From(record.OrganizationId),
            CatalogProductId.From(record.ProductId),
            record.LineNumber,
            record.SystemOnHandSnapshot,
            record.CountedQuantity);

    public static StockCountRecord ToRecord(StockCount count) =>
        new()
        {
            Id = count.Id.Value,
            OrganizationId = count.OrganizationId.Value,
            CountNumber = count.CountNumber,
            Status = StockCountStatuses.ToCode(count.Status),
            CountDate = count.CountDate,
            Title = count.Title,
            Notes = count.Notes,
            StartedAtUtc = count.StartedAtUtc,
            StartedBy = count.StartedBy,
            CompletedAtUtc = count.CompletedAtUtc,
            CompletedBy = count.CompletedBy,
            CancelledAtUtc = count.CancelledAtUtc,
            CancelledBy = count.CancelledBy,
            CreatedAtUtc = count.CreatedAtUtc,
            UpdatedAtUtc = count.UpdatedAtUtc
        };

    public static StockCountLineRecord ToRecord(StockCountLine line) =>
        new()
        {
            Id = line.Id.Value,
            StockCountId = line.StockCountId.Value,
            OrganizationId = line.OrganizationId.Value,
            ProductId = line.ProductId.Value,
            LineNumber = line.LineNumber,
            SystemOnHandSnapshot = line.SystemOnHandSnapshot,
            CountedQuantity = line.CountedQuantity
        };

    public static void ApplyToRecord(StockCount count, StockCountRecord record)
    {
        record.CountNumber = count.CountNumber;
        record.Status = StockCountStatuses.ToCode(count.Status);
        record.CountDate = count.CountDate;
        record.Title = count.Title;
        record.Notes = count.Notes;
        record.StartedAtUtc = count.StartedAtUtc;
        record.StartedBy = count.StartedBy;
        record.CompletedAtUtc = count.CompletedAtUtc;
        record.CompletedBy = count.CompletedBy;
        record.CancelledAtUtc = count.CancelledAtUtc;
        record.CancelledBy = count.CancelledBy;
        record.UpdatedAtUtc = count.UpdatedAtUtc;
    }
}
