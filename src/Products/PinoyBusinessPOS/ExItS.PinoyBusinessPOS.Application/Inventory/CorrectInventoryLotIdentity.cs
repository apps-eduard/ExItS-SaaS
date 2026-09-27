using ExItS.PinoyBusinessPOS.Application.Abstractions;
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
/// Corrects lot expiration date and optional batch/lot number when the lot has no downstream usage
/// and is not referenced by an active transfer draft. Metadata only — never changes quantity.
/// </summary>
public sealed class CorrectInventoryLotIdentity
{
    private readonly IInventoryLotRepository _lots;
    private readonly IInventoryLotIdentityEditSupport _identitySupport;
    private readonly IInventoryLotIdentityCorrectionRepository _corrections;
    private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CorrectInventoryLotIdentity(
        IInventoryLotRepository lots,
        IInventoryLotIdentityEditSupport identitySupport,
        IInventoryLotIdentityCorrectionRepository corrections,
        ICatalogProductRepository products,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _lots = lots;
        _identitySupport = identitySupport;
        _corrections = corrections;
        _products = products;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PosInventoryLotDto>> ExecuteAsync(
        Guid organizationId,
        Guid branchId,
        Guid productId,
        Guid lotId,
        CorrectInventoryLotIdentityRequest request,
        Guid actorId,
        int warningDays,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<PosInventoryLotDto>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required to correct lot identity.");
        }

        if (branchId == Guid.Empty)
        {
            return ApplicationResult<PosInventoryLotDto>.Failure(
                ApplicationErrorCodes.InventoryBranchRequired,
                "A selected branch is required to correct lot identity.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var catalogProductId = CatalogProductId.From(productId);
        var branch = PosBranchId.From(branchId);
        var inventoryLotId = InventoryLotId.From(lotId);

        var product = await _products.GetByIdAsync(orgId, catalogProductId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<PosInventoryLotDto>.Failure(
                ApplicationErrorCodes.InventoryProductNotFound,
                "Product was not found.");
        }

        try
        {
            var lot = await _lots.GetByIdAsync(orgId, inventoryLotId, cancellationToken).ConfigureAwait(false);
            if (lot is null
                || lot.ProductId != catalogProductId
                || lot.BranchId is null
                || lot.BranchId != branch)
            {
                return ApplicationResult<PosInventoryLotDto>.Failure(
                    ApplicationErrorCodes.InventoryLotNotFound,
                    "Lot was not found at this location.");
            }

            if (request.ExpectedUpdatedAtUtc is { } expected
                && lot.UpdatedAtUtc != expected)
            {
                return ApplicationResult<PosInventoryLotDto>.Failure(
                    ApplicationErrorCodes.InventoryLotChanged,
                    "Lot information changed. Reload and try again.");
            }

            var lotIds = new[] { lot.Id.Value };
            var movementsByLot = await _identitySupport
                .ListDistinctMovementTypesByLotIdsAsync(orgId, lotIds, cancellationToken)
                .ConfigureAwait(false);
            var draftRefs = await _identitySupport
                .ListLotIdsReferencedByActiveTransferDraftAsync(orgId, lotIds, cancellationToken)
                .ConfigureAwait(false);

            movementsByLot.TryGetValue(lot.Id.Value, out var movementTypes);
            movementTypes ??= Array.Empty<StockMovementType>();
            var lockReason = InventoryLotIdentityEditPolicy.ResolveLockReason(
                movementTypes,
                draftRefs.Contains(lot.Id.Value));
            if (!InventoryLotIdentityEditPolicy.CanEditIdentity(lockReason))
            {
                return ApplicationResult<PosInventoryLotDto>.Failure(
                    ApplicationErrorCodes.InventoryLotIdentityLocked,
                    "Lot information is locked because stock from this lot has already been used or is referenced by an active document.",
                    new Dictionary<string, string> { ["identityLockReason"] = lockReason });
            }

            var (_, targetNormalized) = InventoryLot.NormalizeLotNumber(request.LotNumber);
            var conflict = await _lots
                .FindAsync(orgId, catalogProductId, request.ExpirationDate, targetNormalized, branch, cancellationToken)
                .ConfigureAwait(false);
            if (conflict is not null && conflict.Id != lot.Id)
            {
                return ApplicationResult<PosInventoryLotDto>.Failure(
                    ApplicationErrorCodes.InventoryLotIdentityConflict,
                    "A lot with this expiration date and batch/lot number already exists at this location.");
            }

            var utcNow = _clock.UtcNow;
            var oldExpiry = lot.ExpirationDate;
            var oldLotNumber = lot.LotNumber;
            var changed = lot.CorrectIdentity(request.ExpirationDate, request.LotNumber, utcNow);
            if (changed)
            {
                var correction = InventoryLotIdentityCorrection.Create(
                    orgId,
                    branch,
                    catalogProductId,
                    lot.Id,
                    oldExpiry,
                    lot.ExpirationDate,
                    oldLotNumber,
                    lot.LotNumber,
                    request.Reason,
                    actorId,
                    utcNow);
                await _corrections.AddAsync(correction, cancellationToken).ConfigureAwait(false);
                await _lots.UpdateAsync(lot, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Validate reason even on no-op so clients get consistent validation.
                _ = InventoryLotIdentityCorrection.NormalizeReason(request.Reason);
            }

            var today = InventoryLot.BusinessDateOf(utcNow);
            return ApplicationResult<PosInventoryLotDto>.Success(
                InventoryLotQueryService.Map(
                    lot,
                    today,
                    warningDays,
                    canEditIdentity: true,
                    identityLockReason: InventoryLotIdentityEditPolicy.LockReasonNone));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PosInventoryLotDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
