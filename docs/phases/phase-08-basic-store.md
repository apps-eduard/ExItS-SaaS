# Phase 8 — Basic Store

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-07-offline-sync.md) | [Next](phase-09-mvp-hardening.md)

## Objective

Deliver the Basic Store paid plan.

## Status

**In Progress** — P8-WP01 through P8-WP06 complete with documented risks. Do **not** begin P8-WP07 until explicitly authorized.

Feature commit (P8-WP01): `5573822ca116ab46f1a5cdce407e1d7b4f58f796`

## Work packages

### P8-WP01 — Catalog and Barcode

Status: **Complete with documented risks**

Phase marker: `P8-WP01-catalog-and-barcode`

Report: [P8-WP01-catalog-and-barcode.md](../reports/P8-WP01-catalog-and-barcode.md)

Feature commit: `5573822ca116ab46f1a5cdce407e1d7b4f58f796`

#### Risks retained

- R-109 open (no interactive Android validation; `adb` unavailable)
- Development-stage org/commercial headers — not production-secure
- Catalog online-only by design (no offline cache/queue)
- No sales, stock, or inventory claimed

#### Approved scope (clarified)

Organization-owned retail **product catalog and barcode foundation only**:

- POS product and flat category domain models
- Optional SKU and one optional primary barcode per product
- Controlled unit-of-measure set
- Required selling price (`decimal`, ≥ 0, ≤ 2 decimal places)
- Catalog CRUD and Active/Inactive lifecycle (no hard delete)
- Exact SKU and barcode lookup
- PostgreSQL migration `AddPosCatalogAndBarcodes` (`pos.product_categories`, `pos.products`)
- Typed POS API under `/api/v1/pos/catalog/*`
- MAUI catalog screens (`/catalog`, products, categories, barcode lookup)
- Feature grants `store-catalog-view` / `store-catalog-manage` with continuity matrix
- Online-only (no offline catalog cache or queue ops)
- Tests, documentation, Android evidence

#### Product fields (MVP)

ProductId, OrganizationId, Name (required), optional Description, optional SKU, optional primary Barcode, optional CategoryId, UnitOfMeasure (required controlled set), SellingPrice (required ≥ 0), Status Active|Inactive, CreatedAtUtc, UpdatedAtUtc, concurrency metadata.

#### Category fields (MVP)

CategoryId, OrganizationId, Name (required), Status, CreatedAtUtc, UpdatedAtUtc, concurrency metadata. Flat only; active names unique per org (normalized).

#### SKU / barcode rules (MVP)

- SKU optional; trim; uniqueness via uppercase invariant; display form preserved; charset letters/digits/hyphen/underscore/period/slash; inactive SKU remains reserved.
- Barcode optional; digits only; length 8–14; check digits for EAN-8, UPC-A, EAN-13, GTIN-14; inactive barcode remains reserved; lookup exact normalized; no generation/labels/multi-barcode.

#### Unit of measure (MVP)

Piece, Pack, Box, Bottle, Can, Sachet, Kilogram, Gram, Liter, Milliliter, Meter — stable codes; localized labels.

#### Commercial policy

- Trialing / Active / GracePeriod: view + manage when grants permit
- PastDue / Cancelled / Expired: view only when `store-catalog-view` granted
- Suspended / missing / stale / unknown: deny
- Mutations require `store-catalog-manage`

#### Explicit exclusions (P8-WP02+)

Sales/checkout, inventory/stock, suppliers/purchasing, discounts/tax/VAT, receipts/invoices, multiple prices, customer/Utang on sales, barcode generation/printing, offline catalog mutations/cache, gateways/QR/cards, POS operational roles.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (684 / 0 / 0; baseline 619).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded (`5573822ca116ab46f1a5cdce407e1d7b4f58f796`).
- [x] Working tree clean.

### P8-WP02 — Simple Sales

Status: **Complete with documented risks**

Phase marker: `P8-WP02-simple-sales`

Feature commit: `72a6fa9b1bb6f48610563d01ee10e608e99806e1`

Report: [P8-WP02-simple-sales.md](../reports/P8-WP02-simple-sales.md)

#### Approved scope (clarified)

Organization-isolated **simple retail sales** using the P8-WP01 catalog:

- Sale + sale-line domain (Completed | Voided)
- Payment methods: Cash, ManualGCash only (exactly one per sale)
- Temporary client cart; server-authoritative checkout totals
- Product lookup reuse (barcode/SKU/name/catalog); only active org products
- Snapshot product name/SKU/barcode/UOM/unit price on lines
- Quantity: whole for Piece/Pack/Box/Bottle/Can/Sachet; ≤3 decimals for Kilogram/Gram/Liter/Milliliter/Meter
- Monetary rounding: `MidpointRounding.AwayFromZero` to 2 decimals (credit/repayment convention)
- Organization-scoped sale number `SALE-YYYYMMDD-<sequence>` via `pos.sale_number_sequences`
- Explicit void with required reason + actor (no refund/inventory)
- Migration `AddPosSimpleSales` (`pos.sales`, `pos.sale_lines`, `pos.sale_number_sequences`)
- API: POST/GET sales, GET by id, POST void; checkout idempotency (`sale.checkout`)
- MAUI `/sales`, `/sales/new`, `/sales/{saleId}`
- Features: `store-sales-view`, `store-sales-create`, `store-sales-void`
- Continuity: PastDue/Cancelled/Expired view-only; Suspended/unknown deny
- Online-only (no offline sale queue/cache)

