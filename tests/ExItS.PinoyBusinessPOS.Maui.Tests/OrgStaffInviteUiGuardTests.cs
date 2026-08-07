namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class OrgStaffInviteUiGuardTests
{
    [Fact]
    public void Invite_page_is_compact_and_keeps_supported_invite_flow()
    {
        var page = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Organization", "OrgStaffInvite.razor"));
        var en = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.fil-PH.resx"));

        Assert.Contains("@page \"/org/staff/invite\"", page, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack Href=\"/org/staff\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-invite__back", page, StringComparison.Ordinal);
        Assert.Contains("Org_InviteFindHeading", page, StringComparison.Ordinal);
        Assert.Contains("Org_InviteResolveHint", page, StringComparison.Ordinal);
        Assert.Contains("Personal_AddByExItsId", page, StringComparison.Ordinal);
        Assert.Contains("/personal/resolve-user?purpose=staff-invite", page, StringComparison.Ordinal);
        Assert.Contains("Register_Email", page, StringComparison.Ordinal);
        Assert.Contains("Required=\"true\"", page, StringComparison.Ordinal);
        Assert.Contains("Org_InviteDisplayNameHint", page, StringComparison.Ordinal);
        Assert.Contains("Org_InviteRoleStaff", page, StringComparison.Ordinal);
        Assert.Contains("Org_InviteRoleHint", page, StringComparison.Ordinal);
        Assert.Contains("Org_InviteConfirmHint", page, StringComparison.Ordinal);
        Assert.Contains("Org_WebAdminReminder", page, StringComparison.Ordinal);
        Assert.Contains("Org_InviteSubmit", page, StringComparison.Ordinal);
        Assert.Contains("ButtonVariant.Primary", page, StringComparison.Ordinal);
        Assert.Contains("IsLoading=\"_busy\"", page, StringComparison.Ordinal);
        Assert.Contains("Disabled=\"_busy\"", page, StringComparison.Ordinal);
        Assert.Contains("if (_busy)", page, StringComparison.Ordinal);
        Assert.Contains("finally", page, StringComparison.Ordinal);
        Assert.Contains("_busy = false", page, StringComparison.Ordinal);
        Assert.Contains("FormValidationSummary", page, StringComparison.Ordinal);
        Assert.Contains("_emailInvalid", page, StringComparison.Ordinal);
        Assert.Contains("Org_InviteValidation", page, StringComparison.Ordinal);
        Assert.Contains("Org_InviteFailed", page, StringComparison.Ordinal);
        Assert.Contains("InviteRoleCode = \"OrganizationMember\"", page, StringComparison.Ordinal);
        Assert.Contains("CreateOrganizationInvitationAsync", page, StringComparison.Ordinal);
        Assert.Contains("pos-invite__actions", page, StringComparison.Ordinal);
        Assert.Contains("safe-area-inset-bottom", page, StringComparison.Ordinal);
        // Authorization remains API-backed; invite still posts through Platform.
        Assert.Contains("CurrentUser.Session?.OrganizationId", page, StringComparison.Ordinal);

        Assert.DoesNotContain("ButtonVariant.Secondary\" OnClick=\"GoBack\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", page, StringComparison.Ordinal);
        Assert.DoesNotContain("InlineMessage", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Camera scan", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("QR", page, StringComparison.Ordinal);

        foreach (var key in new[]
                 {
                     "Org_InviteFindHeading", "Org_InviteDisplayNameHint", "Org_InviteRoleStaff"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }

        Assert.Contains("Find staff member", en, StringComparison.Ordinal);
        Assert.Contains("Optional: look up by ExItS ID", en, StringComparison.Ordinal);
        Assert.Contains("Email and acceptance are still required", en, StringComparison.Ordinal);
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
