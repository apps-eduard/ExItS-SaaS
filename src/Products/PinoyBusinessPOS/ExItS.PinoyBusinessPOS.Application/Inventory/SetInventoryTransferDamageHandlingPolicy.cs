using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public sealed class SetInventoryTransferDamageHandlingPolicy
{
    private readonly IInventoryTransferRepository _transfers;
    private readonly IOrganizationBranchDirectory _branches;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SetInventoryTransferDamageHandlingPolicy(
        IInventoryTransferRepository transfers,
        IOrganizationBranchDirectory branches,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _transfers = transfers;
        _branches = branches;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<InventoryTransfer>> ExecuteAsync(
        Guid organizationId,
        Guid transferId,
        string damageHandlingPolicy,
        Guid actorId,
        Guid actingBranchId,
        CancellationToken cancellationToken = default)
    {
        if (actorId == Guid.Empty)
        {
            return ApplicationResult<InventoryTransfer>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An actor identifier is required.");
        }

        try
        {
            var orgId = PosOrganizationId.From(organizationId);
            var transfer = await _transfers
                .GetByIdAsync(orgId, InventoryTransferId.From(transferId), cancellationToken)
                .ConfigureAwait(false);
            if (transfer is null)
            {
                return ApplicationResult<InventoryTransfer>.Failure(
                    ApplicationErrorCodes.InventoryTransferNotFound,
                    "Inventory transfer was not found.");
            }

            var branchGuard = await InventoryTransferAuthorization
                .EnsureSourceActorAsync(
                    _branches,
                    organizationId,
                    transfer.SourceBranchId.Value,
                    transfer.DestinationBranchId.Value,
                    actingBranchId,
                    cancellationToken)
                .ConfigureAwait(false);
            if (branchGuard is not null)
            {
                return branchGuard;
            }

            var policy = InventoryTransferDamageHandlingPolicies.Parse(damageHandlingPolicy);
            transfer.SetDamageHandlingPolicy(policy, _clock.UtcNow);
            await _transfers.UpdateAsync(transfer, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<InventoryTransfer>.Success(transfer);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<InventoryTransfer>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<InventoryTransfer>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed record SetInventoryTransferDamageHandlingPolicyRequest(string DamageHandlingPolicy);
