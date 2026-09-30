using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Atomically enables <em>branch-scoped</em> expiration tracking for the current workspace branch.
/// Authoritative on-hand is the current branch physical quantity (not organization total).
/// Does not mutate CatalogProduct.TracksExpiration (legacy compatibility field).
/// Does not change product on-hand (no ApplyMovementEffect).
/// </summary>
public sealed class EnableExpirationTracking
{
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _balances;
    private readonly IInventoryLotRepository _lots;
    private readonly IInventoryBranchExpirationSettingRepository _expirationSettings;
    private readonly InventoryLotStockService _lotStock;
    private readonly IOrganizationBranchDirectory _branches;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public EnableExpirationTracking(
        ICatalogProductRepository products,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository balances,
        IInventoryLotRepository lots,
        IInventoryBranchExpirationSettingRepository expirationSettings,
        InventoryLotStockService lotStock,
        IOrganizationBranchDirectory branches,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _products = products;
        _inventory = inventory;
        _balances = balances;
        _lots = lots;
        _expirationSettings = expirationSettings;
        _lotStock = lotStock;
        _branches = branches;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<EnableExpirationTrackingResponse>> ExecuteAsync(
        Guid organizationId,
        Guid productId,
        Guid actorId,
        int? expirationWarningDays,
        IReadOnlyList<ExistingStockLotInput>? existingStockLots,
        decimal? expectedOnHandQuantity = null,
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to enable expiration tracking.");
        }

        if (branchId is null || branchId.Value == Guid.Empty)
        {
            return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                DomainErrorCodes.InventoryExpirationBranchRequired,
                "A selected branch is required to enable expiration tracking.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var catalogProductId = CatalogProductId.From(productId);
        var branch = PosBranchId.From(branchId.Value);
        var lines = existingStockLots ?? [];

        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(
                    async ct =>
                    {
                        if (!await _branches
                                .ExistsInOrganizationAsync(organizationId, branch.Value, ct)
                                .ConfigureAwait(false)
                            || !await _branches
                                .IsActiveInOrganizationAsync(organizationId, branch.Value, ct)
                                .ConfigureAwait(false))
                        {
                            return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                                DomainErrorCodes.InvalidBranchId,
                                "The selected branch is not active for inventory operations.");
                        }

                        var product = await _products
                            .GetByIdAsync(orgId, catalogProductId, ct)
                            .ConfigureAwait(false);
                        if (product is null)
                        {
                            return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                                ApplicationErrorCodes.InventoryProductNotFound,
                                "Product was not found.");
                        }

                        var account = await _inventory
                            .GetByProductIdAsync(orgId, catalogProductId, ct)
                            .ConfigureAwait(false);
                        var primaryBranchId = await _branches
                            .GetPrimaryBranchIdAsync(organizationId, ct)
                            .ConfigureAwait(false);
                        var branchOnHand = await ResolveBranchOnHandAsync(
                                orgId,
                                catalogProductId,
                                branch,
                                primaryBranchId,
                                account,
                                ct)
                            .ConfigureAwait(false);

                        await _lots
                            .AdoptOrgLevelLotsForBranchAsync(orgId, catalogProductId, branch, ct)
                            .ConfigureAwait(false);

                        var existingSetting = await _expirationSettings
                            .GetAsync(orgId, branch, catalogProductId, ct)
                            .ConfigureAwait(false);
                        var policy = BranchExpirationPolicyResolver.FromSetting(existingSetting);

                        if (policy.TracksExpiration)
                        {
                            return await BuildAlreadyEnabledAsync(
                                    orgId,
                                    product,
                                    account,
                                    branchOnHand,
                                    actorId,
                                    expirationWarningDays,
                                    lines,
                                    expectedOnHandQuantity,
                                    branch,
                                    primaryBranchId,
                                    existingSetting!,
                                    ct)
                                .ConfigureAwait(false);
                        }

                        if (expectedOnHandQuantity is decimal expected && expected != branchOnHand)
                        {
                            return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                                ApplicationErrorCodes.ExpirationAllocationStockChanged,
                                "Branch on-hand quantity changed before expiration tracking could be enabled. Reload and retry.");
                        }

                        var utcNow = _clock.UtcNow;

                        if (branchOnHand == 0m)
                        {
                            if (lines.Count > 0 && lines.Sum(l => l.Quantity) != 0m)
                            {
                                return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                                    ApplicationErrorCodes.ExpirationAllocationMismatch,
                                    "Existing-stock lot quantities must sum exactly to branch on-hand (currently zero).");
                            }

                            await PersistEnabledSettingAsync(
                                    orgId,
                                    branch,
                                    catalogProductId,
                                    existingSetting,
                                    expirationWarningDays,
                                    actorId,
                                    utcNow,
                                    ct)
                                .ConfigureAwait(false);
                            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                            return ApplicationResult<EnableExpirationTrackingResponse>.Success(
                                await MapResponseAsync(orgId, product, account, branch, primaryBranchId, ct)
                                    .ConfigureAwait(false));
                        }

