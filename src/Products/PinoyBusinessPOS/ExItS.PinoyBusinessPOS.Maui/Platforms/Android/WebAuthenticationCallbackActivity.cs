using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Maui.Authentication;

namespace ExItS.PinoyBusinessPOS.Maui;

/// <summary>Receives Platform Google OAuth completion redirects for WebAuthenticator.</summary>
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "exitspos",
    DataHost = "auth",
    DataPath = "/callback")]
public sealed class WebAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity
{
}
