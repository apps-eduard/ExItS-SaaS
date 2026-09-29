using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Branch grouping metadata for a caller, resolved from Platform in one authorization-filtered read.
/// Branches absent from this list are not visible to the caller and must not contribute to any total.
/// </summary>
public sealed record AuthorizedBranchGrouping(
    Guid BranchId,
    string BranchName,
    Guid? AreaId,
    string? AreaName);

/// <summary>
/// Authorized branches plus how broad that authorization is. Organization-wide callers
/// (Owner, Administrator, or all-active staff) may see authoritative organization totals;
/// Area- or branch-scoped callers may not, even when their branches happen to cover everything.
/// </summary>
public sealed record AuthorizedBranchScope(
    bool IsOrganizationWide,
    IReadOnlyList<AuthorizedBranchGrouping> Branches)
{
    public static AuthorizedBranchScope None { get; } = new(false, []);
}

public interface IAuthorizedBranchGroupingDirectory
{
    Task<AuthorizedBranchScope> ListAuthorizedAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}

public sealed record PosInventoryBranchRollupDto(
    Guid BranchId,
    string BranchName,
    decimal OnHandQuantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity,
    decimal PendingReturnQuantity = 0m,
    decimal InspectionHoldQuantity = 0m,
    decimal DamagedQuantity = 0m,
    decimal? SellableQuantity = null,
    decimal? ExpiredQuantity = null,
    decimal? SalePolicyBlockedQuantity = null,
    decimal InTransitOutboundQuantity = 0m,
    decimal InTransitInboundQuantity = 0m);

/// <summary>
/// Derived area subtotal. Never persisted: moving a branch between areas changes this projection only.
/// </summary>
public sealed record PosInventoryAreaRollupDto(
    Guid? AreaId,
    string? AreaName,
    bool IsUnassigned,
    decimal OnHandQuantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity,
    IReadOnlyList<PosInventoryBranchRollupDto> Branches);

/// <summary>
/// Organization totals are the authoritative <see cref="InventoryAccount"/> figures and are populated
/// only for organization-wide viewers. Accessible totals are always derived from the caller's authorized
/// branches and must never be presented as organization inventory.
/// Available quantities honor expiry sale eligibility when branch expiration tracking is on.
/// </summary>
public sealed record PosInventoryStockRollupDto(
    Guid ProductId,
    string ProductName,
    string UnitOfMeasure,
    bool IsTracked,
    bool OrganizationTotalsVisible,
    decimal? OrganizationOnHandQuantity,
    decimal? OrganizationReservedQuantity,
    decimal? OrganizationAvailableQuantity,
    decimal AccessibleOnHandQuantity,
    decimal AccessibleReservedQuantity,
    decimal AccessibleAvailableQuantity,
    bool HasAreas,
    IReadOnlyList<PosInventoryAreaRollupDto> Areas);

/// <summary>
/// Hierarchical Organization → Area → Branch stock read (AREA-02).
/// Organization on-hand/reserved stay the authoritative <see cref="InventoryAccount"/> figures; available
/// is capped by sellable lot qty when expiration tracking is on. Area values are derived sums of
/// authorized branch balances only. Read-only: no stock authority lives on an area.
/// </summary>
public sealed class InventoryStockRollupQuery
{
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryBranchBalanceRepository _balances;
    private readonly IAuthorizedBranchGroupingDirectory _grouping;
    private readonly IInventoryLotRepository _lots;
    private readonly BranchExpirationPolicyResolver _expirationPolicies;
    private readonly ExpirySalePolicyResolver _expirySalePolicies;
    private readonly IClock _clock;

    public InventoryStockRollupQuery(
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        IInventoryBranchBalanceRepository balances,
        IAuthorizedBranchGroupingDirectory grouping,
        IInventoryLotRepository? lots = null,
        BranchExpirationPolicyResolver? expirationPolicies = null,
        ExpirySalePolicyResolver? expirySalePolicies = null,
        IClock? clock = null)
    {
        _inventory = inventory;
        _products = products;
        _balances = balances;
        _grouping = grouping;
        _lots = lots ?? EmptyInventoryLotRepository.Instance;
        _expirationPolicies = expirationPolicies
            ?? new BranchExpirationPolicyResolver(EmptyBranchExpirationSettings.Instance);
        _expirySalePolicies = expirySalePolicies
            ?? new ExpirySalePolicyResolver(
                EmptyOrganizationExpirySalePolicyRepository.Instance,
                EmptyOrganizationCategoryExpirySalePolicyRepository.Instance,
                EmptyBranchExpirySalePolicyRepository.Instance,
                EmptyBranchCategoryExpirySalePolicyRepository.Instance);
        _clock = clock ?? SystemClock.Instance;
    }

