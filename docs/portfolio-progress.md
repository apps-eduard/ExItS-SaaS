# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [Approved architecture](engineering/approved-architecture-summary.md) | [P6-WP01 report](reports/P6-WP01-customers.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (ignored nested `HealthCare/`) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 6 — Utang MVP (**In Progress**) |
| Current work package | P6-WP01 — Customers (**Complete with documented risks**) |
| Overall status | **P6-WP01 complete** — customers-only; next P6-WP02 when authorized |
| Latest verified commit | _(feature hash after push)_ |
| Open blockers | Missing production auth (JWT/MFA/SSO/AD); R-022 refresh durations; R-035 calendar EOM; no credit/ledger yet; no payment gateway; no interactive Android emulator (R-109); OD-07/OD-08 open |
| Last updated | 2026-07-30 |

## Delivery sequence

```text
Phase 5 ✓
        ↓
P6-WP01 ✓ Customers (complete with risks)
        ↓
P6-WP02 — Remarks-Based Credit (not started — do not begin until authorized)
```

## Phase progress

| Phase | Name | Status | Completed | Total | Progress | Link |
|---:|---|---|---:|---:|---:|---|
| 0 | Existing HealthCare Assessment | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-00-healthcare-assessment.md) |
| 1 | Platform Boundary and Architecture | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-01-platform-boundary.md) |
| 2 | Platform Extraction and HealthCare Reconnection | **Complete with documented risks** | 6 | 6 | 100% | [Open](phases/phase-02-platform-extraction.md) |
| 3 | Portfolio Billing, Plans and Entitlements | **Complete with documented risks** | 5 | 5 | 100% | [Open](phases/phase-03-billing-entitlements.md) |
| 4 | Platform Admin Expansion | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-04-platform-admin.md) |
| 5 | PinoyBusinessPOS MAUI Foundation | **Complete with documented risks** | 5 | 5 | 100% | [Open](phases/phase-05-pos-maui-foundation.md) |
| 6 | Utang MVP | **In Progress** | 1 | 6 | 16.67% | [Open](phases/phase-06-utang-mvp.md) |
| 7 | Offline Synchronization | Not Started | 0 | 5 | 0% | [Open](phases/phase-07-offline-sync.md) |
| 8 | Basic Store | Not Started | 0 | 7 | 0% | [Open](phases/phase-08-basic-store.md) |
| 9 | MVP Hardening and Release | Not Started | 0 | 6 | 0% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | Future | 0 | 8 | 0% | [Open](phases/phase-10-full-pos.md) |

**MVP phases 0–9:** 29 / 52 = **55.77%**.

## Phase 6 work packages

| WP | Status | Key commit |
|---|---|---|
| P6-WP01 | Complete with risks | _(after push)_ |
| P6-WP02 | Not Started | — |
| P6-WP03 | Not Started | — |
| P6-WP04 | Not Started | — |
| P6-WP05 | Not Started | — |
| P6-WP06 | Not Started | — |

## Phase 5 work packages

| WP | Status | Key commit |
|---|---|---|
| P5-WP01 | Complete | 3015925d16560be13953270565c1ab99a8d69934 |
| P5-WP02 | Complete | 3d3cba840ffff20dc07ae7237d7f81c3873a502e |
| P5-WP03 | Complete | 1dea793407adaa9e8a27c19f45727bc90d866f60 |
| P5-WP04 | Complete | 763b0dc7cd73ab21ada2d101d115423c23d90cfa |
| P5-WP05 | Complete | 81eaa892cb6ac1ffb1b201b69dc7e390e5536586 |

## Permanent workflow rules

`.cursor/rules/exits-workflow.mdc` — HealthCare freeze, Git, build/test, security/architecture, documentation/reporting.

## Latest tests

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Platform unit | 261 | 0 | 0 |
| Architecture | 41 | 0 | 0 |
| Admin unit | 27 | 0 | 0 |
| DesignSystem | 28 | 0 | 0 |
| ApiClient | 17 | 0 | 0 |
| Maui | 27 | 0 | 0 |
| POS unit | 8 | 0 | 0 |
| POS integration | 4 | 0 | 0 |
| Platform integration | 84 | 0 | 0 |
| **Total** | **497** | **0** | **0** |
