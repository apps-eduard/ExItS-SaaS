namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class RegistersPageGuardTests
{
    [Fact]
    public void Registers_routes_cover_list_detail_create_edit_and_activity()
    {
        var pages = RegistersPagesDirectory();

        var list = File.ReadAllText(Path.Combine(pages, "RegistersList.razor"));
        Assert.Contains("@page \"/registers\"", list, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewRegisters", list, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManageRegisters", list, StringComparison.Ordinal);
        Assert.Contains("Registers_MainBadge", list, StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(pages, "RegisterDetail.razor"));
        Assert.Contains("@page \"/registers/{RegisterId:guid}\"", detail, StringComparison.Ordinal);
        Assert.Contains("GetActivityAsync", detail, StringComparison.Ordinal);
        Assert.Contains("ActivateAsync", detail, StringComparison.Ordinal);
        Assert.Contains("DeactivateAsync", detail, StringComparison.Ordinal);

        var create = File.ReadAllText(Path.Combine(pages, "RegisterCreate.razor"));
        Assert.Contains("@page \"/registers/new\"", create, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManageRegisters", create, StringComparison.Ordinal);

        var edit = File.ReadAllText(Path.Combine(pages, "RegisterEdit.razor"));
        Assert.Contains("@page \"/registers/{RegisterId:guid}/edit\"", edit, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManageRegisters", edit, StringComparison.Ordinal);
    }

    [Fact]
    public void MoreHub_gates_registers_with_view_capability()
    {
        var more = File.ReadAllText(Path.Combine(PagesDirectory(), "MoreHub.razor"));
        Assert.Contains("UtangCapability.ViewRegisters", more, StringComparison.Ordinal);
        Assert.Contains("GoRegisters", more, StringComparison.Ordinal);
    }

    private static string RegistersPagesDirectory() => Path.Combine(PagesDirectory(), "Registers");

    private static string PagesDirectory() => Path.Combine(
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
