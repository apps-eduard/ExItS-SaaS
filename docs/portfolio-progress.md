# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [Approved architecture](engineering/approved-architecture-summary.md) | [P5-WP05 report](reports/P5-WP05-authentication-onboarding-and-closeout.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (ignored nested `HealthCare/`) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 5 — PinoyBusinessPOS MAUI Foundation (**Complete with documented risks**) |
| Current work package | P5-WP05 — Authentication, Onboarding and Closeout (**Complete**) |
| Overall status | **Phase 5 complete with risks** — Dev/Testing auth + onboarding; next Phase 6 when authorized |
| Latest verified commit | *(feature hash after push)* |
| Open blockers | Missing production auth (JWT/MFA/SSO/AD); R-022 refresh durations; R-035 calendar EOM; no product delivery; no payment gateway; POS foundation not offline business; no interactive Android emulator attached (R-109); no POS operational roles |
| Last updated | 2026-07-30 |

## Delivery sequence

```text
Phase 4 ✓
        ↓
P5-WP01 … P5-WP05 ✓ (Phase 5 complete with documented risks)
        ↓
Phase 6 — Utang MVP (not started — do not begin until authorized)
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
| 6 | Utang MVP | Not Started | 0 | 6 | 0% | [Open](phases/phase-06-utang-mvp.md) |
| 7 | Offline Synchronization | Not Started | 0 | 5 | 0% | [Open](phases/phase-07-offline-sync.md) |
| 8 | Basic Store | Not Started | 0 | 7 | 0% | [Open](phases/phase-08-basic-store.md) |
| 9 | MVP Hardening and Release | Not Started | 0 | 6 | 0% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | Future | 0 | 8 | 0% | [Open](phases/phase-10-full-pos.md) |

**MVP phases 0–9:** 28 / 52 = **53.85%**.

## Phase 5 work packages

| WP | Status | Key commit |
|---|---|---|
| P5-WP01 | Complete | 3015925d16560be13953270565c1ab99a8d69934 |
| P5-WP02 | Complete | 3d3cba840ffff20dc07ae7237d7f81c3873a502e |
| P5-WP03 | Complete | 1dea793407adaa9e8a27c19f45727bc90d866f60 |
| P5-WP04 | Complete | 763b0dc7cd73ab21ada2d101d115423c23d90cfa |
| P5-WP05 | Complete | *(feature hash after push)* |

## Phase 4 work packages

| WP | Status | Key commit |
|---|---|---|
| P4-WP01 | Complete | `aa340e1` |
| P4-WP02 | Complete | `6f1cacb` |
| P4-WP03 | Complete | `91e88c3` |
| P4-WP04 | Complete | `74ed46d` |

## Permanent workflow rules

`.cursor/rules/exits-workflow.mdc` — HealthCare freeze, Git, build/test, security/architecture, documentation/reporting.

## Latest tests

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Unit | 261 | 0 | 0 |
| Architecture | 41 | 0 | 0 |
| Admin unit | 27 | 0 | 0 |
| DesignSystem | 17 | 0 | 0 |
| ApiClient | 17 | 0 | 0 |
| Maui | 15 | 0 | 0 |
| Integration | 84 | 0 | 0 |
| **Total** | **462** | **0** | **0** |
