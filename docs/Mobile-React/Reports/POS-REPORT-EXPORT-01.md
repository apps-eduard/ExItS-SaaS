# POS-REPORT-EXPORT-01

**Status:** COMPLETE  
**Branch:** `feat/organization`  
**TASK:** POS-REPORT-EXPORT-01  
**START_SHA:** `5c696019a9c0abfb4c9095668677f0e53f4689a9`  
**FEATURE_SHA:** `753f5f813a4465812b015e15203a4cb2677fd81b`  

## EXPORT_ARCHITECTURE

Client-side CSV generation for Organization report pages.

1. User opens an operational or classic report (existing filters / branch scope / date range).
2. Entitled users see **Export CSV** (`canExportData` → `store-export`).
3. Export reuses the **same report query functions and parameters** as the on-screen report.
4. Rows are serialized via shared `@/lib/csv` helpers and downloaded as a UTF-8 CSV (with BOM).

No separate export calculation service. No PDF/XLSX. No backend endpoints added.

## EXPORT_SOURCE_OF_TRUTH

**Same report APIs + same filters as the UI.**

- Operational: `buildOperationalReportExport` → existing `get*Report` clients with `workspace`, `range`, `reportBranchId`.
- Classic: `buildClassicReportExport` → existing classic `getSalesReport` / `getUtangReport` / `getInventoryReport` / `getExpensesReport`.
- Product profitability prefers already-loaded query rows when present (full filtered result, not the screen slice).

## SUPPORTED_REPORTS

| Report | Classification | Export shape |
|--------|----------------|--------------|
| Sales by product | EXPORT_NOW | Full `rows` (not UI 25-row slice) |
| Product profitability | EXPORT_NOW | Typed product columns; null COGS/profit blank |
| Sales by payment | EXPORT_NOW | Payment method rows |
| Inventory status | EXPORT_NOW | Optional `rows` or Metric/Value summary |
| Inventory movements | EXPORT_NOW | `byType` table |
| Purchasing summary | EXPORT_NOW | Metric/Value |
| Purchase outstanding | EXPORT_NOW | Optional `rows` or Metric/Value |
| Supplier purchasing | EXPORT_NOW | Optional `rows` |
| Supplier payables | EXPORT_NOW | As-of payables rows (Supplier/Source/Receipt Date/amounts/Due Date/Status); no internal IDs |
| Expense summary | EXPORT_NOW | Optional `byCategory` or Metric/Value |
| Utang by product | EXPORT_NOW | Optional `byProduct` or Metric/Value |
| Profitability summary | EXPORT_NOW | Metric/Value (includes waste/stock-use aggregates already on report) |
| Sales summary / overview / returns / shifts / cash-variance / stock-count-variance | EXPORT_NOW | Metric/Value (honest “what you see”) |
| Classic sales / utang / inventory / expenses | EXPORT_NOW | Nested lists when present; else Metric/Value |

## DEFERRED_REPORTS

| Report | Classification | Reason |
|--------|----------------|--------|
| Sales by cashier | DEFERRED | No report kind / API |
| Dedicated Waste/Loss report | DEFERRED | Only profitability aggregates today |
| Dedicated Stock Use report | DEFERRED | Only profitability aggregates today |
| Dedicated Production report | DEFERRED | No dedicated report surface |
| Dedicated Discounts report | DEFERRED | Discount fields already on sales exports |
| Management dashboard | NOT_TABULAR | Cards, not a tabular dataset |
| Customer-facing surfaces | NOT_TABULAR | No export wiring; entitlement unused there |

## EXPORT_SCOPE_MODEL

`EXPORT_SCOPE=FULL_FILTERED_RESULT`

Export re-queries (or reuses loaded full DTO rows) with the active filters. Does **not** export only the first 25 sales-by-product lines shown on screen.

Empty data sets: no download; UI shows “No data to export”.

## BRANCH_SCOPE_POLICY

Uses existing `ReportScopeControls` / `resolveReportBranchIdQuery` semantics:

- **BRANCH** reports: CSV uses selected branch or all-branches when explicitly selected.
- **ORGANIZATION_ONLY** reports: CSV is organization-wide (no fake branch filter).

