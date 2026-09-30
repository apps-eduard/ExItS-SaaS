using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>Effective stop-selling rule for normal POS sales at a branch + category.</summary>
public readonly record struct EffectiveExpirySalePolicy(
    int StopSellingDaysBeforeExpiry,
    ExpirySalePolicySource Source,
    int OrganizationDefaultDays,
    int? OrganizationCategoryDays,
    int? BranchDefaultDays,
    int? BranchCategoryDays);

/// <summary>
/// Canonical hierarchy resolver for expiry sale cutoff.
/// Precedence: Branch+Category → Branch → Org Category → Org Default.
/// Does not overload ExpirationWarningDays.
/// </summary>
public sealed class ExpirySalePolicyResolver
{
    private readonly IOrganizationExpirySalePolicyRepository _orgDefaults;
    private readonly IOrganizationCategoryExpirySalePolicyRepository _orgCategories;
    private readonly IBranchExpirySalePolicyRepository _branchDefaults;
    private readonly IBranchCategoryExpirySalePolicyRepository _branchCategories;

    public ExpirySalePolicyResolver(
        IOrganizationExpirySalePolicyRepository orgDefaults,
        IOrganizationCategoryExpirySalePolicyRepository orgCategories,
        IBranchExpirySalePolicyRepository branchDefaults,
        IBranchCategoryExpirySalePolicyRepository branchCategories)
    {
        _orgDefaults = orgDefaults;
        _orgCategories = orgCategories;
        _branchDefaults = branchDefaults;
        _branchCategories = branchCategories;
    }

    public async Task<EffectiveExpirySalePolicy> ResolveAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        Guid? categoryId,
        CancellationToken cancellationToken = default)
    {
        var batch = await ResolveManyAsync(
                organizationId,
                branchId,
                categoryId is Guid id ? [id] : [null],
                cancellationToken)
            .ConfigureAwait(false);
        var key = categoryId ?? Guid.Empty;
        return batch[key];
    }

    /// <summary>
    /// Resolve effective policies for many category keys.
    /// Use <see cref="Guid.Empty"/> (or null in the input list) for products with no category.
    /// </summary>
    public async Task<IReadOnlyDictionary<Guid, EffectiveExpirySalePolicy>> ResolveManyAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyCollection<Guid?> categoryIds,
        CancellationToken cancellationToken = default)
    {
        var orgSetting = await _orgDefaults.GetAsync(organizationId, cancellationToken).ConfigureAwait(false);
        var orgDefaultDays = orgSetting?.StopSellingDaysBeforeExpiry
            ?? InventoryLotSaleEligibility.DefaultStopSellingDays;

        var orgCategoryRules = await _orgCategories
            .ListByOrganizationAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var orgByCategory = orgCategoryRules.ToDictionary(r => r.CategoryId);

        var branchSetting = await _branchDefaults
            .GetAsync(organizationId, branchId, cancellationToken)
            .ConfigureAwait(false);
        int? branchDefaultDays = branchSetting?.StopSellingDaysBeforeExpiry;

        var branchCategoryRules = await _branchCategories
            .ListByBranchAsync(organizationId, branchId, cancellationToken)
            .ConfigureAwait(false);
        var branchByCategory = branchCategoryRules.ToDictionary(r => r.CategoryId);

        var distinct = categoryIds
            .Select(id => id ?? Guid.Empty)
            .Distinct()
            .ToList();
        var result = new Dictionary<Guid, EffectiveExpirySalePolicy>(distinct.Count);
        foreach (var categoryKey in distinct)
        {
            Guid? categoryId = categoryKey == Guid.Empty ? null : categoryKey;
            int? orgCategoryDays = null;
            int? branchCategoryDays = null;
            if (categoryId is Guid cid)
            {
                if (orgByCategory.TryGetValue(cid, out var orgCat))
                {
                    orgCategoryDays = orgCat.StopSellingDaysBeforeExpiry;
                }

                if (branchByCategory.TryGetValue(cid, out var branchCat))
                {
                    branchCategoryDays = branchCat.StopSellingDaysBeforeExpiry;
                }
            }

            result[categoryKey] = Compose(
                orgDefaultDays,
                orgCategoryDays,
                branchDefaultDays,
                branchCategoryDays);
        }

        return result;
    }

    public static EffectiveExpirySalePolicy Compose(
        int organizationDefaultDays,
        int? organizationCategoryDays,
        int? branchDefaultDays,
        int? branchCategoryDays)
    {
        var orgDefault = InventoryLotSaleEligibility.NormalizeStopSellingDays(organizationDefaultDays);
        if (branchCategoryDays is int bc)
        {
            return new EffectiveExpirySalePolicy(
                InventoryLotSaleEligibility.NormalizeStopSellingDays(bc),
                ExpirySalePolicySource.BranchCategory,
                orgDefault,
                organizationCategoryDays,
                branchDefaultDays,
                bc);
        }

        if (branchDefaultDays is int bd)
        {
            return new EffectiveExpirySalePolicy(
                InventoryLotSaleEligibility.NormalizeStopSellingDays(bd),
                ExpirySalePolicySource.Branch,
                orgDefault,
                organizationCategoryDays,
                bd,
                null);
        }

        if (organizationCategoryDays is int oc)
        {
            return new EffectiveExpirySalePolicy(
                InventoryLotSaleEligibility.NormalizeStopSellingDays(oc),
                ExpirySalePolicySource.OrganizationCategory,
                orgDefault,
                oc,
                null,
                null);
        }

        return new EffectiveExpirySalePolicy(
            orgDefault,
            ExpirySalePolicySource.OrganizationDefault,
            orgDefault,
            null,
            null,
            null);
    }
}
