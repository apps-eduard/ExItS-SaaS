namespace ExItS.PinoyBusinessPOS.Maui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        blazorWebView.BlazorWebViewInitialized += OnBlazorWebViewInitialized;
    }

    private void OnBlazorWebViewInitialized(object? sender, Microsoft.AspNetCore.Components.WebView.BlazorWebViewInitializedEventArgs e)
    {
#if ANDROID && DEBUG
        // Blazor Hybrid serves the app origin over HTTPS (https://0.0.0.1/). Local Validation
        // APIs are plain HTTP on the emulator host loopback, so mixed content must be allowed
        // for DEBUG validation (browser fetch diagnostics and any JS-side probes).
        e.WebView.Settings.MixedContentMode = Android.Webkit.MixedContentHandling.AlwaysAllow;
#endif
    }
}
