namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public sealed record SupplyRouteDto(
    Guid RouteId,
    Guid OrganizationId,
    Guid SourceLocationId,
    Guid DestinationLocationId,
    bool IsPreferred,
    bool IsActive,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record UpsertSupplyRouteItemRequest(
    Guid SourceLocationId,
    bool IsPreferred = false,
    bool IsActive = true,
    string? Notes = null);

public sealed record UpsertSupplyRoutesRequest(
    Guid DestinationLocationId,
    IReadOnlyList<UpsertSupplyRouteItemRequest> Routes);

public sealed record SetPreferredSupplyRouteRequest(Guid SourceLocationId);

public sealed record StockRequestLineRequest(Guid ProductId, decimal RequestedQuantity);

public sealed record CreateStockRequestRequest(
    Guid DestinationLocationId,
    Guid RequestedSourceLocationId,
    IReadOnlyList<StockRequestLineRequest> Lines,
    string? Notes = null);

public sealed record RejectStockRequestRequest(string Reason);

public sealed record FulfillStockRequestLineRequest(Guid ProductId, decimal Quantity, Guid? SourceLotId = null);

public sealed record FulfillStockRequestViaTransferRequest(IReadOnlyList<FulfillStockRequestLineRequest> Lines, string? Notes = null);

public sealed record StockRequestLineDto(
    Guid LineId,
    Guid ProductId,
    int LineNumber,
    decimal RequestedQuantity,
    decimal FulfilledQuantity,
    decimal InProgressQuantity,
    string NameSnapshot,
    string UnitOfMeasure);

public sealed record StockRequestLinkedTransferDto(
    Guid TransferId,
    string? TransferNumber,
    string Status,
    decimal TotalSentQty,
    decimal TotalReceivedQty,
    DateTimeOffset UpdatedAtUtc);

public sealed record StockRequestDto(
    Guid StockRequestId,
    Guid OrganizationId,
    Guid DestinationLocationId,
    string? DestinationLocationName,
    Guid RequestedSourceLocationId,
    string? RequestedSourceLocationName,
    string? RequestNumber,
    string Status,
    string? Notes,
    Guid RequestedBy,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    Guid? RejectedBy,
    DateTimeOffset? RejectedAtUtc,
    string? RejectionReason,
    Guid? CancelledBy,
    DateTimeOffset? CancelledAtUtc,
    IReadOnlyList<StockRequestLineDto> Lines,
    IReadOnlyList<StockRequestLinkedTransferDto> LinkedTransfers);

public sealed record StockRequestListItemDto(
    Guid StockRequestId,
    string? RequestNumber,
    string Status,
    Guid DestinationLocationId,
    string? DestinationLocationName,
    Guid RequestedSourceLocationId,
    string? RequestedSourceLocationName,
    int LineCount,
    DateTimeOffset UpdatedAtUtc);
