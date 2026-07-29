# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [Approved architecture](engineering/approved-architecture-summary.md) | [P2-WP03 report](reports/P2-WP03-products-plans-entitlements.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (ignored nested `HealthCare/`) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 2 — Platform Extraction and HealthCare Reconnection |
| Current work package | P2-WP03 — Products, Plans and Entitlement Foundation (**Ready for Review**) |
| Overall status | P2-WP01–02 **Complete**; commercial catalog/entitlement domain foundation implemented |
| Latest verified commit | `10f99c5f549d2161c1f033be6223ccc26a6e0eaf` (`fix(platform): keep trial duration configurable`) |
| Open blockers | 0 for P2-WP03 acceptance; root remote empty (R-016); not pushed |
| Last updated | 2026-07-29 |

## Delivery sequence

```text
P2-WP01 foundation ✓
        ↓
P2-WP02 identity/organization ✓
        ↓
P2-WP03 products/plans/entitlements  ← in review
        ↓
P2-WP04 HealthCare contract adaptation (not started)
```

## Phase progress

| Phase | Name | Status | Completed | Total | Progress | Link |
|---:|---|---|---:|---:|---:|---|
| 0 | Existing HealthCare Assessment | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-00-healthcare-assessment.md) |
| 1 | Platform Boundary and Architecture | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-01-platform-boundary.md) |
| 2 | Platform Extraction and HealthCare Reconnection | In Progress | 2 | 6 | 33.33% | [Open](phases/phase-02-platform-extraction.md) |
| 3 | Portfolio Billing, Plans and Entitlements | Not Started | 0 | 5 | 0% | [Open](phases/phase-03-billing-entitlements.md) |
| 4 | Platform Admin Expansion | Not Started | 0 | 4 | 0% | [Open](phases/phase-04-platform-admin.md) |
| 5 | PinoyBusinessPOS MAUI Foundation | Not Started | 0 | 5 | 0% | [Open](phases/phase-05-pos-maui-foundation.md) |
| 6 | Utang MVP | Not Started | 0 | 6 | 0% | [Open](phases/phase-06-utang-mvp.md) |
| 7 | Offline Synchronization | Not Started | 0 | 5 | 0% | [Open](phases/phase-07-offline-sync.md) |
| 8 | Basic Store | Not Started | 0 | 7 | 0% | [Open](phases/phase-08-basic-store.md) |
| 9 | MVP Hardening and Release | Not Started | 0 | 6 | 0% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | Future | 0 | 8 | 0% | [Open](phases/phase-10-full-pos.md) |

**MVP phases 0–9:** 10 / 52 = **19.23%** (Phase 0+1 + P2-WP01–02 complete; P2-WP03 not counted until accepted).

## Phase 2 work packages

| WP | Status | Key commit |
|---|---|---|
| P2-WP01 | Complete | `4827b7f` |
| P2-WP02 | Complete | `49f8ae8` |
| P2-WP03 | Ready for Review | `6e866d7` + `10f99c5` (trial duration fix) |
| P2-WP04–06 | Not Started | — |

## Commercial boundary snapshot (P2-WP03)

| Item | Value |
|---|---|
| Domain | Product, Feature, Plan, PlanVersion, Trial, Subscription, Override, Snapshot |
| Composer | `EntitlementSnapshotComposer` (deterministic; override precedence) |
| POS Utang features | `customer-credit-view`, `customer-credit-repay`, `customer-credit-create` |
| Trial duration | Configurable in Platform; POS requirement = **three calendar months** (not 90 days; EOM rule open) |
| API business routes | None (still `/` + `/health`) |
| Tests | 86 passed / 0 failed / 0 skipped |
| API port | `http://127.0.0.1:5288` |

## Latest tests

Root Release: **86 passed / 0 failed / 0 skipped**. HealthCare not rebuilt (frozen).

## Next approved action

**P2-WP04 — HealthCare Contract Adaptation** after P2-WP03 acceptance. Do **not** begin until authorized.
