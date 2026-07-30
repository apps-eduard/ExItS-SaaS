# UI Design System and Reusable Components

[Home](../index.md) | [UI reuse assessment](../reuse/healthcare-ui-reuse-assessment.md) | [Component catalog](reusable-component-catalog.md) | [Localization](localization.md) | [Themes](theme-system.md) | [ADR-010](../decisions/ADR-010-separate-ui-implementations-platform-and-pos.md) | [Approved architecture](approved-architecture-summary.md)

Validated against HealthCare evidence in **P0-WP03**. No application UI was implemented in this work package.

---

## Product UI decisions

| Product | UI stack | Notes |
|---|---|---|
| HealthCare Staff Web | **Ant Design Blazor** (retain) | No rewrite/modernization in current ExITS work |
| HealthCare PatientWeb | Native CSS (retain) | Product-specific; pattern source for new native apps |
| HealthCare MAUI | Existing native CSS (retain) | No rewrite in current work |
| **New ExItS Platform Admin** | **Native CSS + CSS isolation + Razor components** | Blazor Web App; **no Ant Design**; **no Tailwind** |
| PinoyBusinessPOS | **Same native foundation** (MAUI Blazor Hybrid) | Shared tokens/localization/models with Platform Admin; **no Ant**; **no Tailwind** |
| Shared | Models, token **names**, localization keys, validation/formatting | Not one framework-switching component; **not** Ant |

Visual consistency across **new** Platform Admin and POS comes from shared semantic tokens, typography, spacing, theme/localization/accessibility/motion standards, and UI-independent models — **not** from Ant Design.

### Platform Admin redesign (P4-WP04)

Commercial Admin shell: collapsible sidebar (checkbox CSS), mobile drawer, sticky header, environment chip, shared design-system components (page header, filters, empty/loading/error, audit timeline, theme/language selectors), responsive tables/cards (≈320–1920px). Permission-aware nav is UI convenience only. Keyboard-usable controls; `prefers-reduced-motion` respected. No Ant Design; no Tailwind.

### Shared DesignSystem library (P5-WP01)

`src/Shared/ExItS.DesignSystem` is a `net10.0` Razor class library with semantic `--exits-*` tokens, System/Light/Dark theme hooks, compact/comfortable density attributes, shared Blazor primitives (inputs, layout, feedback/empty/error), and `DesignSystemResources` (`en` + `fil-PH`). No Ant Design, Tailwind, Bootstrap, EF Core, or Platform/product Infrastructure references. Consumed by PinoyBusinessPOS MAUI; Platform Admin continues native Admin CSS tokens (`--color-*`) while sharing semantic conventions and terminology.

### Proposed project boundaries (updated P5-WP01)

```text
Shared/
└── ExItS.DesignSystem          # tokens, primitives, DesignSystemResources (P5-WP01)

Platform/
└── ExItS.Platform.Admin (Blazor Web App — native UI; P4-WP01–04)

Products/PinoyBusinessPOS/
├── ExItS.PinoyBusinessPOS.Application
├── ExItS.PinoyBusinessPOS.ApiClient
└── ExItS.PinoyBusinessPOS.Maui   # Android-first MAUI Blazor Hybrid (P5-WP01)
```

Platform Admin: Blazor Web App. POS: .NET MAUI Blazor Hybrid (Android-first; future iOS/Windows). Both use native shared conventions above.

---

## Density modes

### Compact

Use for: Platform administration, Windows POS, dense reports, inventory, subscription tables.

Goals: high information density, reduced whitespace, fast scanning — without tiny text or inaccessible targets.

### Comfortable

Use for: phones, tablets, forms, customer/Utang workflows, touch-heavy cashier steps.

### Proposed token values (documentation only — not implemented)

| Token | Compact | Comfortable |
|---|---|---|
| Body font size | 13–14px | 15–16px |
| Input height | 32px | 40–44px |
| Button height | 32px | 40–44px |
| Table row height | 36–40px | 48–52px |
| Base spacing unit | 4px scale (8/12/16) | 4px scale (12/16/24) |
| Card padding | 12px | 16–20px |
| Border radius | 4–6px | 6–8px |
| Icon size | 16px | 20px |
| Touch-target minimum | ≥44×44 CSS px for primary touch actions | ≥44×44 CSS px |

Compact must never hide validation, status, or critical financial information. Dense table rows on desktop may use smaller row height but interactive controls still meet touch minimums when the surface is touch-capable.

---

## Design tokens (semantic)

Prefer semantic names over component-hardcoded colors:

```text
background / surface / surface-elevated
text-primary / text-secondary
border
primary / primary-hover
success / warning / danger / information
focus-ring
overlay / shadow
density-compact-* / density-comfortable-*
motion-fast / motion-base / motion-slow
```

New Platform Admin and POS both use CSS custom properties (`--exits-*`) per [theme-system.md](theme-system.md). Existing HealthCare Staff Web may keep its Ant/`--hc-*` styling unchanged.

---

## Typography and spacing

- Expressive, readable type (avoid default Inter/Roboto/Arial as brand identity for POS marketing surfaces; product UI may use a deliberate system stack).
- Spacing scale based on 4px.
- Support zoom 100–200% without loss of critical controls.

---

## Themes

