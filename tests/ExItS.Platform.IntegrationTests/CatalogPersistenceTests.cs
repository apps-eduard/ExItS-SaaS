using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class CatalogPersistenceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private static string UniqueCode(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..Math.Min(64, prefix.Length + 33)];

    [Fact]
    public async Task CreateProduct_persists_and_reloads()
    {
        var code = UniqueCode("persist-pos");
        await using var provider = CatalogTestServices.Build(fixture.ConnectionString);
        var create = provider.GetRequiredService<CreateProduct>();
        var products = provider.GetRequiredService<IProductRepository>();

        var result = await create.ExecuteAsync(code, "Pinoy Business POS");
        Assert.True(result.IsSuccess);

        var reloaded = await products.GetByIdAsync(result.Value!.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(code, reloaded!.Code.Value);
        Assert.Equal("Pinoy Business POS", reloaded.DisplayName);
    }

    [Fact]
    public async Task Duplicate_product_code_returns_conflict()
    {
        var code = UniqueCode("dup-product");
        await using var provider = CatalogTestServices.Build(fixture.ConnectionString);
        var create = provider.GetRequiredService<CreateProduct>();

        Assert.True((await create.ExecuteAsync(code, "HealthCare")).IsSuccess);
        var duplicate = await create.ExecuteAsync(code.ToUpperInvariant(), "HealthCare Two");
        Assert.False(duplicate.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.DuplicateProductCode, duplicate.ErrorCode);
    }

    [Fact]
    public async Task Duplicate_feature_code_per_product_returns_conflict()
    {
        var productCode = UniqueCode("feat-dup");
        await using var provider = CatalogTestServices.Build(fixture.ConnectionString);
        var createProduct = provider.GetRequiredService<CreateProduct>();
        var createFeature = provider.GetRequiredService<CreateFeatureDefinition>();

        await createProduct.ExecuteAsync(productCode, "POS");
        Assert.True((await createFeature.ExecuteAsync(
            productCode,
            FeatureCode.CustomerCreditView,
            "View Credit",
            FeatureValueType.Boolean)).IsSuccess);

        var duplicate = await createFeature.ExecuteAsync(
            productCode,
            FeatureCode.CustomerCreditView,
            "View Credit Again",
            FeatureValueType.Boolean);

        Assert.False(duplicate.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.DuplicateFeatureCode, duplicate.ErrorCode);
    }

    [Fact]
    public async Task Duplicate_plan_code_per_product_returns_conflict()
    {
        var productCode = UniqueCode("plan-dup");
        await using var provider = CatalogTestServices.Build(fixture.ConnectionString);
        var createProduct = provider.GetRequiredService<CreateProduct>();
        var createPlan = provider.GetRequiredService<CreatePlan>();

        await createProduct.ExecuteAsync(productCode, "POS");
        Assert.True((await createPlan.ExecuteAsync(productCode, "utang", "Utang")).IsSuccess);

        var duplicate = await createPlan.ExecuteAsync(productCode, "utang", "Utang Two");
        Assert.False(duplicate.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.DuplicatePlanCode, duplicate.ErrorCode);
    }

    [Fact]
    public async Task Duplicate_plan_version_number_returns_conflict()
    {
        var productCode = UniqueCode("ver-dup");
        await using var provider = CatalogTestServices.Build(fixture.ConnectionString);
        var createProduct = provider.GetRequiredService<CreateProduct>();
        var createFeature = provider.GetRequiredService<CreateFeatureDefinition>();
        var createPlan = provider.GetRequiredService<CreatePlan>();
        var createVersion = provider.GetRequiredService<CreateDraftPlanVersion>();

        await createProduct.ExecuteAsync(productCode, "POS");
        await createFeature.ExecuteAsync(
            productCode,
            FeatureCode.CustomerCreditView,
            "View",
            FeatureValueType.Boolean);

        var plan = (await createPlan.ExecuteAsync(productCode, "utang", "Utang")).Value!;
        var grants = new[] { FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true) };

        Assert.True((await createVersion.ExecuteAsync(
            plan.Id, 1, BillingPeriod.Monthly, true, grants, T0)).IsSuccess);

        var duplicate = await createVersion.ExecuteAsync(
            plan.Id, 1, BillingPeriod.Monthly, true, grants, T0);

        Assert.False(duplicate.IsSuccess);
        Assert.Equal(DomainErrorCodes.InvalidPlanVersionNumber, duplicate.ErrorCode);
    }

    [Fact]
    public async Task Published_plan_version_rejects_grant_replacement()
    {
        var productCode = UniqueCode("pub-immutable");
        await using var provider = CatalogTestServices.Build(fixture.ConnectionString);
        var createProduct = provider.GetRequiredService<CreateProduct>();
        var createFeature = provider.GetRequiredService<CreateFeatureDefinition>();
        var createPlan = provider.GetRequiredService<CreatePlan>();
        var createVersion = provider.GetRequiredService<CreateDraftPlanVersion>();
        var publish = provider.GetRequiredService<PublishExistingPlanVersion>();
        var replaceGrants = provider.GetRequiredService<ReplaceDraftPlanVersionGrants>();

        await createProduct.ExecuteAsync(productCode, "POS");
        await createFeature.ExecuteAsync(
            productCode,
            FeatureCode.CustomerCreditView,
            "View",
            FeatureValueType.Boolean);

        var plan = (await createPlan.ExecuteAsync(productCode, "utang", "Utang")).Value!;
        var grants = new[] { FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true) };
        await createVersion.ExecuteAsync(plan.Id, 1, BillingPeriod.Monthly, true, grants, T0);
        Assert.True((await publish.ExecuteAsync(plan.Id, 1)).IsSuccess);

        var mutation = await replaceGrants.ExecuteAsync(
            plan.Id,
            1,
            new[] { FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), false) });

        Assert.False(mutation.IsSuccess);
        Assert.Equal(DomainErrorCodes.PlanVersionImmutable, mutation.ErrorCode);
    }

    [Fact]
    public async Task Trial_persists_duration_ticks()
    {
        var productCode = UniqueCode("trial-ticks");
        await using var provider = CatalogTestServices.Build(fixture.ConnectionString);
        var createProduct = provider.GetRequiredService<CreateProduct>();
        var createFeature = provider.GetRequiredService<CreateFeatureDefinition>();
        var createTrial = provider.GetRequiredService<CreateTrialDefinition>();
        var trials = provider.GetRequiredService<ITrialDefinitionRepository>();

        await createProduct.ExecuteAsync(productCode, "POS");
        await createFeature.ExecuteAsync(
            productCode,
            FeatureCode.CustomerCreditView,
            "View",
            FeatureValueType.Boolean);

        var duration = TimeSpan.FromDays(90);
        var featureGrants = new[] { FeatureGrantSpec.Boolean(FeatureCode.Create(FeatureCode.CustomerCreditView), true) };
        var result = await createTrial.ExecuteAsync(
            productCode,
            "90-day trial",
            duration,
            featureGrants,
            Array.Empty<FeatureGrantSpec>());

        Assert.True(result.IsSuccess);

        var reloaded = await trials.GetByIdAsync(result.Value!.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(duration.Ticks, reloaded!.Duration.Ticks);
    }

    [Fact]
    public async Task Timestamps_round_trip_as_utc()
    {
        var code = UniqueCode("utc-test");
        await using var provider = CatalogTestServices.Build(fixture.ConnectionString);
        var create = provider.GetRequiredService<CreateProduct>();
        var products = provider.GetRequiredService<IProductRepository>();

        var result = await create.ExecuteAsync(code, "UTC Test");
        Assert.True(result.IsSuccess);
        Assert.Equal(TimeSpan.Zero, result.Value!.CreatedAtUtc.Offset);
        Assert.Equal(TimeSpan.Zero, result.Value.UpdatedAtUtc.Offset);

        var reloaded = await products.GetByCodeAsync(ProductCode.Create(code));
        Assert.NotNull(reloaded);
        Assert.Equal(TimeSpan.Zero, reloaded!.CreatedAtUtc.Offset);
        Assert.Equal(TimeSpan.Zero, reloaded.UpdatedAtUtc.Offset);
    }
}
