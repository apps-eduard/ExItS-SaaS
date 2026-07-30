# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [Approved architecture](engineering/approved-architecture-summary.md) | [P9-WP05 report](reports/P9-WP05-pilot-and-deployment.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (ignored nested `HealthCare/`) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 9 — MVP Hardening and Release (**In Progress**) |
| Current work package | P9-WP05 — Pilot and Deployment (**Complete** with documented risks) |
| Overall status | **Phase 9 in progress** — P9-WP01–P9-WP05 complete with documented risks; Ready for controlled internal technical pilot; **not** Production-ready; next P9-WP06 when authorized |
| Latest verified commit | _(feature commit recorded after push)_ |
| Open blockers | Missing production auth (JWT/MFA/SSO/AD) (R-091); R-022 refresh durations / no offline entitlement grace; R-035 calendar EOM; no payment gateway; no interactive Android emulator (R-109) including TalkBack/theme interactive QA; Production HTTPS/MAUI cleartext replacement gate; SQLitePCLRaw NU1903 (R-129); unsynced local MAUI data not recoverable from server backups; PITR deferred; Production off-host encrypted backup scheduling environment-owned; catalog/sales/inventory/expenses/reports online-only by design; report export deferred; no POS operational roles; full MVP-scale load/soak not proven in CI |
| Last updated | 2026-07-31 |

## Delivery sequence

```text
P9-WP01 ✓ Security and Privacy Hardening (complete with risks)
        ↓
P9-WP02 ✓ Performance and Reliability (complete with risks)
        ↓
P9-WP03 ✓ Backup and Restore (complete with risks)
        ↓
P9-WP04 ✓ Accessibility, Localization and Theme QA (complete with risks)
        ↓
P9-WP05 ✓ Pilot and Deployment (complete with risks)
        ↓
P9-WP06 ○ Commercial MVP Closeout (not started — do not begin until authorized)
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
| 7 | Offline Synchronization | **Complete with documented risks** | 5 | 5 | 100% | [Open](phases/phase-07-offline-sync.md) |
| 8 | Basic Store | **Complete with documented risks** | 7 | 7 | 100% | [Open](phases/phase-08-basic-store.md) |
| 9 | MVP Hardening and Release | **In Progress** | 5 | 6 | ~83% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | Future | 0 | 8 | 0% | [Open](phases/phase-10-full-pos.md) |

**MVP phases 0–9:** 50 / 52 = **96.15%**.

## Phase 9 work packages

| WP | Status | Key commit |
|---|---|---|
| P9-WP01 | Complete with risks | de4fac64739f5b368a6b1f2490223fa032201b65 |
| P9-WP02 | Complete with risks | 46a4ac7bacfad0736fba4741817958862fadf9e2 |
| P9-WP03 | Complete with risks | 3bbb0c716da60bd7d87a191c35bd0eced1bde380 |
| P9-WP04 | Complete with risks | f7b3aecec614eea8b1de601cd08e843f4aea91f8 |
| P9-WP05 | Complete with risks | _(recorded after feature commit)_ |
| P9-WP06 | Not Started | — |

## Phase 8 work packages

| WP | Status | Key commit |
|---|---|---|
| P8-WP01 | Complete with risks | 5573822ca116ab46f1a5cdce407e1d7b4f58f796 |
| P8-WP02 | Complete with risks | 72a6fa9b1bb6f48610563d01ee10e608e99806e1 |
| P8-WP03 | Complete with risks | cd58f5c7dc1b9d31497429ef1d025546a0def09c |
| P8-WP04 | Complete with risks | 64f05e7fd5ab868beb62c7cce88ad7a15e21c7b8 |
| P8-WP05 | Complete with risks | ca956921fbfcfad8499f01acb9d9726fff2d81d4 |
| P8-WP06 | Complete with risks | a0028f36a0d8e2ea76c3101b2b65ba82bfd4fd02 |
| P8-WP07 | Complete with risks | 0bc5ebb999c0708e6ac76b04a30d522037eec3cb |

## Permanent workflow rules

Follow `.cursor/rules/exits-workflow.mdc`. HealthCare remains frozen. Do not begin unauthorized work packages.
