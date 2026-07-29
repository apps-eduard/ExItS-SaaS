# ExITS SaaS Portfolio Progress Dashboard

> Primary status page. Cursor must update this file after every completed work package. Percentages are calculated from approved work packages, never estimated.

[Documentation Home](index.md) | [All Phases](phases/README.md)

## Current status

| Field | Value |
|---|---|
| Portfolio | ExITS SaaS |
| Existing product | HealthCare SaaS MVP at ignored nested `HealthCare/` |
| New product | PinoyBusinessPOS |
| Current phase | Phase 0 — Existing HealthCare Assessment |
| Current work package | P0-WP02 — Baseline Build, Tests, Runtime and Repository Safety Map (**Ready for Review**) |
| Overall status | In Progress |
| Latest verified commit | `6b56e6dfec93f49e43a9c1a92baea1300d148b28` (`chore(repo): establish safe healthcare baseline`) |
| Open blockers | 0 extraction blockers; root remote empty (first push needs user authorization); Android SDK env wiring incomplete |
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
| 0 | Existing HealthCare Assessment | In Progress | 2 | 4 | 50% | [Open](phases/phase-00-healthcare-assessment.md) |
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

**MVP phases 0–9:** 2 / 52 work packages = **3.85%** (2÷52).

## Completed work packages

### P0-WP01 — Repository and Reuse Inventory — Complete

Accepted. Assessment commit `663b5bf3269ee934d107bacc467d253a4bf28a90`.

Artifacts: [reuse assessment](reuse/healthcare-reuse-assessment.md), [matrix](reuse/reuse-classification-matrix.md), [report](reports/P0-WP01-completion.md).

### P0-WP02 — Baseline Build, Tests, Runtime and Repository Safety Map — Ready for Review

- [x] Root `.gitignore` excludes `HealthCare/` and common secrets/build outputs.
- [x] Nested Git + empty root remote documented.
- [x] Toolchain inventory recorded.
- [x] Non-MAUI build verified; full solution XA5300 classified environmental.
- [x] Windows-safe tests re-run: **1102 passed / 0 failed / 0 skipped**.
- [x] Runtime, ports, DB, config maps written.
- [x] Completion report created.
- [x] Commit hash recorded after focused commit.
- [x] Working tree clean after commit (ignored HealthCare only).

Artifacts:

- [Runtime baseline](reuse/healthcare-runtime-baseline.md)
- [Repository boundaries](engineering/repository-boundaries.md)
- [Development environment](engineering/development-environment.md)
- [P0-WP02 report](reports/P0-WP02-baseline-runtime-map.md)

## Git boundary status

| Item | Status |
|---|---|
| Root tracks docs only | Yes |
| `HealthCare/` ignored | Yes (`gitignore:6:HealthCare/`) |
| HealthCare files in root index | None |
| Nested `HealthCare/.git` | Present; not removed |
| Root `origin` | `https://github.com/apps-eduard/ExItS-SaaS.git` (**empty remote**) |
| Upstream | `main...origin/main [gone]` — first push requires user authorization |

## Latest tests (P0-WP02 re-run)

| Suite | Passed | Failed | Skipped |
|---:|---:|---:|---:|
| UnitTests | 566 | 0 | 0 |
| ArchitectureTests | 20 | 0 | 0 |
| Web.Tests | 340 | 0 | 0 |
| PatientWeb.Tests | 13 | 0 | 0 |
| Mobile.Tests | 163 | 0 | 0 |
| **Total** | **1102** | **0** | **0** |

Integration/E2E not run (Docker/Playwright environments).

## Build results (P0-WP02)

| Command | Result |
|---|---|
| `dotnet restore HealthCare.sln` | Exit 0 (NU1903 warning on DbMigrate Newtonsoft.Json) |
| Non-MAUI Release builds | Exit 0 |
| `dotnet build HealthCare.sln -c Release` | Exit 1 — `XA5300` Android SDK directory not found |

## Risks

See [risks-and-issues.md](risks-and-issues.md). R-013 mitigated; R-016 open (empty remote).

## Next approved action

**P0-WP03 — Ant Design and UI Reuse Review** after P0-WP02 acceptance. Do not extract Platform or create POS.
