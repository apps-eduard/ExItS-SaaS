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

public sealed class EnsureBnplLocalValidationCatalogTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Reference_registration_is_idempotent_and_independent_of_pos_and_plm()
    {
        var harness = CreateHarness();
        var pos = Product.Create(ProductCode.Create(ProductCode.PinoyBusinessPos), "Pinoy Business POS", T0);
        await harness.Products.AddAsync(pos);
        var plm = Product.Create(ProductCode.Create(ProductCode.PinoyLoanManager), "Pinoy Loan Manager", T0);
        await harness.Products.AddAsync(plm);
        var afterOthers = harness.Products.AddCount;

        await harness.Ensure.EnsureReferenceAsync();
        await harness.Ensure.EnsureReferenceAsync();

        Assert.Equal(afterOthers + 1, harness.Products.AddCount);
        var bnpl = await harness.Products.GetByCodeAsync(ProductCode.Create(ProductCode.PinoyBuyNowPayLater));
        var posAgain = await harness.Products.GetByCodeAsync(ProductCode.Create(ProductCode.PinoyBusinessPos));
        var plmAgain = await harness.Products.GetByCodeAsync(ProductCode.Create(ProductCode.PinoyLoanManager));
        Assert.NotNull(bnpl);
        Assert.Equal(EnsureBnplLocalValidationCatalog.ProductDisplayName, bnpl!.DisplayName);
        Assert.Equal(ProductStatus.Active, bnpl.Status);
        Assert.NotNull(posAgain);
        Assert.NotNull(plmAgain);
        Assert.NotEqual(bnpl.Id, posAgain!.Id);
        Assert.NotEqual(bnpl.Id, plmAgain!.Id);

        var plan = await harness.Plans.GetByProductAndCodeAsync(
            bnpl.Code,
            PlanCode.Create(LocalValidationOptions.BnplLocalValidationPlanCode));
        Assert.NotNull(plan);
        Assert.Equal(PlanStatus.Active, plan!.Status);
        Assert.Equal(0m, plan.MonthlyPrice);
        Assert.Equal(LocalValidationOptions.BnplLocalValidationPlanDisplayName, plan.DisplayName);
    }

    [Fact]
    public async Task Commercial_fixture_is_independent_of_pos_and_plm()
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

        var bnplCode = ProductCode.Create(ProductCode.PinoyBuyNowPayLater);
        var posCode = ProductCode.Create(ProductCode.PinoyBusinessPos);
        var allowedBnplSub = await harness.Subscriptions.GetCurrentForOrganizationProductAsync(allowedOrg.Id, bnplCode);
        var allowedPosSub = await harness.Subscriptions.GetCurrentForOrganizationProductAsync(allowedOrg.Id, posCode);
        var deniedBnplSub = await harness.Subscriptions.GetCurrentForOrganizationProductAsync(deniedOrg.Id, bnplCode);
        Assert.NotNull(allowedBnplSub);
        Assert.Null(allowedPosSub);
        Assert.Null(deniedBnplSub);

        var allowedSnapshot = await harness.Snapshots.GetLatestForOrganizationProductAsync(allowedOrg.Id, bnplCode);
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

        var ensure = new EnsureBnplLocalValidationCatalog(
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
        EnsureBnplLocalValidationCatalog Ensure,
        InMemoryProductRepository Products,
        InMemoryPlanRepository Plans,
        InMemorySubscriptionRepository Subscriptions,
        InMemoryEntitlementSnapshotRepository Snapshots,
        InMemoryPlatformOrganizationRepository Organizations,
        NoOpUnitOfWork UnitOfWork,
        FixedClock Clock);
}
