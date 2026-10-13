using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.UnitTests.Support;
using ExItS.Platform.UnitTests.TestSupport;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class CustomerLinkEligibilityTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Existing_personal_user_can_be_customer_linked()
    {
        var h = await Harness.CreateAsync();

        var result = await h.Evaluate.ExecuteAsync(h.Org.Id, h.Personal.PublicUserId!, actorUserId: h.Owner.Id);

        Assert.Equal(CustomerLinkEligibilityStatuses.Eligible, result.Value!.Status);
        Assert.Null(result.Value.RelationshipContext);
        var create = await LinkAsync(h, h.Personal);
        Assert.True(create.IsSuccess, create.ErrorMessage);
        Assert.Null(await h.Memberships.FindActiveByUserAndOrganizationAsync(h.Personal.Id, h.Org.Id));
    }

    [Fact]
    public async Task Owner_can_also_be_a_customer_without_losing_ownership()
    {
        var h = await Harness.CreateAsync();
        var membership = OrganizationMembership.Create(h.Org.Id, h.Owner.Id, OrganizationRole.OrganizationOwner, T0);
        await h.Memberships.AddAsync(membership);

        var result = await h.Evaluate.ExecuteAsync(h.Org.Id, h.Owner.PublicUserId!, actorUserId: h.Owner.Id);

        Assert.Equal(CustomerLinkEligibilityStatuses.Eligible, result.Value!.Status);
        Assert.Equal(OrganizationRoleDisplay.Owner, result.Value.RelationshipContext);
        var create = await LinkAsync(h, h.Owner);
        Assert.True(create.IsSuccess, create.ErrorMessage);
        var stillOwner = await h.Memberships.FindActiveByUserAndOrganizationAsync(h.Owner.Id, h.Org.Id);
        Assert.Equal(OrganizationRole.OrganizationOwner, stillOwner!.Role);
        Assert.Equal(membership.Id, stillOwner.Id);
    }

    [Theory]
    [InlineData(OrganizationRole.OrganizationAdministrator, "Administrator", null)]
    [InlineData(OrganizationRole.OrganizationMember, "Staff", null)]
    [InlineData(OrganizationRole.OrganizationMember, "Cashier", ProductLocalRoleCodes.Cashier)]
    [InlineData(OrganizationRole.OrganizationMember, "Manager", ProductLocalRoleCodes.Manager)]
    public async Task Staff_member_can_also_be_a_customer(
        OrganizationRole role,
        string expectedContext,
        string? productRole)
    {
        var h = await Harness.CreateAsync();
        var staff = await AddLinkedStaffAsync(h, role, productRole);

        var result = await h.Evaluate.ExecuteAsync(h.Org.Id, h.Personal.PublicUserId!, actorUserId: h.Owner.Id);

        Assert.Equal(CustomerLinkEligibilityStatuses.Eligible, result.Value!.Status);
        Assert.Equal(expectedContext, result.Value.RelationshipContext);
        var create = await LinkAsync(h, h.Personal);
        Assert.True(create.IsSuccess, create.ErrorMessage);
        Assert.Equal(h.Personal.Id.Value, create.Value!.TargetUserIdentityId);
        var staffMembership = await h.Memberships.FindActiveByUserAndOrganizationAsync(staff.Id, h.Org.Id);
        Assert.Equal(role, staffMembership!.Role);
        Assert.Null(await h.Memberships.FindActiveByUserAndOrganizationAsync(h.Personal.Id, h.Org.Id));
        Assert.False(h.Customer.IsOrganizationStaff);
    }

    [Fact]
    public void Collector_is_not_an_organization_or_product_role()
    {
        Assert.DoesNotContain(
            "Collector",
            ProductLocalRoleCodes.All,
            StringComparer.OrdinalIgnoreCase);
        Assert.False(Enum.TryParse<OrganizationRole>("Collector", ignoreCase: true, out _));
    }

    [Fact]
    public async Task Staff_exits_id_links_the_personal_user_instead_of_creating_another_user()
    {
        var h = await Harness.CreateAsync();
        var staff = await AddLinkedStaffAsync(h, OrganizationRole.OrganizationMember, ProductLocalRoleCodes.Cashier);
        staff.AssignPublicUserId("EX-9000-0003", T0);

        var result = await h.Evaluate.ExecuteAsync(h.Org.Id, staff.PublicUserId!, actorUserId: h.Owner.Id);

        Assert.Equal(CustomerLinkEligibilityStatuses.Eligible, result.Value!.Status);
        Assert.Equal(h.Personal.Id.Value, result.Value.UserIdentityId);
        Assert.Equal(h.Personal.PublicUserId, result.Value.PublicUserId);
        Assert.Equal(ProductRoleDisplay.Cashier, result.Value.RelationshipContext);

        var create = await h.CreateRequest.ExecuteAsync(
            h.Org.Id,
            h.Customer.Id,
            email: null,
            h.Owner.Id,
            staff.Id,
            staff.PublicUserId);
        Assert.True(create.IsSuccess, create.ErrorMessage);
        Assert.Equal(h.Personal.Id.Value, create.Value!.TargetUserIdentityId);
        var listed = await h.Users.ListAsync(null, null, null, null, false, 0, 20);
        Assert.Equal(3, listed.TotalCount);
    }

    [Fact]
    public async Task Same_user_can_be_staff_and_customer_of_one_org_and_customer_of_another()
    {
        var h = await Harness.CreateAsync();
        await AddLinkedStaffAsync(h, OrganizationRole.OrganizationMember, ProductLocalRoleCodes.Cashier);
        var orgB = PlatformOrganization.Create("Other Store", "other-elig", T0);
        await h.Orgs.AddAsync(orgB);
        var customerB = BusinessCustomer.Create(orgB.Id, "Maria at Other", T0);
        await h.Customers.AddAsync(customerB);

        var here = await LinkAsync(h, h.Personal);
        Assert.True(here.IsSuccess, here.ErrorMessage);
        var there = await h.CreateRequest.ExecuteAsync(
            orgB.Id,
            customerB.Id,
            email: null,
            h.Owner.Id,
            h.Personal.Id,
            h.Personal.PublicUserId);
        Assert.True(there.IsSuccess, there.ErrorMessage);

        var atHome = await h.Evaluate.ExecuteAsync(h.Org.Id, h.Personal.PublicUserId!);
        Assert.Equal(CustomerLinkEligibilityStatuses.PendingInvitation, atHome.Value!.Status);
        var away = await h.Evaluate.ExecuteAsync(orgB.Id, h.Personal.PublicUserId!);
        Assert.Equal(CustomerLinkEligibilityStatuses.PendingInvitation, away.Value!.Status);
        Assert.Null(away.Value.RelationshipContext);
    }

    [Fact]
    public async Task Same_linked_user_cannot_be_added_as_a_customer_twice()
    {
        var h = await Harness.CreateAsync();
        h.Customer.LinkAppUser(h.Personal.Id, T0);
        await h.Links.AddAsync(LinkedCustomerAppUser.CreateFromAcceptedLink(
            h.Org.Id,
            h.Customer.Id,
            h.Personal.Id,
            CustomerLinkRequestId.New(),
            T0));
        var other = BusinessCustomer.Create(h.Org.Id, "Second record", T0);
        await h.Customers.AddAsync(other);

        var eligibility = await h.Evaluate.ExecuteAsync(h.Org.Id, h.Personal.PublicUserId!);
        Assert.Equal(CustomerLinkEligibilityStatuses.AlreadyLinked, eligibility.Value!.Status);

        var duplicate = await h.CreateRequest.ExecuteAsync(
            h.Org.Id,
            other.Id,
            email: null,
            h.Owner.Id,
            h.Personal.Id,
            h.Personal.PublicUserId);
        Assert.False(duplicate.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CustomerLinkRequestConflict, duplicate.ErrorCode);
    }

    [Fact]
    public async Task Unknown_exits_id_is_rejected()
    {
        var h = await Harness.CreateAsync();

        var result = await h.Evaluate.ExecuteAsync(h.Org.Id, "EX-0000-0099");

        Assert.Equal(CustomerLinkEligibilityStatuses.InvalidTarget, result.Value!.Status);
        Assert.Contains("couldn't find", result.Value.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Platform_staff_and_unlinked_staff_login_are_not_customer_targets()
    {
        var h = await Harness.CreateAsync();
        var platform = PlatformUser.CreatePlatformStaff(
            "olivia.staff",
            "Olivia",
            "Staff",
            "Olivia Staff",
            "olivia.staff@example.com",
            "STF-000099",
            T0);
        platform.AssignPublicUserId("EX-9000-0088", T0);
        await h.Users.AddAsync(platform);
        var staffLogin = PlatformUser.CreateOrganizationStaff(
            "solo.staff",
            "solo@ORG123456",
            "solo@example.com",
            h.Org.Id,
            "Solo Staff",
            T0);
        staffLogin.AssignPublicUserId("EX-9000-0077", T0);
        await h.Users.AddAsync(staffLogin);

        var platformResult = await h.Evaluate.ExecuteAsync(h.Org.Id, platform.PublicUserId!);
        var staffResult = await h.Evaluate.ExecuteAsync(h.Org.Id, staffLogin.PublicUserId!);

        Assert.Equal(CustomerLinkEligibilityStatuses.InvalidTarget, platformResult.Value!.Status);
        Assert.Equal(CustomerLinkEligibilityStatuses.InvalidTarget, staffResult.Value!.Status);
        Assert.DoesNotContain("can't also be linked", platformResult.Value.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("can't also be linked", staffResult.Value.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Customer_link_does_not_grant_staff_authority()
    {
        var h = await Harness.CreateAsync();
        var created = await LinkAsync(h, h.Personal);
        Assert.True(created.IsSuccess, created.ErrorMessage);

        Assert.Null(await h.Memberships.FindActiveByUserAndOrganizationAsync(h.Personal.Id, h.Org.Id));
        Assert.False(OrganizationMembershipGuard.CanManageOrganizationStaff(OrganizationRole.OrganizationMember));
        Assert.False(OrganizationMembershipGuard.CanManageOrganizationStaff(OrganizationRole.OrganizationAdministrator));
        Assert.True(OrganizationMembershipGuard.CanManageOrganizationStaff(OrganizationRole.OrganizationOwner));
        Assert.False(h.Customer.IsOrganizationStaff);
    }

    [Fact]
    public async Task Existing_customer_can_later_be_invited_and_given_staff_membership()
    {
        var h = await Harness.CreateAsync();
        h.Customer.LinkAppUser(h.Personal.Id, T0);
        var link = LinkedCustomerAppUser.CreateFromAcceptedLink(
            h.Org.Id,
            h.Customer.Id,
            h.Personal.Id,
            CustomerLinkRequestId.New(),
            T0);
        await h.Links.AddAsync(link);

        var invite = new CreateOrganizationInvitationForPersonal(
            h.Orgs,
            new InMemoryOrganizationInvitationRepository(),
            h.Memberships,
            h.Users,
            new FakePublicOrganizationIdGenerator(),
            new ResolveStaffInviteTarget(h.Users),
            h.Uow,
            h.Clock);
        var invited = await invite.ExecuteAsync(h.Org.Id, h.Personal.PublicUserId!, h.Owner.Id, productRole: "Cashier");

        Assert.True(invited.IsSuccess, invited.ErrorMessage);
        Assert.Equal(h.Personal.Id.Value, invited.Value!.TargetPersonalUserId);
        Assert.NotNull(await h.Links.FindActiveByUserAndOrganizationAsync(h.Personal.Id, h.Org.Id));

        var staff = await AddLinkedStaffAsync(h, OrganizationRole.OrganizationMember, ProductLocalRoleCodes.Cashier);
        Assert.NotNull(await h.Memberships.FindActiveByUserAndOrganizationAsync(staff.Id, h.Org.Id));
        Assert.NotNull(await h.Links.FindActiveByUserAndOrganizationAsync(h.Personal.Id, h.Org.Id));
        Assert.False(OrganizationMembershipGuard.CanManageOrganizationStaff(OrganizationRole.OrganizationMember));
    }

    [Fact]
    public async Task Removing_customer_link_keeps_staff_membership()
    {
        var h = await Harness.CreateAsync();
        var staff = await AddLinkedStaffAsync(h, OrganizationRole.OrganizationMember, null);
        h.Customer.LinkAppUser(h.Personal.Id, T0);
        var link = LinkedCustomerAppUser.CreateFromAcceptedLink(
            h.Org.Id,
            h.Customer.Id,
            h.Personal.Id,
            CustomerLinkRequestId.New(),
            T0);
        await h.Links.AddAsync(link);

        var unlink = new UnlinkAcceptedCustomerLink(h.Links, h.Customers, h.Uow, h.Clock);
        var removed = await unlink.ExecuteAsync(link.Id, h.Org.Id);

        Assert.True(removed.IsSuccess, removed.ErrorMessage);
        Assert.Null(await h.Links.FindActiveByUserAndOrganizationAsync(h.Personal.Id, h.Org.Id));
        Assert.Null(h.Customer.LinkedUserIdentityId);
        var membership = await h.Memberships.FindActiveByUserAndOrganizationAsync(staff.Id, h.Org.Id);
        Assert.Equal(OrganizationRole.OrganizationMember, membership!.Role);
    }

    [Fact]
    public async Task Removing_staff_membership_keeps_customer_link()
    {
        var h = await Harness.CreateAsync();
        var staff = await AddLinkedStaffAsync(h, OrganizationRole.OrganizationMember, null);
        var membership = (await h.Memberships.FindActiveByUserAndOrganizationAsync(staff.Id, h.Org.Id))!;
        h.Customer.LinkAppUser(h.Personal.Id, T0);
        await h.Links.AddAsync(LinkedCustomerAppUser.CreateFromAcceptedLink(
            h.Org.Id,
            h.Customer.Id,
            h.Personal.Id,
            CustomerLinkRequestId.New(),
            T0));

        membership.Remove(T0, "Left the business.", "owner");
        await h.Memberships.UpdateAsync(membership);

        Assert.Null(await h.Memberships.FindActiveByUserAndOrganizationAsync(staff.Id, h.Org.Id));
        var link = await h.Links.FindActiveByUserAndOrganizationAsync(h.Personal.Id, h.Org.Id);
        Assert.NotNull(link);
        Assert.Equal(h.Personal.Id, link!.UserIdentityId);
        Assert.Equal(h.Customer.LinkedUserIdentityId, h.Personal.Id);
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

    private static Task<ApplicationResult<CustomerLinkRequestDto>> LinkAsync(Harness h, PlatformUser target) =>
        h.CreateRequest.ExecuteAsync(
            h.Org.Id,
            h.Customer.Id,
            email: null,
            h.Owner.Id,
            target.Id,
            target.PublicUserId);

    private static async Task<PlatformUser> AddLinkedStaffAsync(
        Harness h,
        OrganizationRole role,
        string? productRole)
    {
        var staff = PlatformUser.CreateOrganizationStaff(
            username: "maria.staff",
            staffLogin: "maria@ORG123456",
            contactEmail: "maria.work@example.com",
            homeOrganizationId: h.Org.Id,
            displayName: "Maria Staff",
            utcNow: T0,
            linkedPersonalUserId: h.Personal.Id);
        await h.Users.AddAsync(staff);
        await h.Memberships.AddAsync(
            OrganizationMembership.Create(h.Org.Id, staff.Id, role, T0));
        if (productRole is not null)
        {
            await h.Grants.AddAsync(ProductLocalRoleGrant.Create(
                h.Org.Id,
                staff.Id,
                ProductCode.PinoyBusinessPos,
                productRole,
                h.Owner.Id,
                T0));
        }

        return staff;
    }

    private sealed class Harness
    {
        public required PlatformOrganization Org { get; init; }
        public required PlatformUser Owner { get; init; }
        public required PlatformUser Personal { get; init; }
        public required BusinessCustomer Customer { get; init; }
        public required InMemoryPlatformUserRepository Users { get; init; }
        public required InMemoryOrganizationMembershipRepository Memberships { get; init; }
        public required InMemoryPlatformOrganizationRepository Orgs { get; init; }
        public required CustomerLinkCompletenessTests.InMemoryBusinessCustomerRepository Customers { get; init; }
        public required CustomerLinkCompletenessTests.InMemoryLinkedCustomerAppUserRepository Links { get; init; }
        public required RecordingProductRoleGrantRepository Grants { get; init; }
        public required NoOpUnitOfWork Uow { get; init; }
        public required FixedClock Clock { get; init; }
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
            var grants = new RecordingProductRoleGrantRepository();

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

            var evaluate = new EvaluateCustomerLinkEligibility(
                users,
                memberships,
                requests,
                links,
                clock,
                productRoles: grants);
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
                Orgs = orgs,
                Customers = customers,
                Links = links,
                Grants = grants,
                Uow = uow,
                Clock = clock,
                Evaluate = evaluate,
                CreateRequest = create
            };
        }
    }

    private sealed class RecordingProductRoleGrantRepository : IProductLocalRoleGrantRepository
    {
        private readonly List<ProductLocalRoleGrant> _items = [];

        public Task<ProductLocalRoleGrant?> GetByIdAsync(
            ProductLocalRoleGrantId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(item => item.Id == id));

        public Task<ProductLocalRoleGrant?> FindAsync(
            PlatformOrganizationId organizationId,
            PlatformUserId userIdentityId,
            string productCode,
            string roleCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(item =>
                item.OrganizationId == organizationId
                && item.UserIdentityId == userIdentityId
                && item.ProductCode == productCode
                && item.RoleCode == roleCode));

        public Task<ProductLocalRoleGrant?> FindActiveByUserOrganizationProductAsync(
            PlatformOrganizationId organizationId,
            PlatformUserId userIdentityId,
            string productCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(item =>
                item.OrganizationId == organizationId
                && item.UserIdentityId == userIdentityId
                && item.ProductCode == productCode
                && item.Status == ProductLocalRoleGrantStatus.Active));

        public Task<IReadOnlyList<ProductLocalRoleGrant>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            ProductLocalRoleGrantStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ProductLocalRoleGrant> items = _items
                .Where(item => item.OrganizationId == organizationId && (status is null || item.Status == status))
                .ToList();
            return Task.FromResult(items);
        }

        public Task<IReadOnlyList<ProductLocalRoleGrant>> ListActiveByUserOrganizationAsync(
            PlatformOrganizationId organizationId,
            PlatformUserId userIdentityId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ProductLocalRoleGrant> items = _items
                .Where(item =>
                    item.OrganizationId == organizationId
                    && item.UserIdentityId == userIdentityId
                    && item.Status == ProductLocalRoleGrantStatus.Active)
                .ToList();
            return Task.FromResult(items);
        }

        public Task AddAsync(ProductLocalRoleGrant grant, CancellationToken cancellationToken = default)
        {
            _items.Add(grant);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ProductLocalRoleGrant grant, CancellationToken cancellationToken = default)
        {
            var index = _items.FindIndex(item => item.Id == grant.Id);
            if (index >= 0)
            {
                _items[index] = grant;
            }

            return Task.CompletedTask;
        }
    }
}
