# POS-REACT-TEST-HARNESS-ORG-SESSION-REPAIR-01

**Status:** COMPLETE  
**Branch:** `feat/organization`  
**START_SHA:** `e75f0c744e73c2ce9a6780a48ea613ce341fdebb`

## Baseline full suite

| Metric | Value |
|--------|-------|
| BASELINE_FULL_SUITE | `npm test` (vitest run) |
| BASELINE_TOTAL | 1210 |
| BASELINE_PASS | 1122 |
| BASELINE_FAIL | 88 |
| BASELINE_ORG_SESSION_FAILURES | 13 (sell-floor 9 + account-shell 4) |
| BASELINE_ORG_REAL_FAILURES | 0 |
| BASELINE_ORG_COPY_I18N | 8 (workspace-grant 2, connectivity 1, inventory detail 2, QR purpose copy 3) |
| BASELINE_UNRELATED_FAILURES | 67 (Personal route UI + Platform HTTP client mocks + session sign-in/out) |

## Production contracts (unchanged)

| Model | Source of truth |
|-------|-----------------|
| ACCOUNT_CLASS_MODEL | Server `accountClass` on `/auth/me` → `sessionAccountClass()`; never inferred from email |
| ORGANIZATION_SESSION_MODEL | `accountClass=Organization`, `homeOrganizationId`, optional `organizationContextLocked` |
| PERSONAL_SESSION_MODEL | `accountClass=Personal`, `homeOrganizationId=null` |
| WORKSPACE_BINDING_MODEL | `boundWorkspace.{organizationId,branchId,branchName,experience}` via WorkspaceProvider |
| BRANCH_BINDING_MODEL | `RequireBranchBound` / operational `X-Pos-Branch-Id`; report scope uses query `branchId` only |
| ORGANIZATION_ROUTE_GUARD | `RequireOrganizationSession` → `RequireAccountClass(allow=["Organization"])` |
| PERSONAL_ROUTE_GUARD | `RequirePersonalSession` |
| SELL_ROUTE_GUARD | Organization session + workspace/branch + Sell readiness (device/shift/commercial) |

**PRODUCTION_GUARD_WEAKENED=NO**

## Harness root cause

`HARNESS_ROOT_CAUSE=` Platform HTTP (`platform-http.ts`) reads JSON via `response.text()`. Many Organization route tests mocked `json()` with a full session body but `text: async () => ""`. Bootstrap therefore received an empty `/auth/me` payload → missing `accountClass` → `RequireAccountClass` correctly denied Organization Sell / shell routes.

Secondary: Sell Floor landscape cart / `sell-pay` only mounts at `min-width: 900px`; tests needed an explicit desktop (or mobile) `matchMedia` stub matching the scenario.

## Canonical helpers

| Helper | Location |
|--------|----------|
| CURRENT_ORG_TEST_HELPER / CANONICAL_ORG_TEST_CONTEXT | `src/test/session-context.ts` → `createOrganizationSessionSnapshot`, `createOrganizationPlatformFetch`, `createOrganizationSellReadyFetch`, `seedOrganizationSellReadyLocalState` |
| CURRENT_PERSONAL_TEST_HELPER / CANONICAL_PERSONAL_TEST_CONTEXT | same file → `createPersonalSessionSnapshot`, `createPersonalPlatformFetch` |
| CURRENT_WORKSPACE_TEST_HELPER | `createOrganizationBoundWorkspace` + existing WorkspaceProvider mocks in feature tests |
| Router helpers | `src/test/render.tsx` → `renderOrganizationAt`, `renderPersonalAt`; `jsonResponse` re-exported (text+json consistent) |

## Fixes applied

| Area | Fix |
|------|-----|
| SELL_FLOOR_ROOT_CAUSE | Empty `text()` dropped Organization `accountClass`; desktop viewport not stubbed |
| SELL_FLOOR_FIX | Use `createOrganizationSellReadyFetch` + `jsonResponse`; stub viewport; Personal→Sell fail-closed test |
| account-shell | Same Organization sell-ready fetch factory |
| SessionGuards | Added Organization allow + Personal deny regressions |
| workspace-grant-hint | Align pending-probe wait with `workspace-grant-loading`; pin mock |
| I18N_FIXES | SMALL_ISOLATED en.ts: ellipsis / em-dash / middle-dot on keys asserted by org-adjacent tests |
| QR_FAILURE_POLICY | Fixed shared en copy corruption only; behavior unchanged |

## Isolation

| Check | Result |
|-------|--------|
| GLOBAL_STATE_LEAK_FOUND | NO (suite vs alone matched for sell-floor once mock fixed) |
| MSW_HANDLER_LEAK_FOUND | NO (no MSW; vitest `fetch` stubs) |
| STORAGE_LEAK_FOUND | Mitigated: `setup.ts` clears `localStorage` + `sessionStorage` after each test |

## Final full suite

| Metric | Value |
|--------|-------|
| FINAL_FULL_SUITE | `npm test` |
| FINAL_TOTAL | 1214 |
| FINAL_PASS | 1148 |
| FINAL_FAIL | 66 |
| FAILURES_REMOVED | 22 |
| FAILURES_REMAINING | 66 |
| REMAINING_ORGANIZATION_FAILURES | 0 |
| REMAINING_PERSONAL_FAILURES | 32 |
| REMAINING_PLATFORM_FAILURES | 27 |
| REMAINING_GLOBAL_SESSION_FAILURES | 7 |
| REMAINING_QR_FAILURES | 0 |
| REMAINING_I18N_FAILURES | 0 |
| REMAINING_OTHER_FAILURES | 0 |
| REMAINING_UNKNOWN_FAILURES | 0 |

Remaining Personal/Platform/session failures still largely use hand-rolled empty `text()` mocks (same defect class). Deferred to avoid an unsafe bulk rewrite; use `jsonResponse` / `createPersonalPlatformFetch` when those packages are repaired.

## Security regressions

| Check | Result |
|-------|--------|
| PERSONAL_TO_ORG_FAILS_CLOSED | PASS (`SellFloorPage account-class gate`, `SessionGuards`) |
| VALID_ORG_SESSION_CAN_ENTER_SELL | PASS |
| WRONG_WORKSPACE_FAILS_CLOSED | PASS (existing branch/workspace guards retained; no production change) |

## Validation evidence

- ORG_HARNESS_TARGETED_TESTS: sell-floor, account-shell, SessionGuards.account-class, workspace-grant-hint — PASS
- SELL_FLOOR_TESTS: 11/11 PASS
- ORG_ROUTER_TESTS: SessionGuards + RequireBranchBound — PASS
- RECENT_FEATURE_REGRESSION_TESTS: Stock Count, Transfer, report-branch-scope, inventory detail — PASS
- TYPECHECK: PASS
- LINT: PASS (0 errors; pre-existing warnings only)
- BUILD: PASS
- NEW_TEST_SKIPS=0 / NEW_TEST_ONLY=0 / CONFLICT_MARKERS=0

## Explicit non-goals

No production account-class / Sell / device / shift / branch / online-only policy changes. No Expenses CRUD, B2B identity, payment gateway, or broad Personal/Platform suite rewrite.

## Next

REASSESS GAP ROADMAP AFTER HARNESS REPAIR; LIKELY POS-EXPENSES-REACT-CRUD-01 OR POS-B2B-IDENTITY-DISPLAY-01 (or Personal/Platform empty-`text()` mock cleanup package).
