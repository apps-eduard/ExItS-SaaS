using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.UnitTests.Catalog;

public sealed class MvpPlanCommercialPackageTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MvpPosPlanCodes_defines_starter_growth_pro_pro_plus()
    {
        Assert.Equal(["starter", "growth", "pro", "pro-plus"], MvpPosPlanCodes.All);
        Assert.Equal(MvpPosPlanCodes.Starter, MvpPosPlanCodes.All[0]);
        Assert.Equal(MvpPosPlanCodes.Growth, MvpPosPlanCodes.All[1]);
        Assert.Equal(MvpPosPlanCodes.Pro, MvpPosPlanCodes.All[2]);
        Assert.Equal(MvpPosPlanCodes.ProPlus, MvpPosPlanCodes.All[3]);
        Assert.Equal("business", MvpPosPlanCodes.LegacyBusiness);
    }

    [Fact]
    public void MvpPosPlanCatalog_defines_business_type_capacities()
    {
        Assert.Equal(1, MvpPosPlanCatalog.Plans.Single(p => p.PlanKey == MvpPosPlanCodes.Starter).MaxActiveBusinessTypes);
        Assert.Equal(3, MvpPosPlanCatalog.Plans.Single(p => p.PlanKey == MvpPosPlanCodes.Growth).MaxActiveBusinessTypes);
        Assert.Equal(6, MvpPosPlanCatalog.Plans.Single(p => p.PlanKey == MvpPosPlanCodes.Pro).MaxActiveBusinessTypes);
        Assert.Equal(12, MvpPosPlanCatalog.Plans.Single(p => p.PlanKey == MvpPosPlanCodes.ProPlus).MaxActiveBusinessTypes);
    }

    [Fact]
    public void BuildGrants_includes_max_active_business_types_and_areas_limit()
    {
        var starter = MvpPosPlanCatalog.Plans.Single(p => p.PlanKey == MvpPosPlanCodes.Starter);
        var grants = EnsureMvpPosPlans.BuildGrants(starter);
        var btLimit = grants.Single(g => g.FeatureCode.Value == FeatureCode.PlanMaxActiveBusinessTypes);
        Assert.Equal(1, btLimit.NumericLimit);
        var areaLimit = grants.Single(g => g.FeatureCode.Value == FeatureCode.PlanMaxAreas);
        Assert.Equal(0, areaLimit.NumericLimit);
        Assert.False(grants.Single(g => g.FeatureCode.Value == FeatureCode.StoreAreaManagement).Enabled);
        Assert.False(grants.Single(g => g.FeatureCode.Value == FeatureCode.StoreWarehouse).Enabled);
    }

    [Fact]
    public void MvpPosPlanCatalog_defines_branch_staff_device_capacities()
    {
        var starter = MvpPosPlanCatalog.Plans.Single(p => p.PlanKey == MvpPosPlanCodes.Starter);
        var growth = MvpPosPlanCatalog.Plans.Single(p => p.PlanKey == MvpPosPlanCodes.Growth);
        var pro = MvpPosPlanCatalog.Plans.Single(p => p.PlanKey == MvpPosPlanCodes.Pro);
        var proPlus = MvpPosPlanCatalog.Plans.Single(p => p.PlanKey == MvpPosPlanCodes.ProPlus);

        Assert.Equal((1, 3, 1), (starter.MaxBranches, starter.MaxActiveStaff, starter.MaxActivePosDevices));
        Assert.Equal((3, 10, 3), (growth.MaxBranches, growth.MaxActiveStaff, growth.MaxActivePosDevices));
        Assert.Equal((10, 30, 10), (pro.MaxBranches, pro.MaxActiveStaff, pro.MaxActivePosDevices));
        Assert.Equal((25, 75, 25), (proPlus.MaxBranches, proPlus.MaxActiveStaff, proPlus.MaxActivePosDevices));
        Assert.True(starter.CustomerCreditEnabled);
        Assert.True(growth.CustomerCreditEnabled);
        Assert.False(starter.AdvancedReportsEnabled);
        Assert.False(growth.AdvancedReportsEnabled);
        Assert.True(pro.AdvancedReportsEnabled);
        Assert.True(pro.ExportEnabled);
        Assert.True(pro.WarehouseEnabled);
        Assert.True(proPlus.WarehouseEnabled);
        Assert.Equal(0, starter.MaxAreas);
        Assert.Equal(0, growth.MaxAreas);
        Assert.Equal(3, pro.MaxAreas);
        Assert.Equal(10, proPlus.MaxAreas);
    }

    [Fact]
    public void Starter_includes_utang_and_excludes_warehouse_area_advanced_reports()
    {
        var starter = MvpPosPlanCatalog.Plans.Single(p => p.PlanKey == MvpPosPlanCodes.Starter);
        var grants = EnsureMvpPosPlans.BuildGrants(starter);
        Assert.True(grants.Single(g => g.FeatureCode.Value == FeatureCode.CustomerCreditCreate).Enabled);
        Assert.True(grants.Single(g => g.FeatureCode.Value == FeatureCode.CustomerCreditView).Enabled);
        Assert.True(grants.Single(g => g.FeatureCode.Value == FeatureCode.CustomerCreditRepay).Enabled);
        Assert.False(grants.Single(g => g.FeatureCode.Value == FeatureCode.StoreWarehouse).Enabled);
        Assert.False(grants.Single(g => g.FeatureCode.Value == FeatureCode.StoreAreaManagement).Enabled);
        Assert.False(grants.Single(g => g.FeatureCode.Value == FeatureCode.StoreAdvancedReports).Enabled);
        Assert.False(grants.Single(g => g.FeatureCode.Value == FeatureCode.StoreExport).Enabled);
        Assert.False(grants.Single(g => g.FeatureCode.Value == FeatureCode.StoreCustomerOrdering).Enabled);
    }

    [Fact]
    public void Pro_and_pro_plus_include_area_warehouse_and_advanced_reports()
    {
        var pro = EnsureMvpPosPlans.BuildGrants(MvpPosPlanCatalog.Plans.Single(p => p.PlanKey == MvpPosPlanCodes.Pro));
        var proPlus = EnsureMvpPosPlans.BuildGrants(
            MvpPosPlanCatalog.Plans.Single(p => p.PlanKey == MvpPosPlanCodes.ProPlus));

        Assert.True(pro.Single(g => g.FeatureCode.Value == FeatureCode.StoreWarehouse).Enabled);
        Assert.True(pro.Single(g => g.FeatureCode.Value == FeatureCode.StoreAreaManagement).Enabled);
        Assert.Equal(3, pro.Single(g => g.FeatureCode.Value == FeatureCode.PlanMaxAreas).NumericLimit);
        Assert.True(proPlus.Single(g => g.FeatureCode.Value == FeatureCode.StoreWarehouse).Enabled);
        Assert.Equal(10, proPlus.Single(g => g.FeatureCode.Value == FeatureCode.PlanMaxAreas).NumericLimit);
        Assert.Equal(25, proPlus.Single(g => g.FeatureCode.Value == FeatureCode.PlanMaxBranches).NumericLimit);
    }

    [Fact]
    public void Plan_validates_max_active_business_types()
    {
        var exception = Assert.Throws<DomainException>(() => Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create("starter-bt-0"),
            "Starter",
            T0,
            maxActiveBusinessTypes: 0));
        Assert.Equal(DomainErrorCodes.InvalidPlanStatusTransition, exception.ErrorCode);
    }

    [Fact]
    public void Plan_allows_max_areas_zero_for_no_area_management()
    {
        var plan = Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create("starter-areas-0"),
            "Starter",
            T0,
            maxAreas: 0);
        Assert.Equal(0, plan.MaxAreas);
    }

    [Fact]
    public void MvpPosPlanCatalog_seeds_four_distinct_plan_keys_for_pos()
    {
        Assert.Equal(4, MvpPosPlanCatalog.Plans.Count);
        var keys = MvpPosPlanCatalog.Plans.Select(p => p.PlanKey).ToArray();
        Assert.Equal(MvpPosPlanCodes.All, keys);
        Assert.Equal(MvpPosPlanCatalog.Plans.Select(p => p.PlanKey).Distinct().Count(), keys.Length);
    }

    [Fact]
    public void Plan_code_is_immutable_after_creation()
    {
        var plan = Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create("starter"),
            "Starter",
            T0);
        Assert.Equal("starter", plan.Code.Value);
        Assert.Null(typeof(Plan).GetProperty("Code")!.SetMethod);
    }

    [Fact]
    public void PlanChangeImpact_blocks_warehouse_and_area_downgrade()
    {
        var pro = Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create("pro"),
            "Pro",
            T0,
            maxBranches: 10,
            maxActiveStaff: 30,
            maxActivePosDevices: 10,
            maxActiveBusinessTypes: 6,
            maxAreas: 3,
            advancedReportsEnabled: true,
            exportEnabled: true);
        pro.Activate(T0);

        var growth = Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create("growth"),
            "Growth",
            T0,
            maxBranches: 3,
            maxActiveStaff: 10,
            maxActivePosDevices: 3,
            maxActiveBusinessTypes: 3,
            maxAreas: 0);
        growth.Activate(T0);

        var preview = ExItS.Platform.Application.Subscriptions.PlanChangeImpact.Evaluate(
            pro,
            growth,
            activeStaffCount: 2,
            activeBranchCount: 2,
            branchCountAvailable: true,
            activeBusinessTypeCount: 1,
            activeAreaCount: 2,
            activeWarehouseBranchCount: 1,
            targetWarehouseEnabled: false);

        Assert.True(preview.HasBlockingUsageConflicts);
        Assert.Contains(preview.UsageConflicts, c => c.Resource == "Warehouse");
        Assert.Contains(preview.UsageConflicts, c => c.Resource == "Areas");
    }
}
