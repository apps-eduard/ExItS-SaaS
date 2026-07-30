# Phase 5 — PinoyBusinessPOS MAUI Foundation

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-04-platform-admin.md) | [Next](phase-06-utang-mvp.md) | [P5-WP04 report](../reports/P5-WP04-reusable-mvp-components.md)

## Objective

Create the PinoyBusinessPOS MAUI foundation with compact native UI, localization and themes.

## Status

**In Progress** — P5-WP01–P5-WP04 complete. Do **not** begin P5-WP05 until explicitly authorized.

## Work packages

### P5-WP01 — MAUI Solution and API Client

Status: **Complete**

#### Outcomes

- Android-first MAUI Blazor Hybrid shell (`ExItS.PinoyBusinessPOS.Maui`, `net10.0-android`) with Home, Settings, deferred nav placeholders
- Shared `ExItS.DesignSystem` Razor class library (semantic tokens, primitives, DesignSystemResources)
- Application abstractions + typed `PosApiClient` (health/connectivity classification, ProblemDetails, offline short-circuit, safe GET retry)
- System / Light / Dark theme preference foundation; EN/`fil-PH` `PosResources` + DesignSystem resources
- Architecture/DesignSystem/ApiClient/Maui tests; Release Android APK produced
- Phase marker `P5-WP01-maui-solution-api-client`
- No auth, sales, inventory, offline sync, or product database

Report: [P5-WP01-maui-solution-and-api-client.md](../reports/P5-WP01-maui-solution-and-api-client.md)

### P5-WP02 — Native UI Tokens, Themes and Compact Layout

Status: **Complete**

#### Outcomes

- Semantic `--exits-*` token expansion, Compact (default) / Comfortable density, shell polish
- Phone/tablet CSS, touch-target minima, motion tokens
- Phase marker `P5-WP02-native-ui-tokens-themes-compact-layout`

Report: [P5-WP02-native-ui-tokens-themes-and-compact-layout.md](../reports/P5-WP02-native-ui-tokens-themes-and-compact-layout.md)

### P5-WP03 — English and Filipino Localization

Status: **Complete**

#### Outcomes

- EN/`fil-PH` DesignSystem + Pos resources, Tagalog UI label, CultureFormatting, ApiStatusLocalizer
- Phase marker `P5-WP03-english-filipino-localization`

Report: [P5-WP03-english-and-filipino-localization.md](../reports/P5-WP03-english-and-filipino-localization.md)

### P5-WP04 — Reusable MVP Components

Status: **Complete**

#### Outcomes

- Forms, validation, confirmation, feedback, responsive data list/table, pagination, money display
- Dev/Testing-only `/dev/components` showcase (neutral samples; not in production nav)
- Components use existing theme, density, and localization foundations; no POS business logic
- Phase marker `P5-WP04-reusable-mvp-components`
- Tests **474** passed; Release Android APK succeeded; no interactive emulator (R-109)

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence.
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [ ] Focused commit created and hash recorded.
- [ ] Working tree clean (after push).

Report: [P5-WP04-reusable-mvp-components.md](../reports/P5-WP04-reusable-mvp-components.md)

### P5-WP05 — Authentication, Onboarding and Closeout

Status: Not Started

#### Required outcomes

- Implement only the approved scope described by the architecture and product documents.
- Add required tests and documentation evidence.
- Preserve security, tenant isolation and product boundaries.

#### Definition of Done

- [ ] Approved outcomes complete.
- [ ] Applicable tests pass with exact evidence.
- [ ] Dashboard and phase page updated.
- [ ] Completion report created.
- [ ] Focused commit created and hash recorded.
- [ ] Working tree clean (after push).

## Phase exit criteria

- [ ] Every work package is complete or explicitly deferred.
- [ ] Risks and decisions are recorded.
- [ ] Required regression/security tests pass.
- [ ] Next phase is explicitly approved.
