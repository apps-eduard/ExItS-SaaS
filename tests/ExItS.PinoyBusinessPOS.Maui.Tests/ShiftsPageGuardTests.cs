namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class ShiftsPageGuardTests
{
    [Fact]
    public void Shift_routes_cover_list_open_and_detail()
    {
        var pages = ShiftsPagesDirectory();

        var list = File.ReadAllText(Path.Combine(pages, "ShiftsList.razor"));
        Assert.Contains("@page \"/shifts\"", list, StringComparison.Ordinal);
        Assert.Contains("IPosCashierShiftClient", list, StringComparison.Ordinal);

        var open = File.ReadAllText(Path.Combine(pages, "ShiftOpen.razor"));
        Assert.Contains("@page \"/shifts/open\"", open, StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(pages, "ShiftDetail.razor"));
        Assert.Contains("@page \"/shifts/{ShiftId:guid}\"", detail, StringComparison.Ordinal);
        Assert.Contains("CloseAsync", detail, StringComparison.Ordinal);
        Assert.Contains("RecordMovementAsync", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Shift_pages_guard_entry_and_gate_capabilities()
    {
        foreach (var file in Directory.EnumerateFiles(ShiftsPagesDirectory(), "*.razor"))
        {
            var text = File.ReadAllText(file);
            Assert.Contains("Gate.CanEnterProtectedShell", text, StringComparison.Ordinal);
            Assert.Contains("ResolveStartRouteAsync", text, StringComparison.Ordinal);
            Assert.Contains("UtangCapability.ViewShifts", text, StringComparison.Ordinal);
            Assert.Contains("UtangCapability.ManageShifts", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Shift_pages_are_online_only()
    {
        foreach (var file in Directory.EnumerateFiles(ShiftsPagesDirectory(), "*.razor"))
        {
            var text = File.ReadAllText(file);
            Assert.Contains("Connectivity.IsConnectedAsync", text, StringComparison.Ordinal);
            Assert.Contains("Shifts_Offline", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Checkout_page_requires_open_shift()
    {
        var checkout = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "Components",
            "Pages",
            "Sales",
            "SaleCheckout.razor"));
        Assert.Contains("GetCurrentAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("Shifts_RequiredMessage", checkout, StringComparison.Ordinal);
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
