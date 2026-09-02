using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.UnitTests.Support;
using ExItS.Platform.UnitTests.TestSupport;

namespace ExItS.Platform.UnitTests.Personal;

public sealed class PersonalWorkplacesUseCasesTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task List_returns_linked_staff_workplace_with_alias_role_and_branch()
    {
        var personal = PlatformUser.Create("kizy", "Kizy", "kizy@gmail.com", T0);
        var orgs = new InMemoryPlatformOrganizationRepository();
        var org = (await new CreatePlatformOrganization(
                orgs,
                new FakePublicOrganizationIdGenerator(),
                new NoOpUnitOfWork(),
                new FixedClock(T0))
            .ExecuteAsync("Mica Store", "mica")).Value!;

        var staffLogin = StaffLoginNameRules.Build("kizy", org.PublicOrganizationId!);
        var staff = PlatformUser.CreateOrganizationStaff(
            StaffLoginNameRules.DeriveUsername(staffLogin),
            staffLogin,
            "kizy@gmail.com",
            org.Id,
            "Kizy Staff",
            T0,
            linkedPersonalUserId: personal.Id);

        var users = new InMemoryPlatformUserRepository();
        await users.AddAsync(personal);
        await users.AddAsync(staff);

        var membership = OrganizationMembership.Create(
            org.Id,
            staff.Id,
            OrganizationRole.OrganizationMember,
            T0);
        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(membership);

        var grant = ProductLocalRoleGrant.Create(
            org.Id,
            staff.Id,
            ProductCode.PinoyBusinessPos,
            ProductLocalRoleCodes.Cashier,
            personal.Id,
            T0);
        var grants = new InMemoryProductLocalRoleGrantRepository();
        await grants.AddAsync(grant);

        var branch = OrganizationBranch.CreateMainBranch(org.Id, T0);
        var branches = new InMemoryOrganizationBranchRepository();
        await branches.AddAsync(branch);

        var assignments = new InMemoryOrganizationMembershipBranchAssignmentRepository();
        await assignments.ReplaceForMembershipAsync(
            org.Id,
            membership.Id,
            [branch.Id],
            T0,
            actorReference: null);

        var otherPersonal = PlatformUser.Create("other", "Other", "other@gmail.com", T0);
        await users.AddAsync(otherPersonal);

        var useCase = new ListPersonalWorkplaces(
            users,
            memberships,
            orgs,
            grants,
            assignments,
            branches);

        var mine = await useCase.ExecuteAsync(personal.Id);
        Assert.True(mine.IsSuccess, mine.ErrorMessage);
        var item = Assert.Single(mine.Value!);
        Assert.Equal(org.Id.Value, item.OrganizationId);
        Assert.Equal("Mica Store", item.OrganizationDisplayName);
        Assert.Equal(StaffLoginNameRules.FormatForDisplay(staffLogin), item.StaffLogin);
        Assert.Equal(nameof(MembershipStatus.Active), item.MembershipStatus);
        Assert.Equal(ProductLocalRoleCodes.Cashier, item.ProductRole);
        Assert.Equal(ProductRoleDisplay.Cashier, item.ProductRoleDisplay);
        Assert.Contains(item.Branches, b => b.BranchId == branch.Id.Value);

        var leaked = await useCase.ExecuteAsync(otherPersonal.Id);
        Assert.True(leaked.IsSuccess);
        Assert.Empty(leaked.Value!);
    }

    [Fact]
    public async Task List_excludes_removed_membership_and_does_not_mark_suspended_as_active()
    {
        var personal = PlatformUser.Create("kizy", "Kizy", "kizy@gmail.com", T0);
        var orgs = new InMemoryPlatformOrganizationRepository();
        var org = (await new CreatePlatformOrganization(
                orgs,
                new FakePublicOrganizationIdGenerator(),
                new NoOpUnitOfWork(),
                new FixedClock(T0))
            .ExecuteAsync("Mica Store", "mica")).Value!;

        var staffLogin = StaffLoginNameRules.Build("kizy", org.PublicOrganizationId!);
        var staff = PlatformUser.CreateOrganizationStaff(
            StaffLoginNameRules.DeriveUsername(staffLogin),
            staffLogin,
            "kizy@gmail.com",
            org.Id,
            "Kizy Staff",
            T0,
            linkedPersonalUserId: personal.Id);

        var users = new InMemoryPlatformUserRepository();
        await users.AddAsync(personal);
        await users.AddAsync(staff);

        var membership = OrganizationMembership.Create(
            org.Id,
            staff.Id,
            OrganizationRole.OrganizationMember,
            T0);
        membership.Suspend(T0, "policy");
        var memberships = new InMemoryOrganizationMembershipRepository();
        await memberships.AddAsync(membership);

        var useCase = new ListPersonalWorkplaces(
            users,
            memberships,
            orgs,
            new InMemoryProductLocalRoleGrantRepository(),
            new InMemoryOrganizationMembershipBranchAssignmentRepository(),
            new InMemoryOrganizationBranchRepository());

        var result = await useCase.ExecuteAsync(personal.Id);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        var item = Assert.Single(result.Value!);
        Assert.Equal(nameof(MembershipStatus.Suspended), item.MembershipStatus);
        Assert.NotEqual(nameof(MembershipStatus.Active), item.MembershipStatus);

        membership.Reactivate(T0);
        membership.Remove(T0, "revoked");
        await memberships.UpdateAsync(membership);

        var afterRemove = await useCase.ExecuteAsync(personal.Id);
        Assert.True(afterRemove.IsSuccess);
        Assert.Empty(afterRemove.Value!);
    }

    private sealed class InMemoryProductLocalRoleGrantRepository : IProductLocalRoleGrantRepository
    {
        private readonly List<ProductLocalRoleGrant> _items = [];

        public Task<ProductLocalRoleGrant?> GetByIdAsync(
            ProductLocalRoleGrantId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id.Equals(id)));

        public Task<ProductLocalRoleGrant?> FindAsync(
            PlatformOrganizationId organizationId,
            PlatformUserId userIdentityId,
            string productCode,
            string roleCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.OrganizationId.Equals(organizationId)
                && x.UserIdentityId.Equals(userIdentityId)
                && string.Equals(x.ProductCode, productCode, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.RoleCode, roleCode, StringComparison.Ordinal)
                && x.Status == ProductLocalRoleGrantStatus.Active));

        public Task<ProductLocalRoleGrant?> FindActiveByUserOrganizationProductAsync(
            PlatformOrganizationId organizationId,
            PlatformUserId userIdentityId,
            string productCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x =>
                x.OrganizationId.Equals(organizationId)
                && x.UserIdentityId.Equals(userIdentityId)
                && string.Equals(x.ProductCode, productCode, StringComparison.OrdinalIgnoreCase)
                && x.Status == ProductLocalRoleGrantStatus.Active));

        public Task<IReadOnlyList<ProductLocalRoleGrant>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            ProductLocalRoleGrantStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<ProductLocalRoleGrant> query = _items.Where(x => x.OrganizationId.Equals(organizationId));
            if (status is not null)
            {
                query = query.Where(x => x.Status == status);
            }

            return Task.FromResult<IReadOnlyList<ProductLocalRoleGrant>>(query.ToList());
        }

        public Task<IReadOnlyList<ProductLocalRoleGrant>> ListActiveByUserOrganizationAsync(
            PlatformOrganizationId organizationId,
            PlatformUserId userIdentityId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProductLocalRoleGrant>>(
                _items.Where(x =>
                        x.OrganizationId.Equals(organizationId)
                        && x.UserIdentityId.Equals(userIdentityId)
                        && x.Status == ProductLocalRoleGrantStatus.Active)
                    .ToList());

        public Task AddAsync(ProductLocalRoleGrant grant, CancellationToken cancellationToken = default)
        {
            _items.Add(grant);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ProductLocalRoleGrant grant, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryOrganizationBranchRepository : IOrganizationBranchRepository
    {
        private readonly List<OrganizationBranch> _items = [];

        public Task AddAsync(OrganizationBranch branch, CancellationToken cancellationToken = default)
        {
            _items.Add(branch);
            return Task.CompletedTask;
        }

        public Task<OrganizationBranch?> GetByIdAsync(
            OrganizationBranchId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id.Equals(id)));

        public Task<IReadOnlyList<OrganizationBranch>> ListByOrganizationAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationBranch>>(
                _items.Where(x => x.OrganizationId.Equals(organizationId)).ToList());

        public Task<int> CountActiveAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _items.Count(x =>
                    x.OrganizationId.Equals(organizationId)
                    && x.Status == OrganizationBranchStatus.Active));

        public Task<OrganizationBranch?> GetPrimaryAsync(
            PlatformOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _items.FirstOrDefault(x =>
                    x.OrganizationId.Equals(organizationId) && x.IsPrimary));

        public Task UpdateAsync(OrganizationBranch branch, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
