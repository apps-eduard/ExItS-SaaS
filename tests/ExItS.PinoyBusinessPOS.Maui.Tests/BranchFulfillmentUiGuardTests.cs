namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class BranchFulfillmentUiGuardTests
{
    [Fact]
    public void Maui_branch_management_is_mobile_card_based()
    {
        var maui = MauiProject();
        var list = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Organization", "Branches.razor"));
        var edit = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Organization", "BranchEdit.razor"));
        Assert.Contains("@page \"/organization/branches\"", list, StringComparison.Ordinal);
        Assert.Contains("Href=\"/manage-business\"", list, StringComparison.Ordinal);
        Assert.Contains("pos-branches__card", list, StringComparison.Ordinal);
        Assert.Contains("pos-branches__title-row", list, StringComparison.Ordinal);
        Assert.Contains("OpenCreate", list, StringComparison.Ordinal);
        Assert.DoesNotContain("ToggleCreate", list, StringComparison.Ordinal);
        Assert.Contains("<button type=\"button\"", list, StringComparison.Ordinal);
        Assert.Contains("Branches_AddAddress", list, StringComparison.Ordinal);
        Assert.DoesNotContain("L[\"Common_Edit\"]", list, StringComparison.Ordinal);
        Assert.DoesNotContain("<table", list, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("@page \"/organization/branches/{BranchId:guid}\"", edit, StringComparison.Ordinal);
        Assert.Contains("BackHref", edit, StringComparison.Ordinal);
        Assert.Contains("branch-settings", edit, StringComparison.Ordinal);
        Assert.DoesNotContain("/org/staff", edit, StringComparison.Ordinal);
        Assert.Contains("PickupEnabled", edit, StringComparison.Ordinal);
        Assert.Contains("DeliveryEnabled", edit, StringComparison.Ordinal);
        Assert.Contains("pos-branches__nav-row", edit, StringComparison.Ordinal);
        Assert.Contains("FocusHours", edit, StringComparison.Ordinal);
        Assert.Contains("FocusDetails", edit, StringComparison.Ordinal);
        Assert.Contains("ToggleDetails", edit, StringComparison.Ordinal);
        Assert.Contains("pos-branches__fold-toggle", edit, StringComparison.Ordinal);
        Assert.DoesNotContain("<details", edit, StringComparison.Ordinal);
        Assert.Contains("FocusFulfillment", edit, StringComparison.Ordinal);
        Assert.Contains("id=\"branch-hours\"", edit, StringComparison.Ordinal);
        Assert.Contains("id=\"branch-fulfillment\"", edit, StringComparison.Ordinal);
        Assert.Contains("Branches_AddAddressLine2", edit, StringComparison.Ordinal);
        Assert.Contains("BranchLocationUi.BindCode", edit, StringComparison.Ordinal);
        Assert.Contains("BranchLocationUi.UnspecifiedCode", edit, StringComparison.Ordinal);
        Assert.Contains("ErrorBoundary", edit, StringComparison.Ordinal);
        Assert.Contains("finally", edit, StringComparison.Ordinal);
        Assert.Contains("Branches_DeliverySetup", edit, StringComparison.Ordinal);
        Assert.Contains("_deliveryOpen", edit, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-branches__text-action", edit, StringComparison.Ordinal);
        Assert.Contains("pos-branches__hours-row", edit, StringComparison.Ordinal);
        Assert.Contains("pos-hours-sheet", edit, StringComparison.Ordinal);
        Assert.Contains("OpenDayEditor", edit, StringComparison.Ordinal);
        Assert.Contains("BranchHoursScheduleUi.ToDto", edit, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-branches__hours-mode", edit, StringComparison.Ordinal);
        Assert.Contains("pos-branches__sticky-actions", edit, StringComparison.Ordinal);
        Assert.Contains("SaveHoursCoreAsync", edit, StringComparison.Ordinal);
        Assert.Contains("ToggleFulfillmentAsync", edit, StringComparison.Ordinal);
        Assert.Contains("CopyMondayToWeekdays", edit, StringComparison.Ordinal);
        Assert.Contains("Branches_CopyHours", edit, StringComparison.Ordinal);
        Assert.Contains("BranchHoursScheduleUi.ShowsTimes", edit, StringComparison.Ordinal);
        Assert.Contains("Branches_OrgCatalog", edit, StringComparison.Ordinal);
        Assert.Contains("Branches_AddressLocation", edit, StringComparison.Ordinal);
        Assert.Contains("env(safe-area-inset-bottom", File.ReadAllText(Path.Combine(maui, "wwwroot", "app.css")), StringComparison.Ordinal);
        Assert.Contains("pos-topbar__subtitle--visible", File.ReadAllText(Path.Combine(maui, "Components", "Shared", "ShellOrganizationIdentity.razor")), StringComparison.Ordinal);
        Assert.DoesNotContain("Copy products from Main", edit, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PickupEnabled = true", File.ReadAllText(Path.Combine(maui, "..", "ExItS.PinoyBusinessPOS.Application", "Platform", "PlatformAccessModels.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void Branch_fulfillment_strings_exist_in_en_and_fil()
    {
        var localization = Path.Combine(MauiProject(), "Localization");
        var en = File.ReadAllText(Path.Combine(localization, "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(localization, "PosResources.fil-PH.resx"));
        foreach (var key in new[]
                 {
                     "Branches_Title",
                     "Branches_ListSection",
                     "Branches_Pickup",
                     "Branches_Delivery",
                     "Branches_DeliverySettings",
                     "Branches_MinimumOrder",
                     "Branches_BaseFee",
                     "Branches_IncludedKm",
                     "Branches_AdditionalPerKm",
                     "Branches_MaximumKm",
                     "Branches_Setup",
                     "Branches_OrgCatalog",
                     "Branches_OrgCustomers",
                     "Branches_CreateHint",
                     "Branches_InventoryNotCopied",
                     "Common_Edit",
                     "Branches_AddAddress",
                     "Branches_CapacityFull",
                     "Branches_OrgWide",
                     "Branches_TimeZoneHint",
                     "Branches_AddressLocation",
                     "Branches_CopyWeekdays",
                     "Branches_CopyHours",
                     "Branches_ApplyToOtherDays",
                     "Branches_HoursDone",
                     "Branches_HoursStart",
                     "Branches_HoursEnd",
                     "Branches_DayFull_Monday",
                     "Branches_Configure",
                     "Branches_StatusOff",
                     "Branches_SaveActions",
                     "Branches_InventorySetUp",
                     "Branches_SetHours",
                     "Branches_AddAddressLine2",
                     "Branches_MissingShort",
                     "Branches_DeliverySetup",
                     "Branches_Country",
                     "Branches_Catalog"
                 })
        {
            Assert.Contains($"<data name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"<data name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    private static string MauiProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("MAUI project not found.");
    }
}
