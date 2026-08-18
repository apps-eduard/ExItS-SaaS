using Xunit;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class DevelopmentTestUserUsernameOnlyGuardTests
{
    [Fact]
    public void Test_user_selection_fills_username_clears_password_does_not_sign_in()
    {
        var root = FindRepoRoot();
        var signIn = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui",
            "Components",
            "Pages",
            "SignIn.razor"));

        Assert.Contains("OnTestUserSelected", signIn, StringComparison.Ordinal);
        Assert.Contains("_password = null", signIn, StringComparison.Ordinal);
        Assert.Contains("Username convenience only", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("OnQuickLoginSelectedAsync", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("SharedPassword", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("SignInWithCredentialsAsync(loginId, sharedPassword", signIn, StringComparison.Ordinal);
        Assert.Contains("SignInPasswordAsync", signIn, StringComparison.Ordinal);
        Assert.Contains("SignIn_TestUserTitle", signIn, StringComparison.Ordinal);
        Assert.Contains("BelowCard", signIn, StringComparison.Ordinal);
        Assert.Contains("pos-auth-page__quick-select", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("SignIn_TestUserHint", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("SignIn_DevelopmentAccess", signIn, StringComparison.Ordinal);
        Assert.Contains("IsDevelopmentAuthenticationEnabled", signIn, StringComparison.Ordinal);
    }

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
