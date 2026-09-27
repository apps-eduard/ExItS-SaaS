using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Canonical resolver for branch-effective expiration tracking.
/// Operational code must use this instead of CatalogProduct.TracksExpiration.
/// </summary>
public sealed class BranchExpirationPolicyResolver
{
    private readonly IInventoryBranchExpirationSettingRepository _settings;

    public BranchExpirationPolicyResolver(IInventoryBranchExpirationSettingRepository settings)
    {
        _settings = settings;
    }

    public async Task<BranchExpirationPolicy> ResolveAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default)
    {
        var setting = await _settings
            .GetAsync(organizationId, branchId, productId, cancellationToken)
            .ConfigureAwait(false);
        return FromSetting(setting);
    }

    public async Task<IReadOnlyDictionary<Guid, BranchExpirationPolicy>> ResolveManyAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, BranchExpirationPolicy>();
        }

        var settings = await _settings
            .ListByBranchAndProductIdsAsync(organizationId, branchId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var byProduct = settings.ToDictionary(s => s.ProductId.Value);
        var result = new Dictionary<Guid, BranchExpirationPolicy>(productIds.Count);
        foreach (var productId in productIds)
        {
            byProduct.TryGetValue(productId.Value, out var setting);
            result[productId.Value] = FromSetting(setting);
        }

        return result;
    }

    public static BranchExpirationPolicy FromSetting(InventoryBranchExpirationSetting? setting) =>
        setting is { TracksExpiration: true }
            ? new BranchExpirationPolicy(true, setting.ExpirationWarningDays)
            : BranchExpirationPolicy.Off;
}

/// <summary>Null-object settings for tests / optional DI fallbacks (always Off).</summary>
internal sealed class EmptyBranchExpirationSettings : IInventoryBranchExpirationSettingRepository
{
    public static EmptyBranchExpirationSettings Instance { get; } = new();

    public Task<InventoryBranchExpirationSetting?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<InventoryBranchExpirationSetting?>(null);

    public Task<IReadOnlyList<InventoryBranchExpirationSetting>> ListByBranchAndProductIdsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<InventoryBranchExpirationSetting>>([]);

    public Task<IReadOnlyDictionary<Guid, InventoryBranchExpirationSetting>> ListEnabledByBranchAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, InventoryBranchExpirationSetting>>(
            new Dictionary<Guid, InventoryBranchExpirationSetting>());

    public Task UpsertAsync(
        InventoryBranchExpirationSetting setting,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
