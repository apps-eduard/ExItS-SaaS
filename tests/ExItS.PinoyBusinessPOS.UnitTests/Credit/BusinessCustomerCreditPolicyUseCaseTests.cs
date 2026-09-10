using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.UnitTests.Credit;

public sealed class BusinessCustomerCreditPolicyUseCaseTests
{
    private static readonly Guid SellerOrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid BuyerOrgId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-30T10:00:00Z");

    [Fact]
    public async Task Upsert_then_approve_returns_approved_policy_with_available_credit()
    {
        var (relationships, policies, uow, connectionId) = await CreateActiveHarnessAsync();

        var upsert = new UpsertBusinessCustomerCreditPolicy(relationships, policies, uow, new FixedClock(Now));
        var configured = await upsert.ExecuteAsync(
            SellerOrgId,
            connectionId,
            creditLimit: 1_000m,
            defaultTermDays: 30,
            Actor,
            reason: "test configure");
        Assert.True(configured.IsSuccess);
        Assert.Equal(nameof(CustomerCreditPolicyStatus.PendingApproval), configured.Value!.Status);
        Assert.Equal(0m, configured.Value.OutstandingAmount);
        Assert.Equal(0m, configured.Value.AvailableCredit);
        Assert.NotNull(configured.Value.ExpectedUpdatedAtUtc);

        var approve = new ApproveBusinessCustomerCreditPolicy(
            relationships,
            policies,
            uow,
            new FixedClock(Now.AddSeconds(1)));
        var approved = await approve.ExecuteAsync(
            SellerOrgId,
            connectionId,
            Actor,
            "test approve",
            configured.Value.ExpectedUpdatedAtUtc!.Value);
        Assert.True(approved.IsSuccess);
        Assert.Equal(nameof(CustomerCreditPolicyStatus.Approved), approved.Value!.Status);
        Assert.Equal(1_000m, approved.Value.AvailableCredit);
        Assert.Equal(0m, approved.Value.OutstandingAmount);
    }

    [Fact]
    public async Task Edit_approved_policy_returns_to_pending_approval()
    {
        var (relationships, policies, uow, connectionId) = await CreateActiveHarnessAsync();
        var clock = new FixedClock(Now);
        var upsert = new UpsertBusinessCustomerCreditPolicy(relationships, policies, uow, clock);
        var configured = await upsert.ExecuteAsync(SellerOrgId, connectionId, 500m, 15, Actor, "cfg");
        Assert.True(configured.IsSuccess);

        var approve = new ApproveBusinessCustomerCreditPolicy(relationships, policies, uow, clock);
        var approved = await approve.ExecuteAsync(
            SellerOrgId,
            connectionId,
            Actor,
            "approve",
            configured.Value!.ExpectedUpdatedAtUtc!.Value);
        Assert.True(approved.IsSuccess);

        var edited = await upsert.ExecuteAsync(
            SellerOrgId,
            connectionId,
            750m,
            15,
            Actor,
            "raise limit",
            approved.Value!.ExpectedUpdatedAtUtc);
        Assert.True(edited.IsSuccess);
        Assert.Equal(nameof(CustomerCreditPolicyStatus.PendingApproval), edited.Value!.Status);
        Assert.Equal(0m, edited.Value.AvailableCredit);
    }

    [Fact]
    public async Task Disable_approved_policy()
    {
        var (relationships, policies, uow, connectionId) = await CreateActiveHarnessAsync();
        var clock = new FixedClock(Now);
        var upsert = new UpsertBusinessCustomerCreditPolicy(relationships, policies, uow, clock);
        var configured = await upsert.ExecuteAsync(SellerOrgId, connectionId, 500m, 15, Actor, "cfg");
        var approve = new ApproveBusinessCustomerCreditPolicy(relationships, policies, uow, clock);
        var approved = await approve.ExecuteAsync(
            SellerOrgId,
            connectionId,
            Actor,
            "approve",
            configured.Value!.ExpectedUpdatedAtUtc!.Value);

        var disable = new DisableBusinessCustomerCreditPolicy(relationships, policies, uow, clock);
        var disabled = await disable.ExecuteAsync(
            SellerOrgId,
            connectionId,
            Actor,
            "stop",
            approved.Value!.ExpectedUpdatedAtUtc!.Value);
        Assert.True(disabled.IsSuccess);
        Assert.Equal(nameof(CustomerCreditPolicyStatus.Disabled), disabled.Value!.Status);
    }

