# POS transaction reference numbering

**Status:** AUTHORITATIVE (current)  
**Scope:** PinoyBusinessPOS server-generated business document numbers  
**Code:** `PosDocumentNumbers`, `PosDocumentPrefixes`, and per-type `*Numbers` wrappers under Domain

---

## 1. Purpose

Give every POS business document a short, human-readable reference that:

- Identifies the **transaction type** at a glance (prefix)
- Groups by **business date** (`YYMMDD`)
- Sequences safely **per organization + document type + business date**
- Supports true **child/family** documents only where the domain already has them (inventory transfer replacements)

This policy does **not** cover external, imported, payment-provider, bank, supplier, or regulatory identifiers.

---

## 2. Format

```text
PREFIX-YYMMDD-NNN
```

Examples:

| Example | Meaning |
|---------|---------|
| `SAL-260922-001` | First sale of business date 2026-09-22 |
| `PO-260922-042` | 42nd purchase order that day |
| `TR-260922-001-R1` | First replacement child of transfer `TR-260922-001` |

Rules:

| Rule | Behavior |
|------|----------|
| Prefix | Server-chosen short code (2–4 letters, uppercase). Clients never select a prefix. |
| Date | Business date as `YYMMDD` (UTC calendar date via `PosDocumentNumbers.BusinessDateOf`) |
| Sequence | Starts at `001` each business date; minimum **3** digits; after `999` continues as `1000`, `1001`, … |
| Daily reset | Next business date resets that type’s counter to `001` |
| Scope | **Organization + document type + business date** (separate sequence tables / locks per type) |
| Isolation | Two types may both show `…-001` on the same day — that is correct |

Shared formatter (do not re-implement string building in wrappers):

```csharp
PosDocumentNumbers.Format(prefix, businessDate, sequence);
PosDocumentNumbers.FormatChild(rootNumber, childSequence);
```

---

## 3. Prefix table

| Document | Prefix | Domain type | Wrapper |
|----------|--------|-------------|---------|
| Sale | `SAL` | Sale | `SaleNumbers` |
| Sale return | `RET` | SaleReturn | `ReturnNumbers` |
| Expense | `EXP` | Expense | `ExpenseNumbers` |
| Stock use | `SU` | StockUse | `StockUseNumbers` |
| Return batch | `RB` | ReturnBatch | `ReturnBatchNumbers` |
| Waste / loss | `WL` | WasteLoss | `WasteLossNumbers` |
| Production | `PRD` | ProductionRun | `ProductionNumbers` |
| Quotation | `QUO` | Quotation | `QuotationNumbers` |
| Stock count | `SC` | StockCount | `StockCountNumbers` |
| Stock request | `SR` | StockRequest | `StockRequestNumbers` |
| Goods receipt | `GRN` | GoodsReceipt | `GoodsReceiptNumbers` |
| Purchase order | `PO` | PurchaseOrder | `PurchaseOrderNumbers` |
| Cashier shift | `SH` | CashierShift | `CashierShiftNumbers` |
| Inventory transfer | `TR` | InventoryTransfer | `InventoryTransferNumbers` |
| Customer order | `ORD` | CustomerOrder | `CustomerOrderNumbers` |
| Direct purchase | `DP` | DirectPurchaseReceipt | `DirectPurchaseReceiptNumbers` |

Allow-list: `PosDocumentPrefixes.All`. Unknown prefixes are rejected on normalize/format.

---

## 4. Sequence scope

```text
(OrganizationId, DocumentType, BusinessDate) → next sequence
```

- Not one global counter across types
- Not shared across organizations
- Concurrency: existing per-type advisory locks / serializable allocation paths

---

## 5. Daily reset

At midnight UTC business-date change (as used by `BusinessDateOf`):

```text
SAL-260922-1000   // last of day
SAL-260923-001    // next day starts over
```

---

## 6. Sequence padding

| Sequence | Display |
|----------|---------|
| 1 | `001` |
| 10 | `010` |
| 999 | `999` |
| 1000 | `1000` |

---

## 7. Transfer children (replacement family)

