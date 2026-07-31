using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Localization;
using Microsoft.JSInterop;

namespace ExItS.Platform.Admin.Services;

public sealed record AdminCultureOption(string Code, string NativeLabel);

/// <summary>
/// Scoped per-circuit language preference. The Admin app uses ASP.NET Core request localization
/// with a cookie provider (<see cref="CookieRequestCultureProvider"/>); switching culture requires
/// a full navigation so the server re-applies <c>RequestLocalizationMiddleware</c> and
/// <c>IStringLocalizer</c> resources render in the new language. localStorage
/// (<c>exits-admin-culture</c>) is used only for the pre-render <c>lang</c> attribute set by
/// <c>wwwroot/theme-boot.js</c> to avoid a flash of the wrong language.
/// </summary>
public sealed class CultureService(IJSRuntime js, NavigationManager nav)
{
    public const string StorageKey = "exits-admin-culture";
    public const string English = "en";
    public const string Filipino = "fil-PH";

    public static readonly IReadOnlyList<AdminCultureOption> Options =
    [
        new(English, "English"),
        new(Filipino, "Tagalog")
    ];

    public async Task<string> GetCurrentAsync()
    {
        var stored = await js.InvokeAsync<string?>("exitsAdminTheme.get", StorageKey);
        return stored == Filipino ? Filipino : English;
    }

    public async Task SetCultureAsync(string cultureCode)
    {
        var normalized = cultureCode == Filipino ? Filipino : English;
        var current = await GetCurrentAsync();
        if (string.Equals(current, normalized, StringComparison.Ordinal))
        {
            return;
        }

        await js.InvokeVoidAsync("exitsAdminTheme.set", StorageKey, normalized);

        var uri = new Uri(nav.Uri);
        var culturePath = $"/culture/set?culture={Uri.EscapeDataString(normalized)}&redirectUri={Uri.EscapeDataString(uri.PathAndQuery)}";
        nav.NavigateTo(culturePath, forceLoad: true);
    }
}
