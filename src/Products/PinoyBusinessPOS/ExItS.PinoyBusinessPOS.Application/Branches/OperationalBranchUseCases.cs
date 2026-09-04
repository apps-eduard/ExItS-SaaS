using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Branches;

public sealed record SelectOperationalBranchRequest(
    Guid BranchId,
    Guid? FromBranchId = null,
    Guid? DeviceBoundBranchId = null);

public sealed record OperationalBranchContextDto(
    Guid OrganizationId,
    Guid BranchId,
    string Name,
    bool DeviceMatchesSelectedBranch,
    Guid? DeviceBoundBranchId,
    bool OpenCashierShiftPresent,
    /// <summary>Retail (default) or Warehouse.</summary>
    string BranchType = "Retail");

/// <summary>
/// Server-side operational branch switch: org-scoped Active branch plus open-shift guard.
/// Does not rebind the POS device and does not grant CreateSale.
/// </summary>
public sealed class SelectOperationalBranch(
    ICashierShiftRepository shifts,
    IOrganizationBranchDirectory branches)
{
    public async Task<ApplicationResult<OperationalBranchContextDto>> ExecuteAsync(
        Guid organizationId,
        Guid actorId,
        Guid requestedBranchId,
        Guid? currentSelectedBranchId,
        Guid? deviceBoundBranchId,
        CancellationToken cancellationToken = default)
    {
        if (requestedBranchId == Guid.Empty)
        {
            return ApplicationResult<OperationalBranchContextDto>.Failure(
                DomainErrorCodes.InvalidBranchId,
                "BranchId cannot be an empty GUID.");
        }

        var active = await branches
            .IsActiveInOrganizationAsync(organizationId, requestedBranchId, cancellationToken)
            .ConfigureAwait(false);
        if (!active)
        {
            return ApplicationResult<OperationalBranchContextDto>.Failure(
                ApplicationErrorCodes.CustomerOrderBranchNotFound,
                "The selected branch is not an Active branch in this organization.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var hasOpenShift = await shifts
            .HasOpenShiftForActorAsync(orgId, actorId, cancellationToken)
            .ConfigureAwait(false);
        if (hasOpenShift)
        {
            var operational = currentSelectedBranchId ?? deviceBoundBranchId;
            if (operational is Guid current && current != requestedBranchId)
            {
                return ApplicationResult<OperationalBranchContextDto>.Failure(
                    ApplicationErrorCodes.OperationalBranchSwitchBlocked,
                    "Close or cancel the open cashier shift before switching POS branch.");
            }
        }

        var names = await branches
            .GetNamesAsync(organizationId, [requestedBranchId], cancellationToken)
            .ConfigureAwait(false);
        names.TryGetValue(requestedBranchId, out var name);
        var branchType = await branches
            .GetBranchTypeAsync(organizationId, requestedBranchId, cancellationToken)
            .ConfigureAwait(false);

        return ApplicationResult<OperationalBranchContextDto>.Success(
            new OperationalBranchContextDto(
                organizationId,
                requestedBranchId,
                string.IsNullOrWhiteSpace(name) ? "Branch" : name,
                deviceBoundBranchId is Guid deviceBranch && deviceBranch == requestedBranchId,
                deviceBoundBranchId,
                hasOpenShift,
                branchType));
    }
}
