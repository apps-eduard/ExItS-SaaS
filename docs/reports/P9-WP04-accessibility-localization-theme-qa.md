# P9-WP04 — Accessibility, Localization and Theme QA

Phase marker: `P9-WP04-accessibility-localization-theme-qa`

## Status

**Complete with documented risks.** Completed accessibility, localization, responsive-layout, and theme QA hardening across Platform Admin and PinoyBusinessPOS MVP. **No new business features.** P9-WP01–P9-WP03 controls preserved. **Not production-ready** while R-091, R-109, R-129, and related blockers remain open. **P9-WP05 was not started.**

Feature commit: f7b3aecec614eea8b1de601cd08e843f4aea91f8

## Accessibility target

Engineering target: **WCAG 2.2 AA** where applicable.

**Not a formal certification.** No accredited audit was performed.

### What was tested

| Area | Method |
|---|---|
| Semantics / ARIA | Source review; Dialog/ConfirmDialog `aria-labelledby` / `aria-describedby`; status `role`/`aria-live`; MoneyDisplay `aria-label`; skip links |
| Keyboard / focus | Source review; Escape closes dialogs; focus moved into dialog on open (best-effort); visible `:focus-visible` tokens |
| Touch targets | DesignSystem `--exits-touch-target-min: 2.75rem` architecture guard |
| Contrast | Automated relative-luminance checks for DesignSystem light/dark text-on-surface (≥ 4.5:1) |
| Color alone | StatusBadge text + `data-status` + marker; DesignSystem Badge tone + label |
| Reduced motion | `prefers-reduced-motion` present in DesignSystem and Admin CSS |
| Localization | EN↔fil-PH resource parity; Admin page headers/states localized; POS bidirectional parity + skip link |
| Theme | System/Light/Dark tokens + preference persistence source guards |
| Charts | No graphical charts in MVP; reports use textual MoneyDisplay summaries |

### Remaining gaps (honest)

- No interactive TalkBack / keyboard walkthrough on a physical device or emulator (**R-109**)
- No axe/Pa11y automated browser scan in CI
- Some Admin runtime toast/confirm body strings in `@code` may still be English (API/dev-facing); primary chrome and PageHeader/state UI localized
- Placeholder technical codes (permission codes, action names) intentionally untranslated
- RTL **unsupported** (verified not enabled in theme-boot)

## Delivered fixes

| Area | Change |
|---|---|
| Admin localization | ~260+ AdminResources EN/fil-PH keys; pages wired to `L[...]` |
| StatusBadge | Localized status labels; color not sole cue |
| Skip links | Admin + POS shells |
| Dialogs | `aria-labelledby`, confirm `aria-describedby`, focus on open, Escape |
| MoneyDisplay | Accessible label for currency amounts |
| Theme/a11y tests | Contrast, skip links, dialog semantics, culture fallback, resource parity |

## Culture / formatting

- Supported cultures: `en`, `fil-PH`
- Unsupported culture preference falls back to `en` (`MauiCulturePreferenceStore.Normalize`)
- Filipino uses `fil-PH` regional conventions for number/date display via `CultureInfo`
- Calendar dates with `asUtc: true` preserve calendar day (verified)
- Monetary rounding remains server-authoritative; display uses `N2` + `PHP` prefix
- RTL: unsupported

## Theme architecture

- DesignSystem `--exits-*` tokens; Admin `--color-*` with `--exits-*` aliases
- Persistence: Admin `exits-admin-theme` localStorage; POS `exits-pos-theme` Preferences + WebView mirror
- System follows `prefers-color-scheme`
- Reduced-motion zeroes motion tokens

## Responsive layout

- Established phone-card / tablet-table (`ResponsiveDataList`) patterns retained
- Long Filipino nav wrap CSS retained
- No separate business logic per layout

## Manual QA matrix

Environment: **source review + automated tests** on developer workstation. Interactive Android device **not available** (R-109).

Legend: P = Pass (source/automated), NT = Not tested interactively, L = Environment limitation

| Workflow | EN+Light | EN+Dark | EN+System | fil+Light | fil+Dark | fil+System |
|---|---|---|---|---|---|---|
| Admin nav / theme / language | P | P | P | P | P | P |
| Admin org/users/subscriptions/payments states | P | P | P | P | P | P |
| Admin audit / unauthorized | P | P | P | P | P | P |
| POS shell / sync status / settings | P | P | P | P | P | P |
| Customer / credit / repayment UI resources | P | P | P | P | P | P |
| Catalog / sales / Utang / inventory / expenses | P | P | P | P | P | P |
| Dashboard / reports textual summaries | P | P | P | P | P | P |
| Offline/reconnect messaging resources | P | P | P | P | P | P |
| TalkBack / large text interactive | L/NT | L/NT | L/NT | L/NT | L/NT | L/NT |
| Touch / keyboard-overlap interactive | L/NT | L/NT | L/NT | L/NT | L/NT | L/NT |

## Android evidence

| Check | Result |
|---|---|
| Android Release (`net10.0-android`) | Succeeded (NU1903 warnings retained as R-129) |
| Interactive TalkBack / font scaling / touch | **Not claimed** (R-109) |

## Build / test evidence

| Check | Result |
|---|---|
| `dotnet build ExItS.slnx -c Release` | Succeeded (0 errors; known NU1903/NU1510 warnings retained) |
| `dotnet test ExItS.slnx -c Release` | **950 / 0 / 0** (baseline 931) |

## Explicit exclusions

- New business workflows; production auth; POS roles
- Tax/refund/accounting/gateway/receipt printing/export
- New languages; new UI framework; full redesign; RTL
- P9-WP05 or later
- Formal WCAG certification

## Security / reliability

P9-WP01 Production guards and P9-WP02 health/readiness unchanged. P9-WP03 backup tooling unchanged. No cross-org weakening.

## HealthCare freeze

Unchanged: ignored, untracked, outside `ExItS.slnx`.

## Unresolved risks / release blockers

- R-091 production auth
- R-109 interactive Android a11y/theme validation
- R-129 SQLitePCLRaw / local encryption
- Residual Admin `@code` English toasts (non-blocking for MVP chrome)
- Contrast beyond primary text/surface pairs not exhaustively measured for every badge/overlay combination
- Production TLS / MAUI cleartext gate

## Exact next work package

**P9-WP05 — Pilot and Deployment** (do not begin until authorized)

## Git evidence

| Item | Value |
|---|---|
| Feature commit | f7b3aecec614eea8b1de601cd08e843f4aea91f8 |
| Docs commit | _(recorded after docs commit)_ |
| Exact next WP | **P9-WP05 — Pilot and Deployment** |
