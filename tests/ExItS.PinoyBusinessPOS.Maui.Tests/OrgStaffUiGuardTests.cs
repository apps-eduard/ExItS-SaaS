namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class OrgStaffUiGuardTests
{
    [Fact]
    public void Staff_page_is_compact_and_shows_people_not_raw_ids()
    {
        var page = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Organization", "OrgStaff.razor"));
        var css = File.ReadAllText(Path.Combine(MauiProject(), "wwwroot", "app.css"));
        var en = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.fil-PH.resx"));

        Assert.Contains("@page \"/org/staff\"", page, StringComparison.Ordinal);
        Assert.Contains("pos-staff", page, StringComparison.Ordinal);
        Assert.Contains("pos-staff__row", page, StringComparison.Ordinal);
        Assert.Contains("GetUserAsync", page, StringComparison.Ordinal);
        Assert.Contains("FriendlyMembershipRole", page, StringComparison.Ordinal);
        Assert.Contains("Org_MembershipOwner", page, StringComparison.Ordinal);
        Assert.Contains("Org_MembershipStaff", page, StringComparison.Ordinal);
        Assert.Contains("PosGrants", page, StringComparison.Ordinal);
        Assert.Contains("GoInvite", page, StringComparison.Ordinal);
        Assert.Contains("GoAssign", page, StringComparison.Ordinal);
        Assert.Contains("displayName=", page, StringComparison.Ordinal);
        Assert.Contains("email=", page, StringComparison.Ordinal);
        Assert.Contains("SuspendMembershipAsync", page, StringComparison.Ordinal);
        Assert.Contains("RevokeMembershipAsync", page, StringComparison.Ordinal);
        Assert.Contains("RevokeProductLocalRoleAsync", page, StringComparison.Ordinal);
        Assert.Contains("pos-staff__back", page, StringComparison.Ordinal);
        Assert.Contains("pos-staff__actions-bar", page, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"plus\")", page, StringComparison.Ordinal);
        Assert.Contains("RestoreSessionAsync", page, StringComparison.Ordinal);
        Assert.Contains("pos-staff__footnote", page, StringComparison.Ordinal);

        Assert.DoesNotContain("UserId.ToString(\"D\")[..8]", page, StringComparison.Ordinal);
        Assert.DoesNotContain("@member.Role", page, StringComparison.Ordinal);
        Assert.DoesNotContain("<Card>", page, StringComparison.Ordinal);
        Assert.DoesNotContain("InlineMessage", page, StringComparison.Ordinal);

        Assert.Contains(".pos-staff__row", css, StringComparison.Ordinal);
        Assert.Contains(".pos-staff__chip", css, StringComparison.Ordinal);
        Assert.Contains(".pos-staff__footnote", css, StringComparison.Ordinal);

        foreach (var key in new[]
                 {
                     "Org_MembershipOwner", "Org_MembershipStaff", "Org_NoPosRoles",
                     "Org_StaffUnknownName", "Org_StatusActive"
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
