using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class BusinessCustomerDeliveryPreferencesTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 31, 4, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Enable_and_disable_persist_on_business_customer()
    {
        var orgId = PlatformOrganizationId.New();
        var orgs = new InMemoryPlatformOrganizationRepository();
        await orgs.AddAsync(PlatformOrganization.Create("Acme", "acme-pref", T0, id: orgId));
        var customers = new CustomerLinkCompletenessTests.InMemoryBusinessCustomerRepository();
        var credit = new StubCreditCustomerRepository();
        var uow = new RecordingUnitOfWork();
        var clock = new FixedClock(T0);

        var create = new CreateBusinessCustomer(orgs, customers, uow, clock);
        var created = await create.ExecuteAsync(orgId, new CreateBusinessCustomerRequest("Pat"));
        Assert.True(created.IsSuccess);
        Assert.False(created.Value!.AllowDeliveryBeyondNormalDistance);

        var update = new UpdateBusinessCustomerDeliveryPreferences(customers, credit, uow, clock);
        var enabled = await update.ExecuteAsync(
            BusinessCustomerId.From(created.Value.Id),
            orgId,
            new UpdateBusinessCustomerDeliveryPreferencesRequest(true));
        Assert.True(enabled.IsSuccess);
        Assert.True(enabled.Value!.AllowDeliveryBeyondNormalDistance);

        var disabled = await update.ExecuteAsync(
            BusinessCustomerId.From(created.Value.Id),
            orgId,
            new UpdateBusinessCustomerDeliveryPreferencesRequest(false));
        Assert.True(disabled.IsSuccess);
        Assert.False(disabled.Value!.AllowDeliveryBeyondNormalDistance);
    }

    [Fact]
    public async Task Cross_org_update_is_denied()
    {
        var orgA = PlatformOrganizationId.New();
        var orgB = PlatformOrganizationId.New();
        var orgs = new InMemoryPlatformOrganizationRepository();
        await orgs.AddAsync(PlatformOrganization.Create("Org A Pref", "org-a-pref", T0, id: orgA));
        var customers = new CustomerLinkCompletenessTests.InMemoryBusinessCustomerRepository();
        var credit = new StubCreditCustomerRepository();
        var uow = new RecordingUnitOfWork();
        var clock = new FixedClock(T0);

        var created = await new CreateBusinessCustomer(orgs, customers, uow, clock)
            .ExecuteAsync(orgA, new CreateBusinessCustomerRequest("Pat"));
        Assert.True(created.IsSuccess);

        var result = await new UpdateBusinessCustomerDeliveryPreferences(customers, credit, uow, clock)
            .ExecuteAsync(
                BusinessCustomerId.From(created.Value!.Id),
                orgB,
                new UpdateBusinessCustomerDeliveryPreferencesRequest(true));
        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.BusinessCustomerNotFound, result.ErrorCode);
    }

    [Fact]
    public void Cashier_role_cannot_manage_customer_preferences_permission()
    {
        // Endpoint uses EnsureCanManageMembershipsAsync → OrganizationOwner only (ManageCustomers gate).
        Assert.True(OrganizationMembershipGuard.CanManageOrganizationStaff(OrganizationRole.OrganizationOwner));
        Assert.False(OrganizationMembershipGuard.CanManageOrganizationStaff(OrganizationRole.OrganizationMember));
        Assert.False(OrganizationMembershipGuard.CanManageOrganizationStaff(OrganizationRole.OrganizationAdministrator));
    }

    private sealed class FixedClock(DateTimeOffset utc) : IClock
    {
        public DateTimeOffset UtcNow => utc;
    }

    private sealed class RecordingUnitOfWork : IPlatformUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubCreditCustomerRepository : ICreditCustomerRepository
    {
        public Task<CreditCustomer?> GetByIdAsync(
            CreditCustomerId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CreditCustomer?>(null);

        public Task<CreditCustomer?> FindActiveByBusinessCustomerAsync(
            BusinessCustomerId businessCustomerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CreditCustomer?>(null);

        public Task<(IReadOnlyList<CreditCustomer> Items, int TotalCount)> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CreditCustomer>, int)>(([], 0));

        public Task AddAsync(CreditCustomer creditCustomer, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(CreditCustomer creditCustomer, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
