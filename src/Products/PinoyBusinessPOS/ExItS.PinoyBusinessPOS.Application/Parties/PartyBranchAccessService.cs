using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Parties;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Application.Parties;

/// <summary>Branch visibility grants and query filters for org-owned customers and suppliers (MB2-04).</summary>
public sealed class PartyBranchAccessService
{
    private readonly ICustomerBranchAccessRepository _customerAccess;
    private readonly ISupplierBranchAccessRepository _supplierAccess;
    private readonly PartyBranchAccessGovernanceAuthority _governance;
    private readonly IPosUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PartyBranchAccessService(
        ICustomerBranchAccessRepository customerAccess,
        ISupplierBranchAccessRepository supplierAccess,
        PartyBranchAccessGovernanceAuthority governance,
        IPosUnitOfWork unitOfWork,
        IClock clock)
    {
        _customerAccess = customerAccess;
        _supplierAccess = supplierAccess;
        _governance = governance;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<bool> CanViewCustomerAsync(
        Guid organizationId,
        Guid branchId,
        Guid customerId,
        PartyBranchAccessActor actor,
        CancellationToken cancellationToken = default)
    {
        if (_governance.CanBypassBranchFilter(actor))
        {
            return true;
        }

        if (branchId == Guid.Empty)
        {
            return false;
        }

        return await _customerAccess
            .HasAccessAsync(
                PosOrganizationId.From(organizationId),
                PosBranchId.From(branchId),
                POSCustomerId.From(customerId),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> CanViewSupplierAsync(
        Guid organizationId,
        Guid branchId,
        Guid supplierId,
        PartyBranchAccessActor actor,
        CancellationToken cancellationToken = default)
    {
        if (_governance.CanBypassBranchFilter(actor))
        {
            return true;
        }

        if (branchId == Guid.Empty)
        {
            return false;
        }

        return await _supplierAccess
            .HasAccessAsync(
                PosOrganizationId.From(organizationId),
                PosBranchId.From(branchId),
                SupplierId.From(supplierId),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task GrantCustomerAccessAsync(
        Guid organizationId,
        Guid branchId,
        Guid customerId,
        PartyBranchGrantSource source,
        Guid? grantedByActorId = null,
        CancellationToken cancellationToken = default,
        bool persistChanges = true)
    {
        if (branchId == Guid.Empty || customerId == Guid.Empty)
        {
            return;
        }

        var access = CustomerBranchAccess.Create(
            PosOrganizationId.From(organizationId),
            PosBranchId.From(branchId),
            POSCustomerId.From(customerId),
            source,
            _clock.UtcNow,
            grantedByActorId);

        await _customerAccess.GrantAsync(access, cancellationToken).ConfigureAwait(false);
        if (persistChanges)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task GrantSupplierAccessAsync(
        Guid organizationId,
        Guid branchId,
        Guid supplierId,
        PartyBranchGrantSource source,
        Guid? grantedByActorId = null,
        CancellationToken cancellationToken = default,
        bool persistChanges = true)
    {
        if (branchId == Guid.Empty || supplierId == Guid.Empty)
        {
            return;
        }

        var access = SupplierBranchAccess.Create(
            PosOrganizationId.From(organizationId),
            PosBranchId.From(branchId),
            SupplierId.From(supplierId),
            source,
            _clock.UtcNow,
            grantedByActorId);

        await _supplierAccess.GrantAsync(access, cancellationToken).ConfigureAwait(false);
        if (persistChanges)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public Task RevokeCustomerExplicitAssignAsync(
        Guid organizationId,
        Guid branchId,
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        _customerAccess.RevokeGrantAsync(
            PosOrganizationId.From(organizationId),
            PosBranchId.From(branchId),
            POSCustomerId.From(customerId),
            PartyBranchGrantSource.ExplicitAssign,
            cancellationToken);

    public Task RevokeSupplierExplicitAssignAsync(
        Guid organizationId,
        Guid branchId,
        Guid supplierId,
        CancellationToken cancellationToken = default) =>
        _supplierAccess.RevokeGrantAsync(
            PosOrganizationId.From(organizationId),
            PosBranchId.From(branchId),
            SupplierId.From(supplierId),
            PartyBranchGrantSource.ExplicitAssign,
            cancellationToken);

    public async Task GrantCustomerExplicitAssignAsync(
        Guid organizationId,
        Guid branchId,
        Guid customerId,
        Guid? grantedByActorId = null,
        CancellationToken cancellationToken = default)
    {
        await GrantCustomerAccessAsync(
                organizationId,
                branchId,
                customerId,
                PartyBranchGrantSource.ExplicitAssign,
                grantedByActorId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task GrantSupplierExplicitAssignAsync(
        Guid organizationId,
        Guid branchId,
        Guid supplierId,
        Guid? grantedByActorId = null,
        CancellationToken cancellationToken = default)
    {
        await GrantSupplierAccessAsync(
                organizationId,
                branchId,
                supplierId,
                PartyBranchGrantSource.ExplicitAssign,
                grantedByActorId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Returns accessible customer ids for list filtering. Null means no branch filter (org governance).
    /// Empty means no customers are visible at the acting branch.
    /// </summary>
    public async Task<IReadOnlyCollection<Guid>?> FilterCustomerIdsAccessibleAsync(
        Guid organizationId,
        PartyBranchAccessActor actor,
        CancellationToken cancellationToken = default)
    {
        if (_governance.CanBypassBranchFilter(actor))
        {
            return null;
        }

        if (actor.ActingBranchId is null || actor.ActingBranchId == Guid.Empty)
        {
            return [];
        }

        var ids = await _customerAccess
            .ListAccessibleCustomerIdsAsync(
                PosOrganizationId.From(organizationId),
                PosBranchId.From(actor.ActingBranchId.Value),
                cancellationToken)
            .ConfigureAwait(false);

        return ids.Select(id => id.Value).ToList();
    }

    /// <summary>
    /// Returns accessible supplier ids for list filtering. Null means no branch filter (org governance).
    /// Empty means no suppliers are visible at the acting branch.
    /// </summary>
    public async Task<IReadOnlyCollection<Guid>?> FilterSupplierIdsAccessibleAsync(
        Guid organizationId,
        PartyBranchAccessActor actor,
        CancellationToken cancellationToken = default)
    {
        if (_governance.CanBypassBranchFilter(actor))
        {
            return null;
        }

        if (actor.ActingBranchId is null || actor.ActingBranchId == Guid.Empty)
        {
            return [];
        }

        var ids = await _supplierAccess
            .ListAccessibleSupplierIdsAsync(
                PosOrganizationId.From(organizationId),
                PosBranchId.From(actor.ActingBranchId.Value),
                cancellationToken)
            .ConfigureAwait(false);

        return ids.Select(id => id.Value).ToList();
    }

    public async Task<bool> EnsureCanViewCustomerOrNotFoundAsync(
        Guid organizationId,
        Guid customerId,
        PartyBranchAccessActor actor,
        CancellationToken cancellationToken = default)
    {
        if (_governance.CanBypassBranchFilter(actor))
        {
            return true;
        }

        if (actor.ActingBranchId is null || actor.ActingBranchId == Guid.Empty)
        {
            return false;
        }

        return await CanViewCustomerAsync(
                organizationId,
                actor.ActingBranchId.Value,
                customerId,
                actor,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> EnsureCanViewSupplierOrNotFoundAsync(
        Guid organizationId,
        Guid supplierId,
        PartyBranchAccessActor actor,
        CancellationToken cancellationToken = default)
    {
        if (_governance.CanBypassBranchFilter(actor))
        {
            return true;
        }

        if (actor.ActingBranchId is null || actor.ActingBranchId == Guid.Empty)
        {
            return false;
        }

        return await CanViewSupplierAsync(
                organizationId,
                actor.ActingBranchId.Value,
                supplierId,
                actor,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
