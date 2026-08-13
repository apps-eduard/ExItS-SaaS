using Microsoft.JSInterop;

namespace ExItS.Web.UI;

public enum ExitsWebTheme
{
    Light,
    Dark,
    System
}

/// <summary>
/// Shared Light / Dark / System preference for ExItS browser hosts.
/// Persisted per-origin in localStorage as <c>exits-web-theme</c>.
/// </summary>
public sealed class ExitsWebThemeService(IJSRuntime js)
{
    public const string StorageKey = "exits-web-theme";

    public ExitsWebTheme Current { get; private set; } = ExitsWebTheme.Light;

    public event Func<Task>? Changed;

    public async Task InitializeAsync()
    {
        var stored = await js.InvokeAsync<string?>("exitsWebTheme.get", StorageKey).ConfigureAwait(false);
        Current = Parse(stored);
        await js.InvokeVoidAsync("exitsWebTheme.set", StorageKey, ToStorageValue(Current)).ConfigureAwait(false);
        await js.InvokeVoidAsync("exitsWebTheme.applyTheme", ToStorageValue(Current)).ConfigureAwait(false);
        if (Changed is not null)
        {
            await Changed.Invoke().ConfigureAwait(false);
        }
    }

    public async Task SetThemeAsync(ExitsWebTheme theme)
    {
        Current = theme is ExitsWebTheme.System or ExitsWebTheme.Dark or ExitsWebTheme.Light
            ? theme
            : ExitsWebTheme.Light;
        var value = ToStorageValue(Current);
        await js.InvokeVoidAsync("exitsWebTheme.set", StorageKey, value).ConfigureAwait(false);
        await js.InvokeVoidAsync("exitsWebTheme.applyTheme", value).ConfigureAwait(false);
        if (Changed is not null)
        {
            await Changed.Invoke().ConfigureAwait(false);
        }
    }

    public static string ToStorageValue(ExitsWebTheme theme) => theme switch
    {
        ExitsWebTheme.Dark => "dark",
        ExitsWebTheme.System => "system",
        _ => "light"
    };

    public static ExitsWebTheme Parse(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "dark" => ExitsWebTheme.Dark,
            "system" => ExitsWebTheme.System,
            _ => ExitsWebTheme.Light
        };
}
