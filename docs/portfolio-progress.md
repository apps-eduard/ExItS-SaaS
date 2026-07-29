# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [All Phases](phases/README.md) | [Extraction sequence](reuse/extraction-sequence.md) | [P1-WP03 report](reports/P1-WP03-extraction-sequence-and-rollback.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (ignored nested `HealthCare/`) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 1 — Platform Boundary and Architecture |
| Current work package | P1-WP03 — Extraction Sequence and Rollback Plan (**Ready for Review**) |
| Overall status | P1-WP01/P1-WP02 **Complete**; Cash/GCash correction **accepted**; P1-WP03 ready for review |
| Latest verified commit | `PENDING_AFTER_COMMIT` (`docs(extraction): define sequence and rollback plan`) |
| Open blockers | 0 for P1-WP03 acceptance; root remote empty |
| Last updated | 2026-07-29 |

## Delivery sequence

```text
Assess completed HealthCare MVP ✓
        ↓
Approve platform/product boundaries ✓ (P1-WP01)
        ↓
Define data ownership and contracts ✓ (P1-WP02 + Cash/GCash)
        ↓
Extraction sequence and rollback  ← P1-WP03 (in review)
        ↓
Architecture approval closeout (P1-WP04)
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

**MVP phases 0–9:** 6 / 52 = **11.54%**. P1-WP03 not counted until accepted.

## Phase 1 work packages

| WP | Status | Key commit |
|---|---|---|
| P1-WP01 | **Complete** | `b6a3133` / hash `a48e7cb` |
| P1-WP02 | **Complete** | `32534fa` / hash `0fd9c59`; Cash/GCash `c5472e8` (**accepted**) |
| P1-WP03 | Ready for Review | `PENDING_AFTER_COMMIT` |
| P1-WP04 | Not Started | — |

## P1-WP03 decisions (summary)

| Topic | Decision |
|---|---|
| Strategy | Build **new** Platform in root; adapt HC patterns; no wholesale copy (ADR-013) |
| HealthCare | Remains frozen/ignored until approved reconnection |
| Stages | 1 foundation → 2 identity → 3 org → 4 catalog/entitlements → 5 Admin UI → 6 HC adapter → 7 POS |
| POS | May start after readiness gate **without** full HC cutover |
| Rollback | Levels L0–L6 documented |

## Latest tests

Docs-only P1-WP03. Prior Windows-safe baseline: **1102 passed / 0 failed / 0 skipped** (P0-WP02).

## Next approved action

**P1-WP04 — Architecture Approval Closeout** after P1-WP03 acceptance. Do **not** begin until authorized. No application projects or HealthCare import in P1-WP03.
