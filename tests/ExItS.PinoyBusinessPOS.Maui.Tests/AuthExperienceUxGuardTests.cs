using Xunit;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class AuthExperienceUxGuardTests
{
    [Fact]
    public void SignIn_and_register_share_rounded_auth_card_with_tabs()
    {
        var maui = MauiProject();
        var signIn = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "SignIn.razor"));
        var register = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Register.razor"));
        var shell = File.ReadAllText(Path.Combine(maui, "Components", "Shared", "AuthExperience.razor"));
        var css = File.ReadAllText(Path.Combine(maui, "wwwroot", "app.css"));

        Assert.Contains("@page \"/signin\"", signIn, StringComparison.Ordinal);
        Assert.Contains("@page \"/register\"", register, StringComparison.Ordinal);
        Assert.Contains("AuthExperience ActiveTab=\"AuthExperienceTab.SignIn\"", signIn, StringComparison.Ordinal);
        Assert.Contains("AuthExperience ActiveTab=\"AuthExperienceTab.SignUp\"", register, StringComparison.Ordinal);
        Assert.Contains("Nav.NavigateTo(\"/signin\")", shell, StringComparison.Ordinal);
        Assert.Contains("Nav.NavigateTo(\"/register\")", shell, StringComparison.Ordinal);
        Assert.Contains("SignIn_TabSignIn", shell, StringComparison.Ordinal);
        Assert.Contains("SignIn_TabSignUp", shell, StringComparison.Ordinal);
        Assert.Contains("SignIn_BrandTitle", shell, StringComparison.Ordinal);
        Assert.Contains("SignIn_BrandSubtitle", shell, StringComparison.Ordinal);
        Assert.Contains(".pos-auth-page__card", css, StringComparison.Ordinal);
        Assert.Contains("--pos-auth-radius-card: 1.5rem", css, StringComparison.Ordinal);
        Assert.Contains("--pos-auth-radius-control: 1rem", css, StringComparison.Ordinal);
        Assert.DoesNotContain("ECOMMERCE", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("ECOMMERCE", signIn, StringComparison.Ordinal);
    }

    [Fact]
    public void Forgot_password_and_activate_use_same_auth_shell_without_tabs()
    {
        var maui = MauiProject();
        var forgot = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "ForgotPassword.razor"));
        var activate = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "ActivateAccount.razor"));

        Assert.Contains("@page \"/forgot-password\"", forgot, StringComparison.Ordinal);
        Assert.Contains("@page \"/activate\"", activate, StringComparison.Ordinal);
        Assert.Contains("<AuthExperience Title=", forgot, StringComparison.Ordinal);
        Assert.Contains("<AuthExperience Title=", activate, StringComparison.Ordinal);
        Assert.Contains("ForgotPassword_Submit", forgot, StringComparison.Ordinal);
        Assert.Contains("ForgotPassword_Back", forgot, StringComparison.Ordinal);
        Assert.Contains("ActivatePersonalAccountAsync", activate, StringComparison.Ordinal);
        Assert.Contains("RegisterPersonalAccountAsync",
            File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Register.razor")),
            StringComparison.Ordinal);
        Assert.DoesNotContain("FormGroup",
            File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Register.razor")),
            StringComparison.Ordinal);
        Assert.DoesNotContain("FormGroup", activate, StringComparison.Ordinal);
    }

    [Fact]
    public void SignIn_pin_is_a_compact_offline_only_link()
    {
        var signIn = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "SignIn.razor"));
        Assert.Contains("ShowOfflinePinAction", signIn, StringComparison.Ordinal);
        Assert.Contains("HasNoNetworkInterfaceAsync", signIn, StringComparison.Ordinal);
        Assert.Contains("_canUsePin && (_isOffline || _offerPinBecauseUnreachable)", signIn, StringComparison.Ordinal);
        Assert.Contains("SignIn_UsePin", signIn, StringComparison.Ordinal);
        Assert.Contains("SignIn_RememberMe", signIn, StringComparison.Ordinal);
        Assert.Contains("SignIn_ForgotPassword", signIn, StringComparison.Ordinal);
        Assert.Contains("/offline-pin", signIn, StringComparison.Ordinal);
        Assert.Contains("EvaluateOfflineColdStartOfferAsync", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-signin__offline-panel", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("SignIn_OfflineLimitedHint", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("SignIn_ContinueOffline", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("else if (_canUsePin)", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("_canUsePin = true;", signIn, StringComparison.Ordinal);
    }

    [Fact]
    public void SignIn_keeps_providers_remember_me_and_dev_selector_outside_card()
    {
        var maui = MauiProject();
        var signIn = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "SignIn.razor"));
        var shell = File.ReadAllText(Path.Combine(maui, "Components", "Shared", "AuthExperience.razor"));
        var options = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Options",
            "LocalValidationClientOptions.cs"));

        Assert.Contains("SignIn_ContinueGoogle", signIn, StringComparison.Ordinal);
        Assert.Contains("SignIn_ContinueFacebook", signIn, StringComparison.Ordinal);
        Assert.Contains("ContinueWithGooglePlaceholderAsync", signIn, StringComparison.Ordinal);
        Assert.Contains("ContinueWithFacebookPlaceholderAsync", signIn, StringComparison.Ordinal);
        Assert.Contains("PreferenceKeys.RememberMe", signIn, StringComparison.Ordinal);
        Assert.Contains("BelowCard", signIn, StringComparison.Ordinal);
        Assert.Contains("pos-auth-page__below", shell, StringComparison.Ordinal);
        Assert.Contains("pos-auth-page__quick-select", signIn, StringComparison.Ordinal);
        Assert.Contains("IsDevelopmentAuthenticationEnabled", signIn, StringComparison.Ordinal);
        Assert.Contains("IsQuickLoginAvailable", signIn, StringComparison.Ordinal);
        Assert.Contains("IsQuickLoginAvailable =>", options, StringComparison.Ordinal);
        Assert.DoesNotContain("SignIn_TestUserHint", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("WebAuthenticator", signIn, StringComparison.Ordinal);
    }

    [Fact]
    public void Unauthenticated_auth_shell_does_not_render_brand_topbar()
    {
        var auth = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Layout", "AuthShell.razor"));
        Assert.Contains("CurrentUser.IsAuthenticated", auth, StringComparison.Ordinal);
        Assert.Contains("StoreHeader", auth, StringComparison.Ordinal);
        Assert.DoesNotContain("StoreHeaderIdentity.Brand", auth, StringComparison.Ordinal);
    }

    [Fact]
    public void Auth_localization_includes_brand_and_tab_keys()
    {
        var maui = MauiProject();
        var en = File.ReadAllText(Path.Combine(maui, "Localization", "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(maui, "Localization", "PosResources.fil-PH.resx"));

        foreach (var key in new[]
                 {
                     "SignIn_BrandTitle", "SignIn_BrandSubtitle", "SignIn_TabSignIn", "SignIn_TabSignUp",
                     "SignIn_AuthTabsLabel", "SignIn_QuickLoginPlaceholder", "SignIn_UsePin"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }

        Assert.Contains("EXPERT IT SOLUTIONS", en, StringComparison.Ordinal);
        Assert.Contains("Pinoy Business POS", en, StringComparison.Ordinal);
        Assert.Contains("EXPERT IT SOLUTIONS", fil, StringComparison.Ordinal);
        Assert.Contains("Pinoy Business POS", fil, StringComparison.Ordinal);
        Assert.DoesNotContain("ECOMMERCE", en, StringComparison.Ordinal);
    }

    private static string MauiProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Maui project not found.");
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
