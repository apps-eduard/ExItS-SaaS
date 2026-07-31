# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [Approved architecture](engineering/approved-architecture-summary.md) | [P10-WP01 scope ambiguity](reports/P10-WP01-scope-ambiguity.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (separate historical product; not in this workspace) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 13 — Production Authentication and Identity (**in progress**) |
| Current work package | P13-WP05 — Trusted API Actor and Organization Context (**complete**) |
| Overall status | **Phase 13 in progress.** P13-WP01–WP05 complete (architecture + credentials + browser session + password lifecycle + trusted org context). Bearer/product-client/MFA/closeout remain. R-091 open. Exact next: P13-WP06 — Product Client Auth Integration (do not begin until authorized). **Not production-ready.** |
| Latest verified commit | `65b261eca7353a7efea2f8f1899c252f0dcee6dc` |
| Open blockers | Missing production auth (R-091); D-P12-03 commercial-state transport; D-P12-04 matrix hygiene; R-109 interactive Android; R-129 / NU1903; Production TLS; MAUI HTTPS-only Production policy; Manual GCash unverified; online-only admin/Full POS mutation limits; report export deferred; PITR deferred; local unsynced ops outside server backups; tax/accounting deferred; formal WCAG cert not claimed |
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
P9-WP06 ✓ Commercial MVP Closeout (complete with risks — Phase 9 closed)
        ↓
P10-WP01 ✓ Suppliers (Option A — master data only)
        ↓
P10-WP02 ✓ Purchasing
        ↓
P10-WP03 ✓ Advanced Inventory
        ↓
P10-WP04 ✓ Cashier Shifts
        ↓
P10-WP05 ✓ Returns and Refunds
        ↓
P10-WP06 ✓ Advanced Permissions and Operational Reports
        ↓
P10-WP07 ✓ Multiple Registers
        ↓
P10-WP08 ✓ Phase 10 Closeout (complete with risks — Phase 10 closed)
        ↓
P11-WP01 ✓ Web UI Audit and Component Inventory
        ↓
P11-WP02 ✓ Global Web Layout and Navigation
        ↓
P11-WP03 ✓ Shared Forms, Validation, and Dialogs
        ↓
P11-WP04 ✓ Shared Tables, Lists, Cards, and Status Components
        ↓
P11-WP05 ✓ Shared Reporting Framework
        ↓
P11-WP06 ✓ Dashboard and Report Refactoring
        ↓
P11-WP07 ✓ Localization, Theme, Accessibility, and Responsive QA
        ↓
P11-WP08 ✓ Phase 11 Closeout (complete with risks — Phase 11 closed)
        ↓
P12-WP01 ✓ Platform–Product Contract Audit
        ↓
P12-WP02 ✓ Authoritative Product Foundation Reference
        ↓
P12-WP03 ✓ Product Documentation Templates
        ↓
P12-WP04 ✓ Cursor Product Context Rule
        ↓
P12-WP05 ✓ Product Bootstrap Prompt
        ↓
P12-WP06 ✓ Reference Product Dry Run
        ↓
P12-WP07 ✓ Foundation Hardening and Closeout (complete with risks — Phase 12 closed)
        ↓
P13-WP01 ✓ Authentication Architecture and Threat Model
        ↓
P13-WP02 ✓ Identity Credentials and Auth Persistence
        ↓
P13-WP03 ✓ Platform Login, Logout, and Browser Session
        ↓
P13-WP04 ✓ Password Lifecycle, Lockout, and Verification
        ↓
P13-WP05 ✓ Trusted API Actor and Organization Context
        ↓
P13-WP06 ○ Product Client Auth Integration (Admin + MAUI/POS) (do not begin until authorized)
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
| 9 | MVP Hardening and Release | **Complete with documented risks** | 6 | 6 | 100% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | **Complete with documented risks** | 8 | 8 | 100% | [Open](phases/phase-10-full-pos.md) |
| 11 | Web UI and Reporting Design System | **Complete with documented risks** | 8 | 8 | 100% | [Open](phases/phase-11-web-ui-reporting-design-system.md) |
| 12 | Reusable SaaS Product Foundation and Bootstrap | **Complete with documented open decisions** | 7 | 7 | 100% | [Open](phases/phase-12-product-foundation-and-bootstrap.md) |
| 13 | Production Authentication and Identity | **In progress** | 5 | 8 | — | [Open](phases/phase-13-production-authentication-and-identity.md) |

**MVP phases 0–9:** 52 / 52 = **100%** (with documented risks; not Production-ready).
**Phase 10 Full POS:** 8 / 8 = **100%** (with documented risks; not Production-ready).
**Phase 11 Web UI / Reporting Design System:** 8 / 8 = **100%** (with documented risks; not Production-ready; no formal WCAG certification).
**Phase 12 Product Foundation:** 7 / 7 = **100%** (with documented open decisions; not Production-ready; no real product scaffold; R-091 open).
**Phase 13 Production Authentication:** 5 / 8 WPs complete (credentials, session, password lifecycle, trusted org context; bearer/product-client/MFA/closeout remain; R-091 still open).

## Phase 13 work packages

