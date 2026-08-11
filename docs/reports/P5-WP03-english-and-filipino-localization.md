# P5-WP03 — English and Filipino Localization

[Phase 5](../phases/phase-05-pos-maui-foundation.md) | [Portfolio](../portfolio-progress.md) | [Previous: P5-WP02](P5-WP02-native-ui-tokens-themes-and-compact-layout.md)

## 1. Status

**Complete.** PinoyBusinessPOS and shared DesignSystem localization foundation covers English (fallback) and Filipino (`fil-PH`) with resource-completeness tests, culture-aware formatting, and localized error mapping. Phase marker: `P5-WP03-english-filipino-localization`.

## 2. Culture and resource architecture

| Resource set | Owner | Purpose |
|---|---|---|
| `DesignSystemResources` | Shared DesignSystem | Empty/error/loading/search defaults, shared actions, status labels |
| `ValidationResources` | Shared DesignSystem | Required / invalid selection / number / format |
| `ErrorResources` | Shared DesignSystem | Unauthorized, forbidden, timeout, offline, unavailable, preference-save failure |
| `PosResources` | PinoyBusinessPOS.Maui | Shell, Home, Settings, deferred, NotFound, API diagnostics |

- Technical culture: **`fil-PH`**
- UI language label: **Tagalog** (consistent)
- English is the default / fallback culture
- Preference persistence via existing `ICulturePreferenceStore` / `CultureController` / `theme-boot.js`
- No runtime machine translation

## 3. Terminology

See [pos-terminology-guide.md](../engineering/pos-terminology-guide.md). Shared DesignSystem stays free of POS product terms (Utang, GCash, PinoyBusinessPOS).

## 4. Shared component and MAUI localization

DesignSystem Dialog/Drawer/Toast/SearchBox/Button loading resolve localized defaults. MAUI shell, Home, Settings, deferred pages, and NotFound use `IStringLocalizer<PosResources>`. `ApiStatusLocalizer` maps `ApiCallStatus` to safe localized title/message pairs (diagnostic codes optional; no stack traces or raw ProblemDetails in UI).

## 5. Formatting

`ExItS.PinoyBusinessPOS.Application.Formatting.CultureFormatting` centralizes date/time (UTC label), number, percent, and display-only currency formatting without mutating stored values or introducing business FX logic.

## 6. Responsive and accessibility

Long Tagalog nav labels use wrap/`line-clamp` CSS from P5-WP02. Accessibility labels (nav primary, close/dismiss/clear, loading) follow the active culture. Theme and density behavior unchanged.

## 7. Explicit exclusions

- Sales, inventory, customers, Utang, repayments, auth, offline sync, gateways
- Interactive emulator validation (no device attached — R-109)
- P5-WP04 reusable MVP components

## 8. Tests

| Suite | Passed |
|---|---:|
| Unit | 261 |
| Architecture | 41 |
| Admin unit | 27 |
| DesignSystem | 17 |
| ApiClient | 17 |
| Maui | 15 |
| Integration | 84 |
| **Total** | **462** |

Baseline 447 not reduced (net +15).

## 9. Android evidence

Release `net10.0-android` build succeeded. `adb devices` empty — interactive validation not claimed.

## 10. HealthCare freeze

Root `HealthCare/` must remain absent/untracked and outside `ExItS.slnx`.

## 11. Exact next work package

**P5-WP04 — Reusable MVP Components**

Do not begin until explicitly authorized.

## 12. Commits

| Kind | Message | Hash |
|---|---|---|
| Feature | `feat(pos): complete English and Filipino localization foundation` | `1dea793407adaa9e8a27c19f45727bc90d866f60` |
