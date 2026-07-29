# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [Approved architecture](engineering/approved-architecture-summary.md) | [P2-WP05 report](reports/P2-WP05-regression-and-migration-validation.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (ignored nested `HealthCare/`) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 2 — Platform Extraction and HealthCare Reconnection |
| Current work package | P2-WP05 — Regression and Migration Validation (**Ready for Review**) |
| Overall status | P2-WP01–04 **Complete**; migration dry-run validation implemented; remote published |
| Latest verified commit | `e001f3d654cc91976bfd2d4de890c8244ce04c7e` |
| Open blockers | 0 for P2-WP05 acceptance; R-016 closed (remote verified) |
| Last updated | 2026-07-29 |

## Delivery sequence

```text
P2-WP01–04 ✓
        ↓
P2-WP05 regression/migration validation  ← in review
        ↓
P2-WP06 extraction closeout (not started)
```

## Phase progress

| Phase | Name | Status | Completed | Total | Progress | Link |
|---:|---|---|---:|---:|---:|---|
| 0 | Existing HealthCare Assessment | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-00-healthcare-assessment.md) |
| 1 | Platform Boundary and Architecture | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-01-platform-boundary.md) |
| 2 | Platform Extraction and HealthCare Reconnection | In Progress | 4 | 6 | 67% | [Open](phases/phase-02-platform-extraction.md) |
| 3 | Portfolio Billing, Plans and Entitlements | Not Started | 0 | 5 | 0% | [Open](phases/phase-03-billing-entitlements.md) |
| 4 | Platform Admin Expansion | Not Started | 0 | 4 | 0% | [Open](phases/phase-04-platform-admin.md) |
| 5 | PinoyBusinessPOS MAUI Foundation | Not Started | 0 | 5 | 0% | [Open](phases/phase-05-pos-maui-foundation.md) |
| 6 | Utang MVP | Not Started | 0 | 6 | 0% | [Open](phases/phase-06-utang-mvp.md) |
| 7 | Offline Synchronization | Not Started | 0 | 5 | 0% | [Open](phases/phase-07-offline-sync.md) |
| 8 | Basic Store | Not Started | 0 | 7 | 0% | [Open](phases/phase-08-basic-store.md) |
| 9 | MVP Hardening and Release | Not Started | 0 | 6 | 0% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | Future | 0 | 8 | 0% | [Open](phases/phase-10-full-pos.md) |

**MVP phases 0–9:** 12 / 52 = **23.08%** (Phase 0+1 + P2-WP01–04 complete; P2-WP05 not counted until accepted).

## Phase 2 work packages

| WP | Status | Key commit |
|---|---|---|
| P2-WP01 | Complete | `4827b7f` |
| P2-WP02 | Complete | `49f8ae8` |
| P2-WP03 | Complete | `6e866d7` + `10f99c5` |
| P2-WP04 | Complete | `3b66095` + `eb9fdfe` |
| P2-WP05 | Ready for Review | `e001f3d` |
| P2-WP06 | Not Started | — |

## Migration validation snapshot (P2-WP05)

| Item | Value |
|---|---|
| Remote `main` (Part A) | `6d9fdee8d98a9cef8f273e6cbf053143f16703cd` |
| `phase-1-approved` peeled | `01ab65b511721d5dd2173188bc6d962a5feea803` |
| Dry-run models | Batch, identity/org/membership candidates, findings |
| Services | Preflight, simulation, compatibility, rollback readiness |
| Real migration | **None** |
| Tests | 121 passed / 0 failed / 0 skipped |
| API port | `http://127.0.0.1:5288` |

## Latest tests

Root Release: **121 passed / 0 failed / 0 skipped**. HealthCare not rebuilt (frozen).

## Next approved action

**P2-WP06 — Extraction Closeout** after P2-WP05 acceptance. Do **not** begin until authorized.
