# Pre-Phase-11 — Admin Theme Persistence and Light-Mode Visibility Reopen

Package: **Pre-Phase-11 Admin Theme Persistence and Light-Mode Visibility Reopen**  
Prior reported (incomplete) tip: `4b02252187cecadfee9a97db63d4de6e65724f57`  
Feature tip (this reopen): `46b99a7f6baa87977fb0ed37e678231fa1eb1344`  
Phase marker: unchanged (`P10-WP08-phase-10-closeout`) — Phase 11 not started.

## Status

**Complete.** Dark preference now survives enhanced sidebar navigation, refresh, and return to Dashboard. Light theme surfaces, borders, sidebar contrast, and hierarchy were strengthened. Runtime reproduction required by this reopen **passed**.

## Mandatory runtime reproduction (passed)

Host: `http://127.0.0.1:5289/admin` (Playwright Chromium against the live Admin process).

| Step | Result |
|---|---|
| Hard refresh `/admin` | `data-theme=system` until selection |
| Select Dark | `localStorage exits-admin-theme=dark`; `<html data-theme=dark>`; `<body data-theme=dark>` |
| Products → Organizations → Subscriptions → Payments → Users → Entitlements → Audit | Each stop remained `html/body data-theme=dark`, storage `dark` |
| Refresh Audit | Remained Dark |
| Navigate to Dashboard | Remained Dark |
| `Blazor.addEventListener` hook | `__exitsThemeEnhancedBound=true` |

Navigation used Blazor enhanced client navigation (URL changes without losing the theme boot hook). No second theme storage key was observed.

## Root cause (actual)

Previous package registered **`document.addEventListener("enhancedload", …)`**. Blazor does **not** emit that event on `document`. Enhanced navigation can strip client-applied `data-theme` from `<html>` while the interactive `ThemeSelector` island often **does not remount**, so `InitializeAsync` never re-ran. CSS then fell back to Light `:root` tokens → UI appeared to “reset to Light” even though storage could still say `dark`.

Contributing gaps fixed in the same reopen:

- No `Blazor.addEventListener('enhancedload', …)` re-apply
- No `NavigationManager.LocationChanged` re-apply belt
- No `<html data-permanent>`
- Light tokens still too flat / low border contrast; sidebar nav text/icons too dim

## One authoritative theme mechanism

| Concern | Authority |
|---|---|
| Preference values | Exactly `system` \| `light` \| `dark` (legacy PascalCase readable) |
| Storage key | `exits-admin-theme` only |
| DOM apply | `exitsAdminTheme.applyTheme` → `data-theme` on `<html>` and `<body>` |
| First paint | `theme-boot.js` in `<head>` |
| After enhanced nav | `Blazor.addEventListener('enhancedload', reapplyFromStorage)` |
| After location change | `ThemeSelector` → `reapplyFromStorage` |
| Interactive write | `ThemeService.SetThemeAsync` / `InitializeAsync` |

## Light visibility changes

- Cooler teal canvas (`#cfdeda`), darker ink/muted text, stronger borders (`#7f9a96`)
- Stronger `--shadow-sm` / `--shadow-md` / sidebar shadow
- Panels/cards/tables/forms use thicker borders and clearer surface lift
- Sidebar: deeper gradient, near-white nav labels/icons, stronger active/hover

## Explicit exclusions

No Phase 11 / P11-WP01; no business features; no report redesign; untracked Phase 11/12 / Product-Foundation docs left untouched.

## Tests

- Full `ExItS.slnx` Release: **1160 passed / 0 failed / 0 skipped**
- Architecture guard now requires `Blazor.addEventListener`, rejects `document.addEventListener("enhancedload"`, requires `LocationChanged` + `data-permanent`

## Portfolio independence

- No root `HealthCare/` tree; `git ls-files -- HealthCare/` empty; solution has no HealthCare project

## Exact next

**Phase 11 — Web UI and Reporting Design System / P11-WP01** when explicitly authorized. Do not begin until approved.
