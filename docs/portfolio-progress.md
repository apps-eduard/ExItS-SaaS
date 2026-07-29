# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [All Phases](phases/README.md) | [Contracts](engineering/platform-product-contracts.md) | [POS requirements](product/pinoy-business-pos-requirements.md) | [P1-WP02 report](reports/P1-WP02-data-ownership-and-contracts.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (ignored nested `HealthCare/`) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 1 — Platform Boundary and Architecture |
| Current work package | P1-WP02 **Complete** (+ POS Cash/GCash MVP payment correction) |
| Overall status | Phase 0 Complete with documented risks; P1-WP01 **Complete**; P1-WP02 **Complete**; awaiting authorization for P1-WP03 |
| Latest verified commit | `c5472e80a3045626672f88ddbe1973cb3f230f8c` (`docs(pos): add cash and gcash MVP payments`) |
| Open blockers | 0 for starting P1-WP03 docs after authorization; root remote empty |
| Last updated | 2026-07-29 |

## Delivery sequence

```text
Assess completed HealthCare MVP ✓
        ↓
Approve platform/product boundaries ✓ (P1-WP01)
        ↓
Define data ownership and contracts ✓ (P1-WP02)
        ↓
Extraction sequence and rollback (P1-WP03)  ← next when authorized
        ↓
…
```

## Phase progress

| Phase | Name | Status | Completed | Total | Progress | Link |
|---:|---|---|---:|---:|---:|---|
| 0 | Existing HealthCare Assessment | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-00-healthcare-assessment.md) |
| 1 | Platform Boundary and Architecture | In Progress | 2 | 4 | 50% | [Open](phases/phase-01-platform-boundary.md) |
| 2 | Platform Extraction and HealthCare Reconnection | Not Started | 0 | 6 | 0% | [Open](phases/phase-02-platform-extraction.md) |
| 3 | Portfolio Billing, Plans and Entitlements | Not Started | 0 | 5 | 0% | [Open](phases/phase-03-billing-entitlements.md) |
| 4 | Platform Admin Expansion | Not Started | 0 | 4 | 0% | [Open](phases/phase-04-platform-admin.md) |
| 5 | PinoyBusinessPOS MAUI Foundation | Not Started | 0 | 5 | 0% | [Open](phases/phase-05-pos-maui-foundation.md) |
| 6 | Utang MVP | Not Started | 0 | 6 | 0% | [Open](phases/phase-06-utang-mvp.md) |
| 7 | Offline Synchronization | Not Started | 0 | 5 | 0% | [Open](phases/phase-07-offline-sync.md) |
| 8 | Basic Store | Not Started | 0 | 7 | 0% | [Open](phases/phase-08-basic-store.md) |
| 9 | MVP Hardening and Release | Not Started | 0 | 6 | 0% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | Future | 0 | 8 | 0% | [Open](phases/phase-10-full-pos.md) |

**MVP phases 0–9:** 6 / 52 work packages = **11.54%** (6÷52). Counts P1-WP01 and P1-WP02 Complete.

## Phase 1 work packages

| WP | Status | Key commit |
|---|---|---|
| P1-WP01 | **Complete** | `b6a3133` / hash `a48e7cb` |
| P1-WP02 | **Complete** | `32534fa` / hash `0fd9c59`; payment correction `c5472e8` |
| P1-WP03 | Not Started | — |
| P1-WP04 | Not Started | — |

## POS MVP payment correction (post P1-WP02)

| Topic | Decision |
|---|---|
| Sale methods | `cash`, `gcash`, `customer-credit` |
| Credit repayment | `cash`, `gcash` |
| GCash MVP | Manual cashier verification; reference required; no API/webhooks/QR |
| Boundaries | SaaSPayment ≠ RetailPayment ≠ CreditPayment; Platform GCash ≠ POS GCash |

## Latest tests

Docs-only — no runtime tests required. Prior Windows-safe baseline: **1102 passed / 0 failed / 0 skipped** (P0-WP02).

## Next approved action

**P1-WP03 — Extraction Sequence and Rollback Plan** when explicitly authorized. Do **not** begin until authorized.
