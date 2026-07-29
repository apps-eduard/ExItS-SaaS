# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [All Phases](phases/README.md) | [Capability boundary](engineering/platform-product-capability-boundary.md) | [P1-WP01 report](reports/P1-WP01-platform-product-capability-boundary.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP (ignored nested `HealthCare/`) |
| New product | PinoyBusinessPOS (SME retail; initial focus Sari-Sari / mini grocery) |
| Current phase | Phase 1 — Platform Boundary and Architecture |
| Current work package | P1-WP01 — Platform vs Product Capability Boundary (**Ready for Review**) |
| Overall status | Phase 0 **Complete with documented risks**; P1-WP01 ready for review |
| Latest verified commit | `b6a3133732f6d29c68159447eb1ca43ea0b1212b` (`docs(architecture): define platform product boundaries`) |
| Open blockers | 0 for P1-WP01 acceptance; root remote empty (user push when authorized) |
| Last updated | 2026-07-29 |

## Delivery sequence

```text
Assess completed HealthCare MVP ✓
        ↓
Approve platform/product boundaries  ← P1-WP01 (in review)
        ↓
Extract or adapt ExITS Platform safely
        ↓
Reconnect and regression-test HealthCare
        ↓
Add portfolio plans, billing and entitlements
        ↓
Build PinoyBusinessPOS MAUI foundation
        ↓
Utang MVP → Offline → Basic Store → Harden → Full POS
```

## Phase progress

| Phase | Name | Status | Completed | Total | Progress | Link |
|---:|---|---|---:|---:|---:|---|
| 0 | Existing HealthCare Assessment | **Complete with documented risks** | 4 | 4 | 100% | [Open](phases/phase-00-healthcare-assessment.md) |
| 1 | Platform Boundary and Architecture | In Progress | 0 | 4 | 0% | [Open](phases/phase-01-platform-boundary.md) |
| 2 | Platform Extraction and HealthCare Reconnection | Not Started | 0 | 6 | 0% | [Open](phases/phase-02-platform-extraction.md) |
| 3 | Portfolio Billing, Plans and Entitlements | Not Started | 0 | 5 | 0% | [Open](phases/phase-03-billing-entitlements.md) |
| 4 | Platform Admin Expansion | Not Started | 0 | 4 | 0% | [Open](phases/phase-04-platform-admin.md) |
| 5 | PinoyBusinessPOS MAUI Foundation | Not Started | 0 | 5 | 0% | [Open](phases/phase-05-pos-maui-foundation.md) |
| 6 | Utang MVP | Not Started | 0 | 6 | 0% | [Open](phases/phase-06-utang-mvp.md) |
| 7 | Offline Synchronization | Not Started | 0 | 5 | 0% | [Open](phases/phase-07-offline-sync.md) |
| 8 | Basic Store | Not Started | 0 | 7 | 0% | [Open](phases/phase-08-basic-store.md) |
| 9 | MVP Hardening and Release | Not Started | 0 | 6 | 0% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | Future | 0 | 8 | 0% | [Open](phases/phase-10-full-pos.md) |

**MVP phases 0–9:** 4 / 52 work packages = **7.69%** (4÷52). P1-WP01 not counted until accepted.

## Completed Phase 0 work packages

| WP | Status | Key commit |
|---|---|---|
| P0-WP01 | Complete | `663b5bf` |
| P0-WP02 | Complete | `6b56e6d` |
| P0-WP03 | Complete (+ UI correction `e310cf8`) | `5d628dd` / `e310cf8` |
| P0-WP04 | Complete (Phase 0 closed with documented risks) | `f52316a` / hash `374e699` |

## P1-WP01 decisions (summary)

| Topic | Decision |
|---|---|
| Platform Organization | Global SaaS customer boundary |
| Multi-product orgs / multi-org users | Yes / Yes (target) |
| Clinics / stores | Product-local; multiple allowed |
| Access vs permissions | Platform access; product operational permissions |
| Subscriptions / entitlements | Platform authoritative; local projections |
| Cross-DB FKs / clinical in Platform | Prohibited |
| Customer vs User / SaaS vs retail payment | Separate |
| Shared code | Only after two verified consumers |
| ADR | ADR-011 Accepted; ADR-009 Accepted via ADR-011 |

## Latest tests

Docs-only P1-WP01 — no runtime tests required. Prior Windows-safe baseline: **1102 passed / 0 failed / 0 skipped** (P0-WP02).

## Next approved action

**P1-WP02 — Data Ownership and Contracts** after P1-WP01 acceptance. Do **not** begin until authorized. Do not create application projects or import HealthCare in P1-WP01.
