using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Domain.Parties;

/// <summary>
/// Branch visibility grant for a canonical org-owned POS customer. Sparse: no row means no access
/// for non-governance actors at that branch.
/// </summary>
public sealed class CustomerBranchAccess
{
    public PosOrganizationId OrganizationId { get; }
    public PosBranchId BranchId { get; }
    public POSCustomerId CustomerId { get; }
    public PartyBranchGrantSource GrantSource { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; }
    public Guid? GrantedByActorId { get; private set; }

    private CustomerBranchAccess(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        POSCustomerId customerId,
        PartyBranchGrantSource grantSource,
        DateTimeOffset grantedAtUtc,
        Guid? grantedByActorId)
    {
        OrganizationId = organizationId;
        BranchId = branchId;
        CustomerId = customerId;
        GrantSource = grantSource;
        GrantedAtUtc = grantedAtUtc;
        GrantedByActorId = grantedByActorId;
    }

    public static CustomerBranchAccess Create(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        POSCustomerId customerId,
        PartyBranchGrantSource grantSource,
        DateTimeOffset grantedAtUtc,
        Guid? grantedByActorId = null) =>
        new(
            organizationId,
            branchId,
            customerId,
            grantSource,
            grantedAtUtc,
            grantedByActorId == Guid.Empty ? null : grantedByActorId);

    public static CustomerBranchAccess Rehydrate(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        POSCustomerId customerId,
        PartyBranchGrantSource grantSource,
        DateTimeOffset grantedAtUtc,
        Guid? grantedByActorId = null) =>
        new(
            organizationId,
            branchId,
            customerId,
            grantSource,
            grantedAtUtc,
            grantedByActorId == Guid.Empty ? null : grantedByActorId);
}
