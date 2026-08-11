using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class OrganizationBusinessTypeEntitlementEnforcementTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 11, 18, 30, 0, TimeSpan.Zero);

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    [Fact]
    public async Task Activation_requires_grant_and_stale_activation_rows_are_retained()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var businessTypes = provider.GetRequiredService<IBusinessTypeRepository>();
        var plans = provider.GetRequiredService<IPlanRepository>();
        var orgs = provider.GetRequiredService<IPlatformOrganizationRepository>();
        var activations = provider.GetRequiredService<IOrganizationBusinessTypeActivationRepository>();
        var uow = provider.GetRequiredService<IPlatformUnitOfWork>();
        var resolver = provider.GetRequiredService<IOrganizationBusinessTypeEntitlementResolver>();
        var activate = provider.GetRequiredService<ActivateOrganizationBusinessType>();

        var sari = await RequireBt(businessTypes, "SariSari");
        var bakery = await RequireBt(businessTypes, "Bakery");
        var pharmacy = await RequireBt(businessTypes, "Pharmacy");

        var candidate = Unique("bt-enf");
        var productCode = candidate[..Math.Min(30, candidate.Length)];
        await provider.GetRequiredService<CreateProduct>().ExecuteAsync(productCode, "POS");
        var plan = (await provider.GetRequiredService<CreatePlan>().ExecuteAsync(productCode, "basic", "Basic")).Value!;
        await provider.GetRequiredService<ActivatePlan>().ExecuteAsync(plan.Id);

        var version = PlanVersion.CreateDraft(
            plan,
            1,
            T0,
            BillingPeriod.Monthly,
            true,
            Array.Empty<FeatureGrantSpec>(),
            T0,
            businessTypeGrants: [sari, bakery]);
        version.Publish(T0.AddMinutes(1));
        await plans.AddVersionAsync(version);
        await uow.SaveChangesAsync();

        var org = (await provider.GetRequiredService<CreatePlatformOrganization>()
            .ExecuteAsync("Enf Org", Unique("enf-org"))).Value!;
        org.AssignPrimaryBusinessType(sari, T0);
        await orgs.UpdateAsync(org);
        await uow.SaveChangesAsync();

        var trial = (await provider.GetRequiredService<CreateTrialDefinition>()
            .ExecuteAsync(
                productCode,
                "Trial",
                TimeSpan.FromDays(14),
                Array.Empty<FeatureGrantSpec>(),
                Array.Empty<FeatureGrantSpec>())).Value!;
        var started = await provider.GetRequiredService<StartTrialSubscription>()
            .ExecuteAsync(org.Id, plan.Id, version.Id, trial.Id);
        Assert.True(started.IsSuccess, started.ErrorMessage);

        var before = await resolver.ResolveAsync(org.Id, ProductCode.Create(productCode));
        Assert.True(before.IsSuccess, before.ErrorMessage);
        Assert.Equal([sari], before.Value!.EffectiveBusinessTypeIds);

        var bakeryActivation = await activate.ExecuteAsync(org.Id.Value, bakery.Value, productCode);
        Assert.True(bakeryActivation.IsSuccess, bakeryActivation.ErrorMessage);

        var withBakery = await resolver.ResolveAsync(org.Id, ProductCode.Create(productCode));
        Assert.Contains(bakery, withBakery.Value!.EffectiveBusinessTypeIds);
        Assert.Contains(sari, withBakery.Value.EffectiveBusinessTypeIds);

        var pharmacyDenied = await activate.ExecuteAsync(org.Id.Value, pharmacy.Value, productCode);
        Assert.False(pharmacyDenied.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.BusinessTypeNotEntitled, pharmacyDenied.ErrorCode);

        var listed = await activations.ListByOrganizationAsync(org.Id);
        Assert.Contains(listed, a => a.BusinessTypeId == bakery);
        Assert.DoesNotContain(listed, a => a.BusinessTypeId == pharmacy);
    }

    private static async Task<BusinessTypeId> RequireBt(IBusinessTypeRepository repo, string code)
    {
        var bt = await repo.GetByCodeAsync(code);
        Assert.NotNull(bt);
        return bt!.Id;
    }
}
