# Phase 10 — Full POS

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-09-mvp-hardening.md)

## Status

**In Progress** — P10-WP01 authorized (Option A — supplier master data only). Do **not** begin P10-WP02 until explicitly authorized. HealthCare remains frozen.

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
- [x] Focused commit created and hash recorded (`6f92dd43b2f66709891d82079f9d3fbd0b5c450e`).
- [ ] Working tree clean.
- [x] Exact next WP recorded: **P10-WP02 — Purchasing** (do not begin).

### P10-WP02 — Purchasing

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

### P10-WP03 — Advanced Inventory

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

### P10-WP04 — Cashier Shifts

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

### P10-WP05 — Returns and Refunds

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
