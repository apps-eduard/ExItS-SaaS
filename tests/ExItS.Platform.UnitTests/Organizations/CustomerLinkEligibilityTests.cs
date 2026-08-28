using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;
using ExItS.Platform.UnitTests.TestSupport;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class CustomerLinkEligibilityTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Owner_personal_identity_cannot_be_customer_linked()
    {
        var h = await Harness.CreateAsync();
        await h.Memberships.AddAsync(
            OrganizationMembership.Create(h.Org.Id, h.Owner.Id, OrganizationRole.OrganizationOwner, T0));

        var result = await h.Evaluate.ExecuteAsync(h.Org.Id, h.Owner.PublicUserId!, actorUserId: h.Owner.Id);
        Assert.True(result.IsSuccess);
        Assert.Equal(CustomerLinkEligibilityStatuses.OwnerOfOrganization, result.Value!.Status);

        var create = await h.CreateRequest.ExecuteAsync(
            h.Org.Id,
            h.Customer.Id,
            email: null,
            h.Owner.Id,
            h.Owner.Id,
            h.Owner.PublicUserId);
        Assert.False(create.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerLinkOwnerSelf, create.ErrorCode);
    }

    [Fact]
    public async Task Same_org_staff_linked_personal_cannot_be_customer_linked()
    {
        var h = await Harness.CreateAsync();
        var staff = PlatformUser.CreateOrganizationStaff(
            username: "maria.staff",
            staffLogin: "maria@ORG123456",
            contactEmail: "maria@example.com",
            homeOrganizationId: h.Org.Id,
            displayName: "Maria Staff",
            utcNow: T0,
            linkedPersonalUserId: h.Personal.Id);
        await h.Users.AddAsync(staff);
        await h.Memberships.AddAsync(
            OrganizationMembership.Create(h.Org.Id, staff.Id, OrganizationRole.OrganizationMember, T0));

        var result = await h.Evaluate.ExecuteAsync(h.Org.Id, h.Personal.PublicUserId!, actorUserId: h.Owner.Id);
        Assert.True(result.IsSuccess);
        Assert.Equal(CustomerLinkEligibilityStatuses.OrganizationStaff, result.Value!.Status);

        var create = await h.CreateRequest.ExecuteAsync(
            h.Org.Id,
            h.Customer.Id,
            email: null,
            h.Owner.Id,
            h.Personal.Id,
            h.Personal.PublicUserId);
        Assert.False(create.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerLinkOrganizationStaff, create.ErrorCode);
    }

    [Fact]
    public async Task Pending_same_personal_blocks_second_customer_request()
    {
        var h = await Harness.CreateAsync();
        var first = await h.CreateRequest.ExecuteAsync(
            h.Org.Id,
            h.Customer.Id,
            email: null,
            h.Owner.Id,
            h.Personal.Id,
            h.Personal.PublicUserId);
        Assert.True(first.IsSuccess, first.ErrorMessage);

        var otherCustomer = BusinessCustomer.Create(h.Org.Id, "Other Customer", T0);
        await h.Customers.AddAsync(otherCustomer);

        var second = await h.CreateRequest.ExecuteAsync(
            h.Org.Id,
            otherCustomer.Id,
            email: null,
            h.Owner.Id,
            h.Personal.Id,
            h.Personal.PublicUserId);
        Assert.False(second.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerLinkPendingExists, second.ErrorCode);

        var eligibility = await h.Evaluate.ExecuteAsync(h.Org.Id, h.Personal.PublicUserId!);
        Assert.Equal(CustomerLinkEligibilityStatuses.PendingInvitation, eligibility.Value!.Status);
    }

    [Fact]
    public async Task Same_customer_pending_retry_is_idempotent()
    {
        var h = await Harness.CreateAsync();
        var first = await h.CreateRequest.ExecuteAsync(
            h.Org.Id,
            h.Customer.Id,
            email: null,
            h.Owner.Id,
            h.Personal.Id,
            h.Personal.PublicUserId);
        Assert.True(first.IsSuccess, first.ErrorMessage);

        var retry = await h.CreateRequest.ExecuteAsync(
            h.Org.Id,
            h.Customer.Id,
            email: null,
            h.Owner.Id,
            h.Personal.Id,
            h.Personal.PublicUserId);
        Assert.True(retry.IsSuccess, retry.ErrorMessage);
        Assert.Equal(first.Value!.Id, retry.Value!.Id);
        Assert.Null(retry.Value.AcceptToken);
    }

    private sealed class Harness
    {
        public required PlatformOrganization Org { get; init; }
        public required PlatformUser Owner { get; init; }
        public required PlatformUser Personal { get; init; }
        public required BusinessCustomer Customer { get; init; }
        public required InMemoryPlatformUserRepository Users { get; init; }
        public required InMemoryOrganizationMembershipRepository Memberships { get; init; }
        public required CustomerLinkCompletenessTests.InMemoryBusinessCustomerRepository Customers { get; init; }
        public required EvaluateCustomerLinkEligibility Evaluate { get; init; }
        public required CreateCustomerLinkRequest CreateRequest { get; init; }

        public static async Task<Harness> CreateAsync()
        {
            var clock = new FixedClock(T0);
            var uow = new NoOpUnitOfWork();
            var users = new InMemoryPlatformUserRepository();
            var memberships = new InMemoryOrganizationMembershipRepository();
            var orgs = new InMemoryPlatformOrganizationRepository();
            var customers = new CustomerLinkCompletenessTests.InMemoryBusinessCustomerRepository();
            var requests = new CustomerLinkCompletenessTests.InMemoryCustomerLinkRequestRepository(clock);
            var links = new CustomerLinkCompletenessTests.InMemoryLinkedCustomerAppUserRepository();

            var org = PlatformOrganization.Create("Kizy Store", "kizy-elig", T0);
            await orgs.AddAsync(org);

            var owner = PlatformUser.Create("owner.elig", "Owner", "owner.elig@example.com", T0);
            owner.AssignPublicUserId("EX-9000-0001", T0);
            await users.AddAsync(owner);

            var personal = PlatformUser.Create("maria.elig", "Maria", "maria.elig@example.com", T0);
            personal.AssignPublicUserId("EX-9000-0002", T0);
            await users.AddAsync(personal);

            var customer = BusinessCustomer.Create(org.Id, "Maria Customer", T0);
            await customers.AddAsync(customer);

            var evaluate = new EvaluateCustomerLinkEligibility(users, memberships, requests, links, clock);
            var create = new CreateCustomerLinkRequest(
                customers,
                requests,
                uow,
                clock,
                users,
                orgs,
                eligibility: evaluate,
                links: links);

            return new Harness
            {
                Org = org,
                Owner = owner,
                Personal = personal,
                Customer = customer,
                Users = users,
                Memberships = memberships,
                Customers = customers,
                Evaluate = evaluate,
                CreateRequest = create
            };
        }
    }
}
