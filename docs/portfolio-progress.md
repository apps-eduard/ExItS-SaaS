# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [Approved architecture](engineering/approved-architecture-summary.md) | [P3-WP03 report](reports/P3-WP03-manual-payment-activation.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (ignored nested `HealthCare/`) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 3 — Portfolio Billing, Plans and Entitlements |
| Current work package | P3-WP03 — Manual Payment Activation (**Ready for Review**) |
| Overall status | Phase 2 complete; catalog + subscriptions + manual payments delivered |
| Latest verified commit | `934c1d6a5f3a1a980748e9effb04345c801e8c37` |
| Open blockers | Payment/subscription APIs unauthenticated (R-045/R-055); R-035 calendar EOM; amount reconciliation deferred (R-053) |
| Last updated | 2026-07-29 |

## Delivery sequence

```text
Phase 2 ✓
        ↓
P3-WP01 Product and Plan Catalog ✓
        ↓
P3-WP02 Trials and Subscription Lifecycle ✓
        ↓
P3-WP03 Manual Payment Activation  ← in review
        ↓
P3-WP04 Entitlement Snapshots and Grace Rules (not started)
```

## Phase progress

| Phase | Name | Status | Completed | Total | Progress | Link |
|---:|---|---|---:|---:|---:|---|
| 0 | Existing HealthCare Assessment | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-00-healthcare-assessment.md) |
| 1 | Platform Boundary and Architecture | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-01-platform-boundary.md) |
| 2 | Platform Extraction and HealthCare Reconnection | **Complete with documented risks** | 6 | 6 | 100% | [Open](phases/phase-02-platform-extraction.md) |
| 3 | Portfolio Billing, Plans and Entitlements | In Progress | 2 | 5 | 40% | [Open](phases/phase-03-billing-entitlements.md) |
| 4 | Platform Admin Expansion | Not Started | 0 | 4 | 0% | [Open](phases/phase-04-platform-admin.md) |
| 5 | PinoyBusinessPOS MAUI Foundation | Not Started | 0 | 5 | 0% | [Open](phases/phase-05-pos-maui-foundation.md) |
| 6 | Utang MVP | Not Started | 0 | 6 | 0% | [Open](phases/phase-06-utang-mvp.md) |
| 7 | Offline Synchronization | Not Started | 0 | 5 | 0% | [Open](phases/phase-07-offline-sync.md) |
| 8 | Basic Store | Not Started | 0 | 7 | 0% | [Open](phases/phase-08-basic-store.md) |
| 9 | MVP Hardening and Release | Not Started | 0 | 6 | 0% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | Future | 0 | 8 | 0% | [Open](phases/phase-10-full-pos.md) |

**MVP phases 0–9:** 16 / 52 = **30.77%** (P3-WP01/02 accepted; P3-WP03 in review).

## Phase 3 work packages

| WP | Status | Key commit |
|---|---|---|
| P3-WP01 | Complete (accepted) | `9d01f26` |
| P3-WP02 | Complete (accepted) | `616d8ad` |
| P3-WP03 | Ready for Review | `934c1d6` |
| P3-WP04–05 | Not Started | — |

## Manual payment snapshot (P3-WP03)

| Item | Value |
|---|---|
| Persistence | `platform.saas_payments` + duplicate-reference unique index + positive-amount constraint |
| Migration | `AddManualSaaSPayments` |
| Methods | Cash, BankTransfer, GCash (manual recording only) |
| Lifecycle | PendingConfirmation → Confirmed → Voided; PendingConfirmation → Rejected |
| Activation | Confirmed payment atomically activates eligible subscription |
| API | `/api/v1/platform/payments/*` (unauthenticated / development-stage) |
| Gateway | **None** — no GCash API, QR, webhook, card storage |
| Tests | 251 passed / 0 failed / 0 skipped |
| HealthCare | Frozen |

## Latest tests

Root Release: **251 passed / 0 failed / 0 skipped** (172 unit + 37 architecture + 42 integration). HealthCare not rebuilt.

## Next approved action

**P3-WP04 — Entitlement Snapshots and Grace Rules** after P3-WP03 acceptance. Do **not** begin until authorized.
