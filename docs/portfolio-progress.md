# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [Approved architecture](engineering/approved-architecture-summary.md) | [P4-WP01 report](reports/P4-WP01-portfolio-navigation-and-product-views.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (ignored nested `HealthCare/`) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 4 — Platform Admin Expansion |
| Current work package | P4-WP01 — Portfolio Navigation and Product Views (**Complete**) |
| Overall status | **P4-WP01 complete** — read-only Platform Admin shell; auth / mutation Admin / delivery still deferred |
| Latest verified commit | _(recorded after push)_ |
| Open blockers | Unauthenticated Admin+API; R-022 refresh durations; R-035 calendar EOM; no product delivery; no auth |
| Last updated | 2026-07-29 |

## Delivery sequence

```text
Phase 3 ✓
        ↓
P4-WP01 Portfolio Navigation and Product Views ✓
        ↓
P4-WP02 Organizations, Users and Product Access (not started — do not begin until authorized)
```

## Phase progress

| Phase | Name | Status | Completed | Total | Progress | Link |
|---:|---|---|---:|---:|---:|---|
| 0 | Existing HealthCare Assessment | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-00-healthcare-assessment.md) |
| 1 | Platform Boundary and Architecture | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-01-platform-boundary.md) |
| 2 | Platform Extraction and HealthCare Reconnection | **Complete with documented risks** | 6 | 6 | 100% | [Open](phases/phase-02-platform-extraction.md) |
| 3 | Portfolio Billing, Plans and Entitlements | **Complete with documented risks** | 5 | 5 | 100% | [Open](phases/phase-03-billing-entitlements.md) |
| 4 | Platform Admin Expansion | In Progress | 1 | 4 | 25% | [Open](phases/phase-04-platform-admin.md) |
| 5 | PinoyBusinessPOS MAUI Foundation | Not Started | 0 | 5 | 0% | [Open](phases/phase-05-pos-maui-foundation.md) |
| 6 | Utang MVP | Not Started | 0 | 6 | 0% | [Open](phases/phase-06-utang-mvp.md) |
| 7 | Offline Synchronization | Not Started | 0 | 5 | 0% | [Open](phases/phase-07-offline-sync.md) |
| 8 | Basic Store | Not Started | 0 | 7 | 0% | [Open](phases/phase-08-basic-store.md) |
| 9 | MVP Hardening and Release | Not Started | 0 | 6 | 0% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | Future | 0 | 8 | 0% | [Open](phases/phase-10-full-pos.md) |

**MVP phases 0–9:** 20 / 52 = **38.46%**.

## Phase 4 work packages

| WP | Status | Key commit |
|---|---|---|
| P4-WP01 | Complete | _(feature hash)_ |
| P4-WP02 | Not Started | — |
| P4-WP03 | Not Started | — |
| P4-WP04 | Not Started | — |

## Permanent workflow rules

`.cursor/rules/exits-workflow.mdc` — HealthCare freeze, Git, build/test, security/architecture, documentation/reporting.

## Latest tests

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Unit | 200 | 0 | 0 |
| Architecture | 39 | 0 | 0 |
| Admin unit | 10 | 0 | 0 |
| Integration | 68 | 0 | 0 |
| **Total** | **317** | **0** | **0** |