                        if (account is null || !account.IsTracked)
                        {
                            return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                                DomainErrorCodes.InventoryNotTracked,
                                "Inventory is not tracked for this product; cannot allocate existing on-hand into lots.");
                        }

                        if (lines.Count == 0)
                        {
                            return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                                ApplicationErrorCodes.ExpirationInitializationRequired,
                                "Existing branch stock must be allocated into lots before enabling expiration tracking.");
                        }

                        var allocatedSum = lines.Sum(l => l.Quantity);
                        if (allocatedSum != branchOnHand)
                        {
                            return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                                ApplicationErrorCodes.ExpirationAllocationMismatch,
                                $"Existing-stock lot quantities ({allocatedSum}) must sum exactly to branch on-hand ({branchOnHand}).");
                        }

                        account = await _inventory
                            .GetByProductIdAsync(orgId, catalogProductId, ct)
                            .ConfigureAwait(false);
                        var reloadedBranchOnHand = await ResolveBranchOnHandAsync(
                                orgId,
                                catalogProductId,
                                branch,
                                primaryBranchId,
                                account,
                                ct)
                            .ConfigureAwait(false);
                        if (reloadedBranchOnHand != branchOnHand || reloadedBranchOnHand != allocatedSum)
                        {
                            return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                                ApplicationErrorCodes.ExpirationAllocationStockChanged,
                                "Branch on-hand quantity changed before expiration lots could be allocated. Reload and retry.");
                        }

                        await _lotStock
                            .AllocateExistingOnHandLotsAsync(
                                orgId,
                                catalogProductId,
                                lines,
                                actorId,
                                utcNow,
                                branch,
                                ct)
                            .ConfigureAwait(false);

                        await PersistEnabledSettingAsync(
                                orgId,
                                branch,
                                catalogProductId,
                                existingSetting,
                                expirationWarningDays,
                                actorId,
                                utcNow,
                                ct)
                            .ConfigureAwait(false);
                        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

                        return ApplicationResult<EnableExpirationTrackingResponse>.Success(
                            await MapResponseAsync(orgId, product, account, branch, primaryBranchId, ct)
                                .ConfigureAwait(false));
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<EnableExpirationTrackingResponse>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException)
        {
            return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                ApplicationErrorCodes.ExpirationAllocationStockChanged,
                "Branch on-hand quantity changed before expiration lots could be allocated. Reload and retry.");
        }
    }

    private async Task<ApplicationResult<EnableExpirationTrackingResponse>> BuildAlreadyEnabledAsync(
        PosOrganizationId orgId,
        CatalogProduct product,
        InventoryAccount? account,
        decimal branchOnHand,
        Guid actorId,
        int? expirationWarningDays,
        IReadOnlyList<ExistingStockLotInput> lines,
        decimal? expectedOnHandQuantity,
        PosBranchId branch,
        Guid? primaryBranchId,
        InventoryBranchExpirationSetting setting,
        CancellationToken cancellationToken)
    {
        var lots = await _lots
            .ListOnHandAsync(orgId, product.Id, branch, includeDepleted: false, cancellationToken)
            .ConfigureAwait(false);
        var lotTotal = InventoryLotFefo.TotalOnHand(lots);

        if (branchOnHand == 0m || lotTotal == branchOnHand)
        {
            if (expirationWarningDays is not null
                && expirationWarningDays != setting.ExpirationWarningDays)
            {
                setting.SetWarningDays(expirationWarningDays, actorId, _clock.UtcNow);
                await _expirationSettings.UpsertAsync(setting, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return ApplicationResult<EnableExpirationTrackingResponse>.Success(
                await MapResponseAsync(orgId, product, account, branch, primaryBranchId, cancellationToken)
                    .ConfigureAwait(false));
        }

        // Repair: setting ON, positive branch on-hand, no lot coverage.
        if (lotTotal == 0m && branchOnHand > 0m)
        {
            if (expectedOnHandQuantity is decimal expected && expected != branchOnHand)
            {
                return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                    ApplicationErrorCodes.ExpirationAllocationStockChanged,
                    "Branch on-hand quantity changed before expiration lots could be allocated. Reload and retry.");
            }

            if (account is null || !account.IsTracked)
            {
                return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                    DomainErrorCodes.InventoryNotTracked,
                    "Inventory is not tracked for this product; cannot allocate existing on-hand into lots.");
            }

            if (lines.Count == 0)
            {
                return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                    ApplicationErrorCodes.ExpirationInitializationRequired,
                    "Existing branch stock must be allocated into lots before expiration setup is complete.");
            }

            var allocatedSum = lines.Sum(l => l.Quantity);
            if (allocatedSum != branchOnHand)
            {
                return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                    ApplicationErrorCodes.ExpirationAllocationMismatch,
                    $"Existing-stock lot quantities ({allocatedSum}) must sum exactly to branch on-hand ({branchOnHand}).");
            }

            account = await _inventory
                .GetByProductIdAsync(orgId, product.Id, cancellationToken)
                .ConfigureAwait(false);
            var reloaded = await ResolveBranchOnHandAsync(
                    orgId,
                    product.Id,
                    branch,
                    primaryBranchId,
                    account,
                    cancellationToken)
                .ConfigureAwait(false);
            if (reloaded != branchOnHand || reloaded != allocatedSum)
            {
                return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                    ApplicationErrorCodes.ExpirationAllocationStockChanged,
                    "Branch on-hand quantity changed before expiration lots could be allocated. Reload and retry.");
            }

            var utcNow = _clock.UtcNow;
            await _lotStock
                .AllocateExistingOnHandLotsAsync(
                    orgId,
                    product.Id,
                    lines,
                    actorId,
                    utcNow,
                    branch,
                    cancellationToken)
                .ConfigureAwait(false);

            if (expirationWarningDays is not null)
            {
                setting.SetWarningDays(expirationWarningDays, actorId, utcNow);
                await _expirationSettings.UpsertAsync(setting, cancellationToken).ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<EnableExpirationTrackingResponse>.Success(
                await MapResponseAsync(orgId, product, account, branch, primaryBranchId, cancellationToken)
                    .ConfigureAwait(false));
        }

        return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
            ApplicationErrorCodes.ExpirationTrackingAlreadyEnabled,
            "Expiration tracking is already enabled for this branch but lot quantities do not match branch on-hand.");
    }

    private async Task PersistEnabledSettingAsync(
        PosOrganizationId orgId,
        PosBranchId branch,
        CatalogProductId productId,
        InventoryBranchExpirationSetting? existing,
        int? expirationWarningDays,
        Guid actorId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (existing is null)
        {
            var created = InventoryBranchExpirationSetting.CreateEnabled(
                orgId,
                branch,
                productId,
                expirationWarningDays,
                actorId,
                utcNow);
            await _expirationSettings.UpsertAsync(created, cancellationToken).ConfigureAwait(false);
            return;
        }

        existing.Enable(expirationWarningDays, actorId, utcNow);
        await _expirationSettings.UpsertAsync(existing, cancellationToken).ConfigureAwait(false);
    }

    private async Task<decimal> ResolveBranchOnHandAsync(
        PosOrganizationId orgId,
        CatalogProductId productId,
        PosBranchId branch,
        Guid? primaryBranchId,
        InventoryAccount? account,
        CancellationToken cancellationToken)
    {
        var orgOnHand = account is { IsTracked: true } ? account.OnHandQuantity : 0m;
        var balances = await _balances
            .ListByProductIdsAsync(orgId, [productId], cancellationToken)
            .ConfigureAwait(false);
        return BranchStockResolver.ResolveOnHand(
            branch,
            primaryBranchId,
            orgOnHand,
            balances,
            productId);
    }

    private async Task<EnableExpirationTrackingResponse> MapResponseAsync(
        PosOrganizationId orgId,
        CatalogProduct product,
        InventoryAccount? account,
        PosBranchId branch,
        Guid? primaryBranchId,
        CancellationToken cancellationToken)
    {
        var utcNow = _clock.UtcNow;
        var today = InventoryLot.BusinessDateOf(utcNow);
        var setting = await _expirationSettings
            .GetAsync(orgId, branch, product.Id, cancellationToken)
            .ConfigureAwait(false);
        var policy = BranchExpirationPolicyResolver.FromSetting(setting);
        var warning = policy.EffectiveWarningDays;
        var lots = await _lots
            .ListOnHandAsync(orgId, product.Id, branch, includeDepleted: true, cancellationToken)
            .ConfigureAwait(false);
        var branchOnHand = await ResolveBranchOnHandAsync(
                orgId,
                product.Id,
                branch,
                primaryBranchId,
                account,
                cancellationToken)
            .ConfigureAwait(false);

        return new EnableExpirationTrackingResponse(
            product.Id.Value,
            product.OrganizationId.Value,
            policy.TracksExpiration,
            policy.ExpirationWarningDays,
            account?.IsTracked ?? false,
            branchOnHand,
            lots.Select(l => InventoryLotQueryService.Map(l, today, warning)).ToList());
    }
}
