# Phase 5 — PinoyBusinessPOS MAUI Foundation

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-04-platform-admin.md) | [Next](phase-06-utang-mvp.md) | [P5-WP02 report](../reports/P5-WP02-native-ui-tokens-themes-and-compact-layout.md)

## Objective

Create the PinoyBusinessPOS MAUI foundation with compact native UI, localization and themes.

## Status

**In Progress** — P5-WP01 and P5-WP02 complete. Do **not** begin P5-WP03 until explicitly authorized.

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

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence.
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded (`3015925`).
- [x] Working tree clean (after push).

### P5-WP02 — Native UI Tokens, Themes and Compact Layout

Status: **Complete**

#### Outcomes

- Standardized semantic `--exits-*` tokens (secondary, accent, info, disabled, z-index, easing, breakpoints)
- Compact (default) and Comfortable density with persistence and pre-paint boot
- Polished shell (top bar, bottom nav, phone/tablet/landscape), Home/Settings density selector
- Touch targets remain ≥44px in compact; reduced-motion honored
- Phase marker `P5-WP02-native-ui-tokens-themes-compact-layout`
- Release Android build succeeded; interactive emulator validation unavailable (documented)

Report: [P5-WP02-native-ui-tokens-themes-and-compact-layout.md](../reports/P5-WP02-native-ui-tokens-themes-and-compact-layout.md)

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence.
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded (`3d3cba8`).
- [x] Working tree clean (after push).

### P5-WP03 — English and Filipino Localization

Status: Not Started

#### Required outcomes

- Add English and Filipino/Tagalog resources.
- Remove hard-coded user-facing strings.
- Add resource-completeness validation.

#### Definition of Done

- [ ] Approved outcomes complete.
- [ ] Applicable tests pass with exact evidence.
- [ ] Dashboard and phase page updated.
- [ ] Completion report created.
- [x] Focused commit created and hash recorded (`3d3cba8`).
- [x] Working tree clean (after push).

### P5-WP04 — Reusable MVP Components

Status: Not Started

#### Required outcomes

- Build only required native components: fields, select, date wrapper, table, dialog and feedback.
- Review HealthCare components before creating each abstraction.
- Keep Ant Design out of POS UI projects.

#### Definition of Done

- [ ] Approved outcomes complete.
- [ ] Applicable tests pass with exact evidence.
- [ ] Dashboard and phase page updated.
- [ ] Completion report created.
- [x] Focused commit created and hash recorded (`3d3cba8`).
- [x] Working tree clean (after push).

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
- [x] Focused commit created and hash recorded (`3d3cba8`).
- [x] Working tree clean (after push).

## Phase exit criteria

- [ ] Every work package is complete or explicitly deferred.
- [ ] Risks and decisions are recorded.
- [ ] Required regression/security tests pass.
- [ ] Next phase is explicitly approved.
