# POS-REACT-TEST-HARNESS-ORG-SESSION-REPAIR-01

**Status:** COMPLETE  
**Branch:** `feat/organization`  
**TASK:** POS-REACT-TEST-HARNESS-ORG-SESSION-REPAIR-01
**START_SHA:** `23ee9143893ed39306d071f0b60b43fd5a1500a3`

## Baseline full suite (at START_SHA)

| Metric | Value |
|--------|-------|
| BASELINE_FULL_SUITE | `npx vitest run` |
| BASELINE_TOTAL | 1256 |
| BASELINE_PASS | 1182 |
| BASELINE_FAIL | 74 |

### Baseline failure classification (pre-repair)

| Bucket | Approx |
|--------|--------|
| PLATFORM (empty `text()` / missing `text()` on fetch mocks) | ~19 files |
| PERSONAL (same session/`text()` defect + one i18n middle-dot) | ~5–6 files |
| SESSION_SHARED (sign-in/out/remote-logout empty `text()`) | ~3–4 files |
| CONNECTIVITY / QR / INVENTORY_I18N / WORKSPACE (Unicode corruption in `en.ts`) | ~5–6 files |
| ORGANIZATION_FEATURE_REGRESSIONS | 0 |
| ORGANIZATION_HARNESS (sell-floor / shell) | 0 at this START_SHA (already repaired earlier on branch) |

## ROOT_CAUSE

`platform-http.ts` reads response bodies via `response.text()` then JSON-parses. Many Platform/Personal/session tests mocked `json: async () => body` with `text: async () => ""` (or omitted `text` entirely). Bootstrap and clients therefore saw empty payloads → missing `accountClass` / parse failures. Production gates were correct; **fixtures were wrong**.

Secondary: `en.ts` had encoding corruption (`…`/`—`/`·` → `?`) on keys asserted by connectivity, QR, inventory detail, workspace grant, and people summary tests.

## SESSION_MODEL / ACCOUNT_CLASS_MODEL / ORGANIZATION_TEST_MODEL

| Model | Source of truth |
|-------|-----------------|
| ACCOUNT_CLASS_MODEL | Server `accountClass` on `/auth/me` → `sessionAccountClass()`; never inferred from email |
| ORGANIZATION_SESSION_MODEL | `accountClass=Organization`, membership, active org/workspace, branch as required by route |
| PERSONAL_SESSION_MODEL | `accountClass=Personal`, `homeOrganizationId=null` |
| SELL_READY | Explicit opt-in: device + register + open shift via `createOrganizationSellReadyFetch` / `renderOrganizationAt({ sellReady: true })` |
| ORGANIZATION_ROUTE_GUARD | `RequireOrganizationSession` / `RequireAccountClass(allow=["Organization"])` — **unchanged** |

## CANONICAL_TEST_HELPER

| Helper | Location |
|--------|----------|
| `jsonResponse(status, body)` | `src/test/session-context.ts` — consistent `.text()` + `.json()` |
| `createOrganizationSessionSnapshot` / `createOrganizationPlatformFetch` / `createOrganizationSellReadyFetch` / `seedOrganizationSellReadyLocalState` | same |
| `createPersonalSessionSnapshot` / `createPersonalPlatformFetch` | same |
| `renderOrganizationAt` / `renderPersonalAt` | `src/test/render.tsx` |

Do **not** invent a second harness. Extend these helpers.

## TEST_STATE_RESET_MODEL

`src/test/setup.ts` `afterEach`: RTL `cleanup`, `vi.unstubAllGlobals()`, `localStorage` + `sessionStorage` clear, theme/lang reset.

## SELL_FLOOR_HARNESS_BEFORE / AFTER

| | |
|--|--|
| BEFORE (historical) | Stale mocks with empty `text()` dropped Organization `accountClass`; Sell correctly denied |
| AFTER (this START_SHA + package) | Sell-floor already on `createOrganizationSellReadyFetch`; suite green; denial tests retained |

## Fixes applied in this package

| Area | Fix |
|------|-----|
| Platform client tests | Rewrite Response stubs to `jsonResponse` |
| Session sign-in/out / remote-logout / reconnect | Same |
| Personal shell / switch / guide / people-lifecycle | Same; people-lifecycle uses shared `jsonResponse` |
| Sign-in antiforgery | Antiforgery + login + `/me` stubs use `jsonResponse` |
| `en.ts` (asserted keys only) | Restore `…` / `—` / `·` on reconnecting, stock adjustment, expiration warning, QR purpose, workspace preparing, people.summary |
| PRODUCTION | No auth/guard/behavior change |

## PRODUCTION_BEHAVIOR_CHANGE / GUARDS

| Check | Result |
|-------|--------|
| PRODUCTION_BEHAVIOR_CHANGE | NO |
| PRODUCTION_GUARDS_WEAKENED | NO |
| AUTH_BYPASS_ADDED | NO |
| ACCOUNT_CLASS_BYPASS_ADDED | NO |

## TARGETED_TESTS

- Platform clients, logout-session, credentials, governance-step-up, org customer links
- sign-out, sign-in-antiforgery, remote-logout-retry
- personal-shell-home, personal-switch-to-business, PersonalGuidePage, people-lifecycle
- sell-floor, sell-floor-remount-sync, sell-readiness, account-shell, SessionGuards.account-class
- workspace-grant-hint, connectivity-ux, QR, InventoryDetailPage(+cost), checkout-personal-customer-picker

All PASS.

## Final full suite

| Metric | Value |
|--------|-------|
| FINAL_FULL_SUITE | `npx vitest run` |
| FINAL_TOTAL | 1256 |
| FINAL_PASS | 1256 |
| FINAL_FAIL | 0 |
| FAILURE_DELTA | −74 |

| Bucket | Count |
|--------|-------|
| ORGANIZATION_HARNESS_FAILURES | 0 |
| ORGANIZATION_REAL_FAILURES | 0 |
| SESSION_SHARED_FAILURES | 0 |
| PERSONAL_FAILURES | 0 |
| PLATFORM_FAILURES | 0 |
| CONNECTIVITY_FAILURES | 0 |
| QR_FAILURES | 0 |
| INVENTORY_I18N_FAILURES | 0 |
| WORKSPACE_FAILURES | 0 |
| OTHER_FAILURES | 0 |

| Anti-cheat | |
|------------|--|
| NEW_TEST_SKIPS | 0 |
| NEW_TEST_ONLY | 0 |
| TEST_EXCLUSIONS_ADDED | 0 |

## Validation

| Check | Result |
|-------|--------|
| TYPECHECK | PASS |
| LINT | PASS (0 errors; 35 pre-existing warnings) |
| BUILD | PASS |
| MIGRATION | N/A |

## Explicit non-goals

No profitability, inventory business features, Stock Count, Transfer, discount/reporting, CustomerOrder COGS, FIFO, supplier payable, permission redesign, Card/GCash, B2B checkout, offline Org mutations, GL. Organization gaps audit **not** refreshed in this package.

## NEXT

`POS-INVENTORY-PERMISSION-I18N-POLISH-01`
