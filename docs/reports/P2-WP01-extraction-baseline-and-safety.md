# P2-WP01 — Extraction Baseline Tag and Safety Checks

## 1. Assignment

| Field | Value |
|---|---|
| Phase | Phase 2 — Platform Extraction and legacy product Reconnection |
| Work package | P2-WP01 — Extraction Baseline Tag and Safety Checks |
| Status | Ready for Review |
| Branch | `main` |
| Date | 2026-07-29 |

## 2. Summary

Created the minimal buildable ExItS root solution (`ExItS.slnx`) with Platform Domain/Application/Infrastructure/Api projects, unit and architecture tests, central build/package conventions, SDK pin (`global.json` 10.0.302), portfolio independence verification safety tests, and annotated local tag `phase-1-approved` → `01ab65b`. API serves `/` and `/health` without a database. No business capabilities, no legacy product changes, no POS/Admin/shared projects.

## 3. Acceptance criteria and evidence

| Criterion | Status | Evidence |
|---|---|---|
| Phase 1 recorded Complete with risks | Met | portfolio-progress; tag on `01ab65b` |
| Solution, global.json, Directory.*.props | Met | repo root |
| Domain/Application/Infrastructure/Api + tests | Met | `src/Platform/*`, `tests/*` |
| Dependency direction + architecture tests | Met | 9 architecture tests passed |
| portfolio independence verification / not in solution / not referenced | Met | `git ls-files` empty; safety tests; `dotnet sln list` |
| No product/shared/Blazor/POS/DB/auth | Met | structure + tests |
| API starts; `/` and `/health` OK | Met | http://127.0.0.1:5288 |
| Restore/build/test Release | Met | 0 warnings/errors; 11 passed |
| Baseline tag | Met | `phase-1-approved` → `01ab65b` (local, not pushed) |
| Docs + commit | Met | This report / hash section |

## 4. Files changed

See completion §9 / git. Key adds: `ExItS.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, Platform projects, test projects, docs updates.

## 5. Architecture/reuse impact

Establishes Stage 1 foundation only. No legacy product import or pattern extraction code.

## 6. Database and migration impact

None. No EF/Npgsql packages or migrations.

## 7. Tests and validation

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| ExItS.Platform.UnitTests | 2 | 0 | 0 |
| ExItS.ArchitectureTests | 9 | 0 | 0 |
| **Total** | **11** | **0** | **0** |

| Command | Exit |
|---|---:|
| `dotnet restore ExItS.slnx` | 0 |
| `dotnet build ExItS.slnx -c Release` | 0 (0 warnings) |
| `dotnet test ExItS.slnx -c Release --no-build` | 0 |

Runtime: `GET /` → 200 `{"service":"ExItS.Platform.Api","status":"ok","phase":"P2-WP01-foundation"}`; `GET /health` → 200 `Healthy`; no DB; process stopped cleanly. Port **5288** (avoids legacy product 5080/7080/5018/7021/5020 and a busy 5188).

## 8. Security and tenant review

No auth/secrets/DB. Root response has no env/machine/secrets. OpenAPI package removed (template had NU1903).

## 9. UI, localization and theme review

No UI projects created. ADR-010 unchanged.

## 10. Documentation updated

portfolio-progress, Phase 2, readiness checklist, development-environment, repository-boundaries, architecture, risks, index, reports README, FILE-MANIFEST, README.

## 11. Risks, blockers, unknowns and deferred items

| Item | Notes |
|---|---|
| R-016 | Root `origin/main` still gone; tag/commit not pushed |
| R-010/R-017/R-026 | Freeze verified; safety tests added |
| Port 5188 conflict | Switched API to **5288** |
| TreatWarningsAsErrors | Left false (practical baseline) |
| NetArchTest.Rules 1.3.2 | Central package; lightweight |

No blockers for P2-WP01 acceptance.

## 12. Git evidence

| Field | Value |
|---|---|
| Commit hash | `4827b7f3ff2cba161df749dd47507f16171ff8da` |
| Commit message | `chore(platform): establish root solution foundation` |
| Branch | `main` |
| Upstream | `origin/main` gone; not pushed |
| Tag | `phase-1-approved` → `01ab65b511721d5dd2173188bc6d962a5feea803` (local only) |
| Final working tree | Clean after hash-record |

## 13. Progress update

Phase 1 Complete with documented risks. P2-WP01 Ready for Review. Next: **P2-WP02 — Shared Identity and Organization Boundary** after acceptance — do not begin.

## 14. Next approved work package

**P2-WP02 — Shared Identity and Organization Boundary** — do not begin until authorized.
