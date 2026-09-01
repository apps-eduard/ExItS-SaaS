namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>Validated branch workspace context for inventory reads (MB2-02A).</summary>
public sealed record BranchInventoryContext(
    Guid OrganizationId,
    Guid BranchId,
    Guid? PrimaryBranchId,
    bool OrganizationGovernance);

/// <summary>Resolved branch inventory quantities for one product.</summary>
public sealed record BranchInventoryProductRead(
    Guid ProductId,
    decimal BranchOnHand,
    decimal OrganizationOnHand,
    decimal? ReorderLevel,
    decimal? ReorderQuantity,
    bool IsLowStock,
    bool IsReorderSuggested,
    decimal? SuggestedOrderQuantity);

public sealed record BranchInventoryListFilter(
    string? Search = null,
    bool? TrackedOnly = null,
    bool? LowStockOnly = null,
    bool? ReorderSuggestedOnly = null,
    string? ProductStatus = null);

public sealed record BranchInventoryListRow(
    Guid ProductId,
    Guid OrganizationId,
    string Name,
    string UnitOfMeasure,
    string ProductStatus,
    bool IsTracked,
    decimal BranchOnHand,
    decimal OrganizationOnHand,
    decimal? ReorderLevel,
    decimal? ReorderQuantity,
    bool IsLowStock,
    bool IsReorderSuggested,
    decimal? SuggestedOrderQuantity,
    DateTimeOffset? LatestMovementAtUtc,
    int MovementCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    bool TracksExpiration,
    int? ExpirationWarningDays,
    bool HasOpeningStock);
