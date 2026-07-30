namespace ExItS.Platform.Admin.Localization;

/// <summary>
/// Marker type for <c>IStringLocalizer&lt;AdminResources&gt;</c>. Resource text lives in
/// <c>AdminResources.resx</c> (English, fallback) and <c>AdminResources.fil-PH.resx</c> (Tagalog).
/// Only shell/shared-component copy is localized here; usernames, emails, and IDs are never
/// translated, and technical/status codes remain language-neutral by design.
/// </summary>
public sealed class AdminResources;
