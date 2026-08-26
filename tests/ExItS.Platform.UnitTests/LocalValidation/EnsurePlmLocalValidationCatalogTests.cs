using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.LocalValidation;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.UnitTests.Support;
using ExItS.Platform.UnitTests.TestSupport;

namespace ExItS.Platform.UnitTests.LocalValidation;

public sealed class EnsurePlmLocalValidationCatalogTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Reference_registration_is_idempotent_and_independent_of_pos()
    {
        var harness = CreateHarness();
        var pos = Product.Create(ProductCode.Create(ProductCode.PinoyBusinessPos), "Pinoy Business POS", T0);
        await harness.Products.AddAsync(pos);
        var afterPos = harness.Products.AddCount;

        await harness.Ensure.EnsureReferenceAsync();
        await harness.Ensure.EnsureReferenceAsync();

        Assert.Equal(afterPos + 1, harness.Products.AddCount);
        var plm = await harness.Products.GetByCodeAsync(ProductCode.Create(ProductCode.PinoyLoanManager));
        var posAgain = await harness.Products.GetByCodeAsync(ProductCode.Create(ProductCode.PinoyBusinessPos));
        Assert.NotNull(plm);
        Assert.Equal(EnsurePlmLocalValidationCatalog.ProductDisplayName, plm!.DisplayName);
        Assert.Equal(ProductStatus.Active, plm.Status);
        Assert.NotNull(posAgain);
        Assert.Equal("Pinoy Business POS", posAgain!.DisplayName);
        Assert.NotEqual(plm.Id, posAgain.Id);

        var plan = await harness.Plans.GetByProductAndCodeAsync(
            plm.Code,
            PlanCode.Create(LocalValidationOptions.PlmLocalValidationPlanCode));
        Assert.NotNull(plan);
        Assert.Equal(PlanStatus.Active, plan!.Status);
        Assert.Equal(0m, plan.MonthlyPrice);
        Assert.Equal(LocalValidationOptions.PlmLocalValidationPlanDisplayName, plan.DisplayName);
    }

    [Fact]
    public async Task Commercial_fixture_is_independent_of_pos_and_supports_allowed_versus_denied_orgs()
    {
        var harness = CreateHarness();
        var publicIds = new FakePublicOrganizationIdGenerator();
        var allowedOrg = (await new CreatePlatformOrganization(
                harness.Organizations,
                publicIds,
                harness.UnitOfWork,
                harness.Clock)
            .ExecuteAsync("ABC Sari-Sari Store", "abc-sari-sari")).Value!;
        var deniedOrg = (await new CreatePlatformOrganization(
                harness.Organizations,
                publicIds,
                harness.UnitOfWork,
                harness.Clock)
            .ExecuteAsync("XYZ Mini Grocery", "xyz-mini-grocery")).Value!;

        var pos = Product.Create(ProductCode.Create(ProductCode.PinoyBusinessPos), "Pinoy Business POS", T0);
        await harness.Products.AddAsync(pos);
        var posPlan = Plan.CreateDraft(pos.Code, PlanCode.Create("growth"), "Growth", T0);
        posPlan.Activate(T0);
        await harness.Plans.AddAsync(posPlan);

        await harness.Ensure.EnsureCommercialAsync(allowedOrg.Id);

        var plmCode = ProductCode.Create(ProductCode.PinoyLoanManager);
        var posCode = ProductCode.Create(ProductCode.PinoyBusinessPos);
        var allowedPlmSub = await harness.Subscriptions.GetCurrentForOrganizationProductAsync(allowedOrg.Id, plmCode);
        var allowedPosSub = await harness.Subscriptions.GetCurrentForOrganizationProductAsync(allowedOrg.Id, posCode);
        var deniedPlmSub = await harness.Subscriptions.GetCurrentForOrganizationProductAsync(deniedOrg.Id, plmCode);
        Assert.NotNull(allowedPlmSub);
        Assert.Null(allowedPosSub);
        Assert.Null(deniedPlmSub);

        var allowedSnapshot = await harness.Snapshots.GetLatestForOrganizationProductAsync(allowedOrg.Id, plmCode);
        var posSnapshot = await harness.Snapshots.GetLatestForOrganizationProductAsync(allowedOrg.Id, posCode);
        Assert.NotNull(allowedSnapshot);
        Assert.Empty(allowedSnapshot!.Grants);
        Assert.Null(posSnapshot);
    }

    private static Harness CreateHarness()
    {
        var clock = new FixedClock(T0);
        var uow = new NoOpUnitOfWork();
        var products = new InMemoryProductRepository();
        var plans = new InMemoryPlanRepository();
        var features = new InMemoryFeatureDefinitionRepository();
        var trials = new InMemoryTrialDefinitionRepository();
        var subscriptions = new InMemorySubscriptionRepository();
        var snapshots = new InMemoryEntitlementSnapshotRepository();
        var overrides = new InMemoryFeatureOverrideRepository();
        var organizations = new InMemoryPlatformOrganizationRepository();

        var ensure = new EnsurePlmLocalValidationCatalog(
            new CreateProduct(products, uow, clock),
            products,
            new CreatePlan(products, plans, uow, clock),
            new ActivatePlan(plans, uow, clock),
            new PublishPlanVersion(plans, features, uow, clock),
            new CreateTrialDefinition(products, plans, trials, uow, clock),
            new StartTrialSubscription(organizations, products, plans, trials, subscriptions, uow, clock),
            new GenerateEntitlementSnapshot(
                subscriptions,
                plans,
                trials,
                overrides,
                snapshots,
                new ProvisionalEntitlementRefreshPolicy(),
                uow,
                clock),
            plans,
            trials,
            subscriptions,
            uow,
            clock);

        return new Harness(ensure, products, plans, subscriptions, snapshots, organizations, uow, clock);
    }

    private sealed record Harness(
        EnsurePlmLocalValidationCatalog Ensure,
        InMemoryProductRepository Products,
        InMemoryPlanRepository Plans,
        InMemorySubscriptionRepository Subscriptions,
        InMemoryEntitlementSnapshotRepository Snapshots,
        InMemoryPlatformOrganizationRepository Organizations,
        NoOpUnitOfWork UnitOfWork,
        FixedClock Clock);
}
