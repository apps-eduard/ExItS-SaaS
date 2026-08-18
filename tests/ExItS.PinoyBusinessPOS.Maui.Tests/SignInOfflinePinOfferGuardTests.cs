using ExItS.PinoyBusinessPOS.Application.Abstractions;
using Xunit;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class SignInOfflinePinOfferGuardTests
{
    [Fact]
    public void SignIn_offers_pin_from_os_radio_off_even_when_debug_treats_none_as_connected()
    {
        var maui = MauiProject();
        var signIn = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "SignIn.razor"));
        var connectivity = File.ReadAllText(Path.Combine(maui, "Services", "MauiConnectivityService.cs"));
        var contract = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Abstractions",
            "IConnectivityService.cs"));

        Assert.Contains("HasNoNetworkInterfaceAsync", contract, StringComparison.Ordinal);
        Assert.Contains("HasNoNetworkInterfaceAsync", connectivity, StringComparison.Ordinal);
        Assert.Contains("NetworkAccess.None", connectivity, StringComparison.Ordinal);
        Assert.Contains("HasNoNetworkInterfaceAsync", signIn, StringComparison.Ordinal);
        Assert.Contains("EvaluateOfflineColdStartOfferAsync", signIn, StringComparison.Ordinal);
        Assert.Contains("_canUsePin = offer.CanOfferPinUnlock", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("_canUsePin = true", signIn, StringComparison.Ordinal);
        Assert.Contains("_canUsePin && (_isOffline || _offerPinBecauseUnreachable)", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("else if (_canUsePin)", signIn, StringComparison.Ordinal);
    }

    [Fact]
    public void SignIn_does_not_emit_offline_unlock_error_on_page_load()
    {
        var signIn = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "SignIn.razor"));
        var init = ExtractMethod(signIn, "OnInitializedAsync");
        Assert.Contains("RefreshOfflineStateAsync", init, StringComparison.Ordinal);
        Assert.DoesNotContain("SignIn_OfflineNoPinMessage", init, StringComparison.Ordinal);
        Assert.DoesNotContain("_errors.Add", init, StringComparison.Ordinal);
        Assert.DoesNotContain("_errors =", init, StringComparison.Ordinal);

        var refresh = ExtractMethod(signIn, "RefreshOfflineStateAsync");
        Assert.DoesNotContain("SignIn_OfflineNoPinMessage", refresh, StringComparison.Ordinal);
        Assert.DoesNotContain("_errors", refresh, StringComparison.Ordinal);

        Assert.Contains("RecordUnreachableSignInOutcome", signIn, StringComparison.Ordinal);
        Assert.Contains("SignIn_OfflineNoPinMessage", ExtractMethod(signIn, "RecordUnreachableSignInOutcome"), StringComparison.Ordinal);
    }

    [Fact]
    public void SignIn_hides_pin_when_radio_returns_and_keeps_existing_unlock_path()
    {
        var signIn = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "SignIn.razor"));
        Assert.Contains("OnConnectivityChanged", signIn, StringComparison.Ordinal);
        Assert.Contains("if (!_isOffline)", signIn, StringComparison.Ordinal);
        Assert.Contains("_offerPinBecauseUnreachable = false", signIn, StringComparison.Ordinal);
        Assert.Contains("Nav.NavigateTo(\"/offline-pin\"", signIn, StringComparison.Ordinal);
        Assert.Contains("ChoosePinInstead", signIn, StringComparison.Ordinal);
        Assert.Contains("IsDevelopmentAuthenticationEnabled", signIn, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Default_HasNoNetworkInterface_inverts_IsConnected()
    {
        IConnectivityService offline = new StubConnectivity(connected: false);
        Assert.True(await offline.HasNoNetworkInterfaceAsync());

        IConnectivityService online = new StubConnectivity(connected: true);
        Assert.False(await online.HasNoNetworkInterfaceAsync());
    }

    private sealed class StubConnectivity(bool connected) : IConnectivityService
    {
        public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(connected);

        public event EventHandler<ConnectivityStatus>? ConnectivityChanged
        {
            add { }
            remove { }
        }
    }

    private static string ExtractMethod(string source, string methodName)
    {
        var marker = $" {methodName}(";
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Method {methodName} not found.");
        var brace = source.IndexOf('{', start);
        Assert.True(brace > start);
        var depth = 0;
        for (var i = brace; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source[start..(i + 1)];
                }
            }
        }

        throw new InvalidOperationException($"Unbalanced braces for {methodName}.");
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