    public async Task<ApplicationResult<PosInventoryStockRollupDto>> GetProductAsync(
        Guid organizationId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var catalogProductId = CatalogProductId.From(productId);
        var product = await _products.GetByIdAsync(orgId, catalogProductId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<PosInventoryStockRollupDto>.Failure(
                ApplicationErrorCodes.InventoryProductNotFound,
                "Product was not found.");
        }

        var scope = await _grouping
            .ListAuthorizedAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);

        var account = await _inventory
            .GetByProductIdAsync(orgId, catalogProductId, cancellationToken)
            .ConfigureAwait(false);
        if (account is null || !account.IsTracked)
        {
            return ApplicationResult<PosInventoryStockRollupDto>.Success(
                new PosInventoryStockRollupDto(
                    productId,
                    product.Name,
                    product.UnitOfMeasure.ToString(),
                    IsTracked: false,
                    OrganizationTotalsVisible: scope.IsOrganizationWide,
                    scope.IsOrganizationWide ? 0m : null,
                    scope.IsOrganizationWide ? 0m : null,
                    scope.IsOrganizationWide ? 0m : null,
                    0m,
                    0m,
                    0m,
                    HasAreas: false,
                    []));
        }

        // Authorized branches drive the shape; a branch with no balance row still reports zero.
        var authorizedBranches = scope.Branches
            .GroupBy(b => b.BranchId)
            .Select(g => g.First())
            .ToList();
        var balances = await _balances
            .ListByProductIdsAsync(orgId, [catalogProductId], cancellationToken)
            .ConfigureAwait(false);
        var balanceByBranchId = balances
            .GroupBy(b => b.BranchId.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var branchIds = authorizedBranches.Select(b => PosBranchId.From(b.BranchId)).ToList();
        var expirationByBranch = await _expirationPolicies
            .ResolveManyBranchesAsync(orgId, catalogProductId, branchIds, cancellationToken)
            .ConfigureAwait(false);

        var anyTracks = expirationByBranch.Values.Any(p => p.TracksExpiration);
        IReadOnlyList<InventoryLot> allLots = [];
        IReadOnlyDictionary<Guid, EffectiveExpirySalePolicy> salePolicyByBranch = new Dictionary<Guid, EffectiveExpirySalePolicy>();
        if (anyTracks)
        {
            allLots = await _lots
                .ListOnHandAsync(orgId, catalogProductId, branchId: null, includeDepleted: false, cancellationToken)
                .ConfigureAwait(false);
            var categoryId = product.CategoryId?.Value;
            var distinctBranchKeys = authorizedBranches.Select(b => b.BranchId).Distinct().ToList();
            var policyMap = new Dictionary<Guid, EffectiveExpirySalePolicy>(distinctBranchKeys.Count);
            foreach (var branchId in distinctBranchKeys)
            {
                policyMap[branchId] = await _expirySalePolicies
                    .ResolveAsync(orgId, PosBranchId.From(branchId), categoryId, cancellationToken)
                    .ConfigureAwait(false);
            }

            salePolicyByBranch = policyMap;
        }

        var lotsByBranch = allLots
            .Where(l => l.BranchId is not null)
            .GroupBy(l => l.BranchId!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<InventoryLot>)g.ToList());
        var orgLevelLots = allLots.Where(l => l.BranchId is null).ToList();
        var today = InventoryLot.BusinessDateOf(_clock.UtcNow);

