using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.UnitTests.Support;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class AccountProfileClassificationTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-07-29T12:00:00Z");

    [Theory]
    [InlineData(AccountClass.Platform)]
    [InlineData(AccountClass.Organization)]
    [InlineData(AccountClass.Personal)]
    public async Task Preferred_class_creates_only_that_profile(AccountClass preferred)
    {
        var (ensure, profiles, _, _) = CreateSut();
        var userId = PlatformUserId.New();

        var created = await ensure.ExecuteAsync(userId, preferred);

        Assert.Equal(preferred, created.AccountClass);
        var active = (await profiles.ListByUserAsync(userId)).Where(p => p.IsActive).ToList();
        Assert.Single(active);
        Assert.Equal(preferred, active[0].AccountClass);
    }

    [Fact]
    public async Task Preferred_class_exclusive_deactivates_unintended_personal_companion()
    {
        var (ensure, profiles, _, _) = CreateSut();
        var userId = PlatformUserId.New();
        await profiles.AddAsync(AccountProfile.Create(userId, AccountClass.Personal, T0));
        await profiles.AddAsync(AccountProfile.Create(userId, AccountClass.Platform, T0));

        await ensure.ExecuteAsync(userId, AccountClass.Platform, exclusivePreferredClass: true);

        var active = (await profiles.ListByUserAsync(userId)).Where(p => p.IsActive).Select(p => p.AccountClass).ToList();
        Assert.Equal([AccountClass.Platform], active);
    }

    [Fact]
    public async Task Preferred_class_non_exclusive_keeps_existing_other_profiles()
    {
        var (ensure, profiles, _, _) = CreateSut();
        var userId = PlatformUserId.New();
        await profiles.AddAsync(AccountProfile.Create(userId, AccountClass.Organization, T0));

        await ensure.ExecuteAsync(userId, AccountClass.Platform, exclusivePreferredClass: false);

        var active = (await profiles.ListByUserAsync(userId)).Where(p => p.IsActive).Select(p => p.AccountClass).OrderBy(c => c.ToString()).ToList();
        Assert.Equal([AccountClass.Organization, AccountClass.Platform], active);
    }

    [Fact]
    public async Task Login_without_preferred_class_reuses_existing_active_profiles_without_adding_personal()
    {
        var (ensure, profiles, _, _) = CreateSut();
        var userId = PlatformUserId.New();
        await profiles.AddAsync(AccountProfile.Create(userId, AccountClass.Organization, T0));

        var selected = await ensure.ExecuteAsync(userId, preferredClass: null);

        Assert.Equal(AccountClass.Organization, selected.AccountClass);
        var active = (await profiles.ListByUserAsync(userId)).Where(p => p.IsActive).ToList();
        Assert.Single(active);
        Assert.DoesNotContain(active, p => p.AccountClass == AccountClass.Personal);
    }

    [Fact]
    public async Task Running_preferred_ensure_twice_does_not_duplicate_profiles()
    {
        var (ensure, profiles, _, _) = CreateSut();
        var userId = PlatformUserId.New();

        await ensure.ExecuteAsync(userId, AccountClass.Personal);
        await ensure.ExecuteAsync(userId, AccountClass.Personal);

        var all = await profiles.ListByUserAsync(userId);
        Assert.Single(all);
        Assert.True(all[0].IsActive);
        Assert.Equal(AccountClass.Personal, all[0].AccountClass);
    }

    [Fact]
    public async Task Platform_and_organization_preferred_users_do_not_receive_personal_profile()
    {
        var (ensure, profiles, _, _) = CreateSut();
        var olivia = PlatformUserId.New();
        var maria = PlatformUserId.New();

        await ensure.ExecuteAsync(olivia, AccountClass.Platform);
        await ensure.ExecuteAsync(maria, AccountClass.Organization);

        Assert.DoesNotContain(
            await profiles.ListByUserAsync(olivia),
            p => p.IsActive && p.AccountClass == AccountClass.Personal);
        Assert.DoesNotContain(
            await profiles.ListByUserAsync(maria),
            p => p.IsActive && p.AccountClass == AccountClass.Personal);
    }

    private static (
        EnsureAccountProfilesForUser Ensure,
        InMemoryAccountProfileRepository Profiles,
        InMemoryPlatformRoleAssignmentRepository Roles,
        InMemoryOrganizationMembershipRepository Memberships) CreateSut()
    {
        var profiles = new InMemoryAccountProfileRepository();
        var roles = new InMemoryPlatformRoleAssignmentRepository();
        var memberships = new InMemoryOrganizationMembershipRepository();
        var ensure = new EnsureAccountProfilesForUser(
            profiles,
            roles,
            memberships,
            new NoOpUnitOfWork(),
            new FixedClock(T0));
        return (ensure, profiles, roles, memberships);
    }
}
