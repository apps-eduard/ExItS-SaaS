using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace ExItS.PinoyBusinessPOS.Maui;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    WindowSoftInputMode = SoftInput.AdjustResize,
    ConfigurationChanges = ConfigChanges.ScreenSize
        | ConfigChanges.Orientation
        | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize
        | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
#if DEBUG
        // Enables chrome://inspect / CDP automation against the Blazor WebView on emulator/device.
        Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
#endif
        base.OnCreate(savedInstanceState);
        // Keep content resizable when the soft keyboard opens so focused fields stay visible.
        Window?.SetSoftInputMode(SoftInput.AdjustResize);
    }
}
