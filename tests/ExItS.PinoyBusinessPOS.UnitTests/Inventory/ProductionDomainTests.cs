using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class ProductionDomainTests
{
    private static readonly Guid Org = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateTimeOffset Utc = new(2026, 8, 29, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ProductionNumbers_format_PRD_pattern()
    {
        var number = ProductionNumbers.Format(new DateOnly(2026, 8, 29), 123);
        Assert.Equal("PRD-20260829-000123", number);
        Assert.Equal(number, ProductionNumbers.Normalize(number.ToLowerInvariant()));
    }

    [Fact]
    public void Definition_create_rejects_self_component()
    {
        var output = CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        var ex = Assert.Throws<DomainException>(() =>
            ProductionDefinition.Create(
                PosOrganizationId.From(Org),
                "Self",
                output,
                10m,
                1m,
                [new ProductionComponentDraft(output, 1m, 1m)],
                Actor,
                Utc));
        Assert.Equal(DomainErrorCodes.ProductionSelfComponentForbidden, ex.ErrorCode);
    }

    [Fact]
    public void Definition_update_increments_revision()
    {
        var output = CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        var flour = CatalogProductId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
        var sugar = CatalogProductId.From(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));
        var definition = ProductionDefinition.Create(
            PosOrganizationId.From(Org),
            "Batch",
            output,
            100m,
            1m,
            [new ProductionComponentDraft(flour, 10m, 1m)],
            Actor,
            Utc);
        Assert.Equal(1, definition.Revision);

        definition.Update(
            "Batch v2",
            output,
            100m,
            1m,
            [new ProductionComponentDraft(flour, 9m, 1m), new ProductionComponentDraft(sugar, 2m, 1m)],
            Actor,
            Utc);
        Assert.Equal(2, definition.Revision);
        Assert.Equal(2, definition.Components.Count);
    }

    [Fact]
    public void Run_void_only_from_posted()
    {
        var run = CreatePostedRun();
        run.Void(Utc.AddMinutes(1), Actor);
        Assert.Equal(ProductionRunStatus.Voided, run.Status);
        var ex = Assert.Throws<DomainException>(() => run.Void(Utc.AddMinutes(2), Actor));
        Assert.Equal(DomainErrorCodes.InvalidProductionRunStatusTransition, ex.ErrorCode);
    }

    [Fact]
    public void Cost_status_complete_when_all_material_costs_known()
    {
        Assert.Equal(ProductionCostStatus.Complete, ProductionCostStatuses.FromMaterialCosts([1m, 2m]));
        Assert.Equal(ProductionCostStatus.Partial, ProductionCostStatuses.FromMaterialCosts([1m, null]));
        Assert.Equal(ProductionCostStatus.Unavailable, ProductionCostStatuses.FromMaterialCosts([null, null]));
    }

    private static ProductionRun CreatePostedRun()
    {
        var output = CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        var flour = CatalogProductId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
        return ProductionRun.Create(
            PosOrganizationId.From(Org),
            ProductionNumbers.Format(ProductionNumbers.BusinessDateOf(Utc), 1),
            ProductionDefinitionId.New(),
            1,
            "Batch",
            output,
            100m,
            1m,
            "Pandesal",
            "pcs",
            [
                new ProductionRunMaterialDraft(flour, 10m, 10m, 1m, "Flour", "kg", UnitCostSnapshot: 5m)
            ],
            Actor,
            Utc);
    }
}
