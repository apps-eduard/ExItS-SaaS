namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Bound-branch physical inventory attention counts for operational screens.
/// Distinct from organization-wide <c>/management/overview</c> metrics.
/// </summary>
public sealed record PosInventoryAttentionSummaryDto(
    int LowStockProductCount,
    int OutOfStockProductCount,
    int ExpiredLotCount,
    int NearExpiryLotCount);
