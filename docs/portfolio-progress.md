# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [All Phases](phases/README.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP at `HealthCare/` — reuse assessed (P0-WP01) |
| New product | PinoyBusinessPOS |
| Current phase | Phase 0 — Existing HealthCare Assessment |
| Current work package | P0-WP01 — Repository and Reuse Inventory (**Ready for Review**) |
| Overall status | In Progress |
| Latest verified commit | `663b5bf3269ee934d107bacc467d253a4bf28a90` (`docs(platform): assess healthcare SaaS reuse`) |
| Open blockers | 0 extraction blockers; nested Git + missing Android SDK noted as risks |
| Last updated | 2026-07-29 |

## Delivery sequence

```text
Assess completed HealthCare MVP
        ↓
Approve platform/product boundaries
        ↓
Extract or adapt ExITS Platform safely
        ↓
Reconnect and regression-test HealthCare
        ↓
Add portfolio plans, billing and entitlements
        ↓
Build PinoyBusinessPOS MAUI foundation
        ↓
Utang MVP
        ↓
Offline synchronization
        ↓
Basic Store
        ↓
MVP hardening and commercial release
        ↓
Full POS after MVP
```

## Phase progress

| Phase | Name | Status | Completed | Total | Progress | Link |
|---:|---|---|---:|---:|---:|---|
| 0 | Existing HealthCare Assessment | In Progress | 1 | 4 | 25% | [Open](phases/phase-00-healthcare-assessment.md) |
| 1 | Platform Boundary and Architecture | Not Started | 0 | 4 | 0% | [Open](phases/phase-01-platform-boundary.md) |
| 2 | Platform Extraction and HealthCare Reconnection | Not Started | 0 | 6 | 0% | [Open](phases/phase-02-platform-extraction.md) |
| 3 | Portfolio Billing, Plans and Entitlements | Not Started | 0 | 5 | 0% | [Open](phases/phase-03-billing-entitlements.md) |
| 4 | Platform Admin Expansion | Not Started | 0 | 4 | 0% | [Open](phases/phase-04-platform-admin.md) |
| 5 | PinoyBusinessPOS MAUI Foundation | Not Started | 0 | 5 | 0% | [Open](phases/phase-05-pos-maui-foundation.md) |
| 6 | Utang MVP | Not Started | 0 | 6 | 0% | [Open](phases/phase-06-utang-mvp.md) |
| 7 | Offline Synchronization | Not Started | 0 | 5 | 0% | [Open](phases/phase-07-offline-sync.md) |
| 8 | Basic Store | Not Started | 0 | 7 | 0% | [Open](phases/phase-08-basic-store.md) |
| 9 | MVP Hardening and Release | Not Started | 0 | 6 | 0% | [Open](phases/phase-09-mvp-hardening.md) |
| 10 | Full POS | Future | 0 | 8 | 0% | [Open](phases/phase-10-full-pos.md) |

Phase 10 is excluded from the first commercial MVP percentage.

**MVP phases 0–9:** 1 / 52 work packages = **1.92%** (1÷52).

## Current work package

### P0-WP01 — Repository and Reuse Inventory

- [x] Locate the copied HealthCare solution and all projects.
- [x] Record framework versions, packages, databases, deployment files and test projects.
- [x] Inventory Ant Design Blazor usage and coupling.
- [x] Identify generic SaaS capabilities already implemented.
- [x] Identify healthcare-only capabilities.
- [x] Record current build/test evidence without changing behavior.
- [x] Produce `docs/reports/P0-WP01-completion.md`.
- [x] Create one focused commit and record the hash.
- [ ] Confirm a clean working tree. *(Docs committed; `HealthCare/` intentionally untracked — see report.)*

Assessment artifacts:

- [HealthCare reuse assessment](reuse/healthcare-reuse-assessment.md)
- [Reuse classification matrix](reuse/reuse-classification-matrix.md)
- [P0-WP01 completion](reports/P0-WP01-completion.md)

## Actual repository findings (P0-WP01)

- Root contains `HealthCare/`, `docs/`, `README.md`, `FILE-MANIFEST.md`.
- Solution: `HealthCare/HealthCare.sln` — 9 src + 7 test + tools projects; **net10.0**; AntDesign **1.6.2**; PostgreSQL/EF Core; JWT+refresh; Hangfire.
- Nested `HealthCare/.git` present (remote `apps-eduard/HealthCare`); parent had no commits before this WP.
- Billing/plans/trials/subscriptions/entitlements: **missing**.
- Localization and Light/Dark/System themes: **missing**.

## Latest tests

| Suite | Passed | Failed | Skipped | Notes |
|---:|---:|---:|---:|---|
| UnitTests | 566 | 0 | 0 | Release `--no-build` |
| ArchitectureTests | 20 | 0 | 0 | |
| Web.Tests | 340 | 0 | 0 | |
| PatientWeb.Tests | 13 | 0 | 0 | |
| Mobile.Tests | 163 | 0 | 0 | Mobile.Core tests (no Android SDK needed) |
| **Total (Windows-safe)** | **1102** | **0** | **0** | |
| IntegrationTests | — | — | — | Not run (Testcontainers; README: not on Windows baseline) |
| EndToEndTests | — | — | — | Not run (Playwright/Compose) |
| Full solution build | — | — | — | Exit 1: `HealthCare.Mobile` XA5300 Android SDK missing |

## Risks

See [risks-and-issues.md](risks-and-issues.md) — especially nested Git (R-010), no EF tenant filters (R-011), missing billing (R-012), Ant→POS coupling (R-005).

## Next approved action

Execute only **`P0-WP02 — Baseline Build, Tests and Runtime Map`** after P0-WP01 review acceptance. Do not begin extraction.
