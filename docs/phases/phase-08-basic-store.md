# Phase 8 — Basic Store

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-07-offline-sync.md) | [Next](phase-09-mvp-hardening.md)

## Objective

Deliver the Basic Store paid plan.

## Status

**In Progress** — P8-WP01 and P8-WP02 complete with documented risks. Do **not** begin P8-WP03 until explicitly authorized.

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

### P8-WP04 — Basic Inventory

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

### P8-WP05 — Expenses

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

### P8-WP06 — Dashboard and Reports

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

### P8-WP07 — Basic Store Closeout

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