```text
Root:   TR-260922-001
Child:  TR-260922-001-R1
Child:  TR-260922-001-R2
```

- Child **keeps the root number** even if created on a later calendar date
- Append `-Rn` only (no new date segment)
- **`RootTransferId` / `ReplacementSequence` on the transfer aggregate are authoritative**
- Never parse the string to infer family membership

---

## 8. Historical compatibility

Do **not** renumber persisted rows.

Readable forms:

| Era | Example |
|-----|---------|
| Current | `SAL-260922-001`, `TR-260922-001-R1` |
| Prior short form (no prefix) | `260922-001`, `260922-001-R1` |
| Older long forms (still in DBs) | `SALE-20260922-000001`, `TR-20260922-000001`, … |

`Normalize` accepts current prefixed and legacy unprefixed `YYMMDD-NNN` / `YYMMDD-NNN-Rn`.  
New allocations always emit the current prefixed form.

---

## 9. External / user-supplied references (excluded)

This policy does **not** apply to:

- Supplier / buyer external PO or delivery references
- Payment-provider transaction IDs
- Bank / GCash / check references
- Imported catalog or third-party document IDs
- Legal / regulatory tax invoice identifiers
- Free-text “reference” fields entered by users

Those remain free-form (with their own max lengths) and must not be forced through `PosDocumentNumbers`.

---

## 10. When each number is allocated

| Type | Allocation moment |
|------|-------------------|
| Sale (`SAL`) | Sale completion / finalize |
| Sale return (`RET`) | Completed return create |
| Return batch (`RB`) | Return batch create / number assignment |
| Expense (`EXP`) | Expense post |
| Cashier shift (`SH`) | Shift open |
| Quotation (`QUO`) | Quotation create / issue |
| Customer order (`ORD`) | Order place |
| Purchase order (`PO`) | PO submit (draft may be null) |
| Goods receipt (`GRN`) | GRN receive |
| Direct purchase (`DP`) | Direct purchase receipt post |
| Stock request (`SR`) | Stock request submit |
| Stock count (`SC`) | Stock count create |
| Stock use (`SU`) | Stock-use post |
| Waste / loss (`WL`) | Waste/loss post |
| Production (`PRD`) | Production run post |
| Inventory transfer (`TR`) | **Dispatch** (not draft create). Replacement children: `{root}-Rn` at child dispatch |

Exact hooks live in Application use cases + Infrastructure `AllocateNext*` repositories.

---

## 11. Examples for end-user documentation

- “Your receipt number is **SAL-260922-015**.”
- “Transfer **TR-260922-003** is in transit to Branch B.”
- “Replacement shipment **TR-260922-003-R1** continues the same transfer family.”
- “PO **PO-260922-008** was submitted today; GRN **GRN-260922-002** received against it.”

Staff should treat the full string as the search key; prefixes prevent confusing a sale with a PO that share the same date/sequence digits.

---

## 12. Developer checklist — adding a NEW transaction type

1. Choose a unique **2–4 letter** prefix; add to `PosDocumentPrefixes` and `All`
2. Add Domain `*Numbers` wrapper with `Prefix` constant calling `PosDocumentNumbers.Format(Prefix, …)`
3. Map domain errors to the type’s `Invalid*Number` code
4. Add sequence persistence (table + advisory lock pattern matching existing types)
5. Wire allocation in the Application use case at the correct lifecycle moment
6. Set EF max length ≥ `PosDocumentNumbers.MaxLength` (24) on the number column
7. Add unit tests: format pad/`1000`/next day/prefix isolation/normalize
8. Add concurrency / uniqueness coverage if a new sequence table is introduced
9. **Update this document** (prefix table + allocation moment)
10. Do not renumber historical rows; do not let clients choose prefixes

---

## 13. Related code

| Artifact | Location |
|----------|----------|
| Shared formatter | `Domain/Common/PosDocumentNumbers.cs` |
| Prefix allow-list | `Domain/Common/PosDocumentPrefixes` (same file) |
| Transfer children | `InventoryTransferNumbers.FormatReplacement` |
| Engineering transfers doc | [pos-branch-inventory-transfers.md](./pos-branch-inventory-transfers.md) |
