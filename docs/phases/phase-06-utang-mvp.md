# Phase 6 — Utang MVP

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-05-pos-maui-foundation.md) | [Next](phase-07-offline-sync.md)

## Objective

Deliver the customer-facing Utang product.

## Status

**Complete** — P6-WP01 through P6-WP06 complete. Phase 6 Utang MVP closed with documented risks. Do **not** begin Phase 7 until explicitly authorized.

## Work packages

### P6-WP01 — Customers

Status: **Complete with documented risks**

Feature commit: `674ad0660b0bd11bca75f2e90e329c4579ff592a`
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

Credit accounts, remarks-based credit, balances, ledger, repayments, due dates, statements, receipts, credit limits, interest/penalties, sales, inventory, offline sync. OD-07/OD-08/OD-09 resolved in P6-WP05.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence.
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded.
- [x] Working tree clean.

### P6-WP02 — Remarks-Based Credit

Status: **Complete with documented risks**

Feature commit: `ead6942187ca9a9c507dcf706bbece2e507a8645`
Report: [P6-WP02-remarks-based-credit.md](../reports/P6-WP02-remarks-based-credit.md)

#### Approved scope

Organization-owned remarks-based customer credit entries only:

- Positive decimal amount
- Required plain-text remarks
- Active customer required to create credit
- Append-only history (no edit/delete of recorded credit)
- Explicit reversal with required reason
- Derived outstanding amount from **active** credit entries only
- Customer credit summary and history
- POS API + MAUI workflows
- PostgreSQL migration `AddPosCreditEntries`
- Organization isolation
- Tests, documentation, Android evidence
- Phase marker `P6-WP02-remarks-based-credit`

#### Explicit exclusions (later WPs)

Repayments, payment allocation, full payment ledger, due dates, statements, receipts, interest, penalties, credit limits, sales, inventory, gateways, QR/cards, offline sync. Platform SaaS payments remain separate. OD-07/OD-08/OD-09 resolved in P6-WP05.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence.
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded.
- [x] Working tree clean.

### P6-WP03 — Payments and Ledger

Status: **Complete with documented risks**

Feature commit: `de39091f6110acbc721ac78da51a92acefd6775a`
Report: [P6-WP03-payments-and-ledger.md](../reports/P6-WP03-payments-and-ledger.md)

#### Approved scope

Organization-owned customer repayments and a unified read-only Utang ledger:

- Positive decimal repayment amount (≤2 decimal places)
- Optional remarks/reference
- Customer must exist (inactive customers may repay existing debt)
- Append-only repayments; explicit reversal with required reason
- Overpayment blocked (outstanding cannot go negative from an active repayment)
- Unified chronological ledger (credits + repayments) as a read model
- Derived outstanding = active credits − active repayments
- POS API + MAUI workflows
- PostgreSQL migration `AddPosRepayments`
- Organization isolation
- Tests, documentation, Android evidence
- Phase marker `P6-WP03-payments-and-ledger`

#### Explicit exclusions (later WPs)

Due dates, statements, printable receipts, trial-expiry behavior, interest, penalties, credit limits, write-offs, installments, sales, inventory, gateways, QR/cards, offline sync. Platform SaaS payments remain separate. OD-07/OD-08/OD-09 resolved in P6-WP05.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence.
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded.
- [x] Working tree clean.

### P6-WP04 — Due Dates and Overdue Monitoring

Status: **Complete with documented risks**

Feature commit: `9947d95cba27c8311091f95ea51c79be1de0acb9`
Report: [P6-WP04-due-dates-and-overdue-monitoring.md](../reports/P6-WP04-due-dates-and-overdue-monitoring.md)

#### Approved scope

Due dates on credit entries and derived overdue monitoring:

- Optional calendar due date per credit entry
- Append-only due-date change history (reason, actor, UTC)
- Current due date denormalized on credit for efficient reads
- FIFO aging of active repayments onto active credits (read model only)
- Overdue derived from unpaid FIFO remainder + due date before effective UTC date
- Customer overdue summary and org overdue lists
- POS API + MAUI workflows
- PostgreSQL migration `AddPosCreditDueDates`
- Organization isolation
- Tests, documentation, Android evidence
- Phase marker `P6-WP04-due-dates-and-overdue-monitoring`

