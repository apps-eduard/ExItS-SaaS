# POS-SUPPLIER-PAYABLES-01

**Status:** COMPLETE  
**Branch:** `feat/organization`  
**TASK:** POS-SUPPLIER-PAYABLES-01  
**START_SHA:** `178100d07b2e248f561a491d0b41120483a9844a`
**FEATURE_SHA:** `c045ea25242a108390412bfba7fce506da94ec47`
**FINAL_SHA:** `8366a8ecabe73d39dd21f29850957c84b3e74ab8`
**ADR:** [ADR-023](../../decisions/ADR-023-organization-supplier-payables.md)

## DOMAIN_BOUNDARY / CUSTOMER_UTANG_SEPARATION

Supplier Payables (AP) are organization POS product data with dedicated `SupplierPayable` / `SupplierPayablePayment` aggregates and tables. Customer Utang / Business Credit / Personal Utang are **not** reused (entities, tables, endpoints, or balances). UI may say “Supplier Credit”; code never calls supplier debt “Utang”. Architecture guard: SupplierPayables domain must not reference `CreditEntry`, `Repayment`, `UtangLedger`, or `CustomerOrderUtang`.

## PAYABLE_ORIGIN

**POSTED_RECEIPT** — GoodsReceipt or DirectPurchaseReceipt post creates the obligation. PO create/submit does not.

## PAYABLE_MODEL / PAYMENT_MODEL / STATUS_MODEL

- Payable: OriginalAmount, PaidAtReceiptAmount, PaidAmount, Balance, DueDate?, Status, SourceType+SourceId
- Payment rows: immutable later settlements only (`SupplierPayablePayment`)
- Status: Open | PartiallyPaid | Paid | Voided (backend authority)

## PAID_AT_RECEIPT_POLICY / POSTED_PAYMENT_POLICY

- PaidNow on receive → `PaidAtReceiptAmount` only (no payment row)
- Later settlements → `SupplierPayablePayment` rows; aggregate into `PaidAmount`

## FULLY_PAID_RECEIPT_POLICY

**A** — Always create a payable when a supplier is present. Fully paid → Status=Paid with PaidAtReceiptAmount = total.

## PARTIAL_PAYMENT_POLICY / OVERPAY_POLICY / DUE_DATE_POLICY

Partial later payments allowed. **OVERPAY_ALLOWED=NO**. Due date optional. Overdue only when Balance > 0, DueDate present, and DueDate &lt; as-of business date. Missing due date is never overdue. Paid/Voided never overdue.

## SOURCE_UNIQUENESS / IDEMPOTENCY

Unique `(organization_id, source_type, source_id)`. Create-from-receipt is idempotent by source. Payment posts use `Idempotency-Key` + `X-Pos-Payload-Hash` (`supplier_payable.payment`).

## RECEIPT_REVERSAL_POLICY / DIRECT_PURCHASE_REVERSAL_POLICY

- Zero `SupplierPayablePayment` rows → void payable with receipt void  
- Any posted payment → **409** `pos.supplier_payable.void.blocked_by_payments`
- Direct purchase follows the same rule  
- React surfaces friendly copy via `supplierPayables.reverseBlockedByPayments`

## SUPPLIER_PAYMENT_REVERSAL_STATUS

**DEFERRED**

## COST_INVENTORY_SEPARATION

Supplier payments never change inventory qty, acquisition UnitCost, Sale COGS, or receipt line costs.

## SUPPLIER_PAYABLE_SCOPE

**ORGANIZATION** (matches org-level purchase stock; no branch ledger).

## REPORT_TIME_MODEL

**AS_OF** — report uses current clock date (`AsOfDate`). Not a period sales report. No fake historical period balances.

## REPORTING_MODEL / SUPPLIER_AGGREGATION_MODEL / CSV_EXPORT_STATUS

- Operational kind `supplier-payables` → `GET /api/v1/pos/reports/supplier-payables`
- Response: `asOfDate`, `summary`, `suppliers` (per-supplier outstanding/overdue/open/oldest due), `payables`
- UI: summary cards + supplier balances + payable detail (desktop table / mobile cards)
- CSV: existing `ReportCsvExportButton` + `buildOperationalReportExport`; machine money; blank null due dates; no internal IDs; UTF-8 BOM / injection protection unchanged
- Filename uses as-of date (`supplier-payables_…_YYYY-MM-DD.csv`)

## PERMISSION_MODEL / PERMISSION_MATRIX

| Actor | View payables / report | Record payment | Export CSV |
|-------|------------------------|----------------|------------|
| Owner / Admin / StoreManager (ManagePurchasing) | YES | YES | if `store-export` |
| InventoryStaff + ViewPurchasing / ManagePurchasing | YES (incl. report kind) | YES if ManagePurchasing | if `store-export` |
| ReportingUser | YES (ViewPurchasing / report access) | NO | if `store-export` |
| ViewPurchasing only | YES | NO | if `store-export` |
| ManageInventory only | Direct receive only; no payable mutate | NO | — |
| Cashier | NO supplier payable surfaces | NO | — |
| Personal / customer | NO | NO | — |

`PosOperationalReportKind.SupplierPayables` included in InventoryStaff `AllowsReport` matrix.

## PAYABLE_QUERY_MODEL / PAYABLE_REPORT_QUERY_MODEL / N_PLUS_ONE

Paged list + supplier summary aggregates in SQL; report: one filtered list (≤10k) + **batched** supplier name lookup (`GetDisplayNamesByIdsAsync`) + in-memory summary/supplier grouping. **N_PLUS_ONE=NO**.

## BACKEND_CHANGE_REQUIRED / MIGRATION / SCHEMA_CHANGE

YES — `20260830140000_AddPosSupplierPayables` (`supplier_payables`, `supplier_payable_payments`). Architecture guards updated for ADR-023.

## DIRECT_PURCHASE NOTES

No supplier + full pay → skip payable. Credit without supplier → validation failure. React: Paid in full / Supplier credit UX; credit mode requires supplier.

## POSTGRES_INTEGRATION_TESTS

`PosSupplierPayablesApiTests` + `PosGoodsReceiptReversalApiTests`: **7 passed / 0 failed** (create/partial/overpay/idempotency/summary/report envelope, permissions view vs manage vs inventory-only vs cashier, fully-paid + void, direct credit + reverse-blocked-when-paid, cross-org, inventory unchanged). Test factories force `LocalValidation:Enabled=false` so host env from local-validation scripts cannot hang the suite. Architecture: SupplierPayables ≠ Customer Utang types.

## REACT / QUALITY

| Gate | Result |
|------|--------|
| REACT_FULL_TEST_COUNT | 1344 |
| REACT_FULL_PASS | 1344 |
| REACT_FULL_FAIL | 0 |
| TYPECHECK | PASS |
| LINT | PASS (0 errors) |
| BUILD | PASS |

## NEXT

Reassess pilot priority among: real payment providers, B2B checkout, remaining UI polish. Device/offline architecture later. Do **not** auto-pick FIFO or GL. Supplier payment reversal remains deferred. Suggested next package after this: **SUPPLIER-PAYABLE-REPORTING-AND-CSV** is complete within this task — next roadmap item is product-priority reassessment (not device/offline by default).
