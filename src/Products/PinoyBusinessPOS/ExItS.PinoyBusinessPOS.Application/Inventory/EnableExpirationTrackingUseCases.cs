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
/// Atomically enables catalog expiration tracking. When authoritative on-hand is greater than
/// zero, existing stock must be allocated into lots whose quantities sum exactly to on-hand.
/// Does not change product on-hand (no product stock movement / ApplyMovementEffect).
/// </summary>
public sealed class EnableExpirationTracking
{
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryLotRepository _lots;
    private readonly InventoryLotStockService _lotStock;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public EnableExpirationTracking(
        ICatalogProductRepository products,
        IInventoryRepository inventory,
        IInventoryLotRepository lots,
        InventoryLotStockService lotStock,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _products = products;
        _inventory = inventory;
        _lots = lots;
        _lotStock = lotStock;
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
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to enable expiration tracking.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var catalogProductId = CatalogProductId.From(productId);
        var lines = existingStockLots ?? [];

        try
        {
            return await _unitOfWork
                .ExecuteInSerializableTransactionAsync(
                    async ct =>
                    {
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
                        var onHand = ResolveAuthoritativeOnHand(account);

                        if (product.TracksExpiration)
                        {
                            return await BuildIdempotentOrConflictAsync(
                                    orgId,
                                    product,
                                    account,
                                    onHand,
                                    ct)
                                .ConfigureAwait(false);
                        }

                        if (expectedOnHandQuantity is decimal expected && expected != onHand)
                        {
                            return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                                ApplicationErrorCodes.ExpirationAllocationStockChanged,
                                "On-hand quantity changed before expiration tracking could be enabled. Reload and retry.");
                        }

                        var utcNow = _clock.UtcNow;

                        if (onHand == 0m)
                        {
                            if (lines.Count > 0 && lines.Sum(l => l.Quantity) != 0m)
                            {
                                return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                                    ApplicationErrorCodes.ExpirationAllocationMismatch,
                                    "Existing-stock lot quantities must sum exactly to on-hand (currently zero).");
                            }

                            product.SetExpirationTracking(true, expirationWarningDays, utcNow);
                            await _products.UpdateAsync(product, ct).ConfigureAwait(false);
                            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                            return ApplicationResult<EnableExpirationTrackingResponse>.Success(
                                await MapResponseAsync(orgId, product, account, ct).ConfigureAwait(false));
                        }

                        // OnHand > 0 requires tracked inventory account and exact lot allocation.
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
                                "Existing stock must be allocated into lots before enabling expiration tracking.");
                        }

                        var allocatedSum = lines.Sum(l => l.Quantity);
                        if (allocatedSum != onHand)
                        {
                            return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                                ApplicationErrorCodes.ExpirationAllocationMismatch,
                                $"Existing-stock lot quantities ({allocatedSum}) must sum exactly to on-hand ({onHand}).");
                        }

                        // Reload on-hand after validation to catch concurrent stock changes.
                        account = await _inventory
                            .GetByProductIdAsync(orgId, catalogProductId, ct)
                            .ConfigureAwait(false);
                        var reloadedOnHand = ResolveAuthoritativeOnHand(account);
                        if (reloadedOnHand != onHand || reloadedOnHand != allocatedSum)
                        {
                            return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
                                ApplicationErrorCodes.ExpirationAllocationStockChanged,
                                "On-hand quantity changed before expiration lots could be allocated. Reload and retry.");
                        }

                        await _lotStock
                            .AllocateExistingOnHandLotsAsync(
                                orgId,
                                catalogProductId,
                                lines,
                                actorId,
                                utcNow,
                                branchId: null,
                                ct)
                            .ConfigureAwait(false);

                        product.SetExpirationTracking(true, expirationWarningDays, utcNow);
                        await _products.UpdateAsync(product, ct).ConfigureAwait(false);
                        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

                        return ApplicationResult<EnableExpirationTrackingResponse>.Success(
                            await MapResponseAsync(orgId, product, account, ct).ConfigureAwait(false));
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
                "On-hand quantity changed before expiration lots could be allocated. Reload and retry.");
        }
    }

    private async Task<ApplicationResult<EnableExpirationTrackingResponse>> BuildIdempotentOrConflictAsync(
        PosOrganizationId orgId,
        CatalogProduct product,
        InventoryAccount? account,
        decimal onHand,
        CancellationToken cancellationToken)
    {
        var lots = await _lots
            .ListOnHandAsync(orgId, product.Id, branchId: null, includeDepleted: false, cancellationToken)
            .ConfigureAwait(false);
        var lotTotal = InventoryLotFefo.TotalOnHand(lots);

        if (onHand == 0m || lotTotal == onHand)
        {
            return ApplicationResult<EnableExpirationTrackingResponse>.Success(
                await MapResponseAsync(orgId, product, account, cancellationToken).ConfigureAwait(false));
        }

        return ApplicationResult<EnableExpirationTrackingResponse>.Failure(
            ApplicationErrorCodes.ExpirationTrackingAlreadyEnabled,
            "Expiration tracking is already enabled but lot quantities do not match on-hand.");
    }

    private async Task<EnableExpirationTrackingResponse> MapResponseAsync(
        PosOrganizationId orgId,
        CatalogProduct product,
        InventoryAccount? account,
        CancellationToken cancellationToken)
    {
        var utcNow = _clock.UtcNow;
        var today = InventoryLot.BusinessDateOf(utcNow);
        var warning = product.EffectiveExpirationWarningDays;
        var lots = await _lots
            .ListOnHandAsync(orgId, product.Id, branchId: null, includeDepleted: true, cancellationToken)
            .ConfigureAwait(false);

        return new EnableExpirationTrackingResponse(
            product.Id.Value,
            product.OrganizationId.Value,
            product.TracksExpiration,
            product.ExpirationWarningDays,
            account?.IsTracked ?? false,
            ResolveAuthoritativeOnHand(account),
            lots.Select(l => InventoryLotQueryService.Map(l, today, warning)).ToList());
    }

    private static decimal ResolveAuthoritativeOnHand(InventoryAccount? account) =>
        account is { IsTracked: true } ? account.OnHandQuantity : 0m;
}
