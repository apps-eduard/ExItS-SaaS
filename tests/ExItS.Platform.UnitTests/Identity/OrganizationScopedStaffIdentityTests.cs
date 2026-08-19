using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.LocalValidation;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;
using ExItS.Platform.UnitTests.TestSupport;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class OrganizationScopedStaffIdentityTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 8, 12, 0, 0, TimeSpan.Zero);
    private const string StaffPassword = "SecurePass1!";

    [Fact]
    public async Task Staff_login_allocator_collides_to_maria_then_maria2()
    {
        var users = new InMemoryPlatformUserRepository();
        var orgId = PlatformOrganizationId.New();
        var publicOrgId = "ORG001842";
        var allocator = new FakeStaffLoginNameAllocator(users);

        var first = await allocator.AllocateAsync("maria@example.com", publicOrgId);
        Assert.Equal("maria@ORG001842", first);

        await users.AddAsync(PlatformUser.CreateOrganizationStaff(
            "maria_org001842",
            first,
            "maria@example.com",
            orgId,
            "Maria One",
            T0));

        // Seed maria1 so the next collision lands on maria2 (as in production allocator loop).
        await users.AddAsync(PlatformUser.CreateOrganizationStaff(
            "maria1_org001842",
            "maria1@ORG001842",
            "maria.other@example.com",
            orgId,
            "Maria Mid",
            T0));

        var second = await allocator.AllocateAsync("maria@example.com", publicOrgId);
        Assert.Equal("maria2@ORG001842", second);
    }

    [Fact]
    public void CreateOrganizationStaff_normalizes_login_case_insensitively()
    {
        var orgId = PlatformOrganizationId.New();
        var a = PlatformUser.CreateOrganizationStaff(
            "maria_org001842",
            "Maria@ORG001842",
            "maria@example.com",
            orgId,
            "Maria A",
            T0);
        var b = PlatformUser.CreateOrganizationStaff(
            "maria_b_org001842",
            "maria@org001842",
            "other@example.com",
            orgId,
            "Maria B",
            T0);

        Assert.Equal("maria@org001842", a.NormalizedEmail);
        Assert.Equal(a.NormalizedEmail, b.NormalizedEmail);
        Assert.Equal(orgId, a.HomeOrganizationId);
        Assert.True(a.IsOrganizationScopedStaff);
        Assert.Equal("maria@example.com", a.NormalizedContactEmail);
    }

    [Fact]
    public async Task CreateOrganizationStaff_login_uniqueness_is_case_insensitive_in_repository()
    {
        var users = new InMemoryPlatformUserRepository();
        var orgId = PlatformOrganizationId.New();
        var staff = PlatformUser.CreateOrganizationStaff(
            "maria_org001842",
            "maria@ORG001842",
            "maria@example.com",
            orgId,
            "Maria",
            T0);
        await users.AddAsync(staff);

        var found = await users.GetByNormalizedEmailAsync(PlatformUser.NormalizeEmail("MARIA@org001842"));
        Assert.NotNull(found);
        Assert.Equal(staff.Id, found!.Id);
    }

    [Fact]
    public async Task Create_invitation_does_not_create_user_accept_creates_home_org_staff()
    {
        var harness = await StaffInviteHarness.CreateAsync();
        var create = harness.CreateInvitation;
        var usersBeforeInvite = harness.Users.AddCount;

        var invited = await create.ExecuteAsync(
            harness.OrgA.Id,
            "ana@example.com",
            OrganizationRole.OrganizationMember,
            invitedByUserId: harness.Owner.Id,
            actorMembershipRole: OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false,
            displayName: "Ana Reyes",
            firstName: "Ana",
            lastName: "Reyes");

        Assert.True(invited.IsSuccess, invited.ErrorMessage);
        Assert.Equal(usersBeforeInvite, harness.Users.AddCount);
        Assert.NotNull(invited.Value!.AcceptToken);
        Assert.Null(await harness.Users.GetByNormalizedEmailAsync("ana@example.com"));

        var accept = await harness.AcceptInvitation.ExecuteAsync(
            invited.Value.AcceptToken!,
            StaffPassword);

        Assert.True(accept.IsSuccess, accept.ErrorMessage);
        Assert.Equal(usersBeforeInvite + 1, harness.Users.AddCount);

        var staff = await harness.Users.GetByIdAsync(PlatformUserId.From(accept.Value!.UserId));
        Assert.NotNull(staff);
        Assert.Equal(harness.OrgA.Id, staff!.HomeOrganizationId);
        Assert.Equal("ana@example.com", staff.NormalizedContactEmail);
        Assert.StartsWith("ana@", accept.Value.StaffLogin, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(harness.OrgA.PublicOrganizationId!, accept.Value.StaffLogin, StringComparison.OrdinalIgnoreCase);
        Assert.True(staff.IsOrganizationScopedStaff);

        var acceptedMail = harness.Messages.LastOfKind(
            PlatformAuthOutboundMessageKinds.OrganizationStaffInvitationAccepted);
        Assert.NotNull(acceptedMail);
        Assert.Equal(accept.Value.StaffLogin, acceptedMail!.StaffLogin);
        Assert.Equal("ana@example.com", acceptedMail.ContactEmail);
        Assert.Equal(harness.OrgA.DisplayName, acceptedMail.OrganizationName);
        Assert.True(string.IsNullOrEmpty(acceptedMail.OpaqueToken));
    }

    [Fact]
    public async Task Same_contact_email_can_create_staff_in_two_organizations()
    {
        var harness = await StaffInviteHarness.CreateAsync(createOrgB: true);
        const string contact = "shared.staff@example.com";

        var inviteA = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgA.Id,
            contact,
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);
        Assert.True(inviteA.IsSuccess, inviteA.ErrorMessage);

        var acceptA = await harness.AcceptInvitation.ExecuteAsync(inviteA.Value!.AcceptToken!, StaffPassword);
        Assert.True(acceptA.IsSuccess, acceptA.ErrorMessage);

        var inviteB = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgB!.Id,
            contact,
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);
        Assert.True(inviteB.IsSuccess, inviteB.ErrorMessage);

        var acceptB = await harness.AcceptInvitation.ExecuteAsync(inviteB.Value!.AcceptToken!, StaffPassword);
        Assert.True(acceptB.IsSuccess, acceptB.ErrorMessage);

        Assert.NotEqual(acceptA.Value!.UserId, acceptB.Value!.UserId);
        Assert.NotEqual(acceptA.Value.StaffLogin, acceptB.Value.StaffLogin);

        var listed = await harness.Users.ListByNormalizedContactEmailAsync(PlatformUser.NormalizeEmail(contact));
        Assert.Equal(2, listed.Count);
        Assert.Contains(listed, u => u.HomeOrganizationId == harness.OrgA.Id);
        Assert.Contains(listed, u => u.HomeOrganizationId == harness.OrgB.Id);
    }

    [Fact]
    public async Task SetSessionOrganizationContext_blocks_staff_org_switch()
    {
        var harness = await StaffInviteHarness.CreateAsync(createOrgB: true);
        var invite = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgA.Id,
            "locked.staff@example.com",
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);
        var accept = await harness.AcceptInvitation.ExecuteAsync(invite.Value!.AcceptToken!, StaffPassword);
        Assert.True(accept.IsSuccess, accept.ErrorMessage);

        var staffId = PlatformUserId.From(accept.Value!.UserId);
        var staff = (await harness.Users.GetByIdAsync(staffId))!;
        var credential = (await harness.Credentials.GetByUserIdAsync(staffId))!;
        const string opaque = "staff-session-token";
        var session = PlatformAuthSession.Create(
            staff.Id,
            AccountProfileId.New(),
            AccountClass.Organization,
            harness.Tokens.HashToken(opaque),
            credential.SecurityStamp,
            T0,
            TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(12),
            selectedOrganizationId: harness.OrgA.Id);
        await harness.Sessions.AddAsync(session);

        var setContext = new SetSessionOrganizationContext(
            harness.Sessions,
            harness.Tokens,
            harness.Memberships,
            harness.Organizations,
            harness.Users,
            harness.OrgPreferences,
            new NoOpAuditWriter(),
            harness.UnitOfWork,
            harness.Clock,
            new ListEligibleOrganizationsForSession(
                harness.Sessions,
                harness.Tokens,
                harness.Memberships,
                harness.Organizations));

        var switchAway = await setContext.ExecuteAsync(opaque, harness.OrgB!.Id.Value);
        Assert.False(switchAway.IsSuccess);
        Assert.Equal(DomainErrorCodes.StaffOrganizationSwitchDenied, switchAway.ErrorCode);

        var clear = await setContext.ExecuteAsync(opaque, organizationId: null);
        Assert.False(clear.IsSuccess);
        Assert.Equal(DomainErrorCodes.StaffOrganizationSwitchDenied, clear.ErrorCode);

        var stayHome = await setContext.ExecuteAsync(opaque, harness.OrgA.Id.Value);
        Assert.True(stayHome.IsSuccess, stayHome.ErrorMessage);
        Assert.Equal(harness.OrgA.Id.Value, stayHome.Value!.SelectedOrganizationId);
    }

    [Fact]
    public async Task Suspend_or_deactivate_orgA_staff_does_not_affect_personal_or_orgB_staff()
    {
        var harness = await StaffInviteHarness.CreateAsync(createOrgB: true);
        const string contact = "multi.identity@example.com";

        var personal = (await new CreatePlatformUser(
                harness.Users,
                harness.UnitOfWork,
                harness.Clock,
                new SequentialPublicUserIdGenerator())
            .ExecuteAsync("personal_multi", "Personal Multi", contact)).Value!;

        var inviteA = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgA.Id,
            contact,
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);
        var staffA = (await harness.AcceptInvitation.ExecuteAsync(inviteA.Value!.AcceptToken!, StaffPassword)).Value!;

        var inviteB = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgB!.Id,
            contact,
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);
        var staffB = (await harness.AcceptInvitation.ExecuteAsync(inviteB.Value!.AcceptToken!, StaffPassword)).Value!;

        var orgAUser = (await harness.Users.GetByIdAsync(PlatformUserId.From(staffA.UserId)))!;
        orgAUser.Suspend(T0.AddMinutes(1), "org A only");
        await harness.Users.UpdateAsync(orgAUser);

        Assert.Equal(AccountStatus.Suspended, (await harness.Users.GetByIdAsync(orgAUser.Id))!.Status);
        Assert.Equal(AccountStatus.Active, (await harness.Users.GetByIdAsync(personal.Id))!.Status);
        Assert.Equal(AccountStatus.Active, (await harness.Users.GetByIdAsync(PlatformUserId.From(staffB.UserId)))!.Status);

        orgAUser = (await harness.Users.GetByIdAsync(orgAUser.Id))!;
        orgAUser.Deactivate(T0.AddMinutes(2), "disable org A staff");
        await harness.Users.UpdateAsync(orgAUser);

        Assert.Equal(AccountStatus.Deactivated, (await harness.Users.GetByIdAsync(orgAUser.Id))!.Status);
        Assert.Equal(AccountStatus.Active, (await harness.Users.GetByIdAsync(personal.Id))!.Status);
        Assert.Equal(AccountStatus.Active, (await harness.Users.GetByIdAsync(PlatformUserId.From(staffB.UserId)))!.Status);
    }

    [Fact]
    public async Task Accept_invitation_wrong_token_or_expired_fails()
    {
        var harness = await StaffInviteHarness.CreateAsync();
        var invited = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgA.Id,
            "expire@example.com",
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);
        Assert.True(invited.IsSuccess, invited.ErrorMessage);

        var usersBeforeAccept = harness.Users.AddCount;

        var wrong = await harness.AcceptInvitation.ExecuteAsync("not-a-real-token", StaffPassword);
        Assert.False(wrong.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InvitationNotFound, wrong.ErrorCode);

        harness.Clock.UtcNow = T0.AddDays(30);
        var expired = await harness.AcceptInvitation.ExecuteAsync(invited.Value!.AcceptToken!, StaffPassword);
        Assert.False(expired.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvitationExpired, expired.ErrorCode);
        Assert.Equal(usersBeforeAccept, harness.Users.AddCount);
    }

    [Fact]
    public async Task Personal_email_login_still_works()
    {
        var users = new InMemoryPlatformUserRepository();
        var credentials = new InMemoryPlatformUserCredentialRepository();
        var sessions = new InMemoryPlatformAuthSessionRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var preferences = new InMemoryOrganizationContextPreferenceRepository();
        var profiles = new InMemoryAccountProfileRepository();
        var roles = new InMemoryPlatformRoleAssignmentRepository();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);
        var hasher = new StubPasswordHasher();
        var ensure = new EnsureAccountProfilesForUser(profiles, roles, memberships, uow, clock);

        var user = (await new CreatePlatformUser(users, uow, clock, new SequentialPublicUserIdGenerator())
            .ExecuteAsync("ada", "Ada Lovelace", "ada@example.com")).Value!;
        var credential = PlatformUserCredential.Create(user.Id, hasher.HashPassword(StaffPassword), hasher.Algorithm, T0);
        credential.MarkEmailVerified(T0);
        await credentials.AddAsync(credential);
        await ensure.ExecuteAsync(user.Id, AccountClass.Personal, exclusivePreferredClass: true);

        var login = new LoginPlatformUser(
            users,
            credentials,
            sessions,
            memberships,
            orgs,
            preferences,
            ensure,
            hasher,
            new StubSessionTokenService(),
            new NoOpAuditWriter(),
            uow,
            clock,
            Options.Create(new PlatformLockoutOptions()),
            Options.Create(new PlatformSessionOptions()),
            new NullPlatformMfaReadinessService(),
            Options.Create(new LocalValidationOptions()));

        var result = await login.ExecuteAsync("ada@example.com", StaffPassword, ipAddress: null, userAgent: null);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(user.Id.Value, result.Value!.UserId);
        Assert.False(result.Value.OrganizationContextLocked);
        Assert.Null(result.Value.HomeOrganizationId);
    }

    private sealed class StaffInviteHarness
    {
        public required InMemoryPlatformUserRepository Users { get; init; }
        public required InMemoryPlatformUserCredentialRepository Credentials { get; init; }
        public required InMemoryPlatformOrganizationRepository Organizations { get; init; }
        public required InMemoryOrganizationMembershipRepository Memberships { get; init; }
        public required InMemoryOrganizationInvitationRepository Invitations { get; init; }
        public required InMemoryPlatformAuthSessionRepository Sessions { get; init; }
        public required InMemoryOrganizationContextPreferenceRepository OrgPreferences { get; init; }
        public required StubSessionTokenService Tokens { get; init; }
        public required NoOpUnitOfWork UnitOfWork { get; init; }
        public required FixedClock Clock { get; init; }
        public required PlatformUser Owner { get; init; }
        public required PlatformOrganization OrgA { get; init; }
        public PlatformOrganization? OrgB { get; init; }
        public required CreateOrganizationInvitation CreateInvitation { get; init; }
        public required AcceptOrganizationInvitation AcceptInvitation { get; init; }
        public required CapturingAuthOutboundMessageSink Messages { get; init; }

        public static async Task<StaffInviteHarness> CreateAsync(bool createOrgB = false)
        {
            var clock = new FixedClock(T0);
            var uow = new NoOpUnitOfWork();
            var users = new InMemoryPlatformUserRepository();
            var credentials = new InMemoryPlatformUserCredentialRepository();
            var orgs = new InMemoryPlatformOrganizationRepository();
            var memberships = new InMemoryOrganizationMembershipRepository();
            var invitations = new InMemoryOrganizationInvitationRepository();
            var sessions = new InMemoryPlatformAuthSessionRepository();
            var preferences = new InMemoryOrganizationContextPreferenceRepository();
            var profiles = new InMemoryAccountProfileRepository();
            var roles = new InMemoryPlatformRoleAssignmentRepository();
            var publicOrgIds = new FakePublicOrganizationIdGenerator();
            var tokens = new StubSessionTokenService();
            var ensure = new EnsureAccountProfilesForUser(profiles, roles, memberships, uow, clock);
            var addMembership = new AddOrganizationMembership(users, orgs, memberships, new InMemoryOrganizationMembershipBranchAssignmentRepository(), ensure, uow, clock);
            var products = new InMemoryProductRepository();
            var subscriptions = new InMemorySubscriptionRepository();
            var snapshots = new InMemoryEntitlementSnapshotRepository();
            var assignments = new InMemoryProductAccessAssignmentRepository();
            var roleGrants = new InMemoryProductLocalRoleGrantRepository();
            var grantAccess = new GrantProductAccess(
                users, orgs, memberships, products, subscriptions, snapshots, assignments, uow, clock);
            var assignRole = new AssignProductLocalRole(
                users, orgs, memberships, products, roleGrants, grantAccess, uow, clock);
            var hasher = new StubPasswordHasher();
            var staffLogins = new FakeStaffLoginNameAllocator(users);

            var owner = (await new CreatePlatformUser(users, uow, clock, new SequentialPublicUserIdGenerator())
                .ExecuteAsync("owner", "Org Owner", "owner@example.com")).Value!;

            var orgA = (await new CreatePlatformOrganization(orgs, publicOrgIds, uow, clock)
                .ExecuteAsync("Org A", "org-a")).Value!;
            _ = await addMembership.ExecuteAsync(orgA.Id, owner.Id, OrganizationRole.OrganizationOwner);

            PlatformOrganization? orgB = null;
            if (createOrgB)
            {
                orgB = (await new CreatePlatformOrganization(orgs, publicOrgIds, uow, clock)
                    .ExecuteAsync("Org B", "org-b")).Value!;
                _ = await addMembership.ExecuteAsync(orgB.Id, owner.Id, OrganizationRole.OrganizationOwner);
            }

            var messages = new CapturingAuthOutboundMessageSink();
            var createInvitation = new CreateOrganizationInvitation(
                orgs,
                invitations,
                users,
                publicOrgIds,
                messages,
                uow,
                clock);

            var acceptInvitation = new AcceptOrganizationInvitation(
                invitations,
                orgs,
                users,
                credentials,
                staffLogins,
                publicOrgIds,
                hasher,
                ensure,
                addMembership,
                assignRole,
                messages,
                uow,
                clock,
                Options.Create(new PlatformPasswordOptions()));

            return new StaffInviteHarness
            {
                Users = users,
                Credentials = credentials,
                Organizations = orgs,
                Memberships = memberships,
                Invitations = invitations,
                Sessions = sessions,
                OrgPreferences = preferences,
                Tokens = tokens,
                UnitOfWork = uow,
                Clock = clock,
                Owner = owner,
                OrgA = orgA,
                OrgB = orgB,
                CreateInvitation = createInvitation,
                AcceptInvitation = acceptInvitation,
                Messages = messages
            };
        }
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

    private sealed class StubPasswordHasher : IPlatformPasswordHasher
    {
        public string Algorithm => "stub";

        public string HashPassword(string password) => $"hash:{password}";

        public PlatformPasswordVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword) =>
            hashedPassword == $"hash:{providedPassword}"
                ? PlatformPasswordVerificationResult.Success
                : PlatformPasswordVerificationResult.Failed;
    }

    private sealed class NullPlatformMfaReadinessService : IPlatformMfaReadinessService
    {
        public Task<PlatformMfaReadinessDto> GetForUserAsync(
            PlatformUserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PlatformMfaReadinessDto(
                MfaEnabled: false,
                EnrollmentAvailable: false,
                EnforcementRequired: false,
                ChallengeRequired: false,
                RegisteredFactorCount: 0,
                ReadinessState: PlatformMfaReadinessService.StateNotEnrolled));
    }
}
