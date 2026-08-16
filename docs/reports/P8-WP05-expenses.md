# P8-WP05 — Expenses

Phase marker: `P8-WP05-expenses`

## Status

**Complete with documented risks.** Organization-isolated store expense categories and immutable expense entries (Cash / ManualGCash), explicit void corrections, and derived period summaries. **No** AP, payroll, GL, tax OCR, offline expenses, or P&L. P8-WP06 was completed separately.

Feature commit: `ca956921fbfcfad8499f01acb9d9726fff2d81d4`

## Delivered capability

| Area | Delivered |
|---|---|
| Categories | Flat Active/Inactive; active normalized name unique per org; no hierarchy/hard delete |
| Expenses | Immutable Recorded entries; amount > 0 (≤2 dp AwayFromZero); required description; calendar ExpenseDate |
| Payments | Cash or ManualGCash (exactly one); optional GCash reference; manual confirmation only |
| Number | `EXP-YYYYMMDD-<sequence>` via `pos.expense_number_sequences` |
| Void | Recorded → Voided with reason + actor; no refund/reimbursement transaction |
| Summary | Derived totals by date range, category, payment method; voided excluded from net |
| Persistence | Migration `AddPosExpenses` (`20260730201050`) |
| API | `/api/v1/pos/expense-categories`, `/api/v1/pos/expenses*` + idempotent create (`expense.create`) |
| MAUI | `/expenses`, `/new`, `/{id}`, `/categories`, `/summary` |
| Features | `store-expenses-view` / `store-expenses-manage` |

## Business rules

- Inactive categories cannot be used for new expenses; historical entries retain category snapshot identity via FK.
- No default category seeding (not authorized in product docs).
- Corrections: void + create replacement expense; no direct edit/delete.
- Platform SaaS billing and customer Utang repayments are not store expenses.
- Expense number is not a tax invoice number.

## Summary calculations

For a selected date range (inclusive calendar dates on `ExpenseDate`):

- Total recorded amount / count
- Total voided amount / count
- Net active = sum of Recorded amounts only
- Breakdowns by category and payment method
- Latest expense date among matching rows

All values derived; no persisted summary totals.

## Commercial matrix

| State | View | Manage (create/void/categories) |
|---|---:|---:|
| Trialing / Active / GracePeriod | Grant | Grant |
| PastDue / Cancelled / Expired | Grant | Deny |
| Suspended / missing / stale / unknown | Deny | Deny |

## Explicit exclusions

AP/suppliers/POs/receiving, payroll, reimbursements/advances, recurring automation, budgets/approvals, GL/journal, tax/VAT rules, OCR/attachments, split payments, cards/gateways/QR/GCash verification, offline expenses, P&L reporting, POS operational roles.

## Online-only

No expense queue handlers, local projections, or offline create/void. `OfflineOperationTypes.ExpenseCreate` exists only for server idempotency headers.

## Tests and Android

| Suite | Passed | Failed | Skipped |
|---:|---:|---:|---:|
| Full `ExItS.slnx` Release (test projects) | **830** | **0** | **0** |

Baseline 805 preserved and exceeded.

Android Release APK:

`src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/bin/Release/net10.0-android/com.exits.pinoybusinesspos-Signed.apk`

Interactive device validation **not** claimed (`adb` unavailable) — **R-109** remains open.

## Risks

| ID | Notes |
|---|---|
| R-109 | No interactive Android expense validation |
| Dev headers | Org/commercial/actor headers Development/Testing-only |
| Manual GCash | No independent verification (by design) |
| Online-only | Offline expense policy deferred |

## Portfolio independence

Root a nested foreign product tree must remain absent/untracked and outside `ExItS.slnx`.

## Documentation and Git

| Field | Value |
|---|---|
| Feature commit | `ca956921fbfcfad8499f01acb9d9726fff2d81d4` |
| Docs hash-record commit | `abe37d40e7fd9d7c7468d564eac29e9e42576921` |
| Final working tree | clean after push |

## Exact next work package

**P8-WP06 — Dashboard and Reports** completed separately; next authorized WP is **P8-WP07 — Basic Store Closeout**.
