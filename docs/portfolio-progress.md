# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [Approved architecture](engineering/approved-architecture-summary.md) | [P7-WP03 report](reports/P7-WP03-customer-and-credit-sync.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (ignored nested `HealthCare/`) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 7 — Offline Synchronization (**In Progress**) |
| Current work package | P7-WP03 — Customer and Credit Sync (**Complete** with documented risks) |
| Overall status | **P7-WP03 complete** — encrypted customer/credit offline sync; no offline repayments; next P7-WP04 when authorized |
| Latest verified commit | `_(recorded after push)_` (P7-WP03) |
| Open blockers | Missing production auth (JWT/MFA/SSO/AD); R-022 refresh durations / no offline entitlement grace; R-035 calendar EOM; no payment gateway; no interactive Android emulator (R-109); commercial/actor headers Development-stage only; org timezone undefined for due dates; OD-11 open; no POS operational roles; SQLitePCLRaw NU1903 (R-129 — **WP03 chose row-level AES-GCM**; SQLCipher still deferred) |
| Last updated | 2026-07-30 |

## Delivery sequence

```text
P7-WP01 ✓ SQLite and Device Identity
        ↓
P7-WP02 ✓ Offline Queue and Idempotency (complete with risks)
        ↓
P7-WP03 ✓ Customer and Credit Sync (complete with risks)
        ↓
P7-WP04 — Payment Sync and Recovery (not started — do not begin until authorized)
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
| 7 | Offline Synchronization | **In Progress** | 3 | 5 | 60% | [Open](phases/phase-07-offline-sync.md) |
| 8 | Basic Store | Not Started | 0 | 7 | 0% | [Open](phases/phase-08-basic-store.md) |
| 9 | MVP Hardening and Release | Not Started | 0 | 6 | 0% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | Future | 0 | 8 | 0% | [Open](phases/phase-10-full-pos.md) |

**MVP phases 0–9:** 37 / 52 = **71.15%**.

## Phase 7 work packages

| WP | Status | Key commit |
|---|---|---|
| P7-WP01 | Complete with risks | a82a4be07e90ddfad59b741f6822022369cda68e |
| P7-WP02 | Complete with risks | aa1f92eba97bc77775f59de8209b42c9d7a475cc |
| P7-WP03 | Complete with risks | _(recorded after push)_ |
| P7-WP04 | Not Started | — |
| P7-WP05 | Not Started | — |

## Permanent workflow rules

Follow `.cursor/rules/exits-workflow.mdc`. HealthCare remains frozen. Do not begin unauthorized work packages.
