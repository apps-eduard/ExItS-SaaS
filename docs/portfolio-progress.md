# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [Approved architecture](engineering/approved-architecture-summary.md) | [P8-WP02 report](reports/P8-WP02-simple-sales.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (ignored nested `HealthCare/`) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 8 — Basic Store (**In Progress**) |
| Current work package | P8-WP02 — Simple Sales (**Complete** with documented risks) |
| Overall status | **Phase 8 in progress** — catalog + simple Cash/ManualGCash sales (online-only); no inventory/Utang sales; not production-ready; next P8-WP03 when authorized |
| Latest verified commit | `72a6fa9b1bb6f48610563d01ee10e608e99806e1` (P8-WP02) |
| Open blockers | Missing production auth (JWT/MFA/SSO/AD); R-022 refresh durations / no offline entitlement grace; R-035 calendar EOM; no payment gateway; no interactive Android emulator (R-109); commercial/actor headers Development-stage only; org timezone undefined for due dates; OD-11 open; no POS operational roles; SQLitePCLRaw NU1903 (R-129); catalog/sales online-only by design |
| Last updated | 2026-07-30 |

## Delivery sequence

```text
P7-WP05 ✓ Offline Closeout (Phase 7 closed)
        ↓
P8-WP01 ✓ Catalog and Barcode (complete with risks)
        ↓
P8-WP02 ✓ Simple Sales (complete with risks)
        ↓
P8-WP03 ○ Product-Based Utang (not started — do not begin until authorized)
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
| 8 | Basic Store | **In Progress** | 2 | 7 | ~29% | [Open](phases/phase-08-basic-store.md) |
| 9 | MVP Hardening and Release | Not Started | 0 | 6 | 0% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | Future | 0 | 8 | 0% | [Open](phases/phase-10-full-pos.md) |

**MVP phases 0–9:** 40 / 52 = **76.92%**.

## Phase 8 work packages

| WP | Status | Key commit |
|---|---|---|
| P8-WP01 | Complete with risks | 5573822ca116ab46f1a5cdce407e1d7b4f58f796 |
| P8-WP02 | Complete with risks | 72a6fa9b1bb6f48610563d01ee10e608e99806e1 |
| P8-WP03 | Not Started | — |
| P8-WP04 | Not Started | — |
| P8-WP05 | Not Started | — |
| P8-WP06 | Not Started | — |
| P8-WP07 | Not Started | — |

## Phase 7 work packages

| WP | Status | Key commit |
|---|---|---|
| P7-WP01 | Complete with risks | a82a4be07e90ddfad59b741f6822022369cda68e |
| P7-WP02 | Complete with risks | aa1f92eba97bc77775f59de8209b42c9d7a475cc |
| P7-WP03 | Complete with risks | 3763ca0fe406067eb539b3d8adca21447f813dcf |
| P7-WP04 | Complete with risks | 9c862b4bcd1604a351334120823bdf1e4a2014cb |
| P7-WP05 | Complete with risks | 3b5a1e72294eb102f51f46c995e784138685faa4 |

## Permanent workflow rules

Follow `.cursor/rules/exits-workflow.mdc`. HealthCare remains frozen. Do not begin unauthorized work packages.
