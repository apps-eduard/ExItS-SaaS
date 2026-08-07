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
        Assert.Contains("pos-registers__row", list, StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(pages, "RegisterDetail.razor"));
        Assert.Contains("@page \"/registers/{RegisterId:guid}\"", detail, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack Href=\"/registers\"", detail, StringComparison.Ordinal);
        Assert.Contains("GetActivityAsync", detail, StringComparison.Ordinal);
        Assert.Contains("ActivateAsync", detail, StringComparison.Ordinal);
        Assert.Contains("DeactivateAsync", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("Registers_BackToList", detail, StringComparison.Ordinal);

        var create = File.ReadAllText(Path.Combine(pages, "RegisterCreate.razor"));
        Assert.Contains("@page \"/registers/new\"", create, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack Href=\"/registers\"", create, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManageRegisters", create, StringComparison.Ordinal);
        Assert.Contains("Registers_Cancel", create, StringComparison.Ordinal);

        var edit = File.ReadAllText(Path.Combine(pages, "RegisterEdit.razor"));
        Assert.Contains("@page \"/registers/{RegisterId:guid}/edit\"", edit, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack Href=\"/registers\"", edit, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManageRegisters", edit, StringComparison.Ordinal);
        Assert.Contains("Registers_Cancel", edit, StringComparison.Ordinal);
        Assert.DoesNotContain("Registers_BackToList", edit, StringComparison.Ordinal);
    }

    [Fact]
    public void Registers_list_uses_compact_toolbar_segments_and_rows()
    {
        var list = File.ReadAllText(Path.Combine(RegistersPagesDirectory(), "RegistersList.razor"));
        Assert.Contains("pos-registers__title", list, StringComparison.Ordinal);
        Assert.Contains("Registers_Subtitle", list, StringComparison.Ordinal);
        Assert.Contains("pos-registers__add", list, StringComparison.Ordinal);
        Assert.Contains("Registers_Add", list, StringComparison.Ordinal);
        Assert.Contains("pos-registers__toolbar", list, StringComparison.Ordinal);
        Assert.Contains("pos-registers__search-input", list, StringComparison.Ordinal);
        Assert.Contains("Registers_ClearSearch", list, StringComparison.Ordinal);
        Assert.Contains("pos-registers__refresh", list, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"refresh\")", list, StringComparison.Ordinal);
        Assert.Contains("role=\"radiogroup\"", list, StringComparison.Ordinal);
        Assert.Contains("pos-registers__segment", list, StringComparison.Ordinal);
        Assert.Contains("Registers_Filter_All", list, StringComparison.Ordinal);
        Assert.Contains("Registers_Status_Active", list, StringComparison.Ordinal);
        Assert.Contains("Registers_Status_Inactive", list, StringComparison.Ordinal);
        Assert.Contains("pos-registers__row", list, StringComparison.Ordinal);
        Assert.Contains("pos-registers__badge", list, StringComparison.Ordinal);
        Assert.Contains("RegisterCode", list, StringComparison.Ordinal);
        Assert.Contains("HasOpenShift", list, StringComparison.Ordinal);
        Assert.Contains("Registers_OpenShiftHint", list, StringComparison.Ordinal);
        Assert.Contains("pos-registers__chevron", list, StringComparison.Ordinal);
        Assert.Contains("GoDetail", list, StringComparison.Ordinal);
        Assert.DoesNotContain("ResponsiveDataList", list, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"pos-link\"", list, StringComparison.Ordinal);
        Assert.DoesNotContain("Registers_Col_Name", list, StringComparison.Ordinal);
        Assert.DoesNotContain("Registers_FiltersTitle", list, StringComparison.Ordinal);
        Assert.DoesNotContain("RegisterId.ToString", list, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", list, StringComparison.Ordinal);
        Assert.DoesNotContain("SearchToolbar", list, StringComparison.Ordinal);
    }

    [Fact]
    public void Registers_search_filter_paging_and_offline_preserve_query_behavior()
    {
        var list = File.ReadAllText(Path.Combine(RegistersPagesDirectory(), "RegistersList.razor"));
        Assert.Contains("Task.Delay(250", list, StringComparison.Ordinal);
        Assert.Contains("_page = 1", list, StringComparison.Ordinal);
        Assert.Contains("ListAsync", list, StringComparison.Ordinal);
        Assert.Contains("name: _search", list, StringComparison.Ordinal);
        Assert.Contains("status: string.IsNullOrWhiteSpace(_statusFilter)", list, StringComparison.Ordinal);
        Assert.Contains("TotalPages > 1", list, StringComparison.Ordinal);
        Assert.Contains("Pagination", list, StringComparison.Ordinal);
        Assert.Contains("Disabled=\"@_isOffline\"", list, StringComparison.Ordinal);
        Assert.Contains("ConnectivityChanged", list, StringComparison.Ordinal);
        Assert.Contains("onlineActionsEnabled", list, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(!onlineActionsEnabled)\"", list, StringComparison.Ordinal);
        Assert.Contains("Disabled=\"@_isOffline\"", list, StringComparison.Ordinal);
        Assert.Contains("Registers_OfflineMessage", list, StringComparison.Ordinal);
        Assert.Contains("EmptyState", list, StringComparison.Ordinal);
        Assert.Contains("Registers_NoMatchTitle", list, StringComparison.Ordinal);
        Assert.Contains("ErrorState", list, StringComparison.Ordinal);
        Assert.Contains("OnRetry", list, StringComparison.Ordinal);
        Assert.Contains("role=\"status\"", list, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", list, StringComparison.Ordinal);
    }

    [Fact]
    public void MoreHub_gates_registers_with_view_capability()
    {
        var more = File.ReadAllText(Path.Combine(PagesDirectory(), "MoreHub.razor"));
        Assert.Contains("UtangCapability.ViewRegisters", more, StringComparison.Ordinal);
        Assert.Contains("GoRegisters", more, StringComparison.Ordinal);
    }

    [Fact]
    public void Registers_compact_styles_and_localization_keys_exist()
    {
        var root = FindRepoRoot();
        var css = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "wwwroot",
            "app.css"));
        Assert.Contains(".pos-registers", css, StringComparison.Ordinal);
        Assert.Contains(".pos-registers__toolbar", css, StringComparison.Ordinal);
        Assert.Contains(".pos-registers__segments", css, StringComparison.Ordinal);
        Assert.Contains(".pos-registers__row", css, StringComparison.Ordinal);
        Assert.Contains(".pos-registers__badge--active", css, StringComparison.Ordinal);
        Assert.Contains(".pos-registers__pager", css, StringComparison.Ordinal);

        var loc = Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Localization");
        var en = File.ReadAllText(Path.Combine(loc, "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(loc, "PosResources.fil-PH.resx"));
        foreach (var key in new[]
                 {
                     "Registers_ClearSearch",
                     "Registers_NoMatchTitle",
                     "Registers_NoMatchMessage",
                     "Registers_OpenShiftHint",
                     "Registers_Filter_All",
                     "Registers_Status_Active",
                     "Registers_Status_Inactive",
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
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
