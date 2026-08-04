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