Required modes: **Light**, **Dark**, **System**.

- Persist preference (user + local for signed-out).
- Immediate switch; follow OS when System.
- Accessible contrast and visible focus in both themes.
- Theme change must not restart the app or lose form state.
- See [theme-system.md](theme-system.md).

**P4-WP04:** Platform Admin implements System / Light / Dark via semantic CSS tokens, header selector, `localStorage` persistence, and `theme-boot.js` flash prevention. HealthCare today: light canvas tokens + dark **sider only** — not a product theme system.

**P5-WP01:** PinoyBusinessPOS MAUI implements System / Light / Dark via DesignSystem `--exits-*` tokens, Settings selector, MAUI Preferences + `localStorage` mirror, and `theme-boot.js`. Density tokens exist; compact layout polish is P5-WP02.

---

## Localization

Initial languages: **English (`en`)** and **Filipino (`fil` / `fil-PH`)**.

- Resource-based; no hard-coded strings in reusable POS components.
- Localized navigation, validation, statuses, empty states, dates, numbers, currency (PHP).
- Persist language preference; English fallback; detect missing keys in tests.
- Configuration language name: **Filipino**; Tagalog wording may appear in copy.
- Do **not** claim all Philippine languages.

**P4-WP04:** Platform Admin ships `AdminResources` (`en` + `fil-PH`) for shell/nav/shared components; see [localization.md](localization.md) and [admin-terminology-guide.md](admin-terminology-guide.md). HealthCare today: **no** localization foundation (do not add during Phase 0).

**P5-WP01:** PinoyBusinessPOS ships `PosResources` + DesignSystem `DesignSystemResources` (`en` + `fil-PH`); see [localization.md](localization.md) and [pos-terminology-guide.md](pos-terminology-guide.md).

---

## Motion

Use motion for hierarchy and feedback, not decoration.

| Interaction | Duration |
|---|---:|
| Button feedback | 100–150 ms |
| Dropdown | 120–180 ms |
| Modal/drawer | 180–250 ms |
| Panel/page transition | 180–300 ms |
| Toast | 200–300 ms |
| Theme transition | 150–250 ms |

Prefer CSS `transform`/`opacity`. Honor **`prefers-reduced-motion`**: full usability with motion disabled. Never block cashier operations. Do not animate every table row or large datasets.

HealthCare lesson: staff `--hc-motion` + reduced-motion disable page enter; Mobile lacks reduced-motion.

---

## Responsive strategy

### Phone

One-column forms; bottom navigation where appropriate; cards/lists instead of wide tables; large touch targets; sticky primary actions; drawers; minimal modal width.

### Tablet

Two-column forms when useful; collapsible sidebar; split POS layouts; touch-optimized tables; landscape support. Future iPad compatible.

### Windows / desktop

Compact sidebar; dense tables; multi-column dashboards; split panels; fast cashier workflows; future keyboard shortcuts / multi-register.

Future iPhone/iPad: same responsive tokens and comfortable density defaults.

---

## Accessibility

Minimum requirements:

- Semantic HTML and associated labels
- Keyboard navigation and visible focus
- Screen-reader names; error association
- Modal focus trap + Escape
- Contrast; non-color status cues
- Touch-target minima; reduced motion; zoom/text scaling
- Logical tab order; accessible table headings

---

## Table strategy

Reusable native table must support: compact + comfortable density; sort; search; filter; pagination; loading/empty/error; row actions; optional selection; localized headings/messages; desktop table + mobile card/list; keyboard/focus; server-side paging via shared models (inspired by `PagedResponse<T>`).

| Surface | Table needs |
|---|---|
| Platform Admin | Dense org/subscription/audit tables (**native** CompactDataTable) |
| Utang customers / ledger | Compact desktop + card mobile; money/date display |
| Products / inventory / sales | Dense Windows; touch-friendly filters |
| Subscriptions | Admin compact tables |

Do not implement in P0-WP03.

---

## Dropdown / selection strategy

Generic `SelectField<T>`: label, placeholder, required/disabled/loading/empty, validation, single select (multi later), searchable later, keyboard, clear, localized text, both densities and themes.

HealthCare picker lesson: debounced search, permission gates, **no free-text IDs** (`ClinicPicker`, `OrganizationPicker`, `PatientPicker`) — reuse the **API**, not Ant `Select`.

---

## Calendar / date strategy

### MVP

`DateField` wraps native/platform date input; CSS-styled; localized label/validation; min/max; clear; themes; densities; mobile-friendly.

### Deferred rich calendar

Only when approved needs require range, presets, disabled dates, overdue highlighting, full keyboard calendar grid, etc.

Do **not** copy Ant `DatePicker` into POS. Appointment calendar pages remain HealthCare-specific UX lessons only.

---

## Reusable components

See [reusable-component-catalog.md](reusable-component-catalog.md) for phase classification (`Ex*` naming).

Naming in earlier drafts (`ExTextField`, …) remains the documentation convention until projects are created.

---

## Explicit non-goals

- No Tailwind in POS or **new** Platform Admin.
- No Ant Design in PinoyBusinessPOS or **new** Platform Admin.
- No single component that switches Ant vs native at runtime.
- No HealthCare UI rewrite in current ExITS MVP work.
- No implementation of tokens/components in P0-WP03.
