# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [Approved architecture](engineering/approved-architecture-summary.md) | [P3-WP05 closeout](reports/P3-WP05-billing-closeout.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (ignored nested `HealthCare/`) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 3 — Portfolio Billing, Plans and Entitlements |
| Current work package | P3-WP05 — Billing Closeout (**Complete**) |
| Overall status | **Phase 3 Complete with documented risks** — commercial foundation closed; Admin UI / auth / delivery deferred |
| Latest verified commit | _(to be recorded)_ |
| Open blockers | Unauthenticated APIs; R-022 refresh durations; R-035 calendar EOM; no product delivery; no auth |
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
P3-WP04 Entitlement Snapshots and Grace Rules ✓
        ↓
P3-WP05 Billing Closeout ✓
        ↓
Phase 4 / P4-WP01 (not started — do not begin until authorized)
```

## Phase progress

| Phase | Name | Status | Completed | Total | Progress | Link |
|---:|---|---|---:|---:|---:|---|
| 0 | Existing HealthCare Assessment | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-00-healthcare-assessment.md) |
| 1 | Platform Boundary and Architecture | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-01-platform-boundary.md) |
| 2 | Platform Extraction and HealthCare Reconnection | **Complete with documented risks** | 6 | 6 | 100% | [Open](phases/phase-02-platform-extraction.md) |
| 3 | Portfolio Billing, Plans and Entitlements | **Complete with documented risks** | 5 | 5 | 100% | [Open](phases/phase-03-billing-entitlements.md) |
| 4 | Platform Admin Expansion | Not Started | 0 | 4 | 0% | [Open](phases/phase-04-platform-admin.md) |
| 5 | PinoyBusinessPOS MAUI Foundation | Not Started | 0 | 5 | 0% | [Open](phases/phase-05-pos-maui-foundation.md) |
| 6 | Utang MVP | Not Started | 0 | 6 | 0% | [Open](phases/phase-06-utang-mvp.md) |
| 7 | Offline Synchronization | Not Started | 0 | 5 | 0% | [Open](phases/phase-07-offline-sync.md) |
| 8 | Basic Store | Not Started | 0 | 7 | 0% | [Open](phases/phase-08-basic-store.md) |
| 9 | MVP Hardening and Release | Not Started | 0 | 6 | 0% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | Future | 0 | 8 | 0% | [Open](phases/phase-10-full-pos.md) |

**MVP phases 0–9:** 19 / 52 = **36.54%**.

## Phase 3 work packages

| WP | Status | Key commit |
|---|---|---|
| P3-WP01 | Complete (accepted) | `9d01f26` |
| P3-WP02 | Complete (accepted) | `616d8ad` |
| P3-WP03 | Complete (accepted) | `934c1d6` |
| P3-WP04 | Complete (accepted) | `44dc236` |
| P3-WP05 | Complete | _(to be recorded)_ |

## Phase 3 closeout snapshot

| Item | Value |
|---|---|
| Decision | Complete with documented risks |
| Tables | 13 in `platform` schema |
| Migrations | 4 (catalog → orgs/subs → payments → entitlements) |
| Tests | 302 passed / 0 failed / 0 skipped |
| Delivery | None — Platform authoritative records only |
| Auth | Development-stage unauthenticated APIs |
| HealthCare | Frozen |

## Latest tests

Root Release: **302 passed / 0 failed / 0 skipped** (200 unit + 38 architecture + 64 integration). HealthCare not rebuilt.

## Next approved action

**Phase 4 / P4-WP01 — Portfolio Navigation and Product Views** when explicitly authorized. Do **not** begin until authorized.