| WP | Status | Key commit |
|---|---|---|
| P13-WP01 — Authentication Architecture and Threat Model | Complete | `40a48349ae2a42e9dc267bde0df64afb004af3ae` |
| P13-WP02 — Identity Credentials and Auth Persistence | Complete | `51ace5b90fc6c0bcb33fe483826481529bdfeb77` |
| P13-WP03 — Platform Login, Logout, and Browser Session | Complete | `6298b668c5d0555a84eb206b2a2313b138c9b892` |
| P13-WP04 — Password Lifecycle, Lockout, and Verification | Complete | `65b261eca7353a7efea2f8f1899c252f0dcee6dc` |
| P13-WP05 — Trusted API Actor and Organization Context | Complete | `e64f352161bb20447a99ae762d1a69ec1a3846fe` |

## Phase 12 work packages

| WP | Status | Key commit |
|---|---|---|
| P12-WP01 — Platform–Product Contract Audit | Complete | `32889be0851fa0969e8abfa6b7c66784b12e9e8b` |
| P12-WP02 — Authoritative Product Foundation Reference | Complete | `8f151d658011a3ad0854aab9f8774361f8a788a6` |
| P12-WP03 — Product Documentation Templates | Complete | `65b02a1dd9336b39b79fc41527969f6289ad7072` |
| P12-WP04 — Cursor Product Context Rule | Complete | `1243c78d65e347b23949b19ce2edf564fe972aad` |
| P12-WP05 — Product Bootstrap Prompt | Complete | `d57b7be48639e30ffa9fa86624da916ef63a563f` |
| P12-WP06 — Reference Product Dry Run | Complete | `5debab509c52ecdbed1cf9bba1ec02147ece693b` |
| P12-WP07 — Foundation Hardening and Closeout | Complete | `2a3de32cb3bcc1c30db34771843c054e74f6a29e` |

## Phase 11 work packages

| WP | Status | Key commit |
|---|---|---|
| P11-WP01 — Web UI Audit and Component Inventory | Complete | `221fe69ab179956e8a73411cf3eb58fd6f199c3c` |
| P11-WP02 — Global Web Layout and Navigation | Complete | `7ce7df139a9494c9aab7d189900e96d5e43fdc1d` |
| P11-WP03 — Shared Forms, Validation, and Dialogs | Complete | `6825b8eb423e73cd5d3dc24e393e7201b04232bc` |
| P11-WP04 — Shared Tables, Lists, Cards, and Status Components | Complete | `0351f547457522a97a168b802ec050ef6f37ee83` |
| P11-WP05 — Shared Reporting Framework | Complete | `4d832b39d85d7f8db55234f609188666035f34c5` |
| P11-WP06 — Dashboard and Report Refactoring | Complete | `6688fa674e5edc139a931dae3faefeb8b25a806b` |
| P11-WP07 — Localization, Theme, Accessibility, and Responsive QA | Complete | `24ee744fa15152bc325568ba6c5a99de78359921` |
| P11-WP08 — Phase 11 Closeout | Complete | `ff2ad9e2e756f6e011fcf60f14e6350a3c15e32e` |

## Phase 10 work packages

| WP | Status | Key commit |
|---|---|---|
| P10-WP01 — Suppliers | Complete (Option A) | 6f92dd43b2f66709891d82079f9d3fbd0b5c450e |
| P10-WP02 — Purchasing | Complete | c0f8130ef99e958bceaee98024a69339b7e8e41a |
| P10-WP03 — Advanced Inventory | Complete | 5c62133 (+ gap-fix 31d809c) |
| P10-WP04 — Cashier Shifts | Complete | 4076485 |
| P10-WP05 — Returns and Refunds | Complete | 58dd6bf (+ Android using fix 6cb06cc) |
| P10-WP06 — Advanced Permissions and Operational Reports | Complete | 1e46f6eb142d1c14455f954e7c8286abeb1ddff3 |
| P10-WP07 — Multiple Registers | Complete | 7dda3baedd452b39cb5d4fab55fb700ef67a9639 |
| P10-WP08 — Phase 10 Closeout | Complete | validation `32395ff1…`; docs tip `de09f97b0045636f9da004f1b7cc95bf7be17441` |

## Phase 9 work packages

| WP | Status | Key commit |
|---|---|---|
| P9-WP01 | Complete with risks | de4fac64739f5b368a6b1f2490223fa032201b65 |
| P9-WP02 | Complete with risks | 46a4ac7bacfad0736fba4741817958862fadf9e2 |
| P9-WP03 | Complete with risks | 3bbb0c716da60bd7d87a191c35bd0eced1bde380 |
| P9-WP04 | Complete with risks | f7b3aecec614eea8b1de601cd08e843f4aea91f8 |
| P9-WP05 | Complete with risks | 9c1bbd0557e252758a772b985c907233da3f5214 |
| P9-WP06 | Complete with risks | f6117c59e9c63d629af5805cf2d4ae7f8ea61225 |

## Permanent workflow rules

Follow `.cursor/rules/exits-workflow.mdc`. Do not begin **P13-WP06 — Product Client Auth Integration (Admin + MAUI/POS)** until explicitly authorized.
