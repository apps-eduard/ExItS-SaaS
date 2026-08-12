namespace ExItS.Platform.Admin.UnitTests;

/// <summary>P24-WP12: source guards for Personal Features Admin configuration surface.</summary>
public sealed class P24Wp12PersonalFeaturesAdminGuardTests
{
    [Fact]
    public void Personal_features_page_exists_and_uses_antdesign_permissions()
    {
        var root = FindRepositoryRoot();
        var page = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "PersonalFeatures.razor"));

        Assert.Contains("@page \"/admin/personal-features\"", page, StringComparison.Ordinal);
        Assert.Contains("ViewPortfolio", page, StringComparison.Ordinal);
        Assert.Contains("ManageCatalog", page, StringComparison.Ordinal);
        Assert.Contains("@using AntDesign", page, StringComparison.Ordinal);
        Assert.Contains("GetPersonalFeatureDefinitionsAsync", page, StringComparison.Ordinal);
        Assert.Contains("UpdatePersonalFeatureDefinitionAsync", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Tailwind", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FluentUI", page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Personal_features_ui_does_not_hard_code_reward_prices_or_durations()
    {
        var root = FindRepositoryRoot();
        var page = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "PersonalFeatures.razor"));

        Assert.DoesNotContain("RewardPointsPrice = 100", page, StringComparison.Ordinal);
        Assert.DoesNotContain("RewardPointsPrice = 150", page, StringComparison.Ordinal);
        Assert.DoesNotContain("DefaultEntitlementDurationDays = 30", page, StringComparison.Ordinal);
        Assert.Contains("_editRewardPrice", page, StringComparison.Ordinal);
        Assert.Contains("_editDurationDays", page, StringComparison.Ordinal);
        Assert.Contains("item.RewardPointsPrice", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_features_api_client_uses_platform_personal_feature_routes()
    {
        var root = FindRepositoryRoot();
        var client = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Services", "PlatformApiClient.cs"));
        var iface = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Services", "IPlatformApiClient.cs"));

        Assert.Contains("/api/v1/platform/personal/features", client, StringComparison.Ordinal);
        Assert.Contains("GetPersonalFeatureDefinitionsAsync", iface, StringComparison.Ordinal);
        Assert.Contains("UpdatePersonalFeatureDefinitionAsync", iface, StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_features_page_keeps_feature_code_read_only()
    {
        var root = FindRepositoryRoot();
        var page = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "PersonalFeatures.razor"));

        Assert.Contains("Id=\"pf-code\"", page, StringComparison.Ordinal);
        Assert.Contains("Value=\"@item.FeatureCode\" Disabled", page, StringComparison.Ordinal);
        Assert.DoesNotContain("@bind-Value=\"_editFeatureCode\"", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_feature_admin_api_requires_view_portfolio_and_manage_catalog()
    {
        var root = FindRepositoryRoot();
        var endpoints = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Api", "Personal", "PersonalFeatureAdminEndpoints.cs"));

        Assert.Contains("MapGroup(\"/api/v1/platform/personal/features\")", endpoints, StringComparison.Ordinal);
        Assert.Contains("PlatformPermission.ViewPortfolio", endpoints, StringComparison.Ordinal);
        Assert.Contains("PlatformPermission.ManageCatalog", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapPatch", endpoints, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
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
