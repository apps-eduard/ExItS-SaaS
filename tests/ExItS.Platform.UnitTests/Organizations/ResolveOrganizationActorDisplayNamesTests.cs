using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class ResolveOrganizationActorDisplayNamesTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Resolves_current_org_actor_display_name()
    {
        var orgId = PlatformOrganizationId.New();
        var user = PlatformUser.Create("maria", "Maria Santos", "maria@example.com", T0);
        var memberships = new InMemoryOrganizationMembershipRepository();
        var users = new InMemoryPlatformUserRepository();
        await users.AddAsync(user);
        await memberships.AddAsync(OrganizationMembership.Create(
            orgId, user.Id, OrganizationRole.OrganizationMember, T0));

        var useCase = new ResolveOrganizationActorDisplayNames(memberships, users);
        var result = await useCase.ExecuteAsync(
            orgId.Value,
            new ResolveOrganizationActorDisplayNamesRequest([user.Id.Value]));

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!);
        Assert.Equal(user.Id.Value, item.ActorId);
        Assert.Equal("Maria Santos", item.DisplayName);
        Assert.Equal(ResolveOrganizationActorDisplayNames.ActorStatusActive, item.ActorStatus);
    }

    [Fact]
    public async Task Cross_org_actor_does_not_leak_account_details()
    {
        var orgA = PlatformOrganizationId.New();
        var orgB = PlatformOrganizationId.New();
        var foreign = PlatformUser.Create("foreign", "Foreign User", "foreign@example.com", T0);
        var memberships = new InMemoryOrganizationMembershipRepository();
        var users = new InMemoryPlatformUserRepository();
        await users.AddAsync(foreign);
        await memberships.AddAsync(OrganizationMembership.Create(
            orgB, foreign.Id, OrganizationRole.OrganizationMember, T0));

        var useCase = new ResolveOrganizationActorDisplayNames(memberships, users);
        var result = await useCase.ExecuteAsync(
            orgA.Value,
            new ResolveOrganizationActorDisplayNamesRequest([foreign.Id.Value]));

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!);
        Assert.Equal(ResolveOrganizationActorDisplayNames.DisplayNameNotAvailable, item.DisplayName);
        Assert.Equal(ResolveOrganizationActorDisplayNames.ActorStatusNotAvailable, item.ActorStatus);
    }

    [Fact]
    public async Task Former_staff_resolves_with_former_status()
    {
        var orgId = PlatformOrganizationId.New();
        var user = PlatformUser.Create("juan", "Juan Dela Cruz", "juan@example.com", T0);
        var memberships = new InMemoryOrganizationMembershipRepository();
        var users = new InMemoryPlatformUserRepository();
        await users.AddAsync(user);
        var membership = OrganizationMembership.Create(
            orgId, user.Id, OrganizationRole.OrganizationMember, T0);
        membership.Remove(T0.AddHours(1), "left company");
        await memberships.AddAsync(membership);

        var useCase = new ResolveOrganizationActorDisplayNames(memberships, users);
        var result = await useCase.ExecuteAsync(
            orgId.Value,
            new ResolveOrganizationActorDisplayNamesRequest([user.Id.Value]));

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!);
        Assert.Equal("Juan Dela Cruz", item.DisplayName);
        Assert.Equal(ResolveOrganizationActorDisplayNames.ActorStatusFormerStaff, item.ActorStatus);
    }

    [Fact]
    public async Task Unknown_actor_is_not_available_without_guid_leak_semantics()
    {
        var orgId = PlatformOrganizationId.New();
        var useCase = new ResolveOrganizationActorDisplayNames(
            new InMemoryOrganizationMembershipRepository(),
            new InMemoryPlatformUserRepository());
        var unknown = Guid.Parse("8c91a84d-87f1-4aaa-bbbb-cccccccccccc");

        var result = await useCase.ExecuteAsync(
            orgId.Value,
            new ResolveOrganizationActorDisplayNamesRequest([unknown]));

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!);
        Assert.Equal(ResolveOrganizationActorDisplayNames.DisplayNameNotAvailable, item.DisplayName);
        Assert.DoesNotContain("8c91a84d", item.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Batch_rejects_oversized_request()
    {
        var orgId = PlatformOrganizationId.New();
        var ids = Enumerable.Range(0, ResolveOrganizationActorDisplayNames.MaxActorIds + 1)
            .Select(_ => Guid.NewGuid())
            .ToList();
        var useCase = new ResolveOrganizationActorDisplayNames(
            new InMemoryOrganizationMembershipRepository(),
            new InMemoryPlatformUserRepository());

        var result = await useCase.ExecuteAsync(
            orgId.Value,
            new ResolveOrganizationActorDisplayNamesRequest(ids));

        Assert.False(result.IsSuccess);
    }
}
