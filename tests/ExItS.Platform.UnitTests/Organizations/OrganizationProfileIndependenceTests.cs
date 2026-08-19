using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;
using ExItS.Platform.UnitTests.TestSupport;

namespace ExItS.Platform.UnitTests.Organizations;

/// <summary>
/// Organization profile is independent of Personal profile (one-time copy, no live sync),
/// multi-org ownership is allowed, and MVP enforces one Owner per organization.
/// </summary>
public sealed class OrganizationProfileIndependenceTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Personal_profile_update_does_not_change_organization_profile()
    {
        var users = new InMemoryPlatformUserRepository();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);

        var user = PlatformUser.Create("owner", "Owner", "owner@example.com", T0);
        await users.AddAsync(user);

        var org = PlatformOrganization.Create("Store A", "store-a", T0);
        org.UpdateProfile(
            OrganizationProfile.Create(
                legalName: null,
                contactEmail: "owner@example.com",
                contactPhone: "+639171111111",
                addressLine1: "1 Main St",
                addressLine2: null,
                city: "Manila",
                region: "NCR",
                postalCode: "1000",
                countryCode: "PH",
                timeZoneId: null,
                locale: null,
                currencyCode: null),
            T0);
        await orgs.AddAsync(org);

        user.UpdateProfile("Owner Renamed", "owner-new@example.com", T0.AddMinutes(1));
        await users.UpdateAsync(user);

        var reloaded = await orgs.GetByIdAsync(org.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("owner@example.com", reloaded!.Profile.ContactEmail);
        Assert.Equal("+639171111111", reloaded.Profile.ContactPhone);
        Assert.Equal("1 Main St", reloaded.Profile.AddressLine1);

        var personal = await users.GetByIdAsync(user.Id);
        Assert.Equal("owner-new@example.com", personal!.NormalizedEmail);
    }

    [Fact]
    public async Task Organization_profile_update_does_not_change_personal_user()
    {
        var users = new InMemoryPlatformUserRepository();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);

        var user = PlatformUser.Create("owner", "Owner", "owner@example.com", T0);
        await users.AddAsync(user);

        var org = (await new CreatePlatformOrganization(
                orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
            .ExecuteAsync("Store A", "store-a")).Value!;

        var update = new UpdateOrganizationProfile(orgs, uow, clock);
        var result = await update.ExecuteAsync(
            org.Id,
            new UpdateOrganizationProfileCommand(
                DisplayName: "Store A Updated",
                LegalName: "Store A LLC",
                ContactEmail: "biz@store-a.example",
                ContactPhone: "+639172222222",
                AddressLine1: "2 Market Rd",
                AddressLine2: null,
                City: "Cebu",
                Region: "VII",
                PostalCode: "6000",
                CountryCode: "PH",
                TimeZoneId: null,
                Locale: "en-PH",
                CurrencyCode: "PHP",
                ExpectedUpdatedAtUtc: null),
            requireActiveOrganization: true);

        Assert.True(result.IsSuccess);
        Assert.Equal("biz@store-a.example", result.Value!.Profile.ContactEmail);

        var personal = await users.GetByIdAsync(user.Id);
        Assert.Equal("owner@example.com", personal!.NormalizedEmail);
        Assert.Equal("Owner", personal.DisplayName);
    }

    [Fact]
    public async Task Same_personal_user_can_own_multiple_organizations()
    {
        var users = new InMemoryPlatformUserRepository();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();
        var profiles = new InMemoryAccountProfileRepository();
        var roles = new InMemoryPlatformRoleAssignmentRepository();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);
        var ensure = new EnsureAccountProfilesForUser(profiles, roles, memberships, uow, clock);
        var add = new AddOrganizationMembership(users, orgs, memberships, new InMemoryOrganizationMembershipBranchAssignmentRepository(), ensure, uow, clock);

        var user = (await new CreatePlatformUser(users, uow, clock, new SequentialPublicUserIdGenerator())
            .ExecuteAsync("multi", "Multi Owner", "multi@example.com")).Value!;
        var orgA = (await new CreatePlatformOrganization(
                orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
            .ExecuteAsync("Org A", "org-a-multi")).Value!;
        var orgB = (await new CreatePlatformOrganization(
                orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
            .ExecuteAsync("Org B", "org-b-multi")).Value!;

        var a = await add.ExecuteAsync(orgA.Id, user.Id, OrganizationRole.OrganizationOwner, exclusiveOrganizationProfile: false);
        var b = await add.ExecuteAsync(orgB.Id, user.Id, OrganizationRole.OrganizationOwner, exclusiveOrganizationProfile: false);

        Assert.True(a.IsSuccess);
        Assert.True(b.IsSuccess);
        Assert.Equal(2, memberships.AddCount);
    }

    [Fact]
    public async Task Second_owner_on_same_organization_fails_with_unique_violation()
    {
        var users = new InMemoryPlatformUserRepository();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();
        var profiles = new InMemoryAccountProfileRepository();
        var roles = new InMemoryPlatformRoleAssignmentRepository();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);
        var ensure = new EnsureAccountProfilesForUser(profiles, roles, memberships, uow, clock);
        var add = new AddOrganizationMembership(users, orgs, memberships, new InMemoryOrganizationMembershipBranchAssignmentRepository(), ensure, uow, clock);

        var ownerX = (await new CreatePlatformUser(users, uow, clock, new SequentialPublicUserIdGenerator())
            .ExecuteAsync("ownerx", "Owner X", "ownerx@example.com")).Value!;
        var ownerY = (await new CreatePlatformUser(users, uow, clock, new SequentialPublicUserIdGenerator())
            .ExecuteAsync("ownery", "Owner Y", "ownery@example.com")).Value!;
        var org = (await new CreatePlatformOrganization(
                orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
            .ExecuteAsync("Sole Owner Org", "sole-owner-org")).Value!;

        var first = await add.ExecuteAsync(org.Id, ownerX.Id, OrganizationRole.OrganizationOwner, exclusiveOrganizationProfile: false);
        Assert.True(first.IsSuccess);

        var second = await add.ExecuteAsync(org.Id, ownerY.Id, OrganizationRole.OrganizationOwner, exclusiveOrganizationProfile: false);
        Assert.False(second.IsSuccess);
        Assert.Equal(DomainErrorCodes.OrganizationOwnerUniqueViolation, second.ErrorCode);
        Assert.Equal(1, memberships.AddCount);
    }

    [Fact]
    public async Task Org_A_profile_update_does_not_change_Org_B_profile()
    {
        var orgs = new InMemoryPlatformOrganizationRepository();
        var uow = new NoOpUnitOfWork();
        var clock = new FixedClock(T0);

        var orgA = (await new CreatePlatformOrganization(
                orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
            .ExecuteAsync("Org A", "profile-org-a")).Value!;
        var orgB = (await new CreatePlatformOrganization(
                orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
            .ExecuteAsync("Org B", "profile-org-b")).Value!;

        orgB.UpdateProfile(
            OrganizationProfile.Create(
                null,
                "keep@org-b.example",
                "+639173333333",
                "B Street",
                null,
                "Davao",
                "XI",
                "8000",
                "PH",
                null,
                null,
                null),
            T0);
        await orgs.UpdateAsync(orgB);

        var update = new UpdateOrganizationProfile(orgs, uow, clock);
        var result = await update.ExecuteAsync(
            orgA.Id,
            new UpdateOrganizationProfileCommand(
                DisplayName: "Org A Renamed",
                LegalName: null,
                ContactEmail: "new@org-a.example",
                ContactPhone: "+639174444444",
                AddressLine1: "A Street",
                AddressLine2: null,
                City: "Manila",
                Region: "NCR",
                PostalCode: "1000",
                CountryCode: "PH",
                TimeZoneId: null,
                Locale: null,
                CurrencyCode: null,
                ExpectedUpdatedAtUtc: null),
            requireActiveOrganization: true);

        Assert.True(result.IsSuccess);

        var reloadedB = await orgs.GetByIdAsync(orgB.Id);
        Assert.Equal("keep@org-b.example", reloadedB!.Profile.ContactEmail);
        Assert.Equal("+639173333333", reloadedB.Profile.ContactPhone);
        Assert.Equal("B Street", reloadedB.Profile.AddressLine1);
        Assert.Equal("Org B", reloadedB.DisplayName);
    }

    [Fact]
    public void StartBusinessRequest_exposes_use_my_contact_details_defaults()
    {
        var ctor = typeof(ExItS.Platform.Application.Personal.StartBusinessRequest).GetConstructors().Single();
        var useMy = ctor.GetParameters().Single(p => p.Name == "UseMyContactDetails");
        Assert.False((bool)useMy.DefaultValue!);
        Assert.Contains(ctor.GetParameters(), p => p.Name == "ContactEmail");
        Assert.Contains(ctor.GetParameters(), p => p.Name == "ContactPhone");
        Assert.Contains(ctor.GetParameters(), p => p.Name == "AddressLine1");
        Assert.Contains(ctor.GetParameters(), p => p.Name == "CountryCode");
    }
}
