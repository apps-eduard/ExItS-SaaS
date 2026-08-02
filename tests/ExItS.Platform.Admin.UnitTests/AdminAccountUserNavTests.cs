using ExItS.Platform.Admin.Services;

namespace ExItS.Platform.Admin.UnitTests;

public sealed class AdminAccountUserNavTests
{
    [Fact]
    public void Platform_administrator_sees_all_five_accounts_items()
    {
        var items = AdminAccountUserNav.PlatformAccounts(canManagePlatformUsers: true);
        Assert.Equal(5, items.Count);
        Assert.Equal(
            ["all-accounts", "platform-accounts", "organization-accounts", "personal-accounts", "needs-review"],
            items.Select(i => i.Key).ToArray());
        Assert.All(items, i =>
        {
            Assert.True(i.Implemented);
            Assert.False(string.IsNullOrWhiteSpace(i.Route));
        });
        var personal = items.Single(i => i.Key == "personal-accounts");
        Assert.True(personal.Implemented);
        Assert.Equal("/admin/users/personal", personal.Route);
    }

    [Fact]
    public void Platform_support_without_manage_users_sees_no_accounts_items()
    {
        var items = AdminAccountUserNav.PlatformAccounts(canManagePlatformUsers: false);
        Assert.Empty(items);
    }

    [Fact]
    public void Organization_owner_sees_people_with_staff_invitations_customers_and_linking()
    {
        var orgId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var items = AdminAccountUserNav.OrganizationPeople(isOrganizationOwnerOrAdmin: true, orgId);
        Assert.Equal(
            ["org-staff", "org-invitations", "org-customers", "org-customer-linking"],
            items.Select(i => i.Key).ToArray());
        Assert.Contains("/members", items.Single(i => i.Key == "org-staff").Route);
        Assert.Contains("tab=invitations", items.Single(i => i.Key == "org-invitations").Route);
        Assert.Null(items.Single(i => i.Key == "org-customers").Route);
        Assert.False(items.Single(i => i.Key == "org-customers").Implemented);
        Assert.Null(items.Single(i => i.Key == "org-customer-linking").Route);
    }

    [Fact]
    public void Organization_member_does_not_see_owner_only_people_items()
    {
        var orgId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var items = AdminAccountUserNav.OrganizationPeople(isOrganizationOwnerOrAdmin: false, orgId);
        Assert.Equal(["org-customers"], items.Select(i => i.Key).ToArray());
        Assert.DoesNotContain(items, i => i.Key is "org-staff" or "org-invitations" or "org-customer-linking");
        Assert.False(items[0].Implemented);
        Assert.Null(items[0].Route);
    }

    [Fact]
    public void Personal_account_sees_contacts_only()
    {
        var items = AdminAccountUserNav.PersonalContacts();
        Assert.Single(items);
        Assert.Equal("personal-contacts", items[0].Key);
        Assert.Equal("/admin/personal/utang/people", items[0].Route);
        Assert.True(items[0].Implemented);
    }

