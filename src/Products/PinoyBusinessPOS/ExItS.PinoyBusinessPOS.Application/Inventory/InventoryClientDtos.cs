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
    bool IsLowStock,
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
    string? ProductStatus = null);
