# POS-HOTFIX-02 — Runtime provenance trace + global copyable error diagnostics

## Package

| Field | Value |
|-------|-------|
| Branch | `feat/pos-react-client` |
| Starting HEAD | `c336d6c62e058419d32f1d197480525bddb0cdc9` |
| Final HEAD | `5cddf222a3c90f0a96988f5ad553df07284930d4` |
| Commit | `5cddf222a3c90f0a96988f5ad553df07284930d4` |

## Executive summary

The owner's live `application.auth.account_scope_denied` on `GET /api/v1/platform/antiforgery/token` was **not** caused by missing source in `AccountScopeGuardMiddleware` at `c336d6c`. The POS React dev server on `:5177` was proxying to a **Docker Platform API** built from **`ExItS-SaaS-PlatformWeb-local-access`** (`4cce7d9f`), which does **not** exempt the antiforgery bootstrap route for Organization sessions.

After stopping the foreign Docker app stack and starting **host** Local Validation from this worktree, antiforgery returns **200** for `kizy@gmail.com` once Local Validation HTTP cookie policy is applied.

## Runtime provenance (Phase 1)

### Before fix — owner-equivalent failure

| PORT | PID / owner | App | Worktree | Branch | HEAD | Expected? |
|------|-------------|-----|----------|--------|------|-----------|
| 8091 | Docker `exits-local-validation-platform-api` | platform-api | `ExItS-SaaS-PlatformWeb-local-access` | `feat/platform-admin-error-diagnostics` | `4cce7d9f…` | **NO** |
| 8092 | Docker `exits-local-validation-pos-api` | pos-api | same | same | same | **NO** |
| 5177 | node/vite ~34124 | POS React | `ExItS-SaaS-pos-react-client` | `feat/pos-react-client` | `c336d6c…` | YES |

Live reproduction on wrong `:8091`:

- Method/path: `GET /api/v1/platform/antiforgery/token`
- Status: **403**
- ErrorCode: `application.auth.account_scope_denied`
- Matches owner UI text exactly.

### After fix — host Local Validation from POS worktree

| PORT | PID | App | Worktree | Branch | HEAD |
|------|-----|-----|----------|--------|------|
| 8091 | host `ExItS.Platform.Api` | Platform API | `ExItS-SaaS-pos-react-client` | `feat/pos-react-client` | `c336d6c…` (+ hotfix commits) |

Live reproduction on corrected `:8091` (Organization session, `kizy@gmail.com`):

- Method/path: `GET /api/v1/platform/antiforgery/token`
- Status: **200**
- ErrorCode: _(none)_
- Token present in JSON body (value not recorded in this report).

## Required flags

```
OWNER_ERROR_REPRODUCED=YES
ACTUAL_8091_PID=Docker exits-local-validation-platform-api (before fix); host ExItS.Platform.Api PID after fix
ACTUAL_8091_WORKTREE=ExItS-SaaS-PlatformWeb-local-access (before fix)
ACTUAL_8091_BRANCH=feat/platform-admin-error-diagnostics (before fix)
ACTUAL_8091_SHA=4cce7d9fc25942e156eab5f9abdc6748504e5f7a (before fix)
WRONG_RUNTIME_CONFIRMED=YES
AGENT3_RUNTIME_CHANGE_RELATED=YES — Agent 3 Docker Local Validation stack (`exits-local-validation` compose from PlatformWeb-local-access) occupied :8091/:8092 while POS Vite proxied to it
CROSS_WORKTREE_PROCESS_CAUSE=YES — launcher previously stopped only dotnet processes whose command line contained the *current* worktree path; foreign-worktree Docker app services were not provenance-checked
ROOT_CAUSE=POS React (:5177) proxied to Docker Platform API on :8091 from PlatformWeb-local-access without Organization antiforgery token exemption; not pos-react-client c336d6c host runtime. Secondary: host Staging antiforgery cookies used SecurePolicy.Always on HTTP until LocalValidation SameAsRequest fix.
```

### After fix

