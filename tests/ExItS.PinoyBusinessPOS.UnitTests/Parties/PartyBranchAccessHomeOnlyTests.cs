using ExItS.PinoyBusinessPOS.Application.Parties;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Parties;

namespace ExItS.PinoyBusinessPOS.UnitTests.Parties;

public sealed class PartyBranchAccessHomeOnlyTests
{
    private static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Main = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Iloilo = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Customer = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task CreateAtBranch_grant_is_home_only_main_does_not_auto_share()
    {
        var (service, _) = PartyBranchAccessTestSupport.Create(Main);
        await service.GrantCustomerAccessAsync(
            Org,
            Main,
            Customer,
            PartyBranchGrantSource.CreateAtBranch);

        var mainActor = FixedPartyBranchAccessActorAccessor.StoreManager(Main).Actor;
        var iloiloActor = FixedPartyBranchAccessActorAccessor.StoreManager(Iloilo).Actor;

        var mainIds = await service.FilterCustomerIdsAccessibleAsync(Org, mainActor);
        var iloiloIds = await service.FilterCustomerIdsAccessibleAsync(Org, iloiloActor);

        Assert.NotNull(mainIds);
        Assert.Contains(Customer, mainIds!);
        Assert.NotNull(iloiloIds);
        Assert.DoesNotContain(Customer, iloiloIds!);
    }

    [Fact]
    public async Task Owner_with_acting_branch_does_not_bypass_home_only_filter()
    {
        var (service, _) = PartyBranchAccessTestSupport.Create();
        await service.GrantCustomerAccessAsync(
            Org,
            Iloilo,
            Customer,
            PartyBranchGrantSource.CreateAtBranch);

        var ownerAtMain = FixedPartyBranchAccessActorAccessor.Owner(Main).Actor;
        var ownerUnscoped = FixedPartyBranchAccessActorAccessor.Owner().Actor;

        var filtered = await service.FilterCustomerIdsAccessibleAsync(Org, ownerAtMain);
        Assert.NotNull(filtered);
        Assert.DoesNotContain(Customer, filtered!);

        var unscoped = await service.FilterCustomerIdsAccessibleAsync(Org, ownerUnscoped);
        Assert.Null(unscoped);
    }

    [Fact]
    public async Task Explicit_share_makes_customer_visible_at_secondary_branch()
    {
        var (service, _) = PartyBranchAccessTestSupport.Create(Main);
        await service.GrantCustomerAccessAsync(
            Org,
            Main,
            Customer,
            PartyBranchGrantSource.CreateAtBranch);
        await service.GrantCustomerExplicitAssignAsync(Org, Iloilo, Customer);

        var iloiloActor = FixedPartyBranchAccessActorAccessor.StoreManager(Iloilo).Actor;
        var ids = await service.FilterCustomerIdsAccessibleAsync(Org, iloiloActor);
        Assert.Contains(Customer, ids!);
    }
}
