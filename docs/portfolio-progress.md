# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [Approved architecture](engineering/approved-architecture-summary.md) | [P9-WP01 report](reports/P9-WP01-security-and-privacy-hardening.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (ignored nested `HealthCare/`) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 9 — MVP Hardening and Release (**In Progress**) |
| Current work package | P9-WP01 — Security and Privacy Hardening (**Complete** with documented risks) |
| Overall status | **Phase 9 in progress** — security/privacy hardening complete with documented risks; not production-ready; next P9-WP02 when authorized |
| Latest verified commit | de4fac64739f5b368a6b1f2490223fa032201b65 |
| Open blockers | Missing production auth (JWT/MFA/SSO/AD) (R-091); R-022 refresh durations / no offline entitlement grace; R-035 calendar EOM; no payment gateway; no interactive Android emulator (R-109); Production HTTPS/MAUI cleartext replacement gate; SQLitePCLRaw NU1903 (R-129); catalog/sales/inventory/expenses/reports online-only by design; report export deferred; no POS operational roles |
| Last updated | 2026-07-31 |

## Delivery sequence

```text
P8-WP07 ✓ Basic Store Closeout (Phase 8 closed)
        ↓
P9-WP01 ✓ Security and Privacy Hardening (complete with risks)
        ↓
P9-WP02 ○ Performance and Reliability (not started — do not begin until authorized)
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
| 9 | MVP Hardening and Release | **In Progress** | 1 | 6 | ~17% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | Future | 0 | 8 | 0% | [Open](phases/phase-10-full-pos.md) |

**MVP phases 0–9:** 46 / 52 = **88.46%**.

## Phase 9 work packages

| WP | Status | Key commit |
|---|---|---|
| P9-WP01 | Complete with risks | de4fac64739f5b368a6b1f2490223fa032201b65 |
| P9-WP02 | Not Started | — |
| P9-WP03 | Not Started | — |
| P9-WP04 | Not Started | — |
| P9-WP05 | Not Started | — |
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
