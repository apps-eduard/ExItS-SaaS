# POS-SUPPLIER-PAYABLES-01

**Status:** COMPLETE  
**Branch:** `feat/organization`  
**TASK:** POS-SUPPLIER-PAYABLES-01  
**START_SHA:** `178100d07b2e248f561a491d0b41120483a9844a`  
**FEATURE_SHA:** `17a29da17f07ca51f3860efb96a2387369ae06f0`  
**ADR:** [ADR-023](../../decisions/ADR-023-organization-supplier-payables.md)

## DOMAIN_BOUNDARY / CUSTOMER_UTANG_SEPARATION

Supplier Payables (AP) are organization POS product data with dedicated `SupplierPayable` / `SupplierPayablePayment` aggregates and tables. Customer Utang / Business Credit / Personal Utang are **not** reused (entities, tables, endpoints, or balances). UI may say “Supplier Credit”; code never calls supplier debt “Utang”.

## PAYABLE_ORIGIN

**POSTED_RECEIPT** — GoodsReceipt or DirectPurchaseReceipt post creates the obligation. PO create/submit does not.

## PAYABLE_MODEL / PAYMENT_MODEL / STATUS_MODEL

- Payable: OriginalAmount, PaidAtReceiptAmount, PaidAmount, Balance, DueDate?, Status, SourceType+SourceId
- Payment rows: immutable later settlements only (`SupplierPayablePayment`)
- Status: Open | PartiallyPaid | Paid | Voided

## FULLY_PAID_RECEIPT_POLICY

**A** — Always create a payable when a supplier is present. PaidNow settles via `PaidAtReceiptAmount` (not a payment row). Status=Paid when balance is 0.

## PARTIAL_PAYMENT_POLICY / OVERPAY_POLICY / DUE_DATE_POLICY

Partial later payments allowed. **OVERPAY_ALLOWED=NO**. Due date optional.

## SOURCE_UNIQUENESS / IDEMPOTENCY

Unique `(organization_id, source_type, source_id)`. Create-from-receipt is idempotent by source. Payment posts use `Idempotency-Key` + `X-Pos-Payload-Hash` (`supplier_payable.payment`).

## RECEIPT_REVERSAL_POLICY / DIRECT_PURCHASE_REVERSAL_POLICY

- Zero `SupplierPayablePayment` rows → void payable with receipt void  
- Any posted payment → **409** block receipt reverse  
- Direct purchase follows the same rule  

## SUPPLIER_PAYMENT_REVERSAL_STATUS

**DEFERRED**

## COST_INVENTORY_SEPARATION

Supplier payments never change inventory qty, acquisition UnitCost, Sale COGS, or receipt line costs.

## SUPPLIER_PAYABLE_SCOPE

**ORGANIZATION** (matches org-level purchase stock; no branch ledger).

## PERMISSION_MODEL

ViewPurchasing reads; ManagePurchasing records payments and GRN path; Direct Purchase create remains ManageInventory (payable created in that transaction). ReportingUser cannot pay. CSV export uses `store-export` / `ExportData`.

## AUDIT_TRAIL_MODEL

Payable + payment retain actor/time/method/reference/notes; no physical delete of payment history.

## REPORTING_MODEL / CSV_EXPORT_STATUS

Operational report kind `supplier-payables` + client CSV via existing `ReportCsvExportButton`.

## PAYABLE_QUERY_MODEL / N_PLUS_ONE

Paged list + supplier summary aggregates in SQL; payment list by payable id; report rows batched. **N_PLUS_ONE=NO**.

## BACKEND_CHANGE_REQUIRED / MIGRATION / SCHEMA_CHANGE

YES — `20260830140000_AddPosSupplierPayables` (`supplier_payables`, `supplier_payable_payments`). Architecture guards updated for ADR-023 (legacy AccountsPayable/SupplierInvoice still forbidden).

## DIRECT_PURCHASE NOTES

No supplier + full pay → skip payable. Credit without supplier → validation failure. Default PaidNow = full total in UI.

## POSTGRES_INTEGRATION_TESTS

`PosSupplierPayablesApiTests` (3 facts) covering create/partial/overpay/idempotency/summary/report, fully-paid + void, direct credit + reverse-blocked-when-paid, cross-org, inventory unchanged. Reversal suite still green.

## REACT / QUALITY

| Gate | Result |
|------|--------|
| REACT_FULL_TEST_COUNT | 1321 |
| REACT_FULL_PASS | 1321 |
| REACT_FULL_FAIL | 0 |
| TYPECHECK | PASS |
| LINT | PASS |
| BUILD | PASS |
| POSTGRES_INTEGRATION | 3 payables facts (+ reversal suite green) |

## NEXT

Reassess pilot priority among: real payment providers, B2B checkout, device enforcement/offline architecture, remaining UI polish. Do **not** auto-pick FIFO or GL. Supplier payment reversal remains deferred.
