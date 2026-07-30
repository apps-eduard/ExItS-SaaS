# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [Approved architecture](engineering/approved-architecture-summary.md) | [P10-WP01 scope ambiguity](reports/P10-WP01-scope-ambiguity.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (separate historical product; not in this workspace) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 10 — Full POS |
| Current work package | P10-WP03 — Advanced Inventory (**in progress**) |
| Overall status | **P10-WP03 authorized and in progress.** Do not begin P10-WP04. |
| Latest verified commit | 933600283f8032783dfca2c01ae09f0af781abd9 |
| Open blockers | Missing production auth (R-091); R-109 interactive Android; R-129 / NU1903; Production TLS; MAUI HTTPS-only Production policy; POS operational roles; Manual GCash unverified; online-only Basic Store limits; report export deferred; PITR deferred; local unsynced ops outside server backups; tax/refund/accounting deferred |
| Last updated | 2026-07-31 |

## Delivery sequence

```text
P9-WP01 ✓ Security and Privacy Hardening (complete with risks)
        ↓
P9-WP02 ✓ Performance and Reliability (complete with risks)
        ↓
P9-WP03 ✓ Backup and Restore (complete with risks)
        ↓
P9-WP04 ✓ Accessibility, Localization and Theme QA (complete with risks)
        ↓
P9-WP05 ✓ Pilot and Deployment (complete with risks)
        ↓
P9-WP06 ✓ Commercial MVP Closeout (complete with risks — Phase 9 closed)
        ↓
P10-WP01 ✓ Suppliers (Option A — master data only)
        ↓
P10-WP02 ✓ Purchasing
        ↓
P10-WP03 ● Advanced Inventory (in progress)
        ↓
P10-WP04 ○ Cashier Shifts (not started)
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
| 6 | Utang MVP | **Complete with documented risks** | 6 | 6 | 100% | [Open](phases/phase-06-utang-mvp.md) |
| 7 | Offline Synchronization | **Complete with documented risks** | 5 | 5 | 100% | [Open](phases/phase-07-offline-sync.md) |
| 8 | Basic Store | **Complete with documented risks** | 7 | 7 | 100% | [Open](phases/phase-08-basic-store.md) |
| 9 | MVP Hardening and Release | **Complete with documented risks** | 6 | 6 | 100% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | **In Progress** | 2 | 7 | 29% | [Open](phases/phase-10-full-pos.md) |

**MVP phases 0–9:** 52 / 52 = **100%** (with documented risks; not Production-ready).

## Phase 10 work packages

| WP | Status | Key commit |
|---|---|---|
| P10-WP01 — Suppliers | Complete (Option A) | 6f92dd43b2f66709891d82079f9d3fbd0b5c450e |
| P10-WP02 — Purchasing | Complete | c0f8130ef99e958bceaee98024a69339b7e8e41a |
| P10-WP03 — Advanced Inventory | In Progress | — |
| P10-WP04 — Cashier Shifts | Not Started | — |
| P10-WP05 — Returns and Refunds | Not Started | — |
| P10-WP06 — Advanced Permissions and Reports | Not Started | — |
| P10-WP07 — Multiple Registers | Not Started | — |
| P10-WP08 — Full POS Closeout | Not Started | — |

## Phase 9 work packages

| WP | Status | Key commit |
|---|---|---|
| P9-WP01 | Complete with risks | de4fac64739f5b368a6b1f2490223fa032201b65 |
| P9-WP02 | Complete with risks | 46a4ac7bacfad0736fba4741817958862fadf9e2 |
| P9-WP03 | Complete with risks | 3bbb0c716da60bd7d87a191c35bd0eced1bde380 |
| P9-WP04 | Complete with risks | f7b3aecec614eea8b1de601cd08e843f4aea91f8 |
| P9-WP05 | Complete with risks | 9c1bbd0557e252758a772b985c907233da3f5214 |
| P9-WP06 | Complete with risks | f6117c59e9c63d629af5805cf2d4ae7f8ea61225 |

## Permanent workflow rules

Follow `.cursor/rules/exits-workflow.mdc`. Do not begin **P10-WP04 — Cashier Shifts** until explicitly authorized.
