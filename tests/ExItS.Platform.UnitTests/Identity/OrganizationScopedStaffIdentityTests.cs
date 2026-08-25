using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.LocalValidation;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
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
        Assert.Null(staff.LinkedPersonalUserId);

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

        var personal = await harness.CreateVerifiedPersonalAsync(
            "personal_multi",
            "Personal Multi",
            contact,
            StaffPassword);

        var inviteA = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgA.Id,
            contact,
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);
        var staffA = (await harness.AcceptInvitation.ExecuteForAuthenticatedPersonalAsync(
            personal.Id,
            inviteA.Value!.AcceptToken!,
            StaffPassword + "A")).Value!;

        var inviteB = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgB!.Id,
            contact,
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);
        var staffB = (await harness.AcceptInvitation.ExecuteForAuthenticatedPersonalAsync(
            personal.Id,
            inviteB.Value!.AcceptToken!,
            StaffPassword + "B")).Value!;

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
    public async Task Authenticated_personal_accept_creates_linked_staff_without_converting_personal()
    {
        var harness = await StaffInviteHarness.CreateAsync();
        const string email = "paul@gmail.com";
        const string personalPassword = "SecurePass1!";
        const string staffPassword = "SecurePass2!";
        var personal = await harness.CreateVerifiedPersonalAsync("paul", "Paul", email, personalPassword);

        var invite = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgA.Id,
            email,
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false,
            displayName: "Paul Staff");
        Assert.True(invite.IsSuccess, invite.ErrorMessage);

        var anonymous = await harness.AcceptInvitation.ExecuteAsync(invite.Value!.AcceptToken!, staffPassword);
        Assert.False(anonymous.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InvitationRequiresAuthenticatedPersonal, anonymous.ErrorCode);

        var accept = await harness.AcceptInvitation.ExecuteForAuthenticatedPersonalAsync(
            personal.Id,
            invite.Value.AcceptToken!,
            staffPassword);
        Assert.True(accept.IsSuccess, accept.ErrorMessage);
        Assert.NotEqual(personal.Id.Value, accept.Value!.UserId);
        Assert.Equal(personal.Id.Value, accept.Value.LinkedPersonalUserId);
        Assert.StartsWith("paul@", accept.Value.StaffLogin, StringComparison.OrdinalIgnoreCase);

        var unchangedPersonal = (await harness.Users.GetByIdAsync(personal.Id))!;
        Assert.Null(unchangedPersonal.HomeOrganizationId);
        Assert.Equal(email, unchangedPersonal.NormalizedEmail);
        Assert.Equal(AccountStatus.Active, unchangedPersonal.Status);

        var staff = (await harness.Users.GetByIdAsync(PlatformUserId.From(accept.Value.UserId)))!;
        Assert.Equal(harness.OrgA.Id, staff.HomeOrganizationId);
        Assert.Equal(personal.Id, staff.LinkedPersonalUserId);
        Assert.Equal(email, staff.NormalizedContactEmail);
        Assert.NotEqual(personal.Id, staff.Id);

        var login = harness.CreateLogin();
        var personalLogin = await login.ExecuteAsync(email, personalPassword, null, null);
        Assert.True(personalLogin.IsSuccess, personalLogin.ErrorMessage);
        Assert.Equal(personal.Id.Value, personalLogin.Value!.UserId);

        var personalAsStaff = await login.ExecuteAsync(accept.Value.StaffLogin, personalPassword, null, null);
        Assert.False(personalAsStaff.IsSuccess);

        var staffLogin = await login.ExecuteAsync(accept.Value.StaffLogin, staffPassword, null, null);
        Assert.True(staffLogin.IsSuccess, staffLogin.ErrorMessage);
        Assert.Equal(staff.Id.Value, staffLogin.Value!.UserId);

        var staffAsPersonal = await login.ExecuteAsync(email, staffPassword, null, null);
        Assert.False(staffAsPersonal.IsSuccess);

        Assert.Contains(
            harness.Audit.Entries,
            e => e.Action == PlatformAuditActions.InvitationAccepted
                 && e.Actor == $"platform-user:{personal.Id.Value:D}"
                 && e.OrganizationId == harness.OrgA.Id
                 && e.Summary is not null
                 && e.Summary.Contains(accept.Value.UserId.ToString("D"), StringComparison.Ordinal)
                 && e.Summary.Contains(personal.Id.Value.ToString("D"), StringComparison.Ordinal)
                 && !e.Summary.Contains(staffPassword, StringComparison.Ordinal)
                 && !e.Summary.Contains(invite.Value.AcceptToken!, StringComparison.Ordinal));
        Assert.Contains(
            harness.Audit.Entries,
            e => e.Action == PlatformAuditActions.PersonLinkEstablished
                 && e.TargetId == accept.Value.UserId.ToString("D")
                 && e.Actor == $"platform-user:{personal.Id.Value:D}"
                 && e.Summary is not null
                 && e.Summary.Contains("not authorization", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Platform_only_same_email_allows_anonymous_accept_without_person_link()
    {
        var harness = await StaffInviteHarness.CreateAsync();
        const string email = "employee@company.com";
        var platformUser = (await new CreatePlatformUser(
            harness.Users,
            harness.UnitOfWork,
            harness.Clock,
            new SequentialPublicUserIdGenerator())
            .ExecuteAsync("platemp", "Platform Emp", email)).Value!;
        var credential = PlatformUserCredential.Create(
            platformUser.Id,
            harness.Hasher.HashPassword(StaffPassword),
            harness.Hasher.Algorithm,
            harness.Clock.UtcNow);
        credential.MarkEmailVerified(harness.Clock.UtcNow);
        await harness.Credentials.AddAsync(credential);
        await harness.EnsureProfiles.ExecuteAsync(
            platformUser.Id,
            AccountClass.Platform,
            exclusivePreferredClass: true);

        Assert.Null(await harness.Profiles.GetByUserAndClassAsync(platformUser.Id, AccountClass.Personal));

        var invite = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgA.Id,
            email,
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);
        Assert.True(invite.IsSuccess, invite.ErrorMessage);

        var accept = await harness.AcceptInvitation.ExecuteAsync(invite.Value!.AcceptToken!, StaffPassword);
        Assert.True(accept.IsSuccess, accept.ErrorMessage);
        Assert.Null(accept.Value!.LinkedPersonalUserId);
        Assert.NotEqual(platformUser.Id.Value, accept.Value.UserId);

        var staff = (await harness.Users.GetByIdAsync(PlatformUserId.From(accept.Value.UserId)))!;
        Assert.Null(staff.LinkedPersonalUserId);
        Assert.Equal(harness.OrgA.Id, staff.HomeOrganizationId);
        Assert.DoesNotContain(
            harness.Audit.Entries,
            e => e.Action == PlatformAuditActions.PersonLinkEstablished);
    }

    [Fact]
    public async Task Authenticated_platform_user_without_personal_profile_cannot_accept_as_personal()
    {
        var harness = await StaffInviteHarness.CreateAsync();
        const string email = "employee@company.com";
        var platformUser = (await new CreatePlatformUser(
            harness.Users,
            harness.UnitOfWork,
            harness.Clock,
            new SequentialPublicUserIdGenerator())
            .ExecuteAsync("platemp2", "Platform Emp2", email)).Value!;
        var credential = PlatformUserCredential.Create(
            platformUser.Id,
            harness.Hasher.HashPassword(StaffPassword),
            harness.Hasher.Algorithm,
            harness.Clock.UtcNow);
        credential.MarkEmailVerified(harness.Clock.UtcNow);
        await harness.Credentials.AddAsync(credential);
        await harness.EnsureProfiles.ExecuteAsync(
            platformUser.Id,
            AccountClass.Platform,
            exclusivePreferredClass: true);

        var invite = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgA.Id,
            email,
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);

        var denied = await harness.AcceptInvitation.ExecuteForAuthenticatedPersonalAsync(
            platformUser.Id,
            invite.Value!.AcceptToken!,
            StaffPassword);
        Assert.False(denied.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InvitationNotFound, denied.ErrorCode);
    }

    [Fact]
    public async Task Unverified_personal_authenticated_accept_is_rejected()
    {
        var harness = await StaffInviteHarness.CreateAsync();
        const string email = "paul@gmail.com";
        var personal = (await new CreatePlatformUser(
            harness.Users,
            harness.UnitOfWork,
            harness.Clock,
            new SequentialPublicUserIdGenerator())
            .ExecuteAsync("pauluv", "Paul", email)).Value!;
        await harness.Credentials.AddAsync(PlatformUserCredential.Create(
            personal.Id,
            harness.Hasher.HashPassword(StaffPassword),
            harness.Hasher.Algorithm,
            harness.Clock.UtcNow));
        await harness.EnsureProfiles.ExecuteAsync(personal.Id, AccountClass.Personal, exclusivePreferredClass: true);

        var invite = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgA.Id,
            email,
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);

        var anonymous = await harness.AcceptInvitation.ExecuteAsync(invite.Value!.AcceptToken!, StaffPassword);
        Assert.False(anonymous.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InvitationRequiresAuthenticatedPersonal, anonymous.ErrorCode);

        var denied = await harness.AcceptInvitation.ExecuteForAuthenticatedPersonalAsync(
            personal.Id,
            invite.Value.AcceptToken!,
            StaffPassword);
        Assert.False(denied.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InvitationPersonalEmailUnverified, denied.ErrorCode);
    }

    [Fact]
    public async Task Staff_lockout_does_not_lock_personal_or_other_staff_principal()
    {
        var harness = await StaffInviteHarness.CreateAsync(createOrgB: true);
        const string email = "paul@gmail.com";
        const string personalPassword = "SecurePass1!";
        var personal = await harness.CreateVerifiedPersonalAsync("paul", "Paul", email, personalPassword);

        var inviteA = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgA.Id,
            email,
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);
        var inviteB = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgB!.Id,
            email,
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);

        var staffA = (await harness.AcceptInvitation.ExecuteForAuthenticatedPersonalAsync(
            personal.Id, inviteA.Value!.AcceptToken!, "SecurePass2!")).Value!;
        var staffB = (await harness.AcceptInvitation.ExecuteForAuthenticatedPersonalAsync(
            personal.Id, inviteB.Value!.AcceptToken!, "SecurePass3!")).Value!;

        var login = harness.CreateLogin();
        for (var i = 0; i < 5; i++)
        {
            var failed = await login.ExecuteAsync(staffA.StaffLogin, "WrongPass1!!!!", null, null);
            Assert.False(failed.IsSuccess);
        }

        var locked = await login.ExecuteAsync(staffA.StaffLogin, "SecurePass2!", null, null);
        Assert.False(locked.IsSuccess);

        var personalStill = await login.ExecuteAsync(email, personalPassword, null, null);
        Assert.True(personalStill.IsSuccess, personalStill.ErrorMessage);
        Assert.Equal(personal.Id.Value, personalStill.Value!.UserId);

        var staffBStill = await login.ExecuteAsync(staffB.StaffLogin, "SecurePass3!", null, null);
        Assert.True(staffBStill.IsSuccess, staffBStill.ErrorMessage);
        Assert.Equal(staffB.UserId, staffBStill.Value!.UserId);
    }

    [Fact]
    public async Task Multi_org_personal_accept_links_two_staff_principals_with_independent_home_orgs()
    {
        var harness = await StaffInviteHarness.CreateAsync(createOrgB: true);
        const string email = "paul@gmail.com";
        var personal = await harness.CreateVerifiedPersonalAsync("paul", "Paul", email, StaffPassword);

        var inviteA = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgA.Id,
            email,
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);
        var inviteB = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgB!.Id,
            email,
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);

        var staffA = (await harness.AcceptInvitation.ExecuteForAuthenticatedPersonalAsync(
            personal.Id, inviteA.Value!.AcceptToken!, StaffPassword + "A")).Value!;
        var staffB = (await harness.AcceptInvitation.ExecuteForAuthenticatedPersonalAsync(
            personal.Id, inviteB.Value!.AcceptToken!, StaffPassword + "B")).Value!;

        Assert.NotEqual(staffA.UserId, staffB.UserId);
        Assert.Equal(personal.Id.Value, staffA.LinkedPersonalUserId);
        Assert.Equal(personal.Id.Value, staffB.LinkedPersonalUserId);

        var userA = (await harness.Users.GetByIdAsync(PlatformUserId.From(staffA.UserId)))!;
        var userB = (await harness.Users.GetByIdAsync(PlatformUserId.From(staffB.UserId)))!;
        Assert.Equal(harness.OrgA.Id, userA.HomeOrganizationId);
        Assert.Equal(harness.OrgB.Id, userB.HomeOrganizationId);

        var login = harness.CreateLogin();
        Assert.True((await login.ExecuteAsync(staffA.StaffLogin, StaffPassword + "A", null, null)).IsSuccess);
        Assert.False((await login.ExecuteAsync(staffB.StaffLogin, StaffPassword + "A", null, null)).IsSuccess);
        Assert.True((await login.ExecuteAsync(staffB.StaffLogin, StaffPassword + "B", null, null)).IsSuccess);

        const string opaque = "staff-a-session";
        var credentialA = (await harness.Credentials.GetByUserIdAsync(userA.Id))!;
        await harness.Sessions.AddAsync(PlatformAuthSession.Create(
            userA.Id,
            AccountProfileId.New(),
            AccountClass.Organization,
            harness.Tokens.HashToken(opaque),
            credentialA.SecurityStamp,
            T0,
            TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(12),
            selectedOrganizationId: harness.OrgA.Id));
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
        var switchAway = await setContext.ExecuteAsync(opaque, harness.OrgB.Id.Value);
        Assert.False(switchAway.IsSuccess);
        Assert.Equal(DomainErrorCodes.StaffOrganizationSwitchDenied, switchAway.ErrorCode);
    }

    [Fact]
    public async Task Wrong_personal_cannot_accept_and_does_not_create_staff()
    {
        var harness = await StaffInviteHarness.CreateAsync();
        var paul = await harness.CreateVerifiedPersonalAsync("paul", "Paul", "paul@gmail.com", StaffPassword);
        var mary = await harness.CreateVerifiedPersonalAsync("mary", "Mary", "mary@gmail.com", StaffPassword);
        var usersBefore = harness.Users.AddCount;

        var invite = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgA.Id,
            "paul@gmail.com",
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);

        var denied = await harness.AcceptInvitation.ExecuteForAuthenticatedPersonalAsync(
            mary.Id,
            invite.Value!.AcceptToken!,
            StaffPassword);
        Assert.False(denied.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InvitationNotFound, denied.ErrorCode);
        Assert.Equal(usersBefore, harness.Users.AddCount);
        Assert.Null((await harness.Users.GetByIdAsync(paul.Id))!.HomeOrganizationId);
        Assert.DoesNotContain("paul", denied.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Matching_contact_email_does_not_auto_link_legacy_staff()
    {
        var harness = await StaffInviteHarness.CreateAsync();
        var personal = await harness.CreateVerifiedPersonalAsync("legacy", "Legacy", "legacy@example.com", StaffPassword);
        var staff = PlatformUser.CreateOrganizationStaff(
            "legacy_staff",
            "legacy@ORG001111",
            "legacy@example.com",
            harness.OrgA.Id,
            "Legacy Staff",
            T0);
        await harness.Users.AddAsync(staff);

        Assert.Null(staff.LinkedPersonalUserId);
        Assert.Null((await harness.Users.GetByIdAsync(staff.Id))!.LinkedPersonalUserId);
        Assert.Equal(personal.Id, (await harness.Users.GetByNormalizedEmailAsync("legacy@example.com"))!.Id);
        Assert.NotEqual(personal.Id, staff.Id);
    }

    [Fact]
    public async Task Revoked_or_replayed_invitation_does_not_create_staff()
    {
        var harness = await StaffInviteHarness.CreateAsync();
        var invited = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgA.Id,
            "replay@example.com",
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);
        var token = invited.Value!.AcceptToken!;
        var first = await harness.AcceptInvitation.ExecuteAsync(token, StaffPassword);
        Assert.True(first.IsSuccess, first.ErrorMessage);
        var usersAfterFirst = harness.Users.AddCount;

        var replay = await harness.AcceptInvitation.ExecuteAsync(token, StaffPassword);
        Assert.False(replay.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InvitationNotFound, replay.ErrorCode);
        Assert.Equal(usersAfterFirst, harness.Users.AddCount);

        var revokedInvite = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgA.Id,
            "revoked@example.com",
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);
        var revoke = new RevokeOrganizationInvitation(harness.Invitations, harness.UnitOfWork, harness.Clock);
        Assert.True((await revoke.ExecuteAsync(OrganizationInvitationId.From(revokedInvite.Value!.Id))).IsSuccess);
        var revokedAccept = await harness.AcceptInvitation.ExecuteAsync(
            revokedInvite.Value.AcceptToken!,
            StaffPassword);
        Assert.False(revokedAccept.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.InvitationNotFound, revokedAccept.ErrorCode);
    }

    [Fact]
    public async Task Staff_membership_without_pos_grant_then_assign_and_revoke()
    {
        var harness = await StaffInviteHarness.CreateAsync();
        var invite = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgA.Id,
            "cashierless@example.com",
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);
        var accept = await harness.AcceptInvitation.ExecuteAsync(invite.Value!.AcceptToken!, StaffPassword);
        Assert.True(accept.IsSuccess, accept.ErrorMessage);
        var staffId = PlatformUserId.From(accept.Value!.UserId);

        Assert.Null(await harness.RoleGrants.FindActiveByUserOrganizationProductAsync(
            harness.OrgA.Id,
            staffId,
            ProductCode.PinoyBusinessPos));

        var grant = ProductLocalRoleGrant.Create(
            harness.OrgA.Id,
            staffId,
            ProductCode.PinoyBusinessPos,
            ProductLocalRoleCodes.Cashier,
            harness.Owner.Id,
            T0);
        await harness.RoleGrants.AddAsync(grant);
        Assert.NotNull(await harness.RoleGrants.FindActiveByUserOrganizationProductAsync(
            harness.OrgA.Id,
            staffId,
            ProductCode.PinoyBusinessPos));

        var revoke = new RevokeProductLocalRole(harness.RoleGrants, harness.UnitOfWork, harness.Clock);
        Assert.True((await revoke.ExecuteAsync(grant.Id, harness.Owner.Id, "remove POS")).IsSuccess);
        Assert.Null(await harness.RoleGrants.FindActiveByUserOrganizationProductAsync(
            harness.OrgA.Id,
            staffId,
            ProductCode.PinoyBusinessPos));
    }

    [Fact]
    public async Task Person_link_does_not_attach_membership_to_personal_or_authorize_personal_as_staff()
    {
        var harness = await StaffInviteHarness.CreateAsync();
        var personal = await harness.CreateVerifiedPersonalAsync("custpaul", "Paul", "custpaul@example.com", StaffPassword);
        var invite = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgA.Id,
            "custpaul@example.com",
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);
        var accept = await harness.AcceptInvitation.ExecuteForAuthenticatedPersonalAsync(
            personal.Id,
            invite.Value!.AcceptToken!,
            StaffPassword);
        Assert.True(accept.IsSuccess, accept.ErrorMessage);

        var (personalMemberships, _) = await harness.Memberships.ListByUserAsync(personal.Id, status: null, skip: 0, take: 20);
        Assert.Empty(personalMemberships);

        var staffMembership = await harness.Memberships.FindActiveByUserAndOrganizationAsync(
            PlatformUserId.From(accept.Value!.UserId),
            harness.OrgA.Id);
        Assert.NotNull(staffMembership);
    }

    [Fact]
    public async Task Org_a_membership_removal_does_not_touch_personal_or_org_b()
    {
        var harness = await StaffInviteHarness.CreateAsync(createOrgB: true);
        const string email = "remove@example.com";
        var personal = await harness.CreateVerifiedPersonalAsync("remove", "Remove", email, StaffPassword);
        var inviteA = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgA.Id,
            email,
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);
        var inviteB = await harness.CreateInvitation.ExecuteAsync(
            harness.OrgB!.Id,
            email,
            OrganizationRole.OrganizationMember,
            harness.Owner.Id,
            OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: false);
        var staffA = (await harness.AcceptInvitation.ExecuteForAuthenticatedPersonalAsync(
            personal.Id, inviteA.Value!.AcceptToken!, StaffPassword + "A")).Value!;
        var staffB = (await harness.AcceptInvitation.ExecuteForAuthenticatedPersonalAsync(
            personal.Id, inviteB.Value!.AcceptToken!, StaffPassword + "B")).Value!;

        var revoke = new RevokeOrganizationMembership(
            harness.Memberships,
            new InMemoryProductAccessAssignmentRepository(),
            harness.Sessions,
            new InMemoryPlatformAccessTokenRepository(),
            harness.UnitOfWork,
            harness.Clock);
        Assert.True((await revoke.ExecuteAsync(
            OrganizationMembershipId.From(staffA.MembershipId),
            "org A only")).IsSuccess);

        var login = harness.CreateLogin();
        Assert.True((await login.ExecuteAsync(email, StaffPassword, null, null)).IsSuccess);
        Assert.True((await login.ExecuteAsync(staffB.StaffLogin, StaffPassword + "B", null, null)).IsSuccess);

        var userA = (await harness.Users.GetByIdAsync(PlatformUserId.From(staffA.UserId)))!;
        Assert.Equal(personal.Id, userA.LinkedPersonalUserId);
        Assert.Equal(AccountStatus.Active, userA.Status);
        Assert.Equal(AccountStatus.Active, (await harness.Users.GetByIdAsync(personal.Id))!.Status);
        Assert.Equal(AccountStatus.Active, (await harness.Users.GetByIdAsync(PlatformUserId.From(staffB.UserId)))!.Status);
    }

    [Fact]
    public void Person_link_is_staff_only_and_immutable()
    {
        var orgId = PlatformOrganizationId.New();
        var personalId = PlatformUserId.New();
        var staff = PlatformUser.CreateOrganizationStaff(
            "linked_staff",
            "linked@ORG009999",
            "linked@example.com",
            orgId,
            "Linked Staff",
            T0,
            linkedPersonalUserId: personalId);
        Assert.Equal(personalId, staff.LinkedPersonalUserId);

        staff.LinkToPersonalPrincipal(personalId, T0.AddMinutes(1));
        Assert.Equal(personalId, staff.LinkedPersonalUserId);

        var other = PlatformUserId.New();
        var ex = Assert.Throws<DomainException>(() => staff.LinkToPersonalPrincipal(other, T0.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.PersonLinkImmutable, ex.ErrorCode);

        var personal = PlatformUser.Create("personal_only", "Personal Only", "only@example.com", T0);
        var personalEx = Assert.Throws<DomainException>(() => personal.LinkToPersonalPrincipal(personalId, T0));
        Assert.Equal(DomainErrorCodes.PersonLinkStaffRequired, personalEx.ErrorCode);
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
        public required EnsureAccountProfilesForUser EnsureProfiles { get; init; }
        public required IPlatformPasswordHasher Hasher { get; init; }
        public required AssignProductLocalRole AssignRole { get; init; }
        public required InMemoryProductLocalRoleGrantRepository RoleGrants { get; init; }
        public required CapturingAuthOutboundMessageSink Messages { get; init; }
        public required RecordingAuditWriter Audit { get; init; }
        public required InMemoryAccountProfileRepository Profiles { get; init; }

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
            var audit = new RecordingAuditWriter();

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
                profiles,
                staffLogins,
                publicOrgIds,
                hasher,
                ensure,
                addMembership,
                assignRole,
                messages,
                uow,
                clock,
                audit,
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
                EnsureProfiles = ensure,
                Hasher = hasher,
                AssignRole = assignRole,
                RoleGrants = roleGrants,
                Messages = messages,
                Audit = audit,
                Profiles = profiles
            };
        }

        public async Task<PlatformUser> CreateVerifiedPersonalAsync(
            string username,
            string displayName,
            string email,
            string password)
        {
            var user = (await new CreatePlatformUser(Users, UnitOfWork, Clock, new SequentialPublicUserIdGenerator())
                .ExecuteAsync(username, displayName, email)).Value!;
            var credential = PlatformUserCredential.Create(
                user.Id,
                Hasher.HashPassword(password),
                Hasher.Algorithm,
                Clock.UtcNow);
            credential.MarkEmailVerified(Clock.UtcNow);
            await Credentials.AddAsync(credential);
            await EnsureProfiles.ExecuteAsync(user.Id, AccountClass.Personal, exclusivePreferredClass: true);
            return (await Users.GetByIdAsync(user.Id))!;
        }

        public LoginPlatformUser CreateLogin() =>
            new(
                Users,
                Credentials,
                Sessions,
                Memberships,
                Organizations,
                OrgPreferences,
                EnsureProfiles,
                Hasher,
                Tokens,
                new NoOpAuditWriter(),
                UnitOfWork,
                Clock,
                Options.Create(new PlatformLockoutOptions()),
                Options.Create(new PlatformSessionOptions()),
                new NullPlatformMfaReadinessService(),
                Options.Create(new LocalValidationOptions()));
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
