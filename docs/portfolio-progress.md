# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [All Phases](phases/README.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP at ignored nested `HealthCare/` |
| New product | PinoyBusinessPOS |
| Current phase | Phase 0 — Existing HealthCare Assessment |
| Current work package | P0-WP03 — Ant Design and UI Reuse Review (**Ready for Review**) |
| Overall status | In Progress |
| Latest verified commit | Correction `e310cf87cb03befdd55962b8c858ed19dfe5add1` (`docs(ui): correct platform admin UI decision`); prior P0-WP03 `5d628dd60b3793108cc6645992ed0a014e034e27` |
| Open blockers | 0; root remote still empty (first push needs user authorization) |
| Last updated | 2026-07-29 |

## Delivery sequence

```text
Assess completed HealthCare MVP
        ↓
Approve platform/product boundaries
        ↓
Extract or adapt ExITS Platform safely
        ↓
Reconnect and regression-test HealthCare
        ↓
Add portfolio plans, billing and entitlements
        ↓
Build PinoyBusinessPOS MAUI foundation
        ↓
Utang MVP
        ↓
Offline synchronization
        ↓
Basic Store
        ↓
MVP hardening and commercial release
        ↓
Full POS after MVP
```

## Phase progress

| Phase | Name | Status | Completed | Total | Progress | Link |
|---:|---|---|---:|---:|---:|---|
| 0 | Existing HealthCare Assessment | In Progress | 3 | 4 | 75% | [Open](phases/phase-00-healthcare-assessment.md) |
| 1 | Platform Boundary and Architecture | Not Started | 0 | 4 | 0% | [Open](phases/phase-01-platform-boundary.md) |
| 2 | Platform Extraction and HealthCare Reconnection | Not Started | 0 | 6 | 0% | [Open](phases/phase-02-platform-extraction.md) |
| 3 | Portfolio Billing, Plans and Entitlements | Not Started | 0 | 5 | 0% | [Open](phases/phase-03-billing-entitlements.md) |
| 4 | Platform Admin Expansion | Not Started | 0 | 4 | 0% | [Open](phases/phase-04-platform-admin.md) |
| 5 | PinoyBusinessPOS MAUI Foundation | Not Started | 0 | 5 | 0% | [Open](phases/phase-05-pos-maui-foundation.md) |
| 6 | Utang MVP | Not Started | 0 | 6 | 0% | [Open](phases/phase-06-utang-mvp.md) |
| 7 | Offline Synchronization | Not Started | 0 | 5 | 0% | [Open](phases/phase-07-offline-sync.md) |
| 8 | Basic Store | Not Started | 0 | 7 | 0% | [Open](phases/phase-08-basic-store.md) |
| 9 | MVP Hardening and Release | Not Started | 0 | 6 | 0% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | Future | 0 | 8 | 0% | [Open](phases/phase-10-full-pos.md) |

Phase 10 is excluded from the first commercial MVP percentage.

**MVP phases 0–9:** 3 / 52 work packages = **5.77%** (3÷52).

## Completed work packages

### P0-WP01 — Repository and Reuse Inventory — Complete

Commit `663b5bf3269ee934d107bacc467d253a4bf28a90`.

### P0-WP02 — Baseline Build, Tests, Runtime and Repository Safety Map — Complete

Commit `6b56e6dfec93f49e43a9c1a92baea1300d148b28`. Root `.gitignore` ignores `HealthCare/`. Tests baseline 1102/0/0.

### P0-WP03 — Ant Design and UI Reuse Review — Ready for Review

- [x] Inventories Staff Web, PatientWeb, Mobile UI stacks.
- [x] Documents Ant Design 1.6.2 usage and wrappers.
- [x] Platform Admin **native** CSS/Razor (no Ant); POS same native foundation; HC Staff keeps Ant.
- [x] Density, theme, localization, motion, a11y, responsive, table/dropdown/date specs.
- [x] Component catalog + ADR-010.
- [x] Commit hash recorded.
- [x] Working tree clean after commit.

Artifacts:

- [UI reuse assessment](reuse/healthcare-ui-reuse-assessment.md)
- [UI design system](engineering/ui-design-system.md)
- [Component catalog](engineering/reusable-component-catalog.md)
- [ADR-010](decisions/ADR-010-separate-ui-implementations-platform-and-pos.md)
- [P0-WP03 report](reports/P0-WP03-ui-reuse-review.md)

## Latest tests

Unchanged from P0-WP02 Windows-safe baseline (**1102 / 0 / 0**). P0-WP03 is documentation-only; HealthCare tests not re-run.

## UI strategy (P0-WP03, corrected)

| Surface | Decision |
|---|---|
| HealthCare Staff Web | Retain Ant Design Blazor |
| HealthCare PatientWeb / MAUI | Retain existing native implementations |
| **New ExItS Platform Admin** | Native CSS + Razor (Blazor Web App); **no Ant**; **no Tailwind** |
| PinoyBusinessPOS | Same native foundation (MAUI Hybrid); **no Ant**; **no Tailwind** |
| Shared | Token names, localization conventions, UI-independent models |

Correction commit: `e310cf87cb03befdd55962b8c858ed19dfe5add1` (`docs(ui): correct platform admin UI decision`)

## Risks

See [risks-and-issues.md](risks-and-issues.md). R-005/R-006 updated for UI strategy.

## Next approved action

**P0-WP04 — Assessment Closeout and Recommendation** after P0-WP03 acceptance.
