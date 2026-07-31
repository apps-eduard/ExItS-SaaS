# Platform Admin — Live Preview hardening notes (2026-08-01)

Post–P15-WP01 session fixes on `main`. **Do not roll back** these changes; they stabilize Interactive Server auth, nav, culture, and theme.

[Portfolio](../portfolio-progress.md) | [P15-WP01](P15-WP01-antdesign-admin-foundation.md)

## Summary

Live Preview Admin was bouncing users to login, flickering, looping API/culture reloads, showing a development banner, inconsistent EN vs Tagalog menus, and broken dark mode (shell only). This pass fixes those without starting P15-WP02.

## Auth / session (Interactive Server)

| Change | Why |
|---|---|
| `PlatformCircuitSession` + `PlatformSessionCircuitHandler` | Carry session token on the Blazor circuit (HttpContext alone is unreliable after circuit open) |
| Companion cookie `.ExItS.Admin.Session` via `PlatformBrowserSessionService` | Survive circuit reconnect / token resolution |
| `PlatformApiClient` token resolve: circuit → HttpContext/cookie → `AuthenticationStateProvider` | Stop Forbidden / silent auth failures on API calls |
| Softened aggressive session gate | Avoid logging users out when `/auth/me` fails transiently |
| Global `@rendermode InteractiveServer` on `Routes` in `App.razor`; strip per-page islands | One circuit — avoids multi-circuit flicker and request storms |
| Live Preview identity `<select onchange=…requestSubmit()>` | Auto-login on select; no second “Sign in as test user” button |
| Login native field CSS forced light `color-scheme` | Dark native inputs were unreadable |

## Navigation / permissions / UX

| Change | Why |
|---|---|
| `PlatformPermissionState`: OrdinalIgnoreCase; Live Preview fail-open to all permissions if `/authorization/me` fails; `HasPermission` requires `Loaded` | English showed only 2 items while Tagalog showed full menu |
| `AdminNav` waits for permissions; hides empty sections; localized titles | Consistent sidebar |
| Removed `EnvironmentBanner` + related resx | No unauthenticated “Development-stage” banner |
| Navbar: removed username chip (`preview-platform-admin`); Account dropdown remains | Cleaner Pro header |
| Root/login routing stays on login first | Do not land guests on Dashboard |

## Theme (sun / moon)

| Change | Why |
|---|---|
| `ThemeSelector`: native `<button>` + sun/moon SVG (not Ant `Button`/`Title`, not dropdown) | Ant `Button` has no `Title`/`AriaLabel` — those crashed the page |
| `ThemeService`: binary light/dark + `ToggleLightDarkAsync` | Header toggle only |
| `theme-boot.js`: set `data-theme`; swap `ant-design-blazor.css` ↔ `ant-design-blazor.dark.css` (`#exits-antd-theme`) | Dark mode previously only flipped shell CSS; tables stayed light on black void |
| `app.css`: `--exits-surface`; dark overrides after light defaults; taller content surface | Cohesive Pro dark shell |
| Culture selector: same-value / `_ready` guards; `CultureService` no-op if already set | Stop forceLoad remount loop |

## Tests updated

- `AdminArchitectureGuardTests` — ThemeSelector, dark CSS swap, no ThemeHost/EnvironmentBanner/ActorDisplayName chip
- `ThemeServiceTests` — binary storage values
- `PlatformApiClientTests` — token resolution paths
- `LocalizationResourceTests` — banner keys removed as needed

## Explicit non-goals (unchanged)

- Do **not** start P15-WP02 / P14-WP03 without authorization
- POS UI unchanged; `http://localhost:8092/` is API-only (use `/health`)
- No force-push / history rewrite / rollback of this work

## Verify locally

```powershell
.\tools\Start-LivePreviewLocal.ps1
# Admin http://localhost:8090 — login → Organizations; toggle sun/moon; switch English/Tagalog
dotnet test tests/ExItS.Platform.Admin.UnitTests/ExItS.Platform.Admin.UnitTests.csproj
```
