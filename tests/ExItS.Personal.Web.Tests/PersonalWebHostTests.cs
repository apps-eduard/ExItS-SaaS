namespace ExItS.Personal.Web.Tests;

public sealed class PersonalWebHostTests
{
    [Fact]
    public void Personal_web_has_no_checkout_routes()
    {
        var root = FindPages();
        var combined = string.Join('\n', Directory.GetFiles(root, "*.razor", SearchOption.AllDirectories).Select(File.ReadAllText));
        Assert.DoesNotContain("CheckoutAsync", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("/checkout", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("New Sale", combined, StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_web_uses_antdesign_cookie_and_port()
    {
        var root = FindRepo();
        var csproj = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Personal.Web", "ExItS.Personal.Web.csproj"));
        var program = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Personal.Web", "Program.cs"));
        var launch = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Personal.Web", "Properties", "launchSettings.json"));
        var app = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Personal.Web", "Components", "App.razor"));
        var layout = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Personal.Web", "Components", "Layout", "MainLayout.razor"));
        Assert.Contains("AntDesign", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ExItS.Web.UI", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EntityFrameworkCore", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".ExItS.PersonalWeb.Auth", program, StringComparison.Ordinal);
        Assert.Contains("/session/establish", program, StringComparison.Ordinal);
        Assert.Contains("MapExitsCultureSet", program, StringComparison.Ordinal);
        Assert.Contains("CanonicalLoginUrl", program, StringComparison.Ordinal);
        Assert.Contains("8094", launch, StringComparison.Ordinal);
        Assert.Contains("/_content/AntDesign/css/ant-design-blazor.css", app, StringComparison.Ordinal);
        Assert.Contains("ExitsThemeSelector", layout, StringComparison.Ordinal);
        Assert.Contains("ExitsLanguageSelector", layout, StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_web_routes_cover_migrated_admin_pages()
    {
        var combined = string.Join('\n', Directory.GetFiles(FindPages(), "*.razor", SearchOption.AllDirectories).Select(File.ReadAllText));
        Assert.Contains("@page \"/home\"", combined, StringComparison.Ordinal);
        Assert.Contains("@page \"/utang/people\"", combined, StringComparison.Ordinal);
        Assert.Contains("@page \"/utang/lent\"", combined, StringComparison.Ordinal);
        Assert.Contains("@page \"/utang/borrowed\"", combined, StringComparison.Ordinal);
        Assert.Contains("@page \"/utang/invitations\"", combined, StringComparison.Ordinal);
        Assert.Contains("@page \"/notifications\"", combined, StringComparison.Ordinal);
        Assert.Contains("@page \"/profile\"", combined, StringComparison.Ordinal);
        Assert.Contains("Edit profile", combined, StringComparison.Ordinal);
        Assert.Contains("Save changes", combined, StringComparison.Ordinal);
        Assert.Contains("BeginEditProfile", combined, StringComparison.Ordinal);
        Assert.Contains("CancelEditProfile", combined, StringComparison.Ordinal);
        Assert.Contains("_profileSaving", combined, StringComparison.Ordinal);

        var apiClient = File.ReadAllText(Path.Combine(
            FindRepo(), "src", "Platform", "ExItS.Personal.Web", "Services", "PersonalWebSession.cs"));
        Assert.Contains("UpdateProfileAsync", apiClient, StringComparison.Ordinal);
        Assert.Contains("/api/v1/personal/profile", apiClient, StringComparison.Ordinal);
        Assert.Contains("@page \"/settings\"", combined, StringComparison.Ordinal);
        Assert.Contains("@page \"/start-business\"", combined, StringComparison.Ordinal);
        Assert.Contains("CanonicalLoginUrl", combined, StringComparison.Ordinal);
        Assert.Contains("StartBusinessAsync", combined, StringComparison.Ordinal);
        Assert.Contains("GetOnboardingBusinessTypesAsync", combined, StringComparison.Ordinal);
        Assert.Contains("Start Free Trial", combined, StringComparison.Ordinal);
    }

    private static string FindPages() =>
        Path.Combine(FindRepo(), "src", "Platform", "ExItS.Personal.Web", "Components");

    private static string FindRepo()
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
