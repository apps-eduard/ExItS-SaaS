using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Domain.Parties;

/// <summary>
/// Branch visibility grant for a canonical org-owned supplier. Sparse: no row means no access
/// for non-governance actors at that branch.
/// </summary>
public sealed class SupplierBranchAccess
{
    public PosOrganizationId OrganizationId { get; }
    public PosBranchId BranchId { get; }
    public SupplierId SupplierId { get; }
    public PartyBranchGrantSource GrantSource { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; }
    public Guid? GrantedByActorId { get; private set; }

    private SupplierBranchAccess(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        SupplierId supplierId,
        PartyBranchGrantSource grantSource,
        DateTimeOffset grantedAtUtc,
        Guid? grantedByActorId)
    {
        OrganizationId = organizationId;
        BranchId = branchId;
        SupplierId = supplierId;
        GrantSource = grantSource;
        GrantedAtUtc = grantedAtUtc;
        GrantedByActorId = grantedByActorId;
    }

    public static SupplierBranchAccess Create(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        SupplierId supplierId,
        PartyBranchGrantSource grantSource,
        DateTimeOffset grantedAtUtc,
        Guid? grantedByActorId = null) =>
        new(
            organizationId,
            branchId,
            supplierId,
            grantSource,
            grantedAtUtc,
            grantedByActorId == Guid.Empty ? null : grantedByActorId);

    public static SupplierBranchAccess Rehydrate(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        SupplierId supplierId,
        PartyBranchGrantSource grantSource,
        DateTimeOffset grantedAtUtc,
        Guid? grantedByActorId = null) =>
        new(
            organizationId,
            branchId,
            supplierId,
            grantSource,
            grantedAtUtc,
            grantedByActorId == Guid.Empty ? null : grantedByActorId);
}
