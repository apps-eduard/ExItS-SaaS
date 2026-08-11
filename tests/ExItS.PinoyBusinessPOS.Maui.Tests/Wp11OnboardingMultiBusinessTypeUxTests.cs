using ExItS.PinoyBusinessPOS.Application.Platform;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

/// <summary>WP11 — source contracts for multi-BT onboarding / plan capacity UX.</summary>
public sealed class Wp11OnboardingMultiBusinessTypeUxTests
{
    [Fact]
    public void Commercial_plan_dto_includes_max_active_business_types()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Platform",
            "PlatformAccessModels.cs"));
        Assert.Contains("int MaxActiveBusinessTypes = 1", source, StringComparison.Ordinal);
        Assert.Contains("GetOnboardingBusinessTypesAsync", source, StringComparison.Ordinal);
        Assert.Contains("GetOrganizationBusinessTypeEntitlementsAsync", source, StringComparison.Ordinal);
        Assert.Contains("ActivateOrganizationBusinessTypeAsync", source, StringComparison.Ordinal);
        Assert.Contains("DeactivateOrganizationBusinessTypeAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Explore_pos_displays_devices_and_business_type_capacity_from_api_dto()
    {
        var explore = File.ReadAllText(Path.Combine(FindMauiPages(), "Personal", "PersonalExplorePos.razor"));
        Assert.Contains("MaxActivePosDevices", explore, StringComparison.Ordinal);
        Assert.Contains("MaxActiveBusinessTypes", explore, StringComparison.Ordinal);
        Assert.Contains("Personal_ExplorePosFeatureBusinessTypes", explore, StringComparison.Ordinal);
        Assert.DoesNotContain("hard-coded", explore, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Start_business_requires_primary_and_uses_onboarding_business_types()
    {
        var start = File.ReadAllText(Path.Combine(FindMauiPages(), "Personal", "StartBusiness.razor"));
        Assert.Contains("GetOnboardingBusinessTypesAsync", start, StringComparison.Ordinal);
        Assert.Contains("StartBusiness_PrimaryTypeRequired", start, StringComparison.Ordinal);
        Assert.Contains("PrimaryBusinessTypeId", start, StringComparison.Ordinal);
        Assert.Contains("SetBusinessTypeActivationPromptPendingAsync", start, StringComparison.Ordinal);
        Assert.DoesNotContain("GeneralRetail", start, StringComparison.Ordinal);
        Assert.DoesNotContain("GetActiveBusinessTypesAsync", start, StringComparison.Ordinal);
    }

    [Fact]
    public void Onboarding_and_org_business_type_pages_exist_with_capacity_ux()
    {
        var onboarding = File.ReadAllText(Path.Combine(FindMauiPages(), "Personal", "OnboardingActivateBusinessTypes.razor"));
        Assert.Contains("@page \"/onboarding/business-types\"", onboarding, StringComparison.Ordinal);
        Assert.Contains("BusinessTypes_Capacity", onboarding, StringComparison.Ordinal);
        Assert.Contains("ActivateOrganizationBusinessTypeAsync", onboarding, StringComparison.Ordinal);
        Assert.Contains("IsPrimary", onboarding, StringComparison.Ordinal);

        var org = File.ReadAllText(Path.Combine(FindMauiPages(), "Organization", "OrgBusinessTypes.razor"));
        Assert.Contains("@page \"/org/business-types\"", org, StringComparison.Ordinal);
        Assert.Contains("BusinessTypes_OwnerOnlyMessage", org, StringComparison.Ordinal);
        Assert.Contains("DeactivateOrganizationBusinessTypeAsync", org, StringComparison.Ordinal);

        var gate = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "Services",
            "NavigationGate.cs"));
        Assert.Contains("/onboarding/business-types", gate, StringComparison.Ordinal);
        Assert.Contains("GetBusinessTypeActivationPromptPendingAsync", gate, StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_onboarding_business_types_endpoint_is_wired()
    {
        var endpoints = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Platform",
            "ExItS.Platform.Api",
            "Personal",
            "PersonalEndpoints.cs"));
        Assert.Contains("/onboarding/business-types", endpoints, StringComparison.Ordinal);
        Assert.Contains("ListActiveForMerchantsAsync", endpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void Plan_capacity_constants_match_wp10b_starter_growth_pro()
    {
        Assert.Equal(1, MvpStarterMax);
        Assert.Equal(3, MvpGrowthMax);
        Assert.Equal(6, MvpProMax);
        // DTO default must not invent stale commercial prices; limits come from API PlanDto.
        var dto = new CommercialPlanDto(
            Guid.NewGuid(),
            "pinoy-business-pos",
            "growth",
            "Growth",
            "Active",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            MaxActiveBusinessTypes: 3);
        Assert.Equal(3, dto.MaxActiveBusinessTypes);
        Assert.Equal(0m, dto.MonthlyPrice);
    }

    private const int MvpStarterMax = 1;
    private const int MvpGrowthMax = 3;
    private const int MvpProMax = 6;

    private static string FindMauiPages() =>
        Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "Components",
            "Pages");

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

        throw new InvalidOperationException("Repository root not found.");
    }
}