        var visibleRows = authorizedBranches
            .Select(branch =>
            {
                balanceByBranchId.TryGetValue(branch.BranchId, out var balance);
                var onHand = balance?.OnHandQuantity ?? 0m;
                var reserved = balance?.ReservedQuantity ?? 0m;
                var pendingReturn = balance?.PendingReturnQuantity ?? 0m;
                var inspectionHold = balance?.InspectionHoldQuantity ?? 0m;
                var damaged = balance?.DamagedQuantity ?? 0m;
                var available = BranchStockResolver.ResolveAvailable(
                    onHand,
                    reserved,
                    pendingReturn,
                    inspectionHold,
                    damaged);
                decimal? sellable = null;
                decimal? expired = null;
                decimal? saleBlocked = null;
                expirationByBranch.TryGetValue(branch.BranchId, out var expirationPolicy);
                if (expirationPolicy.TracksExpiration)
                {
                    lotsByBranch.TryGetValue(branch.BranchId, out var branchLots);
                    branchLots ??= [];
                    // Branch-scoped lots only. Org-level (BranchId null) lots adjust organization
                    // available below — never attributed to every empty tracking branch.
                    // No lot overlay yet: keep operational available (do not zero-cap).
                    if (branchLots.Count > 0)
                    {
                        salePolicyByBranch.TryGetValue(branch.BranchId, out var salePolicy);
                        var stopDays = salePolicy.StopSellingDaysBeforeExpiry;
                        var buckets = InventoryLotFefo.ProjectSaleBuckets(branchLots, today, stopDays);
                        sellable = buckets.Sellable;
                        expired = buckets.Expired;
                        saleBlocked = buckets.PolicyBlocked;
                        available = Math.Min(available, buckets.Sellable);
                    }
                }

                return new
                {
                    branch.AreaId,
                    branch.AreaName,
                    Row = new PosInventoryBranchRollupDto(
                        branch.BranchId,
                        branch.BranchName,
                        onHand,
                        reserved,
                        available,
                        pendingReturn,
                        inspectionHold,
                        damaged,
                        sellable,
                        expired,
                        saleBlocked)
                };
            })
            .ToList();