Scope label metadata: branch name or `all-branches`.

## DATE_RANGE_POLICY

Uses the applied report period (`fromDate` / `toDate`). As-of reports (`inventory-status`, `purchase-outstanding`) use the DTO `asOfDate` in metadata/filename.

## CSV_FORMAT

- Comma delimiter
- RFC-style quoting (`"` doubled inside fields)
- `\r\n` line endings
- Machine-friendly numbers (no `₱` / thousands separators)
- English column headers for this package

## CSV_UTF8_POLICY

**UTF-8 with BOM** (`\uFEFF`) so Excel on common PH Windows locales correctly displays Filipino / Cebuano / Hiligaynon / Ilocano text.

## CSV_INJECTION_POLICY

Textual cells beginning with `=`, `+`, `-`, or `@` are prefixed with `'` before quoting.

Numeric cells (including negative amounts) are emitted as raw numbers and are **not** string-prefixed.

## NULL_VALUE_POLICY

`null` / `undefined` → empty cell.

Product profitability: unknown `totalCogs` / `grossProfit` / `grossMarginPercent` stay blank (never coerced to `0`).

## FILENAME_MODEL

`{report-name}_{scope}_{from}_{to}.csv` via `buildReportCsvFilename` (filesystem-safe slug parts).

Example: `product-profitability_main-branch_2026-08-01_2026-08-30.csv`

## COLUMN_POLICY

Human-readable labels. Internal IDs (`productId`, `organizationId`, `branchId`) omitted by default. Unknown record-row keys are humanized; `*Id` keys skipped.

## PERMISSION_MODEL

1. User must already be allowed to view the report page (existing access gates).
2. Export button requires `canExportData(sessionGrant)` → feature code `store-export`.

Users without `store-export` do not see Export CSV. Missing feature codes does not silently grant export to Owners.

## CAN_EXPORT_DATA_MODEL

`canExportData` remains strict equality on `store-export` (`grantHasFeatureCode(...) === true`). Growth/Pro plans grant `ExportEnabled` / `store-export` via the commercial catalog; Starter does not.

## CLIENT_VS_SERVER_MODEL

**CLIENT-SIDE** CSV from existing bounded report DTOs.

## BACKEND_CHANGE_REQUIRED

**NO**

## MIGRATION

**N/A**

## EXPORT_QUERY_MODEL

Same existing report GET endpoints / client functions; one additional fetch on Export when data must be refreshed (product profitability may reuse loaded rows).

## EXPORT_ROW_LIMIT_POLICY

Bounded by existing report API payloads (no unbounded browser paging loops). Sales-by-product exports full returned `rows`, not the UI slice of 25.

## N_PLUS_ONE

**NO** — export uses batch report DTOs only.

## RESPONSIVE_UX

Compact outline **Export CSV** beside refresh / under filters. `min-h-11`, wraps with flex. Disabled while report fetch or export in progress. Status/error via `role="status"` / `role="alert"`.

## I18N_KEYS_ADDED

- `reports.export.csv`
- `reports.export.preparing`
- `reports.export.noData`
- `reports.export.failed`

Locales: `en`, `fil-PH`, `ceb-PH`, `hil-PH`, `ilo-PH`.

## TARGETED_TESTS

- `src/lib/csv.test.ts` — escaping, Unicode, nulls, negatives, injection, BOM, filenames, download once
- `src/features/reports/report-csv-export.test.ts` — null COGS, branch/all scope, dates, `canExportData`
- `src/features/reports/report-csv-export-ui.test.tsx` — button visibility, download, failure, no customer-ordering wiring

## VALIDATION

| Check | Result |
|-------|--------|
| REACT_FULL_TEST_COUNT | 1305 |
| REACT_FULL_PASS | 1305 |
| REACT_FULL_FAIL | 0 |
| TYPECHECK | PASS |
| LINT | PASS (0 errors; pre-existing warnings only) |
| BUILD | PASS |

## NEXT

`POS-INVENTORY-OPS-POSTGRES-INTEGRATION-TESTS-01`
