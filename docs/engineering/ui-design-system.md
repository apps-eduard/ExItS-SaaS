# UI Design System and Reusable Components

[Home](../index.md) | [Component catalog](reusable-component-catalog.md) | [Localization](localization.md) | [Themes](theme-system.md) | [ADR-010](../decisions/ADR-010-separate-ui-implementations-platform-and-pos.md) | [Approved architecture](approved-architecture-summary.md)

This document defines the ExItS UI architecture and reusable presentation standards.

---

## Product UI decisions

| Product | UI stack | Notes |
|---|---|---|
| **ExItS Platform Admin** | **Ant Design Blazor** (`AntDesign`, ADR-015) | Pro Blazor as design reference only; **no Tailwind**; **no Fluent UI**; compact enterprise console |
| PinoyBusinessPOS | **Native foundation** (MAUI Blazor Hybrid) | Shared DesignSystem tokens/localization with POS; **no Ant**; **no Tailwind** |
| Shared | Models, token **names**, localization keys, validation/formatting | Not one framework-switching component; Admin uses Ant; POS uses DesignSystem |

Visual consistency for **POS** comes from DesignSystem semantic tokens. **Platform Admin** uses Ant Design Blazor (ADR-015) with restrained ExItS branding tokens — not a dual visible design system and not Fluent UI.

### Platform Admin (P15-WP01 — Ant Design)

Commercial Admin shell: Ant Design `Layout` / `Sider` / `Header` / `Content` / `Menu`, compact tables/forms, Light/Dark/System theme. Permission-aware nav is UI convenience only. Credential login only (no Live Preview / identity quick-login). Residual native/report controls remain only on pages not yet migrated. **No Tailwind. No Fluent UI.**

### Historical note (P4-WP04 native shell)

P4-WP04 delivered a native-CSS Admin shell. That direction is **superseded for Admin UI chrome** by ADR-015 / P15-WP01. Do not reinstate native-only or Fluent Admin requirements.

### Shared DesignSystem library (P5-WP01–P5-WP04)

`src/Shared/ExItS.DesignSystem` is a `net10.0` Razor class library with semantic `--exits-*` tokens (including secondary, accent, info, disabled, z-index, easing, breakpoints), System/Light/Dark theme hooks, Compact/Comfortable density (Compact default for POS), shared Blazor primitives plus MVP forms/feedback/data components, and `DesignSystemResources` (`en` + `fil-PH`). Density preference is an abstraction (`IDensityPreferenceStore`); hosts implement storage. No Ant Design, Tailwind, Bootstrap CSS framework imports, EF Core, or Platform/product Infrastructure references. Consumed by PinoyBusinessPOS MAUI. **Platform Admin uses Ant Design Blazor (ADR-015)** with minimal branding CSS — DesignSystem remains the POS/shared-native library, not the Admin chrome stack. Dev-only component showcase lives in the MAUI host (`/dev/components`), not in DesignSystem.

### Proposed project boundaries (updated P5-WP01)

```text
Shared/
└── ExItS.DesignSystem          # tokens, primitives, DesignSystemResources (P5-WP01)

Platform/
└── ExItS.Platform.Admin (Blazor Web App — Ant Design Blazor; ADR-015 / P15-WP01)

Products/PinoyBusinessPOS/
├── ExItS.PinoyBusinessPOS.Domain
├── ExItS.PinoyBusinessPOS.Application
├── ExItS.PinoyBusinessPOS.Infrastructure
├── ExItS.PinoyBusinessPOS.Api
├── ExItS.PinoyBusinessPOS.ApiClient
└── ExItS.PinoyBusinessPOS.Maui   # Android-first MAUI Blazor Hybrid
```

Platform Admin: Blazor Web App. POS: .NET MAUI Blazor Hybrid (Android-first; future iOS/Windows). Both use native shared conventions above.

---

## Density modes

Density is applied via `[data-density="compact|comfortable"]`. Components read `--exits-control-height`, `--exits-density-*`, and `--exits-touch-target-min` — they are **not** duplicated per density.

### Compact (POS default)

Use for: PinoyBusinessPOS cashier shell, dense scanning, future inventory lists.

Goals: high information density and reduced whitespace while keeping touch targets ≥ **44px** (`--exits-touch-target-min: 2.75rem`).

### Comfortable

Use for: optional larger padding on phones/tablets, forms, future customer/Utang workflows.

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

Platform Admin and POS both use semantic CSS custom properties (`--exits-*`) per [theme-system.md](theme-system.md), with framework-specific implementations.

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

**P4-WP04:** Platform Admin implements System / Light / Dark via semantic CSS tokens, header selector, `localStorage` persistence, and `theme-boot.js` flash prevention.

**P5-WP01:** PinoyBusinessPOS MAUI implements System / Light / Dark via DesignSystem `--exits-*` tokens, Settings selector, MAUI Preferences + `localStorage` mirror, and `theme-boot.js`. Density tokens exist; compact layout polish is P5-WP02.

---

## Localization

Initial languages: **English (`en`)** and **Filipino (`fil` / `fil-PH`)**.

- Resource-based; no hard-coded strings in reusable POS components.
- Localized navigation, validation, statuses, empty states, dates, numbers, currency (PHP).
- Persist language preference; English fallback; detect missing keys in tests.
- Configuration language name: **Filipino**; Tagalog wording may appear in copy.
- Do **not** claim all Philippine languages.

**P4-WP04:** Platform Admin ships `AdminResources` (`en` + `fil-PH`) for shell/nav/shared components; see [localization.md](localization.md) and [admin-terminology-guide.md](admin-terminology-guide.md).

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

All web and mobile surfaces must honor reduced-motion preferences.

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

## Dropdown / selection strategy

Generic `SelectField<T>`: label, placeholder, required/disabled/loading/empty, validation, single select (multi later), searchable later, keyboard, clear, localized text, both densities and themes.

Selection controls use debounced search where needed, permission gates, and typed identifiers rather than free-text IDs.

---

## Calendar / date strategy

### MVP

`DateField` wraps native/platform date input; CSS-styled; localized label/validation; min/max; clear; themes; densities; mobile-friendly.

### Deferred rich calendar

Only when approved needs require range, presets, disabled dates, overdue highlighting, full keyboard calendar grid, etc.

Do **not** couple POS date controls to Ant Design.

---

## Reusable components

See [reusable-component-catalog.md](reusable-component-catalog.md) for phase classification (`Ex*` naming).

Naming in earlier drafts (`ExTextField`, …) remains the documentation convention until projects are created.

---

## Explicit non-goals

- No Tailwind in POS or Platform Admin.
- No Ant Design in PinoyBusinessPOS.
- No single component that switches Ant vs native at runtime.
