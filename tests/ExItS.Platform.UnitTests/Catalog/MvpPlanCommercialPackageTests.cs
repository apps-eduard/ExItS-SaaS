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
    public void MvpPosPlanCodes_defines_starter_business_pro()
    {
        Assert.Equal(["starter", "business", "pro"], MvpPosPlanCodes.All);
        Assert.Equal(MvpPosPlanCodes.Starter, MvpPosPlanCodes.All[0]);
        Assert.Equal(MvpPosPlanCodes.Business, MvpPosPlanCodes.All[1]);
        Assert.Equal(MvpPosPlanCodes.Pro, MvpPosPlanCodes.All[2]);
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
            PlanCode.Create(MvpPosPlanCodes.Business),
            "Business",
            T0);
        plan.Activate(T0.AddMinutes(1));
        plan.Rename("Business Plus", T0.AddMinutes(2));

        Assert.Equal("Business Plus", plan.DisplayName);
        Assert.Equal(MvpPosPlanCodes.Business, plan.PlanKey);
        Assert.Equal(MvpPosPlanCodes.Business, plan.Code.Value);
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
