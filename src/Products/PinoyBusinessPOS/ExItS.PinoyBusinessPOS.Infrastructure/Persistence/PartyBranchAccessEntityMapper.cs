using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Parties;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Parties;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class PartyBranchAccessEntityMapper
{
    public static CustomerBranchAccess ToDomain(CustomerBranchAccessRecord record)
    {
        if (!PartyBranchGrantSources.TryParse(record.GrantSource, out var source))
        {
            source = PartyBranchGrantSource.MigrationBackfill;
        }

        return CustomerBranchAccess.Rehydrate(
            PosOrganizationId.From(record.OrganizationId),
            PosBranchId.From(record.BranchId),
            POSCustomerId.From(record.CustomerId),
            source,
            record.GrantedAtUtc,
            record.GrantedByActorId);
    }

    public static CustomerBranchAccessRecord ToRecord(CustomerBranchAccess access) =>
        new()
        {
            OrganizationId = access.OrganizationId.Value,
            BranchId = access.BranchId.Value,
            CustomerId = access.CustomerId.Value,
            GrantSource = PartyBranchGrantSources.ToCode(access.GrantSource),
            GrantedAtUtc = access.GrantedAtUtc,
            GrantedByActorId = access.GrantedByActorId,
        };

    public static SupplierBranchAccess ToDomain(SupplierBranchAccessRecord record)
    {
        if (!PartyBranchGrantSources.TryParse(record.GrantSource, out var source))
        {
            source = PartyBranchGrantSource.MigrationBackfill;
        }

        return SupplierBranchAccess.Rehydrate(
            PosOrganizationId.From(record.OrganizationId),
            PosBranchId.From(record.BranchId),
            SupplierId.From(record.SupplierId),
            source,
            record.GrantedAtUtc,
            record.GrantedByActorId);
    }

    public static SupplierBranchAccessRecord ToRecord(SupplierBranchAccess access) =>
        new()
        {
            OrganizationId = access.OrganizationId.Value,
            BranchId = access.BranchId.Value,
            SupplierId = access.SupplierId.Value,
            GrantSource = PartyBranchGrantSources.ToCode(access.GrantSource),
            GrantedAtUtc = access.GrantedAtUtc,
            GrantedByActorId = access.GrantedByActorId,
        };
}
