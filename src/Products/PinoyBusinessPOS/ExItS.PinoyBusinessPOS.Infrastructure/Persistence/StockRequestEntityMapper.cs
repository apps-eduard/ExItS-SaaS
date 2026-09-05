using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class StockRequestEntityMapper
{
    public static SupplyRoute ToDomain(SupplyRouteRecord record) =>
        SupplyRoute.Rehydrate(
            SupplyRouteId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            PosBranchId.From(record.SourceLocationId),
            PosBranchId.From(record.DestinationLocationId),
            record.IsPreferred,
            record.IsActive,
            record.Notes,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static SupplyRouteRecord ToRecord(SupplyRoute route) =>
        new()
        {
            Id = route.Id.Value,
            OrganizationId = route.OrganizationId.Value,
            SourceLocationId = route.SourceLocationId.Value,
            DestinationLocationId = route.DestinationLocationId.Value,
            IsPreferred = route.IsPreferred,
            IsActive = route.IsActive,
            Notes = route.Notes,
            CreatedAtUtc = route.CreatedAtUtc,
            UpdatedAtUtc = route.UpdatedAtUtc
        };

    public static void ApplyToRecord(SupplyRoute route, SupplyRouteRecord record)
    {
        record.IsPreferred = route.IsPreferred;
        record.IsActive = route.IsActive;
        record.Notes = route.Notes;
        record.UpdatedAtUtc = route.UpdatedAtUtc;
    }

    public static StockRequest ToDomain(
        StockRequestRecord record,
        IReadOnlyList<StockRequestLineRecord> lines) =>
        StockRequest.Rehydrate(
            StockRequestId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            PosBranchId.From(record.DestinationLocationId),
            PosBranchId.From(record.RequestedSourceLocationId),
            record.RequestNumber,
            record.Notes,
            StockRequestStatuses.Parse(record.Status),
            record.RequestedBy,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.RejectedBy,
            record.RejectedAtUtc,
            record.RejectionReason,
            record.CancelledBy,
            record.CancelledAtUtc,
            lines.OrderBy(l => l.LineNumber).Select(ToDomain).ToList());

    public static StockRequestLine ToDomain(StockRequestLineRecord record) =>
        StockRequestLine.Rehydrate(
            StockRequestLineId.From(record.Id),
            StockRequestId.From(record.StockRequestId),
            CatalogProductId.From(record.ProductId),
            record.LineNumber,
            record.RequestedQuantity,
            record.NameSnapshot,
            UnitOfMeasures.Parse(record.UnitOfMeasure));

    public static StockRequestRecord ToRecord(StockRequest request) =>
        new()
        {
            Id = request.Id.Value,
            OrganizationId = request.OrganizationId.Value,
            DestinationLocationId = request.DestinationLocationId.Value,
            RequestedSourceLocationId = request.RequestedSourceLocationId.Value,
            RequestNumber = request.RequestNumber,
            Status = StockRequestStatuses.ToCode(request.Status),
            Notes = request.Notes,
            RequestedBy = request.RequestedBy,
            CreatedAtUtc = request.CreatedAtUtc,
            UpdatedAtUtc = request.UpdatedAtUtc,
            RejectedBy = request.RejectedBy,
            RejectedAtUtc = request.RejectedAtUtc,
            RejectionReason = request.RejectionReason,
            CancelledBy = request.CancelledBy,
            CancelledAtUtc = request.CancelledAtUtc
        };

    public static void ApplyToRecord(StockRequest request, StockRequestRecord record)
    {
        record.RequestNumber = request.RequestNumber;
        record.Status = StockRequestStatuses.ToCode(request.Status);
        record.Notes = request.Notes;
        record.UpdatedAtUtc = request.UpdatedAtUtc;
        record.RejectedBy = request.RejectedBy;
        record.RejectedAtUtc = request.RejectedAtUtc;
        record.RejectionReason = request.RejectionReason;
        record.CancelledBy = request.CancelledBy;
        record.CancelledAtUtc = request.CancelledAtUtc;
    }

    public static StockRequestLineRecord ToRecord(StockRequestLine line, PosOrganizationId organizationId) =>
        new()
        {
            Id = line.Id.Value,
            StockRequestId = line.StockRequestId.Value,
            OrganizationId = organizationId.Value,
            ProductId = line.ProductId.Value,
            LineNumber = line.LineNumber,
            RequestedQuantity = line.RequestedQuantity,
            NameSnapshot = line.NameSnapshot,
            UnitOfMeasure = line.UnitOfMeasure.ToString()
        };
}
