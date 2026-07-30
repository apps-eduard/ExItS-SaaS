# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [Approved architecture](engineering/approved-architecture-summary.md) | [P7-WP01 report](reports/P7-WP01-sqlite-and-device-identity.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (ignored nested `HealthCare/`) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 7 — Offline Synchronization (**In Progress**) |
| Current work package | P7-WP01 — SQLite and Device Identity (**Complete** with documented risks) |
| Overall status | **P7-WP01 complete** — foundation only; next P7-WP02 when authorized |
| Latest verified commit | `a82a4be07e90ddfad59b741f6822022369cda68e` (P7-WP01) |
| Open blockers | Missing production auth (JWT/MFA/SSO/AD); R-022 refresh durations; R-035 calendar EOM; no payment gateway; no interactive Android emulator (R-109); commercial/actor headers Development-stage only; org timezone undefined for due dates; OD-10/OD-11 open; no POS operational roles; SQLitePCLRaw NU1903 (R-110); DB encryption decision before business-data offline WP |
| Last updated | 2026-07-30 |

## Delivery sequence

```text
Phase 6 ✓
        ↓
P7-WP01 ✓ SQLite and Device Identity (complete with risks)
        ↓
P7-WP02 — Offline Queue and Idempotency (not started — do not begin until authorized)
        ↓
P7-WP03 … P7-WP05
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
| 7 | Offline Synchronization | **In Progress** | 1 | 5 | 20% | [Open](phases/phase-07-offline-sync.md) |
| 8 | Basic Store | Not Started | 0 | 7 | 0% | [Open](phases/phase-08-basic-store.md) |
| 9 | MVP Hardening and Release | Not Started | 0 | 6 | 0% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | Future | 0 | 8 | 0% | [Open](phases/phase-10-full-pos.md) |

**MVP phases 0–9:** 35 / 52 = **67.31%**.

## Phase 7 work packages

| WP | Status | Key commit |
|---|---|---|
| P7-WP01 | Complete with risks | a82a4be07e90ddfad59b741f6822022369cda68e |
| P7-WP02 | Not Started | — |
| P7-WP03 | Not Started | — |
| P7-WP04 | Not Started | — |
| P7-WP05 | Not Started | — |

## Phase 6 work packages

| WP | Status | Key commit |
|---|---|---|
| P6-WP01 | Complete with risks | 674ad0660b0bd11bca75f2e90e329c4579ff592a |
| P6-WP02 | Complete with risks | ead6942187ca9a9c507dcf706bbece2e507a8645 |
| P6-WP03 | Complete with risks | de39091f6110acbc721ac78da51a92acefd6775a |
| P6-WP04 | Complete with risks | 9947d95cba27c8311091f95ea51c79be1de0acb9 |
| P6-WP05 | Complete with risks | 271c518cb8c4051502d6370ec71e6498fbbfd6b5 |
| P6-WP06 | Complete with risks | 9f33420f5f77bade398db6d59728ad9def895683 |

## Permanent workflow rules

Follow `.cursor/rules/exits-workflow.mdc`. HealthCare remains frozen. Do not begin unauthorized work packages.