#### Explicit exclusions (later WPs)

Statements, printable receipts, trial-expiry rules, interest, penalties, credit limits, write-offs, installments, sales, inventory, gateways, QR/cards, offline sync, payment-allocation persistence. Platform SaaS payments remain separate. OD-07/OD-08/OD-09 resolved in P6-WP05.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence.
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded.
- [x] Working tree clean.

### P6-WP05 — Statements, Receipts and Trial Rules

Status: **Complete with documented risks**

Feature commit: `271c518cb8c4051502d6370ec71e6498fbbfd6b5`
Report: [P6-WP05-statements-receipts-and-trial-rules.md](../reports/P6-WP05-statements-receipts-and-trial-rules.md)

#### Approved scope

Statements, repayment receipts, trial/continuity commercial rules, and MAUI preview/handoff:

- Projection-based customer statements (period opening/closing, ledger lines, due/overdue presentation)
- Projection-based repayment receipts with deterministic `RCPT-{guid:N}` reference
- Centralized `UtangCapabilityPolicy` matrix; product entry vs feature authorization separation
- Platform `ProductAccessEligibility.CanEnterPinoyBusinessPos` continuity for PastDue/Cancelled/Expired (POS only); Suspended denies
- Development-stage headers `X-Pos-Subscription-Status`, `X-Pos-Feature-Grants`
- POS API + MAUI statement/receipt preview and share handoff; EN + `fil-PH`
- OD-07 / OD-08 / OD-09 resolved
- No new migration for receipts/statements
- Phase marker `P6-WP05-statements-receipts-and-trial-rules`

#### Explicit exclusions (later / out of scope)

Interest, penalties, credit limits, sales, inventory, gateways, tax invoices, offline sync, payment-allocation persistence. Platform SaaS payments remain separate.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (full solution **541** passed / 0 failed / 0 skipped).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded.
- [x] Working tree clean (pending docs finalize push).

### P6-WP06 — Utang MVP Closeout

Status: **Complete with documented risks**

Feature commit: `9f33420f5f77bade398db6d59728ad9def895683`
Report: [P6-WP06-utang-mvp-closeout.md](../reports/P6-WP06-utang-mvp-closeout.md)

#### Goal

Close Phase 6 Utang MVP by reconciling delivered capability, hardening confirmed closeout defects, and documenting evidence — without starting Phase 7.

#### Closeout outcomes

**Reconcile**

- OD-07 / OD-08 / OD-09 recorded as resolved in P6-WP05; historical WP report drift corrected
- P6-WP01–P6-WP05 delivered capability reconciled against architecture and exclusions
- portfolio independence verification and Platform / product boundaries confirmed

**Harden**

- Production commercial headers ignored (fail closed outside Development/Testing)
- fil-PH statement/receipt localization and localized share/disclaimer text
- Full Utang lifecycle API test and Phase 6 migration-chain test
- Full solution **544** passed / 0 failed / 0 skipped; Android Release APK built
- Organization isolation, commercial capability gates, and architecture boundaries preserved

**Document**

- Completion report and phase exit criteria
- Phase marker `P6-WP06-utang-mvp-closeout`
- Phase 7 not started

#### Explicit exclusions

Interest, penalties, credit limits, sales, inventory, gateways, tax invoices, offline sync (Phase 7), payment-allocation persistence, production authentication. Platform SaaS payments remain separate. Not production-ready.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (full solution **544** passed / 0 failed / 0 skipped).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded.
- [x] Working tree clean.

## Phase exit criteria

- [x] Every work package is complete or explicitly deferred.
- [x] Risks and decisions are recorded.
- [x] Required regression/security tests pass.
- [ ] Next phase is explicitly approved (Phase 7 — Offline Sync when authorized).
