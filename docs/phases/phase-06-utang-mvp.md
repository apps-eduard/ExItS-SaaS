# Phase 6 — Utang MVP

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-05-pos-maui-foundation.md) | [Next](phase-07-offline-sync.md)

## Objective

Deliver the customer-facing Utang product.

## Status

**In Progress** — P6-WP01 **Complete with documented risks**. Do **not** begin P6-WP02 until explicitly authorized.

## Work packages

### P6-WP01 — Customers

Status: **Complete with documented risks**

Feature commit: _(recorded after push)_
Report: [P6-WP01-customers.md](../reports/P6-WP01-customers.md)

#### Approved scope (clarified)

Organization-isolated PinoyBusinessPOS **customer management only**:

- POS customer domain model (`POSCustomer` / `POSCustomerId`)
- Separate POS PostgreSQL database (`ExItS_PinoyBusinessPOS`, schema `pos`)
- Focused migration `AddPosCustomers`
- Create, update permitted profile fields, get, paginated list, search
- Activate/deactivate (soft lifecycle; no physical delete)
- MAUI Customers list, create, edit, detail
- Validation, tests, documentation, runtime evidence
- Phase marker `P6-WP01-customers`

#### Customer fields (MVP)

CustomerId, OrganizationId, DisplayName (required), optional mobile, optional address/location, optional general notes (not credit), Status, CreatedUtc, UpdatedUtc, concurrency token.

#### Duplicate policy (MVP)

Display names need not be unique. When mobile is present, prevent duplicate **active** customers with the same normalized mobile inside the same organization. Same mobile may exist in another organization. Stable conflict response.

#### Explicit exclusions (later WPs)

Credit accounts, remarks-based credit, balances, ledger, repayments, due dates, statements, receipts, credit limits, interest/penalties, sales, inventory, offline sync. OD-07/OD-08 remain open.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence.
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded.
- [x] Working tree clean.

### P6-WP02 — Remarks-Based Credit

Status: Not Started — **do not begin**

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

### P6-WP03 — Payments and Ledger

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

### P6-WP04 — Due Dates and Overdue Monitoring

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

### P6-WP05 — Statements, Receipts and Trial Rules

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

### P6-WP06 — Utang MVP Closeout

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
