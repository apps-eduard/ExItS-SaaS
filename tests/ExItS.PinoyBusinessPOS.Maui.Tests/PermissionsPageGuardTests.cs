namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class PermissionsPageGuardTests
{
    [Fact]
    public void Permissions_list_is_compact_without_raw_guid_primary_text()
    {
        var page = Read("PermissionsHub.razor");
        Assert.Contains("@page \"/permissions\"", page, StringComparison.Ordinal);
        Assert.Contains("Permissions_Title", page, StringComparison.Ordinal);
        Assert.Contains("Permissions_Subtitle", page, StringComparison.Ordinal);
        Assert.Contains("Permissions_Assign", page, StringComparison.Ordinal);
        Assert.Contains("ButtonVariant.Primary", page, StringComparison.Ordinal);
        Assert.Contains("pos-permissions__secondary", page, StringComparison.Ordinal);
        Assert.Contains("Permissions_MyAccess", page, StringComparison.Ordinal);
        Assert.Contains("pos-permissions__row", page, StringComparison.Ordinal);
        Assert.Contains("pos-permissions__row-name", page, StringComparison.Ordinal);
        Assert.Contains("RoleDisplayName", page, StringComparison.Ordinal);
        Assert.Contains("GetUserAsync", page, StringComparison.Ordinal);
        Assert.Contains("ResolveIdentitiesAsync", page, StringComparison.Ordinal);
        Assert.Contains("Permissions_UnknownUser", page, StringComparison.Ordinal);
        Assert.Contains("LooksLikeTechnicalId", page, StringComparison.Ordinal);
        Assert.Contains("Permissions_Status_Active", page, StringComparison.Ordinal);
        Assert.Contains("EmptyState", page, StringComparison.Ordinal);
        Assert.Contains("ErrorState", page, StringComparison.Ordinal);
        Assert.Contains("OnRetry", page, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewPermissions", page, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManagePermissions", page, StringComparison.Ordinal);
        Assert.DoesNotContain("ResponsiveDataList", page, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"pos-link\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("ActorId.ToString", page, StringComparison.Ordinal);
        Assert.DoesNotContain("AssignmentId.ToString", page, StringComparison.Ordinal);
        Assert.DoesNotContain("ShortActorId", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Permissions_AssignedToFormat", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Permissions_Back", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Assign_role_removes_content_back_and_guards_submit()
    {
        var page = Read("AssignmentCreate.razor");
        Assert.Contains("@page \"/permissions/assignments/new\"", page, StringComparison.Ordinal);
        Assert.Contains("Permissions_Assign", page, StringComparison.Ordinal);
        Assert.Contains("Permissions_AssignSubtitle", page, StringComparison.Ordinal);
        Assert.Contains("Permissions_ActorUserId", page, StringComparison.Ordinal);
        Assert.DoesNotContain("L[\"Permissions_ActorId\"]", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Permissions_Back", page, StringComparison.Ordinal);
        Assert.DoesNotContain("GoBack", page, StringComparison.Ordinal);
        Assert.DoesNotContain("OnClick=\"GoBack\"", page, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManagePermissions", page, StringComparison.Ordinal);
        Assert.Contains("if (_acting)", page, StringComparison.Ordinal);
        Assert.Contains("finally", page, StringComparison.Ordinal);
        Assert.Contains("_acting = false", page, StringComparison.Ordinal);
        Assert.Contains("IsLoading=\"@_acting\"", page, StringComparison.Ordinal);
        Assert.Contains("Nav.NavigateTo(\"/permissions\", replace: true)", page, StringComparison.Ordinal);
        // Nested route keeps history so system / shell back can return to the list.
        Assert.DoesNotContain("NavigateTo(\"/permissions/assignments/new\", replace: true)", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Assignment_detail_uses_role_title_human_identity_and_revoke_confirm()
    {
        var page = Read("AssignmentDetail.razor");
        Assert.Contains("@page \"/permissions/assignments/{AssignmentId:guid}\"", page, StringComparison.Ordinal);
        Assert.Contains("_item.RoleDisplayName", page, StringComparison.Ordinal);
        Assert.Contains("Permissions_AssignedUser", page, StringComparison.Ordinal);
        Assert.Contains("_actorPrimary", page, StringComparison.Ordinal);
        Assert.Contains("GetUserAsync", page, StringComparison.Ordinal);
        Assert.Contains("Permissions_UnknownUser", page, StringComparison.Ordinal);
        Assert.DoesNotContain("ShortId(AssignmentId)", page, StringComparison.Ordinal);
        Assert.DoesNotContain("ShortActorId", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Permissions_Back", page, StringComparison.Ordinal);
        Assert.DoesNotContain("GoBack", page, StringComparison.Ordinal);
        Assert.DoesNotContain("AssignmentId.ToString", page, StringComparison.Ordinal);
        Assert.DoesNotContain("<h1>@AssignmentId", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Title=\"@AssignmentId", page, StringComparison.Ordinal);
        Assert.Contains("ConfirmDialog", page, StringComparison.Ordinal);
        Assert.Contains("Danger=\"true\"", page, StringComparison.Ordinal);
        Assert.Contains("Permissions_RevokeConfirmTitle", page, StringComparison.Ordinal);
        Assert.Contains("ButtonVariant.Danger", page, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ManagePermissions", page, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.ViewPermissions", page, StringComparison.Ordinal);
        Assert.Contains("if (_acting)", page, StringComparison.Ordinal);
        Assert.Contains("finally", page, StringComparison.Ordinal);
        Assert.Contains("_acting = false", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Permissions_hub_navigation_preserves_stack_for_nested_pages()
    {
        var hub = Read("PermissionsHub.razor");
        Assert.Contains("Nav.NavigateTo(\"/permissions/assignments/new\")", hub, StringComparison.Ordinal);
        Assert.Contains("Nav.NavigateTo($\"/permissions/assignments/{id:D}\")", hub, StringComparison.Ordinal);
        Assert.DoesNotContain("NavigateTo(\"/permissions/assignments/new\", replace: true)", hub, StringComparison.Ordinal);
        Assert.DoesNotContain("NavigateTo($\"/permissions/assignments/{id:D}\", replace: true)", hub, StringComparison.Ordinal);
    }

    [Fact]
    public void Permissions_compact_styles_and_localization_keys_exist()
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
        Assert.Contains(".pos-permissions", css, StringComparison.Ordinal);
        Assert.Contains(".pos-permissions__row", css, StringComparison.Ordinal);
        Assert.Contains(".pos-permissions__row-name", css, StringComparison.Ordinal);
        Assert.Contains(".pos-permissions__badge--active", css, StringComparison.Ordinal);
        Assert.Contains(".pos-permissions__secondary", css, StringComparison.Ordinal);
        Assert.Contains(".pos-permissions__facts", css, StringComparison.Ordinal);

        var loc = Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Localization");
        var en = File.ReadAllText(Path.Combine(loc, "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(loc, "PosResources.fil-PH.resx"));
        foreach (var key in new[]
                 {
                     "Permissions_ActorUserId",
                     "Permissions_AssignedUser",
                     "Permissions_UnknownUser",
                     "Permissions_AssignedToFormat",
                     "Permissions_Status_Active",
                     "Permissions_Status_Revoked",
                     "Permissions_Cancel",
                     "Permissions_RevokeConfirmTitle",
                     "Permissions_RevokeConfirmMessage",
                     "Permissions_AssignmentIdLabel",
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    private static string Read(string fileName) => File.ReadAllText(Path.Combine(
        FindRepoRoot(),
        "src",
        "Products",
        "PinoyBusinessPOS",
        "ExItS.PinoyBusinessPOS.Maui",
        "Components",
        "Pages",
        "Permissions",
        fileName));

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
