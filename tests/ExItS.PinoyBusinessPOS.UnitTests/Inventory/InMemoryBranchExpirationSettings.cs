using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

internal sealed class InMemoryBranchExpirationSettings : IInventoryBranchExpirationSettingRepository
{
    public List<InventoryBranchExpirationSetting> Items { get; } = [];

    public Task<InventoryBranchExpirationSetting?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(s =>
            s.OrganizationId == organizationId
            && s.BranchId == branchId
            && s.ProductId == productId));

    public Task<IReadOnlyList<InventoryBranchExpirationSetting>> ListByBranchAndProductIdsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default)
    {
        var idSet = productIds.Select(p => p.Value).ToHashSet();
        IReadOnlyList<InventoryBranchExpirationSetting> list = Items
            .Where(s =>
                s.OrganizationId == organizationId
                && s.BranchId == branchId
                && idSet.Contains(s.ProductId.Value))
            .ToList();
        return Task.FromResult(list);
    }

    public Task<IReadOnlyDictionary<Guid, InventoryBranchExpirationSetting>> ListEnabledByBranchAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<Guid, InventoryBranchExpirationSetting> map = Items
            .Where(s =>
                s.OrganizationId == organizationId
                && s.BranchId == branchId
                && s.TracksExpiration)
            .ToDictionary(s => s.ProductId.Value);
        return Task.FromResult(map);
    }

    public Task UpsertAsync(
        InventoryBranchExpirationSetting setting,
        CancellationToken cancellationToken = default)
    {
        Items.RemoveAll(s =>
            s.OrganizationId == setting.OrganizationId
            && s.BranchId == setting.BranchId
            && s.ProductId == setting.ProductId);
        Items.Add(setting);
        return Task.CompletedTask;
    }
}

internal static class BranchExpirationTestHelpers
{
    public static BranchExpirationPolicyResolver CreateResolver(
        InMemoryBranchExpirationSettings? settings = null) =>
        new(settings ?? new InMemoryBranchExpirationSettings());
}
