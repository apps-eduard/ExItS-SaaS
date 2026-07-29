# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [All Phases](phases/README.md) | [Contracts](engineering/platform-product-contracts.md) | [P1-WP02 report](reports/P1-WP02-data-ownership-and-contracts.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (ignored nested `HealthCare/`) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 1 — Platform Boundary and Architecture |
| Current work package | P1-WP02 — Data Ownership and Contracts (**Ready for Review**) |
| Overall status | Phase 0 Complete with documented risks; P1-WP01 **Complete**; P1-WP02 ready for review |
| Latest verified commit | `32534fa31501217f021e73b36ba27f49c448b36c` (`docs(contracts): define data authority and projections`) |
| Open blockers | 0 for P1-WP02 acceptance; root remote empty (user push when authorized) |
| Last updated | 2026-07-29 |

## Delivery sequence

```text
Assess completed HealthCare MVP ✓
        ↓
Approve platform/product boundaries ✓ (P1-WP01)
        ↓
Define data ownership and contracts  ← P1-WP02 (in review)
        ↓
Extraction sequence and rollback (P1-WP03)
        ↓
Extract or adapt ExITS Platform safely
        ↓
…
```

## Phase progress

| Phase | Name | Status | Completed | Total | Progress | Link |
|---:|---|---|---:|---:|---:|---|
| 0 | Existing HealthCare Assessment | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-00-healthcare-assessment.md) |
| 1 | Platform Boundary and Architecture | In Progress | 1 | 4 | 25% | [Open](phases/phase-01-platform-boundary.md) |
| 2 | Platform Extraction and HealthCare Reconnection | Not Started | 0 | 6 | 0% | [Open](phases/phase-02-platform-extraction.md) |
| 3 | Portfolio Billing, Plans and Entitlements | Not Started | 0 | 5 | 0% | [Open](phases/phase-03-billing-entitlements.md) |
| 4 | Platform Admin Expansion | Not Started | 0 | 4 | 0% | [Open](phases/phase-04-platform-admin.md) |
| 5 | PinoyBusinessPOS MAUI Foundation | Not Started | 0 | 5 | 0% | [Open](phases/phase-05-pos-maui-foundation.md) |
| 6 | Utang MVP | Not Started | 0 | 6 | 0% | [Open](phases/phase-06-utang-mvp.md) |
| 7 | Offline Synchronization | Not Started | 0 | 5 | 0% | [Open](phases/phase-07-offline-sync.md) |
| 8 | Basic Store | Not Started | 0 | 7 | 0% | [Open](phases/phase-08-basic-store.md) |
| 9 | MVP Hardening and Release | Not Started | 0 | 6 | 0% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | Future | 0 | 8 | 0% | [Open](phases/phase-10-full-pos.md) |

**MVP phases 0–9:** 5 / 52 work packages = **9.62%** (5÷52). Counts P1-WP01 Complete; P1-WP02 not counted until accepted.

## Phase 1 work packages

| WP | Status | Key commit |
|---|---|---|
| P1-WP01 | **Complete** | `b6a3133` / hash `a48e7cb` |
| P1-WP02 | Ready for Review | `32534fa` |
| P1-WP03 | Not Started | — |
| P1-WP04 | Not Started | — |

## P1-WP02 decisions (summary)

| Topic | Decision |
|---|---|
| Stable IDs | UUID/Guid or immutable codes; no cross-DB FKs |
| Projections | Identity, org, membership, entitlement — minimal fields |
| Delivery | At-least-once; idempotent consumers; transport deferred |
| Entitlement states | Current → Never initialized matrix; fail-closed for financial/privacy |
| Trial expiry | View + pay existing debt; block new credit (OD-07–09 open) |
| Payments | SaaSPayment ≠ RetailPayment ≠ CreditPayment |
| ADR | ADR-012 Accepted |

## Latest tests

Docs-only P1-WP02 — no runtime tests required. Prior Windows-safe baseline: **1102 passed / 0 failed / 0 skipped** (P0-WP02).

## Next approved action

**P1-WP03 — Extraction Sequence and Rollback Plan** after P1-WP02 acceptance. Do **not** begin until authorized. No application projects or HealthCare import in P1-WP02.
