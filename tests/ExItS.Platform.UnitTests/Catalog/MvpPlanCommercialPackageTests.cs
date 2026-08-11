using System.Reflection;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.UnitTests.Catalog;

public sealed class MvpPlanCommercialPackageTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MvpPosPlanCodes_defines_starter_growth_pro()
    {
        Assert.Equal(["starter", "growth", "pro"], MvpPosPlanCodes.All);
        Assert.Equal(MvpPosPlanCodes.Starter, MvpPosPlanCodes.All[0]);
        Assert.Equal(MvpPosPlanCodes.Growth, MvpPosPlanCodes.All[1]);
        Assert.Equal(MvpPosPlanCodes.Pro, MvpPosPlanCodes.All[2]);
        Assert.Equal("business", MvpPosPlanCodes.LegacyBusiness);
    }

    [Fact]
    public void MvpPosPlanCatalog_defines_business_type_capacities_1_3_6()
    {
        Assert.Equal(1, MvpPosPlanCatalog.Plans.Single(p => p.PlanKey == MvpPosPlanCodes.Starter).MaxActiveBusinessTypes);
        Assert.Equal(3, MvpPosPlanCatalog.Plans.Single(p => p.PlanKey == MvpPosPlanCodes.Growth).MaxActiveBusinessTypes);
        Assert.Equal(6, MvpPosPlanCatalog.Plans.Single(p => p.PlanKey == MvpPosPlanCodes.Pro).MaxActiveBusinessTypes);
    }

    [Fact]
    public void BuildGrants_includes_max_active_business_types_limit()
    {
        var starter = MvpPosPlanCatalog.Plans.Single(p => p.PlanKey == MvpPosPlanCodes.Starter);
        var grants = EnsureMvpPosPlans.BuildGrants(starter);
        var limit = grants.Single(g => g.FeatureCode.Value == FeatureCode.PlanMaxActiveBusinessTypes);
        Assert.Equal(1, limit.NumericLimit);
    }

    [Fact]
    public void MvpPosPlanCatalog_defines_branch_staff_device_capacities()
    {
        var starter = MvpPosPlanCatalog.Plans.Single(p => p.PlanKey == MvpPosPlanCodes.Starter);
        var growth = MvpPosPlanCatalog.Plans.Single(p => p.PlanKey == MvpPosPlanCodes.Growth);
        var pro = MvpPosPlanCatalog.Plans.Single(p => p.PlanKey == MvpPosPlanCodes.Pro);

        Assert.Equal((1, 3, 1), (starter.MaxBranches, starter.MaxActiveStaff, starter.MaxActivePosDevices));
        Assert.Equal((3, 10, 3), (growth.MaxBranches, growth.MaxActiveStaff, growth.MaxActivePosDevices));
        Assert.Equal((10, 30, 10), (pro.MaxBranches, pro.MaxActiveStaff, pro.MaxActivePosDevices));
        Assert.False(starter.CustomerCreditEnabled);
        Assert.True(growth.CustomerCreditEnabled);
        Assert.True(pro.CustomerCreditEnabled);
        Assert.False(starter.AdvancedReportsEnabled);
        Assert.True(growth.AdvancedReportsEnabled);
        Assert.True(pro.ExportEnabled);
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
    public void MvpPosPlanCatalog_seeds_three_distinct_plan_keys_for_pos()
    {
        Assert.Equal(3, MvpPosPlanCatalog.Plans.Count);
        var keys = MvpPosPlanCatalog.Plans.Select(p => p.PlanKey).ToArray();
        Assert.Equal(MvpPosPlanCodes.All, keys);
        Assert.Equal(MvpPosPlanCatalog.Plans.Select(p => p.PlanKey).Distinct().Count(), keys.Length);
    }

    [Fact]
    public void Plan_code_is_immutable_after_creation()
    {
        var codeProperty = typeof(Plan).GetProperty(nameof(Plan.Code), BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(codeProperty);
        Assert.Null(codeProperty!.SetMethod);

        var plan = Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create(MvpPosPlanCodes.Starter),
            "Starter",
            T0);
        Assert.Equal(MvpPosPlanCodes.Starter, plan.Code.Value);
        Assert.Equal(MvpPosPlanCodes.Starter, plan.PlanKey);
    }

    [Fact]
    public void Plan_rename_preserves_plan_key()
    {
        var plan = Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create(MvpPosPlanCodes.Growth),
            "Growth",
            T0);
        plan.Activate(T0.AddMinutes(1));
        plan.Rename("Growth Plus", T0.AddMinutes(2));

        Assert.Equal("Growth Plus", plan.DisplayName);
        Assert.Equal(MvpPosPlanCodes.Growth, plan.PlanKey);
        Assert.Equal(MvpPosPlanCodes.Growth, plan.Code.Value);
    }

    [Fact]
    public void Plan_validates_active_pos_device_limit()
    {
        var exception = Assert.Throws<DomainException>(() => Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create(MvpPosPlanCodes.Starter),
            "Starter",
            T0,
            maxActivePosDevices: 0));

        Assert.Equal(DomainErrorCodes.InvalidPlanStatusTransition, exception.ErrorCode);
        Assert.Equal(1, Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create("starter-2"),
            "Starter",
            T0).MaxActivePosDevices);
    }

    [Fact]
    public void Plan_accepts_new_subscriptions_only_when_active()
    {
        var plan = Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            PlanCode.Create(MvpPosPlanCodes.Pro),
            "Pro",
            T0);

        Assert.False(plan.AcceptsNewSubscriptions);
        Assert.Equal(PlanStatus.Draft, plan.Status);

        plan.Activate(T0.AddMinutes(1));
        Assert.True(plan.AcceptsNewSubscriptions);

        plan.Deactivate(T0.AddMinutes(2));
        Assert.Equal(PlanStatus.Inactive, plan.Status);
        Assert.False(plan.AcceptsNewSubscriptions);

        plan.Activate(T0.AddMinutes(3));
        Assert.True(plan.AcceptsNewSubscriptions);

        plan.Retire(T0.AddMinutes(4));
        Assert.Equal(PlanStatus.Retired, plan.Status);
        Assert.False(plan.AcceptsNewSubscriptions);
    }

    [Fact]
    public void Plan_key_is_unique_per_product_not_globally()
    {
        var starter = PlanCode.Create(MvpPosPlanCodes.Starter);
        var posPlan = Plan.CreateDraft(
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            starter,
            "POS Starter",
            T0);
        var otherProductPlan = Plan.CreateDraft(
            ProductCode.Create("healthcare"),
            starter,
            "HC Starter",
            T0);

        Assert.NotEqual(posPlan.Id, otherProductPlan.Id);
        Assert.Equal(MvpPosPlanCodes.Starter, posPlan.PlanKey);
        Assert.Equal(MvpPosPlanCodes.Starter, otherProductPlan.PlanKey);
        Assert.NotEqual(posPlan.ProductCode, otherProductPlan.ProductCode);
    }

    [Fact]
    public void Catalog_offers_retire_not_hard_delete_for_plans()
    {
        var applicationTypes = typeof(CreatePlan).Assembly.GetTypes();
        Assert.DoesNotContain(applicationTypes, t => t.Name == "DeletePlan");

        var repositoryMethods = typeof(IPlanRepository)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToArray();
        Assert.DoesNotContain(repositoryMethods, name => name.Contains("Delete", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(typeof(RetirePlan), applicationTypes);
    }

    [Fact]
    public void Catalog_api_exposes_retire_not_delete_for_plans()
    {
        var catalogSource = File.ReadAllText(
            Path.Combine(
                FindRepoRoot(),
                "src",
                "Platform",
                "ExItS.Platform.Api",
                "Catalog",
                "CatalogEndpoints.cs"));

        Assert.Contains("/retire", catalogSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MapDelete(", catalogSource, StringComparison.Ordinal);
        Assert.DoesNotContain("DeletePlan", catalogSource, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
