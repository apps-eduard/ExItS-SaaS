# Phase 10 — Full POS

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-09-mvp-hardening.md)

## Status

**In Progress** — **P10-WP05 — Returns and Refunds** authorized (prior tip `7a6a2e4` / WP04 complete). Do **not** begin P10-WP06.

## Objective

Deliver complete POS operations after the Commercial MVP while preserving SaaS boundaries:

```text
ExItS Platform
└── owns identity, organizations, memberships, products,
    plans, subscriptions, SaaS payments, entitlements,
    product access, Platform administration, and audit

PinoyBusinessPOS
└── owns product-local roles and store operations,
    including customers, Utang, catalog, sales,
    inventory, expenses, dashboard, and reports
```

## First work package (authoritative)

| Field | Value |
|---|---|
| Number | **P10-WP01** (formally numbered) |
| Title | **Suppliers** |
| Exact name | **P10-WP01 — Suppliers** |
| Implementation status | **Complete** (Option A) |

Product/architecture references that name suppliers as POS-owned Full POS capability:

- `docs/product/pinoy-business-pos-requirements.md` — Full POS includes “Suppliers and purchasing”
- `docs/product/subscriptions-and-billing.md` — Full POS adds suppliers, purchasing, …
- `docs/engineering/data-authority-matrix.md` — Supplier authoritative owner = POS (`SupplierId`)
- `docs/engineering/platform-product-capability-boundary.md` — suppliers / purchasing listed as POS

**Gap:** those sources do **not** define the detailed approved outcomes, exclusions, persistence model, API/UI surface, authorization grants, online/offline rules, or acceptance tests for `P10-WP01` alone. The phase page previously contained only generic stub text (“Implement only the approved scope described by the architecture and product documents”), which is insufficient for implementation under permanent workflow rules.

Decision record: [P10-WP01-scope-ambiguity.md](../reports/P10-WP01-scope-ambiguity.md)

## Work packages

### P10-WP01 — Suppliers

Status: **In Progress** (authorized — Option A)

Phase marker: `P10-WP01-suppliers`

Ambiguity resolution: Option **A — Supplier master data only** authorized (prior tip `97e17c248ddd1c0af588eafaa41ac7ab6910ec2f`).

#### Approved scope (clarified)

Organization-owned supplier master data for PinoyBusinessPOS. **Reference/master data only.** No purchasing, receiving, payables, costing, stock, or financial transactions.

Deliver:

- Supplier aggregate with Active/Inactive lifecycle (no hard delete)
- Server-generated org-scoped `SUP-<sequence>` SupplierCode (immutable)
- Organization-scoped duplicate prevention (active normalized name; optional tax/email/mobile likelihood conflicts)
- Search/filter, detail, create/edit, activate/deactivate
- Feature grants `store-suppliers-view` / `store-suppliers-manage` with commercial-state matrix
- Migration `AddPosSuppliers` in schema `pos`
- Typed API + MAUI screens + tests + documentation

#### Explicit exclusions

- Purchase orders (including drafts), receiving, supplier invoices, AP, payments/balances
- Stock increases, product cost/history, purchase returns, supplier credits, purchasing reports
- Attachments, import/export, offline supplier mutations
- POS operational roles; P10-WP02 or later

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (preserve 1001 baseline; suite now 1047 / 0 / 0).
- [x] Dashboard and phase page updated.
- [x] Completion report created (`docs/reports/P10-WP01-suppliers.md`).
- [x] Focused commit created and hash recorded (`6f92dd43b2f66709891d82079f9d3fbd0b5c450e`; docs `55469c60802d11273669efa10494ff1632efa84d`).
- [x] Working tree clean (after push).
- [x] Exact next WP recorded: **P10-WP02 — Purchasing** (do not begin).

### P10-WP02 — Purchasing

Status: **Complete**

Phase marker: `P10-WP02-purchasing`

#### Approved scope (clarified)

Organization-isolated purchasing for PinoyBusinessPOS using P10-WP01 suppliers:

