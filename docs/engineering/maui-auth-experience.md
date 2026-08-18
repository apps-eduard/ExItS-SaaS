# MAUI rounded authentication experience

[Home](../index.md) | [P18-WP01](../reports/P18-WP01-mobile-foundation-and-authentication.md) | [P19 offline PIN](../reports/P19-offline-operability-foundation.md)

**Status:** Code complete / test guarded. **Not Device Verified.** **Not Production Ready.** Backend and security semantics are unchanged.

## Visual shell

Unauthenticated MAUI auth screens (`/signin`, `/register`, `/forgot-password`, `/activate`) share `AuthExperience`:

- Green hero using current POS `--exits-brand` (`#1f6b45`)
- Centered brand copy: **EXPERT IT SOLUTIONS** / **Pinoy Business POS** (localized keys `SignIn_BrandTitle` / `SignIn_BrandSubtitle`)
- CSS-only translucent circles (no stock images)
- Overlapping white card (~24px radius)
- Auth-scoped rounded inputs/buttons (~16px, ~52px height) so global POS `TextInput` / `Button` styling is not changed
- Unauthenticated `AuthShell` does not render the Brand `StoreHeader` (“ES / ExItS POS” top bar). Authenticated first-time-setup pages on `AuthShell` still use `StoreHeader`.

## Routes and tabs

| Route | Active tab | Notes |
|---|---|---|
| `/signin` | Sign In | Existing password sign-in, remember-me, providers, return route |
| `/register` | Sign Up | Existing personal display-name + email registration and activation handoff |
| `/forgot-password` | none | Same visual shell; existing reset request |
| `/activate` | none | Same visual shell; existing token + password activation |

Tab switching navigates between `/signin` and `/register`. Deep links and navigation guards are unchanged.

Sign Up does **not** invent phone/password registration. Personal registration remains display name + email, then activation.

## Offline PIN on Sign In

- No large offline information panel
- No full-width **Use PIN** / **Continue offline** button in the auth card
- Compact **Use PIN** text link in the Remember / Forgot row **only** when connectivity reports offline **and** `EvaluateOfflineColdStartOfferAsync` says PIN can be offered
- Online (including “PIN exists on this device”) does not show the compact PIN link
- Slow-login prompt still offers **Use PIN instead** when eligible (existing `OnlineLoginProgressController` path)
- Tapping PIN still navigates to `/offline-pin`
- Failed/unreachable login still uses existing error copy (`SignIn_ServerUnreachablePinHint` / `SignIn_OfflineNoPinMessage`)

## Development test user

When `IsDevelopmentAuthenticationEnabled` **and** Local Validation quick-login is available:

- Compact `<select>` is rendered **below** the white card (`BelowCard` / `pos-auth-page__below`)
- No “Development access” heading, no explanatory paragraph, no list of large buttons
- Selection still fills username only and clears password (`OnTestUserSelected`)
- Production still cannot expose the selector (same environment + Local Validation guards)

## Social providers

Circular Facebook / Google actions remain **placeholders**. Accessible names: “Continue with Facebook”, “Continue with Google”. Real OAuth is not implied and is not implemented.

## Honesty

Device Verified: **No**. Production Ready: **No**.
