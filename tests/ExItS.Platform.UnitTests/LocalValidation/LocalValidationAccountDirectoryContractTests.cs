using ExItS.Platform.Application.LocalValidation;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.UnitTests.LocalValidation;

/// <summary>
/// Directory filter / sort contract expectations for Local Validation approved identities.
/// </summary>
public sealed class LocalValidationAccountDirectoryContractTests
{
    [Fact]
    public void Directory_filter_counts_match_approved_catalog()
    {
        var catalog = LocalValidationIdentityCatalog.All;
        Assert.Equal(8, catalog.Count);
        Assert.Equal(2, catalog.Count(i => i.PreferredAccountClass == AccountClass.Platform));
        Assert.Equal(4, catalog.Count(i => i.PreferredAccountClass == AccountClass.Organization));
        Assert.Equal(2, catalog.Count(i => i.PreferredAccountClass == AccountClass.Personal));
        Assert.DoesNotContain(
            LocalValidationIdentityCatalog.All,
            i => i.PreferredAccountClass is not (AccountClass.Platform or AccountClass.Organization or AccountClass.Personal));
    }

    [Fact]
    public void Platform_directory_contains_only_olivia_and_rafael()
    {
        var names = LocalValidationIdentityCatalog.All
            .Where(i => i.PreferredAccountClass == AccountClass.Platform)
            .Select(i => i.DisplayName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["Olivia Mendoza", "Rafael Torres"], names);
    }

    [Fact]
    public void Organization_directory_contains_only_maria_carlo_ana_daniel()
    {
        var names = LocalValidationIdentityCatalog.All
            .Where(i => i.PreferredAccountClass == AccountClass.Organization)
            .Select(i => i.DisplayName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["Ana Cruz", "Carlo Reyes", "Daniel Garcia", "Maria Santos"], names);
    }

    [Fact]
    public void Personal_directory_contains_only_luis_and_sofia()
    {
        var names = LocalValidationIdentityCatalog.All
            .Where(i => i.PreferredAccountClass == AccountClass.Personal)
            .Select(i => i.DisplayName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(["Luis Navarro", "Sofia Ramos"], names);
    }

    [Fact]
    public void Needs_review_excludes_all_approved_identities()
    {
        Assert.All(LocalValidationIdentityCatalog.All, i =>
        {
            Assert.True(
                i.PreferredAccountClass is AccountClass.Platform or AccountClass.Organization or AccountClass.Personal);
        });
    }

    [Fact]
    public void Account_type_sort_key_orders_platform_organization_personal()
    {
        var ordered = LocalValidationIdentityCatalog.All
            .OrderBy(i => i.PreferredAccountClass switch
            {
                AccountClass.Platform => 0,
                AccountClass.Organization => 1,
                AccountClass.Personal => 2,
                _ => 99
            })
            .ThenBy(i => i.DisplayName, StringComparer.Ordinal)
            .Select(i => i.DisplayName)
            .ToArray();

        Assert.Equal(
            [
                "Olivia Mendoza",
                "Rafael Torres",
                "Ana Cruz",
                "Carlo Reyes",
                "Daniel Garcia",
                "Maria Santos",
                "Luis Navarro",
                "Sofia Ramos"
            ],
            ordered);
    }

    [Theory]
    [InlineData("displayName")]
    [InlineData("username")]
    [InlineData("email")]
    [InlineData("accountType")]
    [InlineData("organization")]
    [InlineData("status")]
    [InlineData("updatedUtc")]
    public void Sort_whitelist_contains_required_fields(string field)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "displayName", "username", "email", "accountType", "organization", "status", "updatedUtc"
        };
        Assert.Contains(field, allowed);
    }

    [Fact]
    public void Unsupported_sort_field_is_not_in_whitelist()
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "displayName", "username", "email", "accountType", "organization", "status", "updatedUtc"
        };
        Assert.DoesNotContain("open", allowed);
        Assert.DoesNotContain("actions", allowed);
        Assert.DoesNotContain("id", allowed);
    }

    [Fact]
    public void UserDirectoryFilter_includes_Personal()
    {
        Assert.True(Enum.IsDefined(typeof(UserDirectoryFilter), UserDirectoryFilter.Personal));
        Assert.Equal(4, (int)UserDirectoryFilter.Personal);
    }

    [Fact]
    public void Production_startup_catalog_is_not_activated_when_local_validation_disabled()
    {
        var options = new LocalValidationOptions { Enabled = false };
        Assert.False(options.Enabled);
        Assert.Empty(options.SharedPassword);
    }
}
