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
    decimal? SuggestedOrderQuantity,
    DateTimeOffset? LatestMovementAtUtc,
    int MovementCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    bool TracksExpiration = false,
    int? ExpirationWarningDays = null,
    decimal? SellableQuantity = null,
    decimal? ExpiredQuantity = null,
    decimal? NearExpiryQuantity = null);

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
    decimal? ReorderLevel = null,
    DateOnly? ExpirationDate = null,
    string? LotNumber = null);

public sealed record AdjustInventoryRequest(
    string Direction,
    decimal Quantity,
    string Reason,
    decimal? ReorderLevel = null,
    DateOnly? ExpirationDate = null,
    string? LotNumber = null,
    Guid? LotId = null,
    Guid? ProductUnitId = null,
    Guid? MovementId = null);

public sealed record PosInventoryLotDto(
    Guid LotId,
    Guid ProductId,
    Guid? BranchId,
    string? LotNumber,
    DateOnly ExpirationDate,
    decimal QuantityOnHand,
    string ExpiryStatus,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PosInventoryLotPagedResult(
    List<PosInventoryLotDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record PosExpiringLotDto(
    Guid LotId,
    Guid ProductId,
    string ProductName,
    string? Sku,
    Guid? BranchId,
    string? LotNumber,
    DateOnly ExpirationDate,
    decimal QuantityOnHand,
    string ExpiryStatus,
    int WarningDays);

public sealed record PosExpiringLotPagedResult(
    IReadOnlyList<PosExpiringLotDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int ExpiredCount,
    int NearExpiryCount);

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
    string Title,
    string Status,
    DateOnly CountDate,
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
    string Title,
    DateOnly? CountDate = null,
    string? Notes = null);

public sealed record UpdateStockCountRequest(
    IReadOnlyList<CreateStockCountLineRequest> Lines,
    DateOnly? CountDate = null,
    string? Notes = null,
    string? Title = null);

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
