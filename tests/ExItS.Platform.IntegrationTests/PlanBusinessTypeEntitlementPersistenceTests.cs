using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace ExItS.Platform.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class PlanBusinessTypeEntitlementPersistenceTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 11, 15, 0, 0, TimeSpan.Zero);

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    [Fact]
    public async Task Plan_version_persists_one_and_many_business_type_grants_and_rejects_duplicate_rows()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var plans = provider.GetRequiredService<IPlanRepository>();
        var businessTypes = provider.GetRequiredService<IBusinessTypeRepository>();
        var uow = provider.GetRequiredService<IPlatformUnitOfWork>();

        var sariSari = await RequireSeededBusinessTypeAsync(businessTypes, "SariSari");
        var bakery = await RequireSeededBusinessTypeAsync(businessTypes, "Bakery");

        var candidate = Unique("bt-grant");
        var productCode = candidate[..Math.Min(30, candidate.Length)];
        var createProduct = provider.GetRequiredService<CreateProduct>();
        var createPlan = provider.GetRequiredService<CreatePlan>();
        var activatePlan = provider.GetRequiredService<ActivatePlan>();
        await createProduct.ExecuteAsync(productCode, "POS").ConfigureAwait(false);
        var plan = (await createPlan.ExecuteAsync(productCode, "basic", "Basic").ConfigureAwait(false)).Value!;
        await activatePlan.ExecuteAsync(plan.Id).ConfigureAwait(false);

        var single = PlanVersion.CreateDraft(
            plan,
            1,
            T0,
            BillingPeriod.Monthly,
            true,
            Array.Empty<FeatureGrantSpec>(),
            T0,
            businessTypeGrants: [sariSari]);
        await plans.AddVersionAsync(single).ConfigureAwait(false);
        await uow.SaveChangesAsync().ConfigureAwait(false);

        var reloadedSingle = await plans.GetVersionByIdAsync(single.Id).ConfigureAwait(false);
        Assert.NotNull(reloadedSingle);
        Assert.Single(reloadedSingle!.BusinessTypeGrants);
        Assert.Equal(sariSari, reloadedSingle.BusinessTypeGrants[0]);

        var multi = PlanVersion.CreateDraft(
            plan,
            2,
            T0,
            BillingPeriod.Monthly,
            true,
            Array.Empty<FeatureGrantSpec>(),
            T0,
            businessTypeGrants: [sariSari, bakery]);
        await plans.AddVersionAsync(multi).ConfigureAwait(false);
        await uow.SaveChangesAsync().ConfigureAwait(false);

        var reloadedMulti = await plans.GetVersionByIdAsync(multi.Id).ConfigureAwait(false);
        Assert.NotNull(reloadedMulti);
        Assert.Equal(2, reloadedMulti!.BusinessTypeGrants.Count);
        Assert.Contains(sariSari, reloadedMulti.BusinessTypeGrants);
        Assert.Contains(bakery, reloadedMulti.BusinessTypeGrants);

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO platform.plan_version_business_type_grants (plan_version_id, business_type_id)
            VALUES (@planVersionId, @businessTypeId)
            """,
            connection);
        command.Parameters.AddWithValue("planVersionId", multi.Id.Value);
        command.Parameters.AddWithValue("businessTypeId", sariSari.Value);
        await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
    }

    [Fact]
    public async Task Organization_activation_persists_and_rejects_duplicate_while_primary_unchanged()
    {
        await using var provider = CommercialTestServices.Build(fixture.ConnectionString, T0);
        var orgs = provider.GetRequiredService<IPlatformOrganizationRepository>();
        var activations = provider.GetRequiredService<IOrganizationBusinessTypeActivationRepository>();
        var businessTypes = provider.GetRequiredService<IBusinessTypeRepository>();
        var uow = provider.GetRequiredService<IPlatformUnitOfWork>();
        var createOrg = provider.GetRequiredService<CreatePlatformOrganization>();

        var sariSari = await RequireSeededBusinessTypeAsync(businessTypes, "SariSari");
        var bakery = await RequireSeededBusinessTypeAsync(businessTypes, "Bakery");

        var org = (await createOrg.ExecuteAsync("BT Act Org", Unique("bt-org")).ConfigureAwait(false)).Value!;
        org.AssignPrimaryBusinessType(sariSari, T0);
        await orgs.UpdateAsync(org).ConfigureAwait(false);
        await uow.SaveChangesAsync().ConfigureAwait(false);

        var activation = OrganizationBusinessTypeActivation.Activate(
            org.Id,
            bakery,
            T0.AddMinutes(1),
            primaryBusinessTypeId: sariSari);
        await activations.AddAsync(activation).ConfigureAwait(false);
        await uow.SaveChangesAsync().ConfigureAwait(false);

        var listed = await activations.ListByOrganizationAsync(org.Id).ConfigureAwait(false);
        Assert.Single(listed);
        Assert.Equal(bakery, listed[0].BusinessTypeId);

        var reloadedOrg = await orgs.GetByIdAsync(org.Id).ConfigureAwait(false);
        Assert.Equal(sariSari, reloadedOrg!.PrimaryBusinessTypeId);

        await Assert.ThrowsAsync<DomainException>(() =>
            activations.AddAsync(
                OrganizationBusinessTypeActivation.Activate(org.Id, bakery, T0.AddMinutes(2), sariSari)));
    }

    [Fact]
    public async Task Migration_model_exposes_new_platform_tables()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'platform'
              AND table_name IN (
                'plan_version_business_type_grants',
                'organization_business_type_activations')
            ORDER BY table_name
            """,
            connection);

        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        Assert.Contains("organization_business_type_activations", tables);
        Assert.Contains("plan_version_business_type_grants", tables);
    }

    private static async Task<BusinessTypeId> RequireSeededBusinessTypeAsync(
        IBusinessTypeRepository businessTypes,
        string code)
    {
        var type = await businessTypes.GetByCodeAsync(code).ConfigureAwait(false);
        Assert.NotNull(type);
        return type!.Id;
    }
}
