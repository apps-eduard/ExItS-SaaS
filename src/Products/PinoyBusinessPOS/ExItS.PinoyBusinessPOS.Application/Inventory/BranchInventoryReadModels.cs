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
    decimal BranchReserved,
    decimal BranchAvailable,
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

/// <summary>Stock filter for replenishment catalog: <c>all</c>, <c>low</c>, or <c>out</c>.</summary>
public static class ReplenishmentStockFilters
{
    public const string All = "all";
    public const string Low = "low";
    public const string Out = "out";

    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = All;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var trimmed = value.Trim().ToLowerInvariant();
        if (trimmed is All or Low or Out)
        {
            normalized = trimmed;
            return true;
        }

        return false;
    }
}

public sealed record ReplenishmentCatalogFilter(
    Guid SupplyWarehouseBranchId,
    string? Search = null,
    string StockFilter = ReplenishmentStockFilters.All,
    Guid? CategoryId = null);

public sealed record ReplenishmentCatalogRow(
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
