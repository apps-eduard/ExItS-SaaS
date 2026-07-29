# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [Approved architecture](engineering/approved-architecture-summary.md) | [P3-WP04 report](reports/P3-WP04-entitlement-snapshots-and-grace-rules.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (ignored nested `HealthCare/`) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 3 — Portfolio Billing, Plans and Entitlements |
| Current work package | P3-WP04 — Entitlement Snapshots and Grace Rules (**Ready for Review**) |
| Overall status | Phase 2 complete; catalog + subscriptions + payments + entitlement snapshots delivered |
| Latest verified commit | `44dc236a8aab38cce7071d957aa560470911a4db` |
| Open blockers | Unauthenticated APIs (R-045/R-055/R-062); R-022 refresh durations provisional; R-035 calendar EOM; no product delivery |
| Last updated | 2026-07-29 |

## Delivery sequence

```text
Phase 2 ✓
        ↓
P3-WP01 Product and Plan Catalog ✓
        ↓
P3-WP02 Trials and Subscription Lifecycle ✓
        ↓
P3-WP03 Manual Payment Activation ✓
        ↓
P3-WP04 Entitlement Snapshots and Grace Rules  ← in review
        ↓
P3-WP05 Billing Closeout (not started)
```

## Phase progress

| Phase | Name | Status | Completed | Total | Progress | Link |
|---:|---|---|---:|---:|---:|---|
| 0 | Existing HealthCare Assessment | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-00-healthcare-assessment.md) |
| 1 | Platform Boundary and Architecture | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-01-platform-boundary.md) |
| 2 | Platform Extraction and HealthCare Reconnection | **Complete with documented risks** | 6 | 6 | 100% | [Open](phases/phase-02-platform-extraction.md) |
| 3 | Portfolio Billing, Plans and Entitlements | In Progress | 3 | 5 | 60% | [Open](phases/phase-03-billing-entitlements.md) |
| 4 | Platform Admin Expansion | Not Started | 0 | 4 | 0% | [Open](phases/phase-04-platform-admin.md) |
| 5 | PinoyBusinessPOS MAUI Foundation | Not Started | 0 | 5 | 0% | [Open](phases/phase-05-pos-maui-foundation.md) |
| 6 | Utang MVP | Not Started | 0 | 6 | 0% | [Open](phases/phase-06-utang-mvp.md) |
| 7 | Offline Synchronization | Not Started | 0 | 5 | 0% | [Open](phases/phase-07-offline-sync.md) |
| 8 | Basic Store | Not Started | 0 | 7 | 0% | [Open](phases/phase-08-basic-store.md) |
| 9 | MVP Hardening and Release | Not Started | 0 | 6 | 0% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | Future | 0 | 8 | 0% | [Open](phases/phase-10-full-pos.md) |

**MVP phases 0–9:** 17 / 52 = **32.69%** (P3-WP01–03 accepted; P3-WP04 in review).

## Phase 3 work packages

| WP | Status | Key commit |
|---|---|---|
| P3-WP01 | Complete (accepted) | `9d01f26` |
| P3-WP02 | Complete (accepted) | `616d8ad` |
| P3-WP03 | Complete (accepted) | `934c1d6` |
| P3-WP04 | Ready for Review | `44dc236` |
| P3-WP05 | Not Started | — |

## Entitlement snapshot (P3-WP04)

| Item | Value |
|---|---|
| Persistence | `feature_overrides`, `entitlement_snapshots`, `entitlement_snapshot_grants` |
| Migration | `AddEntitlementSnapshotsAndOverrides` |
| Versioning | Monotonic per org+product; unique DB constraint |
| Composition | Plan/trial → override → status restrictions |
| Refresh | Provisional 24h (`IEntitlementRefreshPolicy`); R-022 open |
| Delivery | **None** — authoritative Platform records only |
| API | `/api/v1/platform/.../entitlements/*`, `feature-overrides/*` (unauthenticated) |
| Tests | 301 passed / 0 failed / 0 skipped |
| HealthCare | Frozen |

## Latest tests

Root Release: **301 passed / 0 failed / 0 skipped** (200 unit + 38 architecture + 63 integration). HealthCare not rebuilt.

## Next approved action

**P3-WP05 — Billing Closeout** after P3-WP04 acceptance. Do **not** begin until authorized.
