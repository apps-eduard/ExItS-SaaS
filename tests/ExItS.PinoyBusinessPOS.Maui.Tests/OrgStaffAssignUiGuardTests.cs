namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class OrgStaffAssignUiGuardTests
{
    [Fact]
    public void Assign_page_is_compact_and_uses_role_cards_not_raw_guid_fields()
    {
        var page = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Organization", "OrgStaffAssign.razor"));
        var en = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.fil-PH.resx"));

        Assert.Contains("@page \"/org/staff/assign\"", page, StringComparison.Ordinal);
        Assert.Contains("pos-assign__back", page, StringComparison.Ordinal);
        Assert.Contains("Org_AssignTitle", page, StringComparison.Ordinal);
        Assert.Contains("Org_AssignUserSection", page, StringComparison.Ordinal);
        Assert.Contains("GetUserAsync", page, StringComparison.Ordinal);
        Assert.Contains("DisplayNameQuery", page, StringComparison.Ordinal);
        Assert.Contains("EmailQuery", page, StringComparison.Ordinal);
        Assert.Contains("Org_StaffUnknownName", page, StringComparison.Ordinal);
        Assert.Contains("GetProductLocalRolesAsync", page, StringComparison.Ordinal);
        Assert.Contains("Avatar", page, StringComparison.Ordinal);
        Assert.Contains("_displayName", page, StringComparison.Ordinal);
        Assert.Contains("_email", page, StringComparison.Ordinal);
        Assert.Contains("role=\"radiogroup\"", page, StringComparison.Ordinal);
        Assert.Contains("role=\"radio\"", page, StringComparison.Ordinal);
        Assert.Contains("aria-checked", page, StringComparison.Ordinal);
        Assert.Contains("RoleOwner = \"Owner\"", page, StringComparison.Ordinal);
        Assert.Contains("RoleManager = \"Manager\"", page, StringComparison.Ordinal);
        Assert.Contains("RoleCashier = \"Cashier\"", page, StringComparison.Ordinal);
        Assert.Contains("Org_RoleOwnerDesc", page, StringComparison.Ordinal);
        Assert.Contains("Org_RoleManagerDesc", page, StringComparison.Ordinal);
        Assert.Contains("Org_RoleCashierDesc", page, StringComparison.Ordinal);
        Assert.Contains("CanSubmit", page, StringComparison.Ordinal);
        Assert.Contains("Disabled=\"@(!CanSubmit)\"", page, StringComparison.Ordinal);
        Assert.Contains("if (_busy)", page, StringComparison.Ordinal);
        Assert.Contains("finally", page, StringComparison.Ordinal);
        Assert.Contains("_busy = false", page, StringComparison.Ordinal);
        Assert.Contains("Org_AssignOwnerConfirmTitle", page, StringComparison.Ordinal);
        Assert.Contains("AssignProductLocalRoleAsync", page, StringComparison.Ordinal);
        Assert.Contains("ButtonVariant.Primary", page, StringComparison.Ordinal);
        Assert.Contains("Org_AssignSubmit", page, StringComparison.Ordinal);
        Assert.Contains("safe-area-inset-bottom", page, StringComparison.Ordinal);
        Assert.Contains("Nav.NavigateTo(\"/org/staff\")", page, StringComparison.Ordinal);

        Assert.DoesNotContain("Org_AssignUserId", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Org_AssignRoleHint", page, StringComparison.Ordinal);
        Assert.DoesNotContain("@bind-Value=\"_userIdText\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("@bind-Value=\"_roleCode\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("RoleManager = \"StoreManager\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("_roleCode = \"StoreManager\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", page, StringComparison.Ordinal);
        Assert.DoesNotContain("ToString(\"D\")", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Org_AssignUserLoadFailed", page, StringComparison.Ordinal);
        Assert.DoesNotContain("_userLoadError = L[\"Org_AssignUserLoadFailed\"]", page, StringComparison.Ordinal);

        foreach (var key in new[]
                 {
                     "Org_AssignUserSection", "Org_AssignRoleSection",
                     "Org_RoleOwnerDesc", "Org_RoleManagerDesc", "Org_RoleCashierDesc",
                     "Org_AssignSelectRole", "Org_AssignUserLoadFailed",
                     "Org_AssignOwnerConfirmTitle", "Org_AssignOwnerConfirmMessage",
                     "Org_AssignOwnerConfirmAction"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    private static string MauiProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Products", "PinoyBusinessPOS",
                "ExItS.PinoyBusinessPOS.Maui");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate PinoyBusinessPOS.Maui project.");
    }
}
