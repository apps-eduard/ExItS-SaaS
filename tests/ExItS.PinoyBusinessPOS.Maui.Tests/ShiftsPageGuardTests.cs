namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class ShiftsPageGuardTests
{
    [Fact]
    public void Shifts_list_covers_current_open_history_filters_and_errors()
    {
        var list = File.ReadAllText(Path.Combine(ShiftsPagesDirectory(), "ShiftsList.razor"));
        Assert.Contains("@page \"/shifts\"", list, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewShifts", list, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManageShifts", list, StringComparison.Ordinal);
        Assert.Contains("GetCurrentAsync", list, StringComparison.Ordinal);
        Assert.Contains("ListAsync", list, StringComparison.Ordinal);
        Assert.Contains("OnRetry", list, StringComparison.Ordinal);
        Assert.Contains("Shifts_CurrentSection", list, StringComparison.Ordinal);
        Assert.Contains("Shifts_HistorySection", list, StringComparison.Ordinal);
        Assert.Contains("Shifts_OpenCurrent", list, StringComparison.Ordinal);
        Assert.Contains("GoOpenAsync", list, StringComparison.Ordinal);
        Assert.Contains("canManage && !hasOpenShift", list, StringComparison.Ordinal);
        Assert.DoesNotContain("Shifts_AlreadyOpen", list, StringComparison.Ordinal);
        Assert.Contains("pos-shifts__row", list, StringComparison.Ordinal);
        Assert.Contains("pos-shifts__chevron", list, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"pos-link\"", list, StringComparison.Ordinal);
        Assert.DoesNotContain("ResponsiveDataList", list, StringComparison.Ordinal);
        Assert.DoesNotContain("Shifts_FiltersTitle", list, StringComparison.Ordinal);
        Assert.Contains("Task.Delay(250", list, StringComparison.Ordinal);
        Assert.Contains("_isOffline", list, StringComparison.Ordinal);
        Assert.Contains("ConnectivityChanged", list, StringComparison.Ordinal);
        Assert.Contains("EmptyState", list, StringComparison.Ordinal);
        Assert.Contains("ErrorState", list, StringComparison.Ordinal);
        Assert.Contains("Shifts_OpenedFormat", list, StringComparison.Ordinal);
        Assert.Contains("RegisterName", list, StringComparison.Ordinal);
        Assert.DoesNotContain("ShiftId.ToString", list, StringComparison.Ordinal);
        Assert.Contains("aria-label", list, StringComparison.Ordinal);
        Assert.Contains("role=\"status\"", list, StringComparison.Ordinal);
    }

    [Fact]
    public void Shifts_list_hides_open_action_when_shift_already_open()
    {
        var list = File.ReadAllText(Path.Combine(ShiftsPagesDirectory(), "ShiftsList.razor"));
        Assert.Contains("hasOpenShift", list, StringComparison.Ordinal);
        Assert.Contains("canManage && !hasOpenShift", list, StringComparison.Ordinal);
        Assert.DoesNotContain("Shifts_AlreadyOpen", list, StringComparison.Ordinal);
        Assert.Contains("_current is not null", list, StringComparison.Ordinal);
        Assert.Contains("GetCurrentAsync", list, StringComparison.Ordinal);
        // Duplicate open blocked before navigate.
        Assert.Contains("Nav.NavigateTo(\"/shifts/open\")", list, StringComparison.Ordinal);
        Assert.Contains("if (_openingNav || _isOffline || _current is not null", list, StringComparison.Ordinal);
    }

    [Fact]
    public void Shifts_list_current_card_shows_number_register_status_opened_time()
    {
        var list = File.ReadAllText(Path.Combine(ShiftsPagesDirectory(), "ShiftsList.razor"));
        Assert.Contains("_current.ShiftNumber", list, StringComparison.Ordinal);
        Assert.Contains("RegisterLabel(_current)", list, StringComparison.Ordinal);
        Assert.Contains("StatusLabel(_current.Status)", list, StringComparison.Ordinal);
        Assert.Contains("pos-shifts__current-top", list, StringComparison.Ordinal);
        Assert.Contains("pos-shifts__current-register", list, StringComparison.Ordinal);
        Assert.Contains("FormatOpenedDisplay", list, StringComparison.Ordinal);
        Assert.Contains("CurrentCashierName", list, StringComparison.Ordinal);
        Assert.Contains("Shifts_OpenedFormat", list, StringComparison.Ordinal);
        Assert.Contains("GoCurrent", list, StringComparison.Ordinal);
        Assert.Contains("$\"/shifts/{_current.ShiftId:D}\"", list, StringComparison.Ordinal);
    }

    [Fact]
    public void Shifts_list_refresh_and_filters_preserve_query_and_offline_rules()
    {
        var list = File.ReadAllText(Path.Combine(ShiftsPagesDirectory(), "ShiftsList.razor"));
        Assert.Contains("_statusFilter", list, StringComparison.Ordinal);
        Assert.Contains("_search", list, StringComparison.Ordinal);
        Assert.Contains("shiftNumber:", list, StringComparison.Ordinal);
        Assert.Contains("status:", list, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(!onlineActionsEnabled)\"", list, StringComparison.Ordinal);
        Assert.Contains("Disabled=\"@_isOffline\"", list, StringComparison.Ordinal);
        Assert.Contains("Shifts_Status_Open", list, StringComparison.Ordinal);
        Assert.Contains("Shifts_Status_Closed", list, StringComparison.Ordinal);
        Assert.Contains("Shifts_Status_Cancelled", list, StringComparison.Ordinal);
    }

    [Fact]
    public void Shifts_open_and_detail_gate_manage_and_close_flow()
    {
        var open = File.ReadAllText(Path.Combine(ShiftsPagesDirectory(), "ShiftOpen.razor"));
        Assert.Contains("@page \"/shifts/open\"", open, StringComparison.Ordinal);
        Assert.Contains("ListAvailableForShiftAsync", open, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManageShifts", open, StringComparison.Ordinal);
        Assert.Contains("ListAsync", open, StringComparison.Ordinal);
        Assert.Contains("Shifts_NoRegisterTitle", open, StringComparison.Ordinal);
        Assert.DoesNotContain("_available.Count == 0 && !_loadingRegisters", open, StringComparison.Ordinal);
        Assert.Contains("GetCurrentAsync", open, StringComparison.Ordinal);
        Assert.Contains("finally", open, StringComparison.Ordinal);
        Assert.Contains("_saving = false", open, StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(ShiftsPagesDirectory(), "ShiftDetail.razor"));
        Assert.Contains("@page \"/shifts/{ShiftId:guid}\"", detail, StringComparison.Ordinal);
        Assert.Contains("CloseAsync", detail, StringComparison.Ordinal);
        Assert.Contains("GetSummaryAsync", detail, StringComparison.Ordinal);
        Assert.Contains("ExpectedCashAmount", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void MoreHub_gates_shifts_with_view_capability()
    {
        var more = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "Components",
            "Pages",
            "MoreHub.razor"));
        Assert.Contains("UtangCapability.ViewShifts", more, StringComparison.Ordinal);
        Assert.Contains("GoShifts", more, StringComparison.Ordinal);
    }

    [Fact]
    public void Shifts_localization_covers_compact_list_keys()
    {
        var en = File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui",
            "Localization", "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui",
            "Localization", "PosResources.fil-PH.resx"));

        foreach (var key in new[]
                 {
                     "Shifts_AlreadyOpen",
                     "Shifts_HistorySection",
                     "Shifts_OpenedFormat",
                     "Shifts_ClosedFormat",
                     "Shifts_ClearSearch",
                     "Shifts_Subtitle",
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Shifts_compact_css_exists()
    {
        var css = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "wwwroot",
            "app.css"));
        Assert.Contains(".pos-shifts", css, StringComparison.Ordinal);
        Assert.Contains(".pos-shifts__current", css, StringComparison.Ordinal);
        Assert.Contains(".pos-shifts__current-top", css, StringComparison.Ordinal);
        Assert.Contains(".pos-shifts__current-register", css, StringComparison.Ordinal);
        Assert.Contains(".pos-shifts__row", css, StringComparison.Ordinal);
        Assert.Contains(".pos-shifts__badge--open", css, StringComparison.Ordinal);
    }

    private static string ShiftsPagesDirectory() => Path.Combine(
        FindRepoRoot(),
        "src",
        "Products",
        "PinoyBusinessPOS",
        "ExItS.PinoyBusinessPOS.Maui",
        "Components",
        "Pages",
        "Shifts");

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