#### Explicit exclusions (P8-WP03+)

Inventory/stock deduction, suppliers/purchasing, Utang/customer-credit sales, split/partial payments, cards/gateways/QR/GCash verification, discounts/tax/VAT/fees/tips, refunds/returns/exchanges/line voids, fiscal invoices, offline sales, POS operational roles.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (759 / 0 / 0; baseline 684).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded (`72a6fa9b1bb6f48610563d01ee10e608e99806e1`).
- [x] Working tree clean.

### P8-WP03 — Product-Based Utang

Status: **Complete with documented risks**

Phase marker: `P8-WP03-product-based-utang`

Feature commit: `cd58f5c7dc1b9d31497429ef1d025546a0def09c`

Report: [P8-WP03-product-based-utang.md](../reports/P8-WP03-product-based-utang.md)

#### Approved scope (clarified)

Atomic **Product-Based Utang** checkout that preserves immutable sale history and immutable customer credit history:

- One completed retail sale + sale lines + one linked remarks-based credit entry in a single transaction
- Sale payment method `Utang`; requires active same-org customer and active catalog products
- Credit amount equals authoritative sale total; system remarks `Product sale {SaleNumber}`; stable `SaleId` / `CreditEntryId` cross-reference
- Reuse existing customer, credit aggregate, ledger, due dates, overdue, statements, org isolation, server idempotency
- Optional initial due date via existing audited due-date mechanism (reason: `Set during Product-Based Utang checkout`)
- Zero-total Utang checkout rejected (no debt)
- Atomic void: void sale + reverse linked credit together; block standalone reversal of linked credit; reject void if subsequent Utang activity prevents safe reversal
- Requires `store-sales-create` + `customer-credit-create` (void: `store-sales-void` + credit-correction / `ReverseCredit`); continuity denies create/void
- Migration `AddProductBasedUtang` (nullable `customer_id` / `linked_credit_entry_id` on sales; `source_sale_id` on credit entries; Utang payment method)
- Extend `POST /api/v1/pos/sales` and MAUI `/sales/new` for Utang + customer + optional due date
- Online-only; tests, docs, Android evidence

#### Explicit exclusions (P8-WP04+)

Inventory/stock deduction, Cash/GCash+Utang split, deposits/partial at checkout, discounts/tax/VAT/fees/tips, credit limits/approvals/interest/penalties, installments, refunds/returns/exchanges, offline Product-Based Utang, receipt printing/tax invoices, POS operational roles.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (775 / 0 / 0; baseline 759).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded (`cd58f5c7dc1b9d31497429ef1d025546a0def09c`).
- [x] Working tree clean.

### P8-WP04 — Basic Inventory

Status: **Complete with documented risks**

Phase marker: `P8-WP04-basic-inventory`

Feature commit: `64f05e7fd5ab868beb62c7cce88ad7a15e21c7b8`

Report: [P8-WP04-basic-inventory.md](../reports/P8-WP04-basic-inventory.md)

#### Approved scope (clarified)

Organization-isolated **basic inventory** for catalog products and completed sales:

- One inventory account per org/product (`IsTracked`, optional `ReorderLevel`); on-hand derived from immutable stock movements (optional denormalized projection transactionally protected)
- Movement types: OpeningStock, ManualIncrease, ManualDecrease, SaleDeduction, SaleVoidRestoration
- Optional enable tracking + opening stock (zero valid; no duplicate opening); disable only when on-hand is zero
- Manual Stock In / Stock Out with required reason; Stock Out cannot go negative; no set-quantity command
- Atomic stock deduction on Cash / ManualGCash / Utang checkout for tracked products; untracked allowed without movement
- Atomic stock restoration on sale void (including Product-Based Utang void coordinating sale + credit + stock)
- UOM quantity precision reused from catalog; block UOM change after inventory activity
- Low stock when tracked and OnHand ≤ ReorderLevel (no auto-reorder)
- Features: `store-inventory-view` / `store-inventory-manage`; continuity view-only; Suspended deny
- Stock deduction is part of authorized checkout (not a bypassable client inventory grant)
- Migration `AddPosBasicInventory` (`pos.inventory_accounts`, `pos.stock_movements`)
- API `/api/v1/pos/inventory/*`; MAUI `/inventory*`
- Online-only; tests, docs, Android evidence

#### Explicit exclusions (P8-WP05+)

Suppliers/purchasing/POs/receiving, warehouses/branches/bins, transfers, batches/lots/serials/expiry, cost/valuation/profit, returns/exchanges/refunds/damaged goods, stock reservation, auto-reorder, barcode labels, offline inventory/sync, negative-stock override, POS operational roles.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (805 / 0 / 0; baseline 775).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded (`64f05e7fd5ab868beb62c7cce88ad7a15e21c7b8`).
- [x] Working tree clean.

