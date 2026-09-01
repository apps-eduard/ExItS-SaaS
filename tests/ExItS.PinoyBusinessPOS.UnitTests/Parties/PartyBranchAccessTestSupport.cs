using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Parties;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Parties;
using ExItS.PinoyBusinessPOS.Domain.Permissions;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Parties;

internal sealed class FixedPartyBranchAccessActorAccessor : IPartyBranchAccessActorAccessor
{
    public FixedPartyBranchAccessActorAccessor(PartyBranchAccessActor actor) => Actor = actor;

    public PartyBranchAccessActor Actor { get; set; }

    public PartyBranchAccessActor GetActor() => Actor;

    public static FixedPartyBranchAccessActorAccessor Owner(Guid? actingBranchId = null) =>
        new(new PartyBranchAccessActor(PosRole.Owner, false, actingBranchId));

    public static FixedPartyBranchAccessActorAccessor StoreManager(Guid actingBranchId) =>
        new(new PartyBranchAccessActor(PosRole.StoreManager, false, actingBranchId));
}

internal static class PartyBranchAccessTestSupport
{
    public static (PartyBranchAccessService Service, FixedPartyBranchAccessActorAccessor ActorAccessor) Create(
        Guid? actingBranchId = null,
        IClock? clock = null)
    {
        var customerAccess = new InMemoryCustomerBranchAccessRepository();
        var supplierAccess = new InMemorySupplierBranchAccessRepository();
        var service = new PartyBranchAccessService(
            customerAccess,
            supplierAccess,
            new PartyBranchAccessGovernanceAuthority(),
            new ImmediateUnitOfWork(),
            clock ?? new FixedClock(DateTimeOffset.Parse("2026-09-01T08:00:00Z")));
        var actorAccessor = actingBranchId is null
            ? FixedPartyBranchAccessActorAccessor.Owner()
            : FixedPartyBranchAccessActorAccessor.StoreManager(actingBranchId.Value);
        return (service, actorAccessor);
    }
}

internal sealed class InMemoryCustomerBranchAccessRepository : ICustomerBranchAccessRepository
{
    private readonly HashSet<(Guid Org, Guid Branch, Guid Customer)> _rows = new();

    public Task<bool> HasAccessAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_rows.Contains((organizationId.Value, branchId.Value, customerId.Value)));

    public Task<IReadOnlyList<POSCustomerId>> ListAccessibleCustomerIdsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var ids = _rows
            .Where(r => r.Org == organizationId.Value && r.Branch == branchId.Value)
            .Select(r => POSCustomerId.From(r.Customer))
            .ToList();
        return Task.FromResult<IReadOnlyList<POSCustomerId>>(ids);
    }

    public Task<IReadOnlyList<POSCustomerId>> FilterAccessibleCustomerIdsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyCollection<POSCustomerId> customerIds,
        CancellationToken cancellationToken = default)
    {
        var wanted = customerIds.Select(id => id.Value).ToHashSet();
        var ids = _rows
            .Where(r => r.Org == organizationId.Value && r.Branch == branchId.Value && wanted.Contains(r.Customer))
            .Select(r => POSCustomerId.From(r.Customer))
            .ToList();
        return Task.FromResult<IReadOnlyList<POSCustomerId>>(ids);
    }

    public Task GrantAsync(CustomerBranchAccess access, CancellationToken cancellationToken = default)
    {
        _rows.Add((access.OrganizationId.Value, access.BranchId.Value, access.CustomerId.Value));
        return Task.CompletedTask;
    }
}

internal sealed class InMemorySupplierBranchAccessRepository : ISupplierBranchAccessRepository
{
    private readonly HashSet<(Guid Org, Guid Branch, Guid Supplier)> _rows = new();

    public Task<bool> HasAccessAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        SupplierId supplierId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_rows.Contains((organizationId.Value, branchId.Value, supplierId.Value)));

    public Task<IReadOnlyList<SupplierId>> ListAccessibleSupplierIdsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var ids = _rows
            .Where(r => r.Org == organizationId.Value && r.Branch == branchId.Value)
            .Select(r => SupplierId.From(r.Supplier))
            .ToList();
        return Task.FromResult<IReadOnlyList<SupplierId>>(ids);
    }

    public Task<IReadOnlyList<SupplierId>> FilterAccessibleSupplierIdsAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyCollection<SupplierId> supplierIds,
        CancellationToken cancellationToken = default)
    {
        var wanted = supplierIds.Select(id => id.Value).ToHashSet();
        var ids = _rows
            .Where(r => r.Org == organizationId.Value && r.Branch == branchId.Value && wanted.Contains(r.Supplier))
            .Select(r => SupplierId.From(r.Supplier))
            .ToList();
        return Task.FromResult<IReadOnlyList<SupplierId>>(ids);
    }

    public Task GrantAsync(SupplierBranchAccess access, CancellationToken cancellationToken = default)
    {
        _rows.Add((access.OrganizationId.Value, access.BranchId.Value, access.SupplierId.Value));
        return Task.CompletedTask;
    }
}

internal sealed class FixedClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow => utcNow;
}

internal sealed class ImmediateUnitOfWork : IPosUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<T> ExecuteInSerializableTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default) =>
        action(cancellationToken);
}
