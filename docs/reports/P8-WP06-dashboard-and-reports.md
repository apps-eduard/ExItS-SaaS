# P8-WP06 — Dashboard and Reports

Phase marker: `P8-WP06-dashboard-and-reports`

## Status

**Complete with documented risks.** Organization-isolated operational dashboard and read-only reports derived from immutable Basic Store records (sales, Utang, inventory, expenses). **No** P&L, valuation, accounting journals, forecasting, offline authoritative caches, or CSV/PDF/Excel file generation. P8-WP07 was not started.

Feature commit: `a0028f36a0d8e2ea76c3101b2b65ba82bfd4fd02`

## Delivered capability

| Area | Delivered |
|---|---|
| Dashboard | Period KPIs, payment breakdown, sales/expenses-by-day, prior-period absolute/% comparison (“Not available” when prior is zero) |
| Sales reports | Completed vs voided; by payment/product/category; top products; Utang sales summary — line snapshots |
| Utang reports | Reuses FIFO aging + outstanding (`CreditFifoAging`, active credit/repayment sums); period credits/repayments; statement navigation |
| Inventory reports | Tracked on-hand, low/out-of-stock, movements by type in period — no valuation |
| Expense reports | Active/voided totals, by category/payment/day, detail list |
| Features | `store-dashboard-view`, `store-reports-view` (continuity/read; Suspended deny). No `store-reports-export` (file export deferred) |
| API | `GET /api/v1/pos/dashboard`, `/api/v1/pos/reports/{sales,utang,inventory,expenses}` (+ by-product/by-category) |
| MAUI | `/dashboard`, `/reports`, `/reports/sales|utang|inventory|expenses` |
| Persistence | Query projections only — **no** migration / report tables |

## Formulas (server-authoritative)

- **Date range:** inclusive calendar `fromDate`–`toDate`; default = UTC today when omitted; max **366** inclusive days (`PosReportOptions.MaxInclusiveDaySpan`); `pos.report.range_too_large` / `pos.report.invalid_date_range`.
- **Sales day membership:** UTC calendar date of `RecordedAtUtc`.
- **Expense day membership:** `ExpenseDate`.
- **Completed sales total/count:** sum/count of sales with `Status=Completed` (voided excluded from active totals; reported separately).
- **Payment breakdown:** completed sales grouped by payment method code.
- **Utang outstanding/overdue:** same as overdue APIs — active credits − active repayments; overdue via `CreditFifoAging.AgeCredits`.
- **Expense active total:** sum of `Recorded` amounts; voided separate; net excludes voided.
- **Inventory on-hand/low-stock:** existing account projection + `IsLowStock`; out-of-stock = tracked and on-hand ≤ 0.
- **Comparison %:** `(current − prior) / prior × 100` when prior ≠ 0; otherwise absolute only and percentage **Not available**.
- **No profit:** sales and expenses are never subtracted into income/P&L.

## Export

Export-ready DTOs are the report response contracts. CSV/PDF/Excel/print/share file generation is **deferred** (no approved CSV mechanism existed). UI states this clearly.

## Commercial matrix

| State | Dashboard / Reports |
|---|---:|
| Trialing / Active / GracePeriod / PastDue / Cancelled / Expired | Grant-controlled |
| Suspended / missing / stale / unknown | Deny |

## Explicit exclusions

Profit/margin/COGS/P&L; accounting journals/balance sheet/cash-flow/tax; supplier/purchasing/payroll/reimbursement reports; inventory valuation; forecasting/AI; scheduled/email reports; PDF/Excel generation; offline authoritative report caches; custom builders; Phase 9+.

## Tests and Android

| Suite | Passed | Failed | Skipped |
|---:|---:|---:|---:|
| Full `ExItS.slnx` Release | **851** | **0** | **0** |

Baseline 830 preserved and exceeded.

Android Release APK build succeeded (path under Maui `bin/Release/net10.0-android/`). Interactive device validation **not** claimed (`adb` unavailable) — **R-109** remains open.

## Performance notes

Validated against MVP-scale integration data (single-org sales/expense fixtures). Reports load bounded by max 366-day span and project in memory after organization/date-filtered repository queries. Known limit: very large orgs with dense history approaching the max span may need future index work — none required for this WP.

## Risks

| ID | Notes |
|---|---|
| R-109 | No interactive Android dashboard/report validation |
| Dev headers | Org/commercial/actor headers Development/Testing-only |
| Category labels | Sales-by-category uses current catalog category assignment for labels; line money/qty remain immutable snapshots |
| Export deferred | No file generation |

## HealthCare freeze

Root `HealthCare/` remains ignored, untracked, outside `ExItS.slnx`.

## Documentation and Git

| Field | Value |
|---|---|
| Feature commit | `a0028f36a0d8e2ea76c3101b2b65ba82bfd4fd02` |
| Docs hash-record commit | *(recorded in follow-up docs commit)* |
| Final working tree | clean after push |

## Exact next work package

**P8-WP07 — Basic Store Closeout** (do not begin until explicitly authorized).