```
PLATFORM_8091_EXPECTED_WORKTREE=C:\Users\speed\Desktop\ExItS-SaaS-pos-react-client
PLATFORM_8091_EXPECTED_SHA=<final commit on feat/pos-react-client>
ANTIFORGERY_TOKEN_ORG_SESSION_LIVE=PASS
WORKSPACE_MAIN_BRANCH_LIVE=BLOCKER — live script branch-context returned 400 after antiforgery PASS (separate bind/data follow-up)
WORKSPACE_SECOND_BRANCH_LIVE=BLOCKER — same
LOGOUT_LIVE=PASS
LOGIN_AFTER_LOGOUT_LIVE=PASS
GLOBAL_ERROR_HANDLER=PASS
COPY_ERROR_DETAILS=PASS
SECRET_REDACTION=PASS
```

## Agent 3 runtime audit (Phase 2)

- `ecc20df4`, `bfeea445`, `b6efcde0`, `4bb1d147` introduced/shared Docker Local Validation from PlatformWeb worktrees.
- POS branch already had compose + `Start-DockerLocalValidation.ps1`, but **did not** prevent a foreign worktree's Docker app profile from owning shared ports `8091`/`8092`.
- No blind cherry-pick; only minimum provenance + HTTP antiforgery cookie fixes ported.

## Delivered changes

### Local Validation launcher hardening

- `tools/LocalValidation.stack.ps1`: enumerate all git worktrees; resolve port owners (host + Docker compose working_dir); print provenance table; stop cross-worktree host apps; post-startup runtime summary + assert expected worktree owns ports.
- `tools/Start-LocalValidation.ps1` / `Start-DockerLocalValidation.ps1`: wired provenance table, cross-worktree stop, enriched port conflict reporting.

### Platform antiforgery (live host mode)

- `PlatformBrowserAntiforgeryExtensions.cs`: when `LocalValidation:Enabled=true`, antiforgery cookie uses `SameAsRequest` on HTTP Staging host mode (fixes 500 `platform.internal_error` after runtime correction).

### POS global copyable diagnostics

- `pos-error-report.ts`, `normalize-pos-error.ts`, `CopyErrorDetailsButton.tsx`, enhanced `ErrorState`, workspace `failureDiagnostic`, redaction for Cookie / Authorization / password / X-XSRF-TOKEN patterns.
- Friendly translated message first; **Copy error details** + expandable **Technical details**.

### Tests

- Vitest: operational error report, ErrorState, GlobalErrorBoundary, existing redaction tests updated.
- Integration: `Organization_cookie_session_can_bootstrap_antiforgery_token_without_scope_denial` (8/8 `ApiBrowserAntiforgeryTests` PASS).
- Architecture: Local Validation provenance helpers; AccountScopeGuard + LocalValidation antiforgery cookie guards.

## Test gates

| Gate | Result |
|------|--------|
| VITEST | **597/597 PASS** |
| TYPECHECK | **PASS** |
| LINT | **PASS** (warnings only) |
| BUILD | **PASS** |
| PLAYWRIGHT POS-LIVE-QR-01 | **7/7 PASS** |
| ApiBrowserAntiforgeryTests | **8/8 PASS** |
| LocalValidationPackagingArchitectureTests | **3/3 PASS** |

## Operator guidance

1. Run `.\tools\Start-LocalValidation.ps1` from **`ExItS-SaaS-pos-react-client`** (not PlatformWeb-local-access Docker apps profile alone).
2. Read the printed **runtime provenance** table; `:8091` must show this worktree + branch + SHA.
3. If ports are owned by another worktree Docker stack, stop it: `docker compose -p exits-local-validation stop platform-api pos-api …` or use the launcher (stops cross-worktree host + Docker app services before host mode).
4. On workspace errors, use **Copy error details** in POS — safe for Cursor chat (no cookies/tokens).

## Explicit exclusions

- Did not modify `AccountScopeGuardMiddleware` exemption logic (already correct at `c336d6c`).
- Did not merge `main`, start COM-INT-04/TAX/B05, or change QR formats.
- 15 PNG WIP screenshots remain unstaged.

## Exact next work package

Resolve live workspace branch bind `400` on branch-context (POS API `:8092` host startup + bind path validation) and re-run full owner browser workspace flow.