### P8-WP05 — Expenses

Status: **Complete with documented risks**

Phase marker: `P8-WP05-expenses`

Feature commit: `ca956921fbfcfad8499f01acb9d9726fff2d81d4`

Report: [P8-WP05-expenses.md](../reports/P8-WP05-expenses.md)

#### Approved scope (clarified)

Organization-isolated **store expense recording and monitoring**:

- Flat expense categories (Active/Inactive; active normalized name unique per org; no hierarchy/hard delete)
- Immutable expense entries: amount > 0 (≤2 dp), Cash or ManualGCash, required description, calendar ExpenseDate, optional payee/GCash reference
- Status Recorded | Voided; corrections via void + replacement expense; no direct edit
- Server expense number `EXP-YYYYMMDD-<sequence>`; idempotent create via existing infrastructure
- Derived period summaries (totals by category/payment, net excludes voided); no P&L/tax claims
- Features: `store-expenses-view` / `store-expenses-manage`; continuity view-only; Suspended deny
- Migration `AddPosExpenses` (`pos.expense_categories`, `pos.expenses`, `pos.expense_number_sequences`)
- API `/api/v1/pos/expense-categories`, `/api/v1/pos/expenses*`
- MAUI `/expenses*`
- Online-only; no default category seeding
- Tests, docs, Android evidence

#### Explicit exclusions (P8-WP06+)

AP/suppliers/POs/receiving, payroll, reimbursements/advances, recurring automation, budgets/approvals, GL/journal, tax/VAT rules, OCR/attachments, split payments, cards/gateways/QR/GCash verification, offline expenses, P&L reporting, POS operational roles.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (830 / 0 / 0; baseline 805).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded (`ca956921fbfcfad8499f01acb9d9726fff2d81d4`).
- [x] Working tree clean.

### P8-WP06 — Dashboard and Reports

Status: **Complete with documented risks**

Phase marker: `P8-WP06-dashboard-and-reports`

Feature commit: `a0028f36a0d8e2ea76c3101b2b65ba82bfd4fd02`

Report: [P8-WP06-dashboard-and-reports.md](../reports/P8-WP06-dashboard-and-reports.md)

#### Approved scope (clarified)

Organization-isolated **operational dashboards and reports** derived from existing Basic Store immutable records (read-only projections; no business-transaction writes):

- Compact period dashboard: completed sales/count, Cash / ManualGCash / Utang sales totals, active customer Utang outstanding, overdue Utang, recorded expenses, low-stock count, voided sale/expense counts; sales/expenses-by-day trends; payment-method breakdown; optional vs preceding equal-length period (absolute/%; “Not available” on divide-by-zero). Do **not** calculate profit (sales − expenses).
- Sales reports: summary, by payment method/product/category, top products by qty/amount, voided-sales summary, Product-Based Utang sales summary — immutable sale-line snapshots; voided excluded from active totals.
- Utang reports: reuse authoritative outstanding/overdue/FIFO aging (no second balance model); credits/repayments in period; customer statement navigation.
- Inventory reports: tracked on-hand, low/out-of-stock, movement totals by type, sale deductions/void restorations/manual adjustments, latest movement date — no valuation/cost/reorder.
- Expense reports: reuse expense summary rules (active/voided, by category/payment/day, count, detail list) — never combine with sales into P&L.
- Filters: date range, payment method, sale status, product, category, customer, expense category, inventory tracking/low-stock — server-side, paginated, deterministic; document any max range if introduced.
- Export: stable export-ready DTOs; CSV/PDF/Excel file generation deferred unless an approved mechanism already exists (none today) — UI download/share preparation only; no heavy reporting dependency.
- Features: `store-dashboard-view`, `store-reports-view` (`store-reports-export` only if export files are implemented). Continuity/read matrix: grant-controlled through PastDue/Cancelled/Expired; Suspended/missing/stale/unknown deny. No POS operational roles.
- Persistence: query projections only — no persisted dashboard/report totals. Migration only if a confirmed index is required (`ExItS_PinoyBusinessPOS` / `pos`).
- API: `/api/v1/pos/dashboard`, `/api/v1/pos/reports/{sales,utang,inventory,expenses}` (+ by-product/by-category as needed).
- MAUI: `/dashboard`, `/reports`, `/reports/sales|utang|inventory|expenses` — online-only authoritative; offline shows reconnect-required.
- Tests, documentation, Android evidence.

#### Explicit exclusions (P8-WP07+)

Profit/margin/COGS/P&L; accounting journals/balance sheet/cash-flow/tax reports; supplier/purchasing/payroll/reimbursement reports; inventory valuation; forecasting/AI; scheduled/email/notification reports; PDF/Excel generation (deferred); offline authoritative report caches; custom report builders; Phase 9+.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (851 / 0 / 0; baseline 830).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded (`a0028f36a0d8e2ea76c3101b2b65ba82bfd4fd02`).
- [x] Working tree clean.

### P8-WP07 — Basic Store Closeout

Status: Not Started — do not begin until authorized

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
