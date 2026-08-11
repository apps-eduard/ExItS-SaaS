using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Local selling-catalog cache for offline checkout browse/lookup. Not a Platform/global catalog.
/// </summary>
public interface ILocalSellingCatalogStore
{
    Task ReplaceCatalogAsync(
        IReadOnlyList<PosProductCategoryDto> categories,
        IReadOnlyList<PosCatalogProductDto> products,
        CancellationToken ct = default);

    Task UpsertProductsAsync(IReadOnlyList<PosCatalogProductDto> products, CancellationToken ct = default);

    Task UpsertCategoriesAsync(IReadOnlyList<PosProductCategoryDto> categories, CancellationToken ct = default);

    Task SaveOpenShiftSnapshotAsync(PosCashierShiftDto shift, CancellationToken ct = default);

    Task ClearOpenShiftSnapshotAsync(CancellationToken ct = default);

    Task<PosCashierShiftDto?> LoadOpenShiftSnapshotAsync(CancellationToken ct = default);

    Task<IReadOnlyList<PosProductCategoryDto>> ListCategoriesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<PosCatalogProductDto>> SearchProductsAsync(
        string? search,
        Guid? categoryId,
        int take,
        CancellationToken ct = default);

    Task<PosCatalogProductDto?> FindBySkuAsync(string sku, CancellationToken ct = default);

    Task<PosCatalogProductDto?> FindByBarcodeAsync(string barcode, CancellationToken ct = default);

    Task ApplyLocalInventoryDeductionAsync(
        IReadOnlyList<(Guid ProductId, decimal Quantity)> deductions,
        CancellationToken ct = default);
}

/// <summary>Local cash-sale projection + transactional outbox enqueue.</summary>
public interface ILocalCashSaleStore
{
    /// <summary>
    /// Atomically persists a local cash sale and enqueues <c>sale.checkout</c>.
    /// Idempotent on <paramref name="command"/>.SaleId / IdempotencyKey.
    /// </summary>
    Task PersistCashSaleAndEnqueueAsync(LocalCashSaleCommitCommand command, CancellationToken ct = default);

    Task<LocalCashSaleProjection?> GetBySaleIdAsync(Guid saleId, CancellationToken ct = default);

    Task MarkSyncedAsync(Guid saleId, string serverReference, CancellationToken ct = default);

    Task MarkSyncFailedAsync(Guid saleId, string safeFailureCode, CancellationToken ct = default);
}

public sealed record LocalCashSaleLineSnapshot(
    Guid ProductId,
    string Name,
    string? Sku,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal Quantity,
    decimal LineTotal,
    bool IsTracked,
    string SellingMode = "PerItem");

public sealed record LocalCashSaleCommitCommand(
    Guid SaleId,
    Guid OperationId,
    string IdempotencyKey,
    string SaleNumber,
    Guid ShiftId,
    decimal Subtotal,
    decimal Total,
    decimal AmountTendered,
    decimal ChangeAmount,
    Guid RecordedBy,
    IReadOnlyList<LocalCashSaleLineSnapshot> Lines,
    CheckoutSaleRequest CheckoutRequest);

public sealed record LocalCashSaleProjection(
    Guid SaleId,
    Guid OrganizationId,
    string SaleNumber,
    Guid? ShiftId,
    string PaymentMethod,
    decimal Subtotal,
    decimal Total,
    decimal? AmountTendered,
    decimal? ChangeAmount,
    DateTimeOffset RecordedAtUtc,
    Guid RecordedBy,
    LocalEntitySyncState EntityState,
    Guid? PendingOperationId,
    string IdempotencyKey,
    string? ServerReference,
    IReadOnlyList<LocalCashSaleLineSnapshot> Lines,
    string? SafeFailureCode);

/// <summary>Online refresh of the local selling catalog + open-shift snapshot.</summary>
public interface ILocalSellingCatalogSyncService
{
    Task RefreshFromServerAsync(CancellationToken ct = default);
}
