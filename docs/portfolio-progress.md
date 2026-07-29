# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [All Phases](phases/README.md) | [Phase 0 final assessment](reports/phase-00-final-assessment-and-recommendation.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (ignored nested `HealthCare/`) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 0 — **Closeout ready** → Phase 1 recommended |
| Current work package | P0-WP04 — Assessment Closeout (**Ready for Review**) |
| Overall status | Phase 0 recommended **Complete with documented risks** |
| Latest verified commit | `f52316ae60198cb3dfee367a8ec99d550965ea44` (`docs(phase0): close assessment and approve next direction`) |
| Open blockers | 0 for P1-WP01 docs; root remote empty (user push when authorized) |
| Last updated | 2026-07-29 |

## Delivery sequence

```text
Assess completed HealthCare MVP ✓
        ↓
Approve platform/product boundaries  ← next (Phase 1)
        ↓
Extract or adapt ExITS Platform safely
        ↓
Reconnect and regression-test HealthCare
        ↓
Add portfolio plans, billing and entitlements
        ↓
Build PinoyBusinessPOS MAUI foundation
        ↓
Utang MVP → Offline → Basic Store → Harden → Full POS
```

## Phase progress

| Phase | Name | Status | Completed | Total | Progress | Link |
|---:|---|---|---:|---:|---:|---|
| 0 | Existing HealthCare Assessment | **Complete*** | 4 | 4 | 100% | [Open](phases/phase-00-healthcare-assessment.md) |
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

\*Complete pending acceptance of P0-WP04; recommendation is close with documented risks.

**MVP phases 0–9:** 4 / 52 work packages = **7.69%** (4÷52).

## Completed Phase 0 work packages

| WP | Status | Key commit |
|---|---|---|
| P0-WP01 | Complete | `663b5bf` |
| P0-WP02 | Complete | `6b56e6d` |
| P0-WP03 | Complete (+ UI correction `e310cf8`) | `5d628dd` / `e310cf8` |
| P0-WP04 | Ready for Review | `f52316ae60198cb3dfee367a8ec99d550965ea44` |

## Final Phase 0 decisions (summary)

| Topic | Decision |
|---|---|
| Reuse | Controlled extraction of identity/org/permission/audit patterns; clinical stays in HC |
| Platform Admin UI | **Native** CSS/Razor — no Ant, no Tailwind |
| HC Staff UI | Retain Ant Design |
| POS UI | Native MAUI Hybrid foundation shared with Platform Admin conventions |
| Repository | Keep HC ignored; build Platform in root later **without** importing HC first |
| Databases | `ExItS_Platform` / `ExItS_HealthCare` / `ExItS_PinoyBusinessPOS` + entitlement snapshots |
| POS market | Broader SME retail; initial focus Sari-Sari / mini grocery; MVP unchanged |

## Latest tests

P0-WP02 Windows-safe baseline: **1102 passed / 0 failed / 0 skipped**. Not re-run in P0-WP03/P0-WP04 (docs-only).

## Next approved action

**P1-WP01 — Platform vs Product Capability Boundary** after P0-WP04 acceptance. Do **not** begin until authorized. Do not import HealthCare or create application projects in P0-WP04.
