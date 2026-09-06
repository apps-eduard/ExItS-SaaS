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

public sealed record UpsertSupplyCoverageBySourceRequest(
    Guid SourceLocationId,
    IReadOnlyList<Guid> DestinationLocationIds,
    IReadOnlyList<Guid>? SetPreferredForDestinationIds = null);

public sealed record SetPreferredSupplyRouteRequest(Guid SourceLocationId);

public sealed record StockRequestLineRequest(Guid ProductId, decimal RequestedQuantity);

public sealed record CreateStockRequestRequest(
    Guid DestinationLocationId,
    Guid RequestedSourceLocationId,
    IReadOnlyList<StockRequestLineRequest> Lines,
    string? Notes = null);

public sealed record ApproveStockRequestLineRequest(Guid ProductId, decimal ApprovedQuantity);

public sealed record ApproveStockRequestRequest(IReadOnlyList<ApproveStockRequestLineRequest> LineApprovals);

public sealed record RejectStockRequestRequest(string Reason);

public sealed record FulfillStockRequestLineRequest(Guid ProductId, decimal Quantity, Guid? SourceLotId = null);

public sealed record FulfillStockRequestViaTransferRequest(IReadOnlyList<FulfillStockRequestLineRequest> Lines, string? Notes = null);

public sealed record StockRequestLineDto(
    Guid LineId,
    Guid ProductId,
    int LineNumber,
    decimal RequestedQuantity,
    decimal? ApprovedQuantity,
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
    Guid? ApprovedBy,
    DateTimeOffset? ApprovedAtUtc,
    Guid? PreparingStartedBy,
    DateTimeOffset? PreparingStartedAtUtc,
    Guid? DispatchedBy,
    DateTimeOffset? DispatchedAtUtc,
    Guid? LinkedInventoryTransferId,
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

public sealed record StockRequestOutgoingSummaryDto(
    int SubmittedCount,
    int InProgressCount,
    int InTransitCount,
    IReadOnlyList<StockRequestListItemDto> Recent);

public sealed record ReplenishmentCatalogItemDto(
    Guid ProductId,
    string Name,
    string? Sku,
    string? Barcode,
    Guid? CategoryId,
    string? CategoryName,
    string UnitOfMeasure,
    decimal BranchOnHandQuantity,
    decimal WarehouseAvailableQuantity,
    bool IsLowStock,
    bool IsTracked,
    string SellingMode = "PerItem",
    decimal? WarehouseUnitCost = null,
    decimal? BranchEffectiveSellingPrice = null);

public sealed record ReplenishmentCatalogResultDto(
    IReadOnlyList<ReplenishmentCatalogItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    Guid SupplyWarehouseBranchId,
    string? SupplyWarehouseName);
