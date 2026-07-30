# Phase 10 — Full POS

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-09-mvp-hardening.md)

## Status

**Authorized to begin — first work package identified; approved implementation scope not yet clarified.** Do **not** implement Phase 10 functionality until `P10-WP01` approved scope is explicitly authorized.

Phase 9 accepted closed at `9c1b86b4488005e81bb9d78b1dafaea66a8e6e4d`.

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
| Implementation status | **Blocked pending approved-scope clarification** |

Product/architecture references that name suppliers as POS-owned Full POS capability:

- `docs/product/pinoy-business-pos-requirements.md` — Full POS includes “Suppliers and purchasing”
- `docs/product/subscriptions-and-billing.md` — Full POS adds suppliers, purchasing, …
- `docs/engineering/data-authority-matrix.md` — Supplier authoritative owner = POS (`SupplierId`)
- `docs/engineering/platform-product-capability-boundary.md` — suppliers / purchasing listed as POS

**Gap:** those sources do **not** define the detailed approved outcomes, exclusions, persistence model, API/UI surface, authorization grants, online/offline rules, or acceptance tests for `P10-WP01` alone. The phase page previously contained only generic stub text (“Implement only the approved scope described by the architecture and product documents”), which is insufficient for implementation under permanent workflow rules.

Decision record: [P10-WP01-scope-ambiguity.md](../reports/P10-WP01-scope-ambiguity.md)

## Work packages

### P10-WP01 — Suppliers

Status: **Not Started** — title confirmed; **approved scope not clarified** (implementation forbidden until authorized)

#### Approved scope (clarified)

**Not yet authorized.** Do not invent supplier domain behavior, schema, APIs, or UI from assumptions.

Pending authorization must define at least:

- Supplier aggregate fields and lifecycle (create/update/deactivate/archive)
- Organization isolation and authorization grant codes
- Relationship to purchasing / POs / receiving (in or out of this WP)
- Whether AP/payables, contacts-only master data, or both
- Online vs offline behavior
- Explicit exclusions (tax, refunds, advanced inventory, shifts, multi-register, Platform coupling, HealthCare)
- Tests and documentation evidence requirements

#### Proposed scope options (for authorization — not selected)

| Option | Summary |
|---|---|
| A — Supplier master data only | Org-scoped supplier records (identity/contact/status); **no** POs, receiving, or stock effects. Purchasing remains `P10-WP02`. |
| B — Suppliers + purchase-order stubs | Master data plus non-receiving PO draft/list without inventory posting. |
| C — Combined suppliers & purchasing slice | Merge early purchasing into WP01 (conflicts with existing `P10-WP02 — Purchasing` split). |
| D — Defer suppliers; reorder Phase 10 | Re-sequence if product priority differs (requires roadmap rewrite authorization). |

#### Explicit exclusions (provisional until scope approved)

- Purchasing/PO receiving unless option C is authorized
- Advanced inventory, shifts, returns/refunds, multi-register
- Tax/VAT, accounting/GL, payroll
- Production authentication (R-091) and POS operational roles unless separately authorized
- HealthCare changes; Platform database ownership of suppliers

#### Definition of Done

- [ ] Approved outcomes complete (after scope authorization).
- [ ] Applicable tests pass with exact evidence.
- [ ] Dashboard and phase page updated.
- [ ] Completion report created.
- [ ] Focused commit created and hash recorded.
- [ ] Working tree clean.
- [ ] Exact next WP recorded (do not begin until authorized).

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