- Purchase orders (Draft → Ordered → PartiallyReceived → Received / Cancelled)
- PO lines with quantity, unit purchase cost (operational only — not valuation/COGS)
- Server `PO-YYYYMMDD-NNNNNN` / `GRN-YYYYMMDD-NNNNNN` numbers
- Immutable goods receipts with partial/complete receiving
- Atomic inventory `PurchaseReceipt` movements for tracked products
- Feature grants `store-purchasing-view` / `store-purchasing-manage`
- Migration `AddPosPurchasing`, typed API, MAUI, tests, docs
- Online-only; no unplanned receiving without a PO

#### Explicit exclusions

- Accounts payable, supplier payments/balances, accounting, tax, purchase returns
- Inventory valuation / COGS from purchase cost
- Offline purchasing mutations; POS operational roles; P10-WP03+

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (baseline 1047 → suite now 1067 / 0 / 0).
- [x] Dashboard and phase page updated.
- [x] Completion report created (`docs/reports/P10-WP02-purchasing.md`).
- [x] Focused commit created and hash recorded (`c0f8130ef99e958bceaee98024a69339b7e8e41a`; docs `bc6dc7477e74c3c03785862dd98317d39c55eee1`).
- [x] Working tree clean (after push).
- [x] Exact next WP recorded: **P10-WP03 — Advanced Inventory** (do not begin).

### P10-WP03 — Advanced Inventory

Status: **Complete**

Prior tip: `882050c` (docs authorize). Feature: `5c62133`. Docs: `8af7a14`. Baseline: **1067 / 0 / 0**. Tests: **1073 / 0 / 0**. Part A HealthCare cleanup remains closed at `fd77f88`.

#### Required outcomes (approved)

Extend the existing immutable inventory subsystem (preserve movement-derived on-hand; one account per tracked product):

- Inventory stock counts (`StockCount` aggregate: Draft → InProgress → Completed; Draft/InProgress → Cancelled)
- Immutable stock-count variance adjustments (new movement type(s); never rewrite history)
- Reorder configuration (ReorderLevel ≥ 0; optional ReorderQuantity > 0; auditable; tracked products only)
- Derived stock states: InStock, LowStock, OutOfStock, ReorderSuggested (no auto PO; no forecasting)
- Low-stock and reorder suggestions surfaces
- Enhanced inventory movement history
- Inventory reconciliation (on-hand vs movement sum; no second balance authority)
- Organization-scoped authorization (reuse / extend inventory grants)
- PostgreSQL migration, typed API, MAUI stock-count and reorder screens, tests, docs

Preserve existing movement sources: Opening, ManualIncrease, ManualDecrease, SaleDeduction, SaleVoidRestore, PurchaseReceipt.

#### Explicit exclusions

Warehouses, branches, transfers, costing, valuation, batches, serials, expiry, purchase returns, accounting, automatic purchase-order creation, demand forecasting, P10-WP04+.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence.
- [x] Dashboard and phase page updated.
- [x] Completion report created (`docs/reports/P10-WP03-advanced-inventory.md`).
- [x] Focused commit created and hash recorded (`5c62133`; docs `8af7a14`).
- [x] Working tree clean.
- [x] Exact next WP recorded: **P10-WP04 — Cashier Shifts** (do not begin).

### P10-WP04 — Cashier Shifts

Status: **Complete**

Prior tip: `a25fe6abd713da84ad99d9d0a2022b99f49765e8` (P10-WP03 complete). Baseline: **1079 / 0 / 0**. Tests: **1097 / 0 / 0**.

#### Required outcomes (approved)

Organization-isolated cashier shifts for operational cash control:

- Shift open with opening cash float (`SHIFT-YYYYMMDD-NNNNNN`); opens in **Open**
- One Open shift per OrganizationId + trusted cashier ActorId
- New Cash / ManualGCash / Utang sales require an active Open shift and immutable `CashierShiftId` linkage
- Immutable CashIn / CashOut movements on Open shifts (deny CashOut that would make expected cash negative)
- Expected physical cash = OpeningCash + NetCashSales + CashIn − CashOut  
  where NetCashSales = completed Cash sales − voided Cash sales (ManualGCash/Utang reported, not in physical cash)
- Close with closing cash declaration, expected snapshot, variance; Closed/Cancelled terminal
- Cancel Open only when no linked financial activity
- Grants `store-shifts-view` / `store-shifts-manage`; Dev/Testing actor only (do not close R-091 / POS-ROLES)
- PostgreSQL migration, typed API, MAUI shift screens, tests, docs
- Legacy pre-migration sales may remain unassigned (no synthetic backfill)