    [Fact]
    public void Platform_accounts_menu_never_includes_people_or_contacts_keys()
    {
        var items = AdminAccountUserNav.PlatformAccounts(true);
        Assert.DoesNotContain(items, i => i.Key.StartsWith("org-", StringComparison.Ordinal));
        Assert.DoesNotContain(items, i => i.Key.Contains("contact", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(items, i => i.LabelKey is "Nav_People" or "Nav_Contacts");
    }

    [Fact]
    public void Organization_people_menu_never_includes_platform_accounts_or_contacts()
    {
        var items = AdminAccountUserNav.OrganizationPeople(true, Guid.NewGuid());
        Assert.DoesNotContain(items, i => i.Key.Contains("account", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(items, i => i.Key == "personal-contacts");
        Assert.DoesNotContain(items, i => i.Route is not null && i.Route.StartsWith("/admin/users", StringComparison.Ordinal));
    }

    [Fact]
    public void Personal_contacts_menu_never_includes_platform_accounts_or_people()
    {
        var items = AdminAccountUserNav.PersonalContacts();
        Assert.DoesNotContain(items, i => i.Route is not null && i.Route.StartsWith("/admin/users", StringComparison.Ordinal));
        Assert.DoesNotContain(items, i => i.Key.StartsWith("org-", StringComparison.Ordinal));
    }

    [Fact]
    public void Planned_items_are_disabled_shape_with_no_route()
    {
        var platformPlanned = AdminAccountUserNav.PlatformAccounts(true).Where(i => !i.Implemented).ToList();
        Assert.Empty(platformPlanned);

        var orgPlanned = AdminAccountUserNav.OrganizationPeople(true, Guid.NewGuid()).Where(i => !i.Implemented).ToList();
        Assert.All(orgPlanned, i => Assert.Null(i.Route));
        Assert.Contains(orgPlanned, i => i.Key == "org-customers");
        Assert.Contains(orgPlanned, i => i.Key == "org-customer-linking");
    }

    [Fact]
    public void Unauthorized_platform_accounts_are_hidden_not_disabled()
    {
        Assert.Empty(AdminAccountUserNav.PlatformAccounts(canManagePlatformUsers: false));
    }

    [Fact]
    public void Session_scope_labels_are_distinct_for_account_classes()
    {
        Assert.Equal("Platform", AdminAccountUserNav.ScopeLabel(AdminShellMode.Platform));
        Assert.Equal("Organization", AdminAccountUserNav.ScopeLabel(AdminShellMode.Organization));
        Assert.Equal("Personal", AdminAccountUserNav.ScopeLabel(AdminShellMode.Personal));
        Assert.Equal("Limited", AdminAccountUserNav.ScopeLabel(AdminShellMode.Limited));
    }

    [Fact]
    public void Local_validation_identities_map_to_expected_account_user_menus()
    {
        // Mirrors LocalValidationIdentityCatalog roles without referencing Application (Admin stays HTTP-bound).
        var identities = new (string Key, string Class, bool PlatformAdmin, bool OrgOwner)[]
        {
            ("olivia-mendoza", "Platform", true, false),
            ("rafael-torres", "Platform", false, false),
            ("maria-santos", "Organization", false, true),
            ("carlo-reyes", "Organization", false, false),
            ("ana-cruz", "Organization", false, true),
            ("daniel-garcia", "Organization", false, false),
            ("luis-navarro", "Personal", false, false),
            ("sofia-ramos", "Personal", false, false)
        };

        Assert.Equal(8, identities.Length);

        foreach (var identity in identities)
        {
            var menu = identity.Class switch
            {
                "Platform" => AdminAccountUserNav.PlatformAccounts(identity.PlatformAdmin),
                "Organization" => AdminAccountUserNav.OrganizationPeople(identity.OrgOwner, Guid.NewGuid()),
                "Personal" => AdminAccountUserNav.PersonalContacts(),
                _ => Array.Empty<AdminAccountUserNav.Item>()
            };

            switch (identity.Class)
            {
                case "Platform":
                    if (identity.PlatformAdmin)
                    {
                        Assert.Equal(5, menu.Count);
                        Assert.Contains(menu, i => i.Key == "all-accounts");
                        Assert.DoesNotContain(menu, i => i.Key.StartsWith("org-", StringComparison.Ordinal));
                        Assert.DoesNotContain(menu, i => i.Key == "personal-contacts");
                    }
                    else
                    {
                        Assert.Empty(menu);
                    }
                    break;
                case "Organization":
                    if (identity.OrgOwner)
                    {
                        Assert.Contains(menu, i => i.Key == "org-staff");
                        Assert.Contains(menu, i => i.Key == "org-invitations");
                        Assert.Contains(menu, i => i.Key == "org-customers");
                        Assert.Contains(menu, i => i.Key == "org-customer-linking");
                    }
                    else
                    {
                        Assert.Equal(["org-customers"], menu.Select(i => i.Key).ToArray());
                    }

                    Assert.DoesNotContain(menu, i => i.Key.Contains("account", StringComparison.OrdinalIgnoreCase));
                    Assert.DoesNotContain(menu, i => i.Key == "personal-contacts");
                    break;
                case "Personal":
                    Assert.Equal(["personal-contacts"], menu.Select(i => i.Key).ToArray());
                    Assert.DoesNotContain(menu, i => i.Key.StartsWith("org-", StringComparison.Ordinal));
                    Assert.DoesNotContain(menu, i => i.Key.Contains("account", StringComparison.OrdinalIgnoreCase));
                    break;
            }
        }
    }

    [Fact]
    public void AdminNav_markup_uses_accounts_people_contacts_terminology()
    {
        var root = FindRepositoryRoot();
        var nav = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "AdminNav.razor"));
        var model = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Services", "AdminAccountUserNav.cs"));
        Assert.Contains("Nav_Accounts", nav, StringComparison.Ordinal);
        Assert.Contains("AdminAccountUserNav", nav, StringComparison.Ordinal);
        Assert.Contains("Nav_AllAccounts", model, StringComparison.Ordinal);
        Assert.Contains("Nav_PlatformAccounts", model, StringComparison.Ordinal);
        Assert.Contains("Nav_OrganizationAccounts", model, StringComparison.Ordinal);
        Assert.Contains("Nav_PersonalAccounts", model, StringComparison.Ordinal);
        Assert.Contains("Nav_NeedsReview", model, StringComparison.Ordinal);
        Assert.Contains("Nav_OrganizationStaff", model, StringComparison.Ordinal);
        Assert.Contains("Nav_Customers", model, StringComparison.Ordinal);
        Assert.Contains("Nav_CustomerLinking", model, StringComparison.Ordinal);
        Assert.Contains("Nav_Contacts", model, StringComparison.Ordinal);
        Assert.Contains("Nav_People", nav, StringComparison.Ordinal);
        Assert.DoesNotContain("Nav_UnassignedUsers", nav, StringComparison.Ordinal);
        Assert.DoesNotContain("Requires Assignment", nav, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("System Users", nav, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Tenant Users", nav, StringComparison.OrdinalIgnoreCase);
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

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
