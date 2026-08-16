namespace ExItS.PinoyBusinessPOS.Maui.Tests;

/// <summary>Static UI guards for offline / session-expired Platform UX.</summary>
public sealed class PlatformSessionRecoveryUiGuardTests
{
    [Fact]
    public void NotificationsShowsOfflineStateWhenOffline()
    {
        var page = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Personal", "PersonalNotifications.razor"));
        Assert.Contains("Platform_OfflineTitle", page, StringComparison.Ordinal);
        Assert.Contains("Personal_NotificationsOfflineMessage", page, StringComparison.Ordinal);
        Assert.Contains("PlatformScreenState.Offline", page, StringComparison.Ordinal);
        Assert.Contains("Connectivity.IsConnectedAsync", page, StringComparison.Ordinal);
    }

    [Fact]
    public void NotificationsDoesNotShowAuthenticationRequiredToUser()
    {
        var page = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Personal", "PersonalNotifications.razor"));
        Assert.DoesNotContain("result.Error?.Detail", page, StringComparison.Ordinal);
        Assert.Contains("Auth_SessionExpiredTitle", page, StringComparison.Ordinal);
        Assert.Contains("Auth_SignInAgain", page, StringComparison.Ordinal);
        Assert.Contains("LooksLikeRawAuthenticationRequired", page, StringComparison.Ordinal);
    }

    [Fact]
    public void OrganizationShowsOfflinePlatformSections()
    {
        var page = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Organization", "OrgSummary.razor"));
        Assert.Contains("Org_SubscriptionOffline", page, StringComparison.Ordinal);
        Assert.Contains("Org_EntitlementOffline", page, StringComparison.Ordinal);
        Assert.Contains("Org_OfflinePlatformHint", page, StringComparison.Ordinal);
        Assert.Contains("_isOffline", page, StringComparison.Ordinal);
    }

    [Fact]
    public void OfflineStateDoesNotSayUnavailable()
    {
        var page = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Organization", "OrgSummary.razor"));
        Assert.Contains("_isOffline ? L[\"Org_SubscriptionOffline\"]", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Org_SubscriptionUnknown\"] : L[\"Org_SubscriptionUnknown\"]", page, StringComparison.Ordinal);
    }

    [Fact]
    public void SessionExpiredShowsSignInAgain()
    {
        var notifications = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Personal", "PersonalNotifications.razor"));
        var org = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Organization", "OrgSummary.razor"));
        Assert.Contains("Auth_SignInAgain", notifications, StringComparison.Ordinal);
        Assert.Contains("Auth_SignInAgain", org, StringComparison.Ordinal);
        Assert.Contains("ReturnRoute.Capture", notifications, StringComparison.Ordinal);
        Assert.Contains("ReturnRoute.Capture", org, StringComparison.Ordinal);
    }

    [Fact]
    public void ServiceUnavailableShowsTryAgain()
    {
        var page = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Personal", "PersonalNotifications.razor"));
        Assert.Contains("Platform_ServiceUnavailableTitle", page, StringComparison.Ordinal);
        Assert.Contains("Platform_ServiceUnavailableMessage", page, StringComparison.Ordinal);
        Assert.Contains("Common_Retry", page, StringComparison.Ordinal);
    }

    [Fact]
    public void ConnectivityIndicatorShowsOfflineAndOnline()
    {
        var shell = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "ShellSyncStatus.razor"));
        Assert.Contains("SyncStatus_Offline", shell, StringComparison.Ordinal);
        Assert.Contains("SyncStatus_Online", shell, StringComparison.Ordinal);
        Assert.Contains("cloud-off", shell, StringComparison.Ordinal);
        Assert.Contains("Platform_OfflineBody", shell, StringComparison.Ordinal);
    }

    [Fact]
    public void NoInfiniteSpinnerOnUnauthorizedOrOffline()
    {
        var page = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Personal", "PersonalNotifications.razor"));
        Assert.Contains("_loading = false", page, StringComparison.Ordinal);
        Assert.Contains("PlatformScreenState.SessionExpired", page, StringComparison.Ordinal);
        Assert.Contains("PlatformScreenState.Offline", page, StringComparison.Ordinal);
        Assert.Contains("finally", page, StringComparison.Ordinal);
    }

    [Fact]
    public void SignInRestoresSafeReturnRoute()
    {
        var signIn = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "SignIn.razor"));
        Assert.Contains("PostSignInReturnRoute", signIn, StringComparison.Ordinal);
        Assert.Contains("ReturnRoute.Take()", signIn, StringComparison.Ordinal);
    }

    [Fact]
    public void MauiRegistersPlatformAccessTokenRecovery()
    {
        var program = File.ReadAllText(Path.Combine(MauiProject(), "MauiProgram.cs"));
        Assert.Contains("IPlatformAccessTokenRecovery", program, StringComparison.Ordinal);
        Assert.Contains("AuthenticationService", program, StringComparison.Ordinal);
        Assert.Contains("PostSignInReturnRoute", program, StringComparison.Ordinal);
    }

    private static string MauiProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "src",
                "Products",
                "PinoyBusinessPOS",
                "ExItS.PinoyBusinessPOS.Maui");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate ExItS.PinoyBusinessPOS.Maui project directory.");
    }
}