#### Explicit exclusions

Payroll, accounting journals, bank reconciliation, cash deposits, branch registers, tax/fiscal closing, expense↔shift auto-coupling, Draft/Suspended/Reopened states, P10-WP05+.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (baseline 1079 → suite now 1097 / 0 / 0).
- [x] Dashboard and phase page updated.
- [x] Completion report created (`docs/reports/P10-WP04-cashier-shifts.md`).
- [x] Focused commit created and hash recorded (`4076485`; docs `df0a092`).
- [x] Working tree clean (after push).
- [x] Exact next WP recorded: **P10-WP05 — Returns and Refunds** (do not begin).

### P10-WP05 — Returns and Refunds

Status: **In Progress** (authorized)

Prior tip: `7a6a2e4eb6d2f8074bb923dd6a0a5eca6654f706` (P10-WP04 complete). Baseline: **1097 / 0 / 0**.

#### Required outcomes (approved)

Organization-isolated post-sale returns/refunds that preserve the original sale:

- Atomic Completed-only `SaleReturn` (`RET-YYYYMMDD-NNNNNN`); no Draft/Pending states
- Partial/full line returns from sale-line snapshots; server-authoritative refundable qty/amount
- Refund method matches original sale tender (Cash / ManualGCash / Utang) — no method switch
- Cash refund through active cashier shift (expected-cash impact); ManualGCash as confirmed external refund; Utang adjusts linked credit
- RestockDisposition ReturnToStock / DoNotRestock; inventory restoration via new immutable movement for tracked products
- Void vs return mutual exclusion: voided sales cannot be returned; sales with completed returns cannot be voided
- Idempotency/concurrency; grants `store-returns-view` / `store-returns-manage`
- PostgreSQL migration, typed API, MAUI return screens, tests, docs
- Online-only; ExItS remains independent of removed HealthCare workspace

#### Explicit exclusions

Exchanges, store credit, gift cards, different/split refund methods, unlinked returns, supplier/PO returns, shipping, restocking fees, tax/VAT, promotions recalculation, payment gateways, GCash API verification, manager approval / POS roles, offline returns, **P10-WP06+**.

#### Definition of Done

- [ ] Approved outcomes complete.
- [ ] Applicable tests pass with exact evidence.
- [ ] Dashboard and phase page updated.
- [ ] Completion report created.
- [ ] Focused commit created and hash recorded.
- [ ] Working tree clean.
- [ ] Exact next WP recorded: **P10-WP06 — Advanced Permissions and Reports** (do not begin).

### P10-WP06 — Advanced Permissions and Reports

Status: Not Started

#### Required outcomes

- Implement only the approved scope described by the architecture and product documents.
- Add required tests and documentation evidence.
- Preserve security, tenant isolation and product boundaries.

#### Definition of Done

- [ ] Approved outcomes complete.
- [ ] Applicable tests pass with exact evidence.
- [ ] Dashboard and phase page updated.
- [ ] Completion report created.
- [ ] Focused commit created and hash recorded.
- [ ] Working tree clean.

### P10-WP07 — Multiple Registers

Status: Not Started

#### Required outcomes

- Implement only the approved scope described by the architecture and product documents.
- Add required tests and documentation evidence.
- Preserve security, tenant isolation and product boundaries.

#### Definition of Done

- [ ] Approved outcomes complete.
- [ ] Applicable tests pass with exact evidence.
- [ ] Dashboard and phase page updated.
- [ ] Completion report created.
- [ ] Focused commit created and hash recorded.
- [ ] Working tree clean.

### P10-WP08 — Full POS Closeout

Status: Not Started

#### Required outcomes

- Implement only the approved scope described by the architecture and product documents.
- Add required tests and documentation evidence.
- Preserve security, tenant isolation and product boundaries.

#### Definition of Done

- [ ] Approved outcomes complete.
- [ ] Applicable tests pass with exact evidence.
- [ ] Dashboard and phase page updated.
- [ ] Completion report created.
- [ ] Focused commit created and hash recorded.
- [ ] Working tree clean.

## Phase exit criteria

- [ ] Every work package is complete or explicitly deferred.
- [ ] Risks and decisions are recorded.
- [ ] Required regression/security tests pass.
- [ ] Next phase is explicitly approved.
