using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Web.Services;
using Xunit;

namespace ExItS.PinoyBusinessPOS.Web.Tests;

public sealed class OrgWebAuthErrorAndBranchesGuardTests
{
    [Fact]
    public void OrgWebUi_sanitizes_development_actor_and_permission_codes()
    {
        var actor = OrgWebUi.Error(new ApiError(
            "Forbidden",
            "Actor 'development-operator:unauthenticated' does not hold permission 'platform.permission.view_portfolio'",
            "forbidden",
            null,
            403));
        // view_portfolio fallthrough after missing PlatformSession → session recovery, not "permission".
        Assert.Contains("verify your access", actor, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("development-operator", actor, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("view_portfolio", actor, StringComparison.OrdinalIgnoreCase);

        var session = OrgWebUi.Error(new ApiError(
            "Unauthorized",
            "Session token is expired or invalid.",
            "unauthorized",
            null,
            401));
        Assert.Equal("Your session has expired. Please sign in again.", session);
    }

    [Fact]
    public void Branches_page_uses_modal_form_and_sanitized_errors()
    {
        var root = FindRepoRoot();
        var page = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Components",
            "Pages",
            "Organization",
            "Branches.razor"));

        Assert.Contains("Branches_Title", page, StringComparison.Ordinal);
        Assert.Contains("Branches_Subtitle", page, StringComparison.Ordinal);
        Assert.Contains("<Modal", page, StringComparison.Ordinal);
        Assert.Contains("OrgWebUi.Error", page, StringComparison.Ordinal);
        Assert.Contains("Branches_EmptyTitle", page, StringComparison.Ordinal);
        Assert.Contains("Branches_PosContextNote", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Location-specific operational state", page, StringComparison.Ordinal);
        Assert.DoesNotContain("view_portfolio", page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Branch_edit_setup_panel_does_not_copy_catalog_or_stock()
    {
        var root = FindRepoRoot();
        var edit = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Components",
            "Pages",
            "Organization",
            "BranchEdit.razor"));
        Assert.Contains("Branches_Setup", edit, StringComparison.Ordinal);
        Assert.Contains("Branches_OrgCatalog", edit, StringComparison.Ordinal);
        Assert.Contains("Branches_InventoryNotCopied", edit, StringComparison.Ordinal);
        Assert.Contains("Branches_AddressLocation", edit, StringComparison.Ordinal);
        Assert.Contains("org-hours-row", edit, StringComparison.Ordinal);
        Assert.Contains("OpenDayEditor", edit, StringComparison.Ordinal);
        Assert.Contains("<Modal", edit, StringComparison.Ordinal);
        Assert.Contains("BranchHoursScheduleUi.ToDto", edit, StringComparison.Ordinal);
        Assert.Contains("org-branch-edit__sticky", edit, StringComparison.Ordinal);
        Assert.Contains("SaveHoursCoreAsync", edit, StringComparison.Ordinal);
        Assert.Contains("ToggleFulfillmentAsync", edit, StringComparison.Ordinal);
        Assert.Contains("CopyMondayToWeekdays", edit, StringComparison.Ordinal);
        Assert.Contains("Branches_CopyHours", edit, StringComparison.Ordinal);
        Assert.Contains("BranchHoursScheduleUi.ShowsTimes", edit, StringComparison.Ordinal);
        Assert.Contains("PickupReady", edit, StringComparison.Ordinal);
        Assert.Contains("DeliveryReady", edit, StringComparison.Ordinal);
        Assert.DoesNotContain("Copy products from Main", edit, StringComparison.OrdinalIgnoreCase);

        var loc = Path.Combine(root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Web", "Localization");
        var en = File.ReadAllText(Path.Combine(loc, "OrgWebResources.resx"));
        var fil = File.ReadAllText(Path.Combine(loc, "OrgWebResources.fil-PH.resx"));
        foreach (var key in new[] { "Branches_AddressLocation", "Branches_CopyWeekdays", "Branches_CopyHours", "Branches_HoursDone", "Branches_DayFull_Monday", "Branches_TimeZoneHint", "Branches_SaveActions", "Branches_StatusOff", "Branches_InventorySetUp" })
        {
            Assert.Contains($"<data name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"<data name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Dev_platform_handler_preserves_platform_session()
    {
        var root = FindRepoRoot();
        var handler = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.ApiClient",
            "DevPlatformUserHeaderHandler.cs"));
        var posAuth = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Services",
            "WebHostServices.cs"));

        Assert.Contains("PlatformSession", handler, StringComparison.Ordinal);
        Assert.Contains("development-operator:unauthenticated", handler, StringComparison.Ordinal);
        Assert.Contains("IsPlatformApiPath", posAuth, StringComparison.Ordinal);
        Assert.Contains("OrgWebPosAuthHeaderHandler", posAuth, StringComparison.Ordinal);
    }

    [Fact]
    public void Org_web_typed_platform_client_registers_session_handlers()
    {
        var root = FindRepoRoot();
        var program = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Program.cs"));

        Assert.Contains("AddHttpClient<IPosApiClient, PosApiClient>", program, StringComparison.Ordinal);
        Assert.Contains("OrgWebCircuitSessionHeaderHandler", program, StringComparison.Ordinal);
        Assert.Contains("OrgWebPosAuthHandlerFilter", program, StringComparison.Ordinal);
        Assert.Contains("PlatformSessionHeaderHandler", program, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Repository root not found.");
    }
}