        var areas = visibleRows
            .GroupBy(x => x.AreaId)
            .Select(group => new PosInventoryAreaRollupDto(
                group.Key,
                group.Key is null ? null : group.Select(x => x.AreaName).FirstOrDefault(name => name is not null),
                IsUnassigned: group.Key is null,
                group.Sum(x => x.Row.OnHandQuantity),
                group.Sum(x => x.Row.ReservedQuantity),
                group.Sum(x => x.Row.AvailableQuantity),
                group
                    .Select(x => x.Row)
                    .OrderBy(row => row.BranchName, StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .OrderBy(area => area.IsUnassigned)
            .ThenBy(area => area.AreaName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var accessibleOnHand = visibleRows.Sum(x => x.Row.OnHandQuantity);
        var accessibleReserved = visibleRows.Sum(x => x.Row.ReservedQuantity);
        var accessibleAvailable = visibleRows.Sum(x => x.Row.AvailableQuantity);

        decimal? organizationAvailable = null;
        if (scope.IsOrganizationWide)
        {
            // Keep account authority for on-hand/reserved; available honors sellable caps when tracked.
            organizationAvailable = account.AvailableQuantity;
            if (anyTracks)
            {
                organizationAvailable = Math.Min(organizationAvailable.Value, accessibleAvailable);
                if (orgLevelLots.Count > 0)
                {
                    var orgBuckets = InventoryLotFefo.ProjectSaleBuckets(
                        orgLevelLots,
                        today,
                        InventoryLotSaleEligibility.DefaultStopSellingDays);
                    var nonSellable = orgBuckets.PolicyBlocked + orgBuckets.Expired;
                    if (nonSellable > 0m)
                    {
                        organizationAvailable = Math.Max(0m, organizationAvailable.Value - nonSellable);
                    }
                }
            }
        }

        return ApplicationResult<PosInventoryStockRollupDto>.Success(
            new PosInventoryStockRollupDto(
                productId,
                product.Name,
                product.UnitOfMeasure.ToString(),
                IsTracked: true,
                OrganizationTotalsVisible: scope.IsOrganizationWide,
                scope.IsOrganizationWide ? account.OnHandQuantity : null,
                scope.IsOrganizationWide ? account.ReservedQuantity : null,
                organizationAvailable,
                accessibleOnHand,
                accessibleReserved,
                accessibleAvailable,
                areas.Any(area => !area.IsUnassigned),
                areas));
    }
}

/// <summary>No-op lot repository for rollup tests / optional DI.</summary>
file sealed class EmptyInventoryLotRepository : IInventoryLotRepository
{
    public static EmptyInventoryLotRepository Instance { get; } = new();

    public Task<InventoryLot?> GetByIdAsync(
        PosOrganizationId organizationId,
        InventoryLotId lotId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<InventoryLot?>(null);

    public Task<InventoryLot?> FindAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        DateOnly expirationDate,
        string normalizedLotNumber,
        PosBranchId? branchId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<InventoryLot?>(null);

    public Task<IReadOnlyList<InventoryLot>> ListOnHandAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        PosBranchId? branchId,
        bool includeDepleted,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<InventoryLot>>([]);

    public Task<IReadOnlyList<InventoryLot>> ListOrgLevelOnHandAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        bool includeDepleted,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<InventoryLot>>([]);

    public Task<(IReadOnlyList<InventoryLot> Items, int TotalCount)> ListPagedAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        PosBranchId? branchId,
        bool includeDepleted,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<(IReadOnlyList<InventoryLot>, int)>(([], 0));

    public Task<(IReadOnlyList<InventoryLot> Items, int TotalCount)> ListExpiringPagedAsync(
        PosOrganizationId organizationId,
        PosBranchId? branchId,
        DateOnly expireOnOrBefore,
        DateOnly? expireOnOrAfter,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<(IReadOnlyList<InventoryLot>, int)>(([], 0));

    public Task<(int ExpiredCount, int NearExpiryCount)> CountExpiryAsync(
        PosOrganizationId organizationId,
        DateOnly today,
        PosBranchId? branchId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult((0, 0));

    public Task AddAsync(InventoryLot lot, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task UpdateAsync(InventoryLot lot, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task AddMovementAsync(InventoryLotMovement movement, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<bool> HasMovementAsync(
        PosOrganizationId organizationId,
        Guid sourceId,
        InventoryLotId lotId,
        StockMovementType movementType,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<IReadOnlyList<InventoryLotMovement>> ListBySourceAsync(
        PosOrganizationId organizationId,
        Guid sourceId,
        StockMovementType movementType,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<InventoryLotMovement>>([]);

    public Task AdoptOrgLevelLotsForBranchAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

file sealed class EmptyOrganizationExpirySalePolicyRepository : IOrganizationExpirySalePolicyRepository
{
    public static EmptyOrganizationExpirySalePolicyRepository Instance { get; } = new();

    public Task<OrganizationExpirySalePolicySetting?> GetAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<OrganizationExpirySalePolicySetting?>(null);

    public Task UpsertAsync(
        OrganizationExpirySalePolicySetting setting,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

file sealed class EmptyOrganizationCategoryExpirySalePolicyRepository : IOrganizationCategoryExpirySalePolicyRepository
{
    public static EmptyOrganizationCategoryExpirySalePolicyRepository Instance { get; } = new();

    public Task<OrganizationCategoryExpirySalePolicy?> GetAsync(
        PosOrganizationId organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<OrganizationCategoryExpirySalePolicy?>(null);

    public Task<IReadOnlyList<OrganizationCategoryExpirySalePolicy>> ListByOrganizationAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OrganizationCategoryExpirySalePolicy>>([]);

    public Task UpsertAsync(
        OrganizationCategoryExpirySalePolicy policy,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DeleteAsync(
        PosOrganizationId organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

file sealed class EmptyBranchExpirySalePolicyRepository : IBranchExpirySalePolicyRepository
{
    public static EmptyBranchExpirySalePolicyRepository Instance { get; } = new();

    public Task<BranchExpirySalePolicySetting?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<BranchExpirySalePolicySetting?>(null);

    public Task UpsertAsync(
        BranchExpirySalePolicySetting setting,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DeleteAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

file sealed class EmptyBranchCategoryExpirySalePolicyRepository : IBranchCategoryExpirySalePolicyRepository
{
    public static EmptyBranchCategoryExpirySalePolicyRepository Instance { get; } = new();

    public Task<BranchCategoryExpirySalePolicy?> GetAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        Guid categoryId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<BranchCategoryExpirySalePolicy?>(null);

    public Task<IReadOnlyList<BranchCategoryExpirySalePolicy>> ListByBranchAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BranchCategoryExpirySalePolicy>>([]);

    public Task UpsertAsync(
        BranchCategoryExpirySalePolicy policy,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DeleteAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        Guid categoryId,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

file sealed class SystemClock : IClock
{
    public static SystemClock Instance { get; } = new();
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
