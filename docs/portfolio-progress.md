# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [Approved architecture](engineering/approved-architecture-summary.md) | [P2-WP01 report](reports/P2-WP01-extraction-baseline-and-safety.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (ignored nested `HealthCare/`) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 2 — Platform Extraction and HealthCare Reconnection |
| Current work package | P2-WP01 — Extraction Baseline Tag and Safety Checks (**Ready for Review**) |
| Overall status | Phase 1 **Complete with documented risks**; root solution foundation created |
| Latest verified commit | `PENDING_AFTER_COMMIT` (`chore(platform): establish root solution foundation`) |
| Open blockers | 0 for P2-WP01 acceptance; root remote empty (R-016); tag/commit not pushed |
| Last updated | 2026-07-29 |

## Delivery sequence

```text
Phase 1 architecture approval ✓
        ↓
P2-WP01 root solution foundation  ← in review
        ↓
P2-WP02 identity and organization (not started)
        ↓
…
```

## Phase progress

| Phase | Name | Status | Completed | Total | Progress | Link |
|---:|---|---|---:|---:|---:|---|
| 0 | Existing HealthCare Assessment | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-00-healthcare-assessment.md) |
| 1 | Platform Boundary and Architecture | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-01-platform-boundary.md) |
| 2 | Platform Extraction and HealthCare Reconnection | In Progress | 0 | 6 | 0% | [Open](phases/phase-02-platform-extraction.md) |
| 3 | Portfolio Billing, Plans and Entitlements | Not Started | 0 | 5 | 0% | [Open](phases/phase-03-billing-entitlements.md) |
| 4 | Platform Admin Expansion | Not Started | 0 | 4 | 0% | [Open](phases/phase-04-platform-admin.md) |
| 5 | PinoyBusinessPOS MAUI Foundation | Not Started | 0 | 5 | 0% | [Open](phases/phase-05-pos-maui-foundation.md) |
| 6 | Utang MVP | Not Started | 0 | 6 | 0% | [Open](phases/phase-06-utang-mvp.md) |
| 7 | Offline Synchronization | Not Started | 0 | 5 | 0% | [Open](phases/phase-07-offline-sync.md) |
| 8 | Basic Store | Not Started | 0 | 7 | 0% | [Open](phases/phase-08-basic-store.md) |
| 9 | MVP Hardening and Release | Not Started | 0 | 6 | 0% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | Future | 0 | 8 | 0% | [Open](phases/phase-10-full-pos.md) |

**MVP phases 0–9:** 8 / 52 = **15.38%** (Phase 0+1 complete; P2-WP01 not counted until accepted).

## Phase 2 work packages

| WP | Status | Key commit |
|---|---|---|
| P2-WP01 | Ready for Review | `PENDING_AFTER_COMMIT` |
| P2-WP02 | Not Started | — |
| P2-WP03–06 | Not Started | — |

## Foundation snapshot (P2-WP01)

| Item | Value |
|---|---|
| Solution | `ExItS.slnx` |
| SDK | `10.0.302` (`global.json`, rollForward latestFeature) |
| Projects | Domain, Application, Infrastructure, Api, UnitTests, ArchitectureTests |
| Tests | 11 passed / 0 failed / 0 skipped |
| Tag | `phase-1-approved` → `01ab65b` (local) |
| API port | `http://127.0.0.1:5288` |

## Latest tests

Root Release: **11 passed / 0 failed / 0 skipped**. HealthCare not rebuilt (frozen).

## Next approved action

**P2-WP02 — Shared Identity and Organization Boundary** after P2-WP01 acceptance. Do **not** begin until authorized.
