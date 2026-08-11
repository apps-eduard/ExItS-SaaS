# P5-WP02 — Native UI Tokens, Themes and Compact Layout

[Phase 5](../phases/phase-05-pos-maui-foundation.md) | [Portfolio](../portfolio-progress.md) | [Previous: P5-WP01](P5-WP01-maui-solution-and-api-client.md)

## 1. Status

**Complete.** Shared ExItS design tokens, Compact/Comfortable density (Compact default), theme/localization polish, and mobile-first shell refinements delivered for PinoyBusinessPOS. No business workflows added. Phase marker: `P5-WP02-native-ui-tokens-themes-compact-layout`.

## 2. Delivered capability

- Standardized semantic `--exits-*` tokens (surfaces, primary/secondary/accent, status, disabled, typography, spacing, radius, shadows, z-index, motion/easing, breakpoints, density)
- Compact (default) and Comfortable density modes with persistence via `IDensityPreferenceStore` / `MauiDensityPreferenceStore` / `DensityController`
- Theme boot JS applies theme + density before first paint (reduces incorrect flash)
- Polished shared components already used by the shell (token-driven; no duplicated density implementations)
- Refined MAUI shell: compact top bar, active bottom nav, safe areas, phone/tablet/landscape CSS
- Home and Settings refined; density selector added; deferred routes unchanged (localized placeholders only)
- Accessibility: visible focus, ≥44px touch targets in compact, reduced-motion, badges+text for status (not color alone)
- EN / `fil-PH` strings for density and nav primary label

## 3. Explicit exclusions

- Sales, inventory, customers, Utang, repayments, reporting
- Authentication / secure token usage (`NullSecureTokenStore` stub only)
- Offline sync / SQLite / outbox
- Payment gateways, QR, card processing
- Product-local roles
- Interactive emulator/device walkthrough (no device attached — see §11)
- P5-WP03+ (dedicated localization completeness WP, reusable MVP components WP, auth/onboarding)

## 4. Token architecture

Documented in `ExItS.DesignSystem/wwwroot/exits-design-system.css` header and [theme-system.md](../engineering/theme-system.md) / [ui-design-system.md](../engineering/ui-design-system.md).

Same semantic names for Light and Dark; System follows `prefers-color-scheme`. Density attributes change spacing/control metrics without forking components.

## 5. Density system

| Mode | Default? | Notes |
|---|---|---|
| Compact | **Yes (POS)** | Tighter card/page padding and control height; `--exits-touch-target-min` remains **2.75rem (44px)** |
| Comfortable | Opt-in | Larger padding, font, row height |

Preference key: `exits-pos-density` (MAUI Preferences + WebView mirror for boot).

## 6. Shell and layouts

- Phone: bottom nav, stacked Home status cards, wrap-friendly Tagalog labels
- Tablet (≥768px): two-column status grid, optional brand tagline, centered bottom nav rail
- Landscape short height: hide nav labels to preserve content height
- Safe-area insets on top bar and bottom nav

## 7. Theme and localization

System / Light / Dark continue to persist and apply without restart. Density and language likewise. New Settings density strings localized EN/`fil-PH`.

## 8. Accessibility

Visible `:focus-visible`, semantic nav `aria-label`, icon-button labels, status badges with text labels, disabled tokens (not opacity-only for fields), `prefers-reduced-motion` zeroes motion tokens and kills transitions/animations.

## 9. Architecture boundaries preserved

MAUI / DesignSystem still have no Infrastructure, EF Core, Npgsql, or DbContext. DesignSystem has no product business logic or host storage APIs. No HealthCare coupling. No Ant Design / Tailwind framework.

## 10. Tests

| Suite | Passed |
|---|---:|
| Unit | 261 |
| Architecture | 41 |
| Admin unit | 27 |
| DesignSystem | 9 |
| ApiClient | 17 |
| Maui | 8 |
| Integration | 84 |
| **Total** | **447** |

Baseline 443 not reduced (net +4 focused tests).

## 11. Android build evidence

Release `net10.0-android` build succeeded with `AndroidSdkDirectory` set. `adb devices` listed **no emulator/device**. Interactive runtime validation (theme flash, density restart, Tagalog wrap on device) is **not claimed**; limitation recorded; risk R-109 remains open.

## 12. HealthCare freeze

Root `HealthCare/` must remain absent/untracked and outside `ExItS.slnx`.

## 13. Exact next work package

**P5-WP03 — English and Filipino Localization**

Do not begin until explicitly authorized.

## 14. Commits

| Kind | Message | Hash |
|---|---|---|
| Feature | `feat(pos): native UI tokens, density modes, and compact layout` | `3d3cba840ffff20dc07ae7237d7f81c3873a502e` |