    [Fact]
    public async Task Get_returns_NotConfigured_200_when_no_row()
    {
        var (relationships, policies, _, connectionId) = await CreateActiveHarnessAsync();
        var get = new GetBusinessCustomerCreditPolicy(relationships, policies);
        var result = await get.ExecuteAsync(SellerOrgId, connectionId);
        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(CustomerCreditPolicyStatus.NotConfigured), result.Value!.Status);
        Assert.Null(result.Value.CreditLimit);
        Assert.Equal(0m, result.Value.OutstandingAmount);
        Assert.Equal(0m, result.Value.AvailableCredit);
    }

    [Fact]
    public async Task Upsert_does_not_create_pos_customer()
    {
        var (relationships, policies, uow, connectionId) = await CreateActiveHarnessAsync();
        var upsert = new UpsertBusinessCustomerCreditPolicy(relationships, policies, uow, new FixedClock(Now));
        var configured = await upsert.ExecuteAsync(SellerOrgId, connectionId, 100m, 7, Actor, "cfg");
        Assert.True(configured.IsSuccess);

        // Policy is keyed by seller+buyer orgs — no POSCustomer repository involvement.
        Assert.Equal(BuyerOrgId, configured.Value!.BuyerOrganizationId);
        Assert.Equal(SellerOrgId, configured.Value.SellerOrganizationId);
        Assert.Single(policies.Policies);
        Assert.Equal(connectionId, policies.Policies[0].ConnectionId);
    }

    [Fact]
    public async Task Get_works_for_disconnected_relationship_without_requiring_active()
    {
        var (relationships, policies, uow, connectionId) = await CreateActiveHarnessAsync();
        var upsert = new UpsertBusinessCustomerCreditPolicy(relationships, policies, uow, new FixedClock(Now));
        Assert.True((await upsert.ExecuteAsync(SellerOrgId, connectionId, 100m, 7, Actor, "cfg")).IsSuccess);

        relationships.Disconnect(connectionId, Now.AddHours(1));
        var get = new GetBusinessCustomerCreditPolicy(relationships, policies);
        var result = await get.ExecuteAsync(SellerOrgId, connectionId);
        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(CustomerCreditPolicyStatus.PendingApproval), result.Value!.Status);
    }

    [Fact]
    public async Task Upsert_requires_active_relationship()
    {
        var (relationships, policies, uow, connectionId) = await CreateActiveHarnessAsync();
        relationships.Disconnect(connectionId, Now.AddMinutes(1));

        var upsert = new UpsertBusinessCustomerCreditPolicy(relationships, policies, uow, new FixedClock(Now));
        var result = await upsert.ExecuteAsync(SellerOrgId, connectionId, 100m, 7, Actor, "cfg");
        Assert.False(result.IsSuccess);
        Assert.Equal(ConnectedSupplierErrorCodes.RelationshipInactive, result.ErrorCode);
    }

    [Fact]
    public void Cashier_is_denied_manage_and_approve_customer_credit_policy_capabilities()
    {
        Assert.False(PosRoleMatrix.Allows(PosRole.Cashier, UtangCapability.ManageCustomerCreditPolicy));
        Assert.False(PosRoleMatrix.Allows(PosRole.Cashier, UtangCapability.ApproveCustomerCreditPolicy));
        Assert.True(PosRoleMatrix.Allows(PosRole.Owner, UtangCapability.ManageCustomerCreditPolicy));
        Assert.True(PosRoleMatrix.Allows(PosRole.Owner, UtangCapability.ApproveCustomerCreditPolicy));
    }

    private static async Task<(
        InMemoryRelationshipRepository Relationships,
        InMemoryBusinessCustomerCreditPolicyRepository Policies,
        ImmediateUnitOfWork UnitOfWork,
        Guid ConnectionId)> CreateActiveHarnessAsync()
    {
        var seller = PosOrganizationId.From(SellerOrgId);
        var buyer = PosOrganizationId.From(BuyerOrgId);
        var relationship = ConnectedSupplierRelationship.InviteBuyer(buyer, seller, Now, Actor);
        relationship.Approve(Now.AddSeconds(1), Actor);
        var relationships = new InMemoryRelationshipRepository();
        await relationships.AddAsync(relationship);
        return (relationships, new InMemoryBusinessCustomerCreditPolicyRepository(), new ImmediateUnitOfWork(), relationship.Id.Value);
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class ImmediateUnitOfWork : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class InMemoryBusinessCustomerCreditPolicyRepository : IBusinessCustomerCreditPolicyRepository
    {
        public List<BusinessCustomerCreditPolicy> Policies { get; } = [];
        private readonly List<BusinessCustomerCreditPolicyChange> _changes = [];

        public Task<BusinessCustomerCreditPolicy?> GetBySellerAndBuyerAsync(
            PosOrganizationId sellerOrganizationId,
            PosOrganizationId buyerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Policies.FirstOrDefault(p =>
                p.SellerOrganizationId == sellerOrganizationId && p.BuyerOrganizationId == buyerOrganizationId));

        public Task<IReadOnlyList<BusinessCustomerCreditPolicy>> ListBySellerAndBuyerIdsAsync(
            PosOrganizationId sellerOrganizationId,
            IReadOnlyCollection<Guid> buyerOrganizationIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BusinessCustomerCreditPolicy>>(
                Policies
                    .Where(p =>
                        p.SellerOrganizationId == sellerOrganizationId
                        && buyerOrganizationIds.Contains(p.BuyerOrganizationId.Value))
                    .ToList());

        public Task AddAsync(BusinessCustomerCreditPolicy policy, CancellationToken cancellationToken = default)
        {
            Policies.Add(policy);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(BusinessCustomerCreditPolicy policy, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AddChangeAsync(BusinessCustomerCreditPolicyChange change, CancellationToken cancellationToken = default)
        {
            _changes.Add(change);
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<BusinessCustomerCreditPolicyChange> Items, int TotalCount)> ListChangesAsync(
            PosOrganizationId sellerOrganizationId,
            PosOrganizationId buyerOrganizationId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var list = _changes
                .Where(c => c.SellerOrganizationId == sellerOrganizationId
                            && c.BuyerOrganizationId == buyerOrganizationId)
                .OrderByDescending(c => c.ChangedAtUtc)
                .ToList();
            return Task.FromResult(((IReadOnlyList<BusinessCustomerCreditPolicyChange>)list.Skip(skip).Take(take).ToList(), list.Count));
        }

        public Task AcquireBusinessCustomerCreditLockAsync(
            PosOrganizationId sellerOrganizationId,
            PosOrganizationId buyerOrganizationId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryRelationshipRepository : IConnectedSupplierRelationshipRepository
    {
        private readonly List<ConnectedSupplierRelationship> _items = [];

        public Task AddAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default)
        {
            _items.Add(relationship);
            return Task.CompletedTask;
        }

        public void Disconnect(Guid connectionId, DateTimeOffset utcNow)
        {
            var r = _items.First(x => x.Id.Value == connectionId);
            r.Disconnect(utcNow);
        }

        public Task<ConnectedSupplierRelationship?> GetAsync(
            ConnectedSupplierRelationshipId id,
            CancellationToken ct = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<ConnectedSupplierRelationship?> FindOpenAsync(
            PosOrganizationId buyer,
            PosOrganizationId supplier,
            CancellationToken ct = default) =>
            Task.FromResult<ConnectedSupplierRelationship?>(null);

        public Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(
            PosOrganizationId organizationId,
            bool supplierView,
            CancellationToken ct = default) =>
            Task.FromResult((IReadOnlyList<ConnectedSupplierRelationship>)_items);

        public Task UpdateAsync(ConnectedSupplierRelationship relationship, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
