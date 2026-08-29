# POS-EXPENSES-REACT-CRUD-01

**Status:** COMPLETE  
**Branch:** `feat/organization`  
**START_SHA:** `e704a585acc7edfc8053c3f6dc9b09405c1f1654`  
**FINAL_SHA:** `7d3038d8302fdb94353f0bfb40c357b22774000c`  
**REMOTE_SHA:** `7d3038d8302fdb94353f0bfb40c357b22774000c`  
**TASK:** POS-EXPENSES-REACT-CRUD-01

## Audit (pre-implementation)

| Field | Value |
|-------|--------|
| EXISTING_EXPENSE_BACKEND | YES — Domain `Expense` / `ExpenseCategory`, Application use cases, `ExpenseEndpoints`, persistence migration `20260730201050_AddPosExpenses`, integration + unit tests |
| EXISTING_EXPENSE_REACT_UI | NO (before this package) — only classic/operational *reports* for expenses |
| EXISTING_EXPENSE_REACT_CLIENT | NO (before this package) |
| EXISTING_EXPENSE_CATEGORY_UI | NO (before this package; MAUI had categories) |
| EXPENSE_SCOPE_MODEL | OrganizationId only — **no BranchId** |
| EXPENSE_SCOPE | ORGANIZATION |
| EXPENSE_CORRECTION_MODEL | Record → Void (+ optional client-side replacement prefills) — no Edit / Unvoid |
| EXPENSE_PAYMENT_MODEL | Cash \| ManualGCash only |
| EXPENSE_STATUS_MODEL | Recorded \| Voided |
| EXPENSE_CATEGORY_STATUS_MODEL | Active \| Inactive |
| EXPENSE_PERMISSION_MODEL | ViewExpenses (list/detail/summary/categories read); ManageExpenses (record/void/category mutation) |
| EXPENSE_DEVICE_POLICY | Device authorization required for **Record** and **Void** only |
| CATEGORY_DEVICE_POLICY | Permission only (ManageExpenses) — **no** POS device authorization |
| EXPENSE_OFFLINE_MODEL | ONLINE_ONLY (`PosExpenseOptions` / domain docs; no PWA outbox) |
| CASH_EXPENSE_SHIFT_INTEGRATION | NONE — CashierShift movements are not expense/accounting |

## Delivered React capability

| Area | Implementation |
|------|----------------|
| Client | `src/api/pos/pos-expense-client.ts` — typed Zod client; org-scoped `expenseWorkspaceScope` (no branch header) |
| Idempotency | `OFFLINE_OPERATION_TYPES.ExpenseCreate` + client-generated expenseId |
| Routes | `/expenses`, `/expenses/new`, `/expenses/categories`, `/expenses/:expenseId` |
| Navigation | More → Expenses (`org-more-expenses`) when `canViewExpenses` |
| Guards | `RequireViewExpenses` / `RequireManageExpenses` |
| Capabilities | `canViewExpenses` / `canManageExpenses` independent of ViewReports |
| Pages | List+summary, Record, Detail+void, Categories CRUD |
| Scope UX | Organization-wide banner; does not claim current-branch ownership |
| i18n | en + fil-PH + ceb-PH + ilo-PH + hil-PH |

## Explicit non-goals (honored)

- No `Expense.BranchId` / migration  
- No Edit expense / Unvoid  
- No Card/Bank/Gateway GCash / FakePaymentGateway  
- No offline expense queue  
- No operating profit / GL / purchase→expense automation  

## Validation

| Check | Result |
|-------|--------|
| BACKEND_CHANGE_REQUIRED | NO |
| MIGRATION | N/A |
| TYPECHECK | PASS |
| LINT | PASS (0 errors; pre-existing warnings only) |
| BUILD | PASS |
| BACKEND_UNIT (Expense filter) | PASS — 12 passed |
| REACT_TARGETED | PASS — expense pages + client + labels + capabilities + message-parity |
| REACT_FULL_TOTAL | 1233 |
| REACT_FULL_PASS | 1167 |
| REACT_FULL_FAIL | 66 |
| EXPENSE_RELATED_FAILURES | 0 |
| OTHER_ORGANIZATION_FAILURES | 0 |
| PERSONAL_FAILURES | ~32 (pre-existing empty-`text()` / Personal UI mocks) |
| PLATFORM_FAILURES | ~27 (pre-existing Platform HTTP client mocks) |
| GLOBAL_SESSION_FAILURES | ~7 (sign-in/out / logout retry) |
| NEW_TEST_SKIPS | 0 |
| NEW_TEST_ONLY | 0 |
| EXPENSE_REACT_N_PLUS_ONE | PASS — server list pagination + server summary; no client aggregation of all history |
| CONFLICT_MARKERS | 0 |
| POS_API_HEALTH | N/A (local E2E not run this package; API contract unchanged) |

## Backend tests (this package)

| Suite | Result |
|-------|--------|
| Unit `FullyQualifiedName~Expense` | 12 passed |
| Integration `PosExpenseApi` | 6 passed |
| Architecture `PosExpenses` | 5 passed |

## Git

| Field | Value |
|-------|--------|
| FINAL_SHA | `7d3038d8302fdb94353f0bfb40c357b22774000c` |
| REMOTE_SHA | `7d3038d8302fdb94353f0bfb40c357b22774000c` |
| PUSH | PASS |
| WORKTREE_CLEAN | YES |

**NEXT:** POS-B2B-IDENTITY-DISPLAY-01
