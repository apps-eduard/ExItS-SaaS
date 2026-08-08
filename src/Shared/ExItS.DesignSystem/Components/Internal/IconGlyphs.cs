using Microsoft.AspNetCore.Components;

namespace ExItS.DesignSystem.Components.Internal;

/// <summary>
/// Minimal inline SVG icon set used by design-system components (IconButton, Alert, Toast,
/// EmptyState, ErrorState, ...). Deliberately small and dependency-free — no icon font, no
/// external icon package. Unknown names fall back to a neutral dot glyph.
/// </summary>
public static class IconGlyphs
{
    private const string Prefix = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\" focusable=\"false\">";
    private const string Suffix = "</svg>";

    private static readonly Dictionary<string, string> Paths = new(StringComparer.OrdinalIgnoreCase)
    {
        // Chevron-left (‹): familiar hierarchical back affordance for top-bar / navigation.
        ["back"] = "<path d=\"M14.5 5.5 8.5 12l6 6.5\"/>",
        ["home"] = "<path d=\"M3 11.5 12 4l9 7.5\"/><path d=\"M5.5 10v9a1 1 0 0 0 1 1H9a1 1 0 0 0 1-1v-4a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1v4a1 1 0 0 0 1 1h2.5a1 1 0 0 0 1-1v-9\"/>",
        ["products"] = "<path d=\"M21 8 12 3 3 8l9 5 9-5Z\"/><path d=\"M3 8v8l9 5 9-5V8\"/><path d=\"M12 13v8\"/>",
        ["sales"] = "<path d=\"M3 3v16a2 2 0 0 0 2 2h16\"/><path d=\"M7 15l4-4 3 3 6-7\"/>",
        ["customers"] = "<circle cx=\"9\" cy=\"8\" r=\"3.25\"/><path d=\"M2.75 20a6.25 6.25 0 0 1 12.5 0\"/><path d=\"M15.5 5.5a3.25 3.25 0 0 1 0 6.3\"/><path d=\"M17.25 14.25a6.25 6.25 0 0 1 4 5.75\"/>",
        ["lent"] = "<path d=\"M7 17 17 7\"/><path d=\"M10 7h7v7\"/>",
        ["borrowed"] = "<path d=\"M17 7 7 17\"/><path d=\"M14 17H7v-7\"/>",
        ["more"] = "<circle cx=\"5\" cy=\"12\" r=\"1.4\" fill=\"currentColor\" stroke=\"none\"/><circle cx=\"12\" cy=\"12\" r=\"1.4\" fill=\"currentColor\" stroke=\"none\"/><circle cx=\"19\" cy=\"12\" r=\"1.4\" fill=\"currentColor\" stroke=\"none\"/>",
        ["settings"] = "<circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.32 9c.14.36.5 1 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1Z\"/>",
        ["edit"] = "<path d=\"M12.5 5.5 18.5 11.5\"/><path d=\"M4.5 19.5v-3.2L15.2 5.6a1.6 1.6 0 0 1 2.3 0l1 1a1.6 1.6 0 0 1 0 2.3L7.7 19.5H4.5Z\"/>",
        ["list"] = "<path d=\"M8 6h13\"/><path d=\"M8 12h13\"/><path d=\"M8 18h13\"/><path d=\"M3.5 6h.01\"/><path d=\"M3.5 12h.01\"/><path d=\"M3.5 18h.01\"/>",
        ["receipt"] = "<path d=\"M6 3.5h12v17l-2-1.2-2 1.2-2-1.2-2 1.2-2-1.2-2 1.2v-17Z\"/><path d=\"M9 8h6\"/><path d=\"M9 12h6\"/><path d=\"M9 16h4\"/>",
        ["share"] = "<circle cx=\"18\" cy=\"5\" r=\"2.5\"/><circle cx=\"6\" cy=\"12\" r=\"2.5\"/><circle cx=\"18\" cy=\"19\" r=\"2.5\"/><path d=\"m8.2 10.8 7.6-4.6\"/><path d=\"m8.2 13.2 7.6 4.6\"/>",
        ["refresh"] = "<path d=\"M21 12a9 9 0 1 1-2.64-6.36\"/><path d=\"M21 3v6h-6\"/>",
        ["close"] = "<path d=\"M18 6 6 18\"/><path d=\"M6 6l12 12\"/>",
        ["plus"] = "<path d=\"M12 5v14\"/><path d=\"M5 12h14\"/>",
        ["menu"] = "<path d=\"M4 6h16\"/><path d=\"M4 12h16\"/><path d=\"M4 18h16\"/>",
        ["check"] = "<path d=\"M20 6 9 17l-5-5\"/>",
        ["warning"] = "<path d=\"M12 3.5 2 20.5h20L12 3.5Z\"/><path d=\"M12 10v4\"/><path d=\"M12 17.2h.01\"/>",
        ["search"] = "<circle cx=\"11\" cy=\"11\" r=\"7\"/><path d=\"m21 21-4.35-4.35\"/>",
        ["qr"] = "<path d=\"M4 4h6v6H4z\"/><path d=\"M14 4h6v6h-6z\"/><path d=\"M4 14h6v6H4z\"/><path d=\"M14 14h2v2h-2z\"/><path d=\"M18 14h2v2h-2z\"/><path d=\"M14 18h2v2h-2z\"/><path d=\"M18 18h2v2h-2z\"/>",
        ["image"] = "<rect x=\"3\" y=\"5\" width=\"18\" height=\"14\" rx=\"2\"/><circle cx=\"8.5\" cy=\"10.5\" r=\"1.5\"/><path d=\"m21 15-5-5-4 4-2-2-5 5\"/>",
        ["info"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 11v6\"/><path d=\"M12 7.2h.01\"/>",
        ["inbox"] = "<path d=\"M3 12h4.5l2 3h5l2-3H21\"/><path d=\"M5.4 5h13.2l2.4 7v6a1.5 1.5 0 0 1-1.5 1.5H4.5A1.5 1.5 0 0 1 3 18v-6l2.4-7Z\"/>",
        ["error"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"m9.5 9.5 5 5\"/><path d=\"m14.5 9.5-5 5\"/>",
        ["cloud-off"] = "<path d=\"M17.5 19H9a6 6 0 1 1 0.5-12 6.5 6.5 0 0 1 11.3 3.2\"/><path d=\"M22 17a3 3 0 0 0-3.5-2.9\"/><path d=\"m2 2 20 20\"/>",
        ["lock"] = "<rect x=\"5\" y=\"11\" width=\"14\" height=\"10\" rx=\"2\"/><path d=\"M8 11V8a4 4 0 0 1 8 0v3\"/>",
    };

    private const string Fallback = "<circle cx=\"12\" cy=\"12\" r=\"2\" fill=\"currentColor\" stroke=\"none\"/>";

    public static MarkupString Get(string? name)
    {
        var body = name is not null && Paths.TryGetValue(name, out var svg) ? svg : Fallback;
        return new MarkupString(Prefix + body + Suffix);
    }
}
