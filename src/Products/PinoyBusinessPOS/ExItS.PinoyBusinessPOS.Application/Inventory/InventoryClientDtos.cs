namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public sealed record PosInventoryAccountDto(
    Guid ProductId,
    Guid OrganizationId,
    string Name,
    string UnitOfMeasure,
    string ProductStatus,
    bool IsTracked,
    decimal OnHandQuantity,
    decimal? ReorderLevel,
    decimal? ReorderQuantity,
    string StockStatus,
    bool IsLowStock,
    bool IsReorderSuggested,
    DateTimeOffset? LatestMovementAtUtc,
    int MovementCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PosStockMovementDto(
    Guid MovementId,
    Guid ProductId,
    Guid InventoryAccountId,
    string MovementType,
    decimal QuantityEffect,
    string Reason,
    string SourceType,
    Guid? SourceId,
    DateTimeOffset RecordedAtUtc,
    Guid RecordedBy);

public sealed record EnableInventoryTrackingRequest(
    decimal? OpeningQuantity = null,
    decimal? ReorderLevel = null);

public sealed record AdjustInventoryRequest(
    string Direction,
    decimal Quantity,
    string Reason,
    decimal? ReorderLevel = null);

public sealed record SetInventoryReorderRequest(
    decimal? ReorderLevel,
    decimal? ReorderQuantity,
    string Reason);

public sealed record PosInventoryReconciliationDto(
    Guid ProductId,
    decimal OnHandQuantity,
    decimal MovementSum,
    decimal Difference,
    bool IsBalanced);

public sealed record PosStockCountLineDto(
    Guid LineId,
    Guid ProductId,
    string ProductName,
    string UnitOfMeasure,
    int LineNumber,
    decimal? SystemOnHandSnapshot,
    decimal? CountedQuantity,
    decimal? Variance);

public sealed record PosStockCountDto(
    Guid StockCountId,
    Guid OrganizationId,
    string? CountNumber,
    string Status,
    string? Notes,
    DateTimeOffset? StartedAtUtc,
    Guid? StartedBy,
    DateTimeOffset? CompletedAtUtc,
    Guid? CompletedBy,
    DateTimeOffset? CancelledAtUtc,
    Guid? CancelledBy,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<PosStockCountLineDto> Lines);

public sealed record CreateStockCountLineRequest(Guid ProductId, decimal? CountedQuantity = null);

public sealed record CreateStockCountRequest(
    IReadOnlyList<CreateStockCountLineRequest> Lines,
    string? Notes = null);

public sealed record UpdateStockCountRequest(
    IReadOnlyList<CreateStockCountLineRequest> Lines,
    string? Notes = null);

public sealed record StockMovementFilter(
    string? MovementType = null,
    string? SourceType = null,
    DateOnly? FromDateUtc = null,
    DateOnly? ToDateUtc = null);

public sealed record PosInventoryAccountPagedResult(
    List<PosInventoryAccountDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record PosStockMovementPagedResult(
    List<PosStockMovementDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

/// <summary>List filter for inventory accounts. Online-only; no offline inventory cache.</summary>
public sealed record InventoryAccountFilter(
    string? Search = null,
    bool? TrackedOnly = null,
    bool? LowStockOnly = null,
    bool? ReorderSuggestedOnly = null,
    string? ProductStatus = null);

public sealed record StockCountFilter(string? Status = null, string? CountNumber = null);
