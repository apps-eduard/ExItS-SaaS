# Pinoy Loan Manager — Risks and Decisions

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)
> Close items only with evidence. Do not invent answers for portfolio-open items.

| Field | Value |
|---|---|
| Product | Pinoy Loan Manager |
| Last updated | 2026-08-19 |

## Portfolio items (always preserve until closed upstream)

| ID | Type | Description | Current state | Impact | Decision point | Resolution criteria |
|---|---|---|---|---|---|---|
| R-091 | Risk | Production authentication missing | **Closed for Phase 13 scope** | Residual MFA/SSO/email/step-up; portfolio Production readiness | Portfolio auth ([P13-WP09](../../../../docs/reports/P13-WP09-phase-13-closeout.md)) | Password credentials, browser sessions, lifecycle tokens, org context, product Bearer, Google/Facebook, recovery email delivered | Residual auth hardening and portfolio Production gates do not reopen R-091 |
| D-P12-03 | Decision | Commercial-state transport to products | Open / provisional | How product learns subscription/entitlements without Platform table reads | Commercial/integration WP | Approved contract + implementation; no direct Platform EF/SQL |
| D-P12-05 | Decision | Honest Dev/Testing vs Production language | **Closed / satisfied for Phase 13 authentication-honesty scope** | Risk of claiming portfolio Production Ready | With R-091 Phase 13 closeout | Dev/Testing shortcuts labeled; Production fail-closed; portfolio still not Production Ready | Portfolio Production readiness gates |

## Product register

| ID | Type | Description | Current state | Impact | Owner / decision point | Evidence | Resolution criteria |
|---|---|---|---|---|---|---|---|
| PLM-D-00-01 | Decision | Product code/slug `pinoy-loan-manager` | **Closed** | Catalog, plans, independent subscription | Product owner | [ADR-001](Decisions/ADR-001-product-identity-and-database-name.md) | Code approved for future Platform catalog registration; catalog row not created here |
| PLM-D-00-02 | Decision | Logical database name `ExItS_PinoyLoanManager` | **Closed for logical name**; creation/placement deferred | Persistence, migrations, operations | Product owner + architecture WP | [ADR-001](Decisions/ADR-001-product-identity-and-database-name.md) | Name approved. Database, schema, connections, partitions, stamps, backups, and migrations remain deferred |
| PLM-D-00-03 | Decision | Physical source/test/deploy layout beside `Docs/` | **Closed for approved target architecture/layout** | PLM-01 scaffold | Architecture WP | [Architecture/source-and-project-layout.md](Architecture/source-and-project-layout.md); [implementation-gates.md](implementation-gates.md) (PLM-DOC-11). Projects **not** on `main`. Parked `feat/plm-01-scaffold` is **not** accepted mainline state | Layout decision complete. Implementation requires Gate A authorization; fresh scaffold or careful rebuild — do not blindly merge parked branch |
| PLM-D-00-04 | Decision | Generic Platform cross-product relationship model | **Open / External Platform dependency** | Personal as POS Customer, Loan Borrower, and future product-specific relationships | Platform architecture WP — do not design in PLM | Conceptual diagram only; PLM product behavior and contracts in ADR-002, PLM-DOC-10; **not an unresolved PLM business rule** | Approved Platform contract/schema implementation |
| PLM-D-00-05 | Decision | Personal-to-Borrower linking mechanism | **Closed for PLM behavior/contract requirements**; Platform implementation **external** | Optional link, consent, no auto-link from EX ID / QR | PLM-04 + Platform | [ADR-019](Decisions/ADR-019-platform-personal-contract-requirements.md); [Architecture/personal-link-and-consent-contract.md](Architecture/personal-link-and-consent-contract.md); [Architecture/personal-facing-loan-api-contract.md](Architecture/personal-facing-loan-api-contract.md) (PLM-DOC-10) | PLM product contract requirements defined. Platform transport, persistence, APIs, and integration implementation remain External Platform work |
| PLM-D-00-06 | Decision | Loan roles and grants | **Closed for MVP** | PLM-03 and all operational WPs | Product owner | [ADR-009](Decisions/ADR-009-role-codes-grant-catalog-and-default-presets.md); [Security/authorization-grant-catalog.md](Security/authorization-grant-catalog.md); [authorization-matrix.md](authorization-matrix.md) (PLM-DOC-05) | Role codes, grant catalog v1, default presets, scope model, assignment lifecycle, and no custom roles in MVP. Custom roles deferred to future explicit decision |
| PLM-D-00-07 | Decision | Operational financial model | **Closed for MVP Product operational financial model** | Origination, payments, collections, cash | Product owner | PLM-DOC-02 through PLM-DOC-06, PLM-DOC-08. Operational Loan subledger, Cash Accountability, settlement/rebate/refund/reversal/variance, Write-Off/Recovery product behavior, GL boundary documented | Concrete persistence schema, journal/export contract, and external GL integration are **implementation/integration work**, not unresolved Product policy |
| PLM-D-00-08 | Decision | Loan business/calculation rules | **Closed for MVP Product business/calculation policy** | PLM-05 through PLM-10 | Product owner | Methods/fees/allocation (PLM-DOC-02). Calendar, DPD, penalties, maturity (PLM-DOC-03). Settlement, prepayment, refunds, reversals (PLM-DOC-04). Restructuring, Write-Off, Recovery, PTP, Collection Case (PLM-DOC-06). Default numeric rates/fees/penalties remain organization configuration subject to PLM-D-00-11 | MVP product calculation and business rules approved; no default numeric pricing; legal validation and schema remain external/deferred |
| PLM-D-00-09 | Decision | Web/MAUI component-sharing strategy | **Closed** | Client scaffold and PLM-13 | Product owner | [Architecture/web-maui-component-sharing-policy.md](Architecture/web-maui-component-sharing-policy.md); [Decisions/ADR-018-branch-treasury-float-and-ui-sharing-policy.md](Decisions/ADR-018-branch-treasury-float-and-ui-sharing-policy.md) (PLM-DOC-09) | Approved sharing/isolation approach; separate Web and MAUI UI; conditional future RCL; no client project until Gate A and owner authorization |
| PLM-D-00-10 | Decision | Product documentation baseline completion / owner approval | **Closed / Product Owner Accepted** | Closing PLM-00 | Product owner | PLM-00 WP01–WP10 completed; GitHub branch reviewed; documentation baseline accepted. Product implementation is deliberately paused while ExItS scale architecture and remaining PLM business/policy decisions are finalized | Owner accepted documentation baseline. Remaining legal and production decisions remain open. Implementation is not currently authorized |
| PLM-D-00-11 | Decision | External legal/compliance validation | **Open / External legal-compliance gate** | Production use | Product owner + external counsel | No rates/workflows claimed compliant. All consumer-facing and collections practices require qualified review | Written legal/compliance validation before Production |
| PLM-D-00-12 | Decision | Exact money rounding mode | **Closed** | Calculation engine | Product owner + accounting | [ADR-004](Decisions/ADR-004-rounding-fees-and-payment-allocation.md) | PHP 2 dp posted; ≥8 intermediate; midpoint To Even; final-installment reconciliation |
| PLM-D-00-13 | Decision | Small-org vs two-person high-risk approval | **Closed** | Operational SoD | Product owner | [ADR-008](Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md); [Security/privileged-access-and-owner-recovery-policy.md](Security/privileged-access-and-owner-recovery-policy.md) | Maker/checker required when another eligible approver exists; controlled Owner Override only for sole eligible Owner with enhanced audit and later review; Collector/Cashier restrictions preserved |

## Accepted engineering / planning baselines (WP04)

These are **planning baselines**, not legal approval and not implementation. PLM-DOC-02 through PLM-DOC-06 **close PLM-D-00-08 for MVP Product business/calculation policy**, **close PLM-D-00-12**, and **close PLM-D-00-13**. PLM-DOC-05 **closes PLM-D-00-06 for MVP**. Remaining schema, journal/export, external GL, custom roles (deferred), and legal items stay open. No default rates or penalty amounts.

- partial payments supported
- multiple payments supported
- deterministic allocation required
- oldest due obligation first (approved schedule-level allocation)
- component order Interest → Principal → Fees → Penalties (MVP, not org-editable)
- true overpayment does not create a general customer wallet in MVP
- financial history append-only / auditable in effect
- loan ledger separate from collector cash ledger
- approval separate from disbursement
- delinquency / collection condition separate from main Loan lifecycle
- penalty-on-penalty **OFF** by engineering default
- penalty-cap **capability** required
- disbursement is preferred Platform usage-billing event
- Traditional and Quick Loan converge into one financial core
- decimal money arithmetic; no binary float for authoritative money
- PHP posted 2 decimal places; intermediate ≥8; midpoint To Even
- Principal, Net Proceeds, and Total Scheduled Repayment are not assumed identical
- Quick Loan MVP: Flat/Add-On only; Traditional: Flat/Add-On or Reducing-Balance Equal-Installment
- deducted finance charge is not scheduled twice

## Accepted engineering / planning baselines (WP05)

These are **planning baselines**, not legal approval and not implementation. Grant identifiers and default presets are accepted in PLM-DOC-05. Custom roles deferred. Offline **final posting** deferred (PLM-DOC-09); read-only cache and offline drafts resolved for MVP planning. Cashier close-with-variance, cash-refund workflow, and PLM-D-00-13 maker/checker are accepted in PLM-DOC-04.

- default roles = Owner, Manager, Cashier, Collector
- role presets backed by explicit grants
- no implicit role hierarchy
- server-authoritative authorization
- multi-branch / resource scope supported
- approval and disbursement separate
- Collector cannot approve own Loan
- Collector cannot approve own waiver
- Collector cannot resolve own cash variance
- Cashier does not normally approve Loans
- Cashier Session concept
- Collector daily cash accountability
- opening / additional float recorded separately
- collected-funds reuse configurable, default OFF
- office and field disbursement supported
- office and field cash payment supported
- partial remittance supported
- end-of-day reconciliation required
- unresolved cash variance must remain visible
- Loan reversal separate from physical cash refund
- financial events are not silently deleted
- server remains authoritative for future offline financial posting

## Accepted engineering / planning baselines (WP06)

These are **planning baselines**, not legal approval, KYC sufficiency, or implementation. They do **not** close PLM-D-00-04 Platform schema.

- Borrower is PLM-owned and may exist without ExItS Personal
- Borrower identity does not depend on POS Customer or another product
- POS Customer ≠ PLM Borrower
- linking is optional; EX ID / QR never auto-links
- explicit Personal consent is required for an active link
- decline / unlink does not delete Borrower, Loan, or payment history
- unlink changes Personal access/relationship only
- Personal must not query PLM tables
- publishing does not create a Loan
- “all” publishing means eligible linked borrowers of that organization, never all ExItS users
- eligibility ≠ approval
- default maximum active Quick Loans = 1 per borrower per organization (configurable)
- manual approval remains default; no auto-approval
- borrower groups are organization-owned; no built-in mandatory groups

## Accepted engineering / planning baselines (WP07)

These are **planning baselines**, not legal approval and not implementation.

- Traditional and Quick Loan remain separate origination experiences
- both converge into one financial core after disbursement
- Traditional flow conceptually Draft → Submitted → Under Review → Approved/Rejected → Awaiting Disbursement → Disbursed → Active
- cancellation/expiry concepts supported; not deletion
- Loan Product is configuration, not a Loan
- manual approval baseline; applicant cannot self-approve; Collector cannot approve; Cashier does not normally approve
- approval snapshots terms; no silent post-approval edits
- material term change before disbursement requires revision/reapproval or cancellation/new approval
- rejected applications remain historically visible with a reason
- disbursement readiness checks are required conceptually before release
- approval ≠ disbursement

## Accepted engineering / planning baselines (WP08)

These are **planning baselines**, not legal forms and not implementation.

- organization dashboard indicators use approved operational KPI formulas (PLM-DOC-08)
- reporting covers Loans, collections, operational financials, cash operations, borrowers, and audit
- PAR / aging formulas approved as operational metrics, not statutory accounting (PLM-DOC-08)
- documents may be issued from snapshotted terms with versioned templates (PLM-DOC-08)
- posted financial events have durable receipt identity independent of print success (PLM-DOC-08)
- notifications must not roll back posted financial events (PLM-DOC-08)
- Personal Loan area is distinct from any Personal P2P “I Lent / I Borrowed” feature
- audit/high-risk history is not ordinary editable notes

## Accepted engineering / planning baselines (WP09)

These are **planning targets**, not created projects.

- future layout: `ExItS.PinoyLoanManager.{Domain,Application,Infrastructure,Api,ApiClient,Web,Maui}` under `src/Products/PinoyLoanManager/` plus `Docs/`
- LocalStore only if/when justified
- Domain persistence-independent; Application must not reference Infrastructure
- no project may reference POS
- separate logical database `ExItS_PinoyLoanManager` (name Closed; not created)
- Personal uses PLM APIs, never PLM tables
- MAUI online/server-authoritative initially; offline read-only cache and drafts allowed in planning; offline final financial posting not authorized (PLM-DOC-09)
- D-P12-03 remains open; no shared DB integration
- follow existing ExItS technology direction; no new framework

## Accepted engineering / planning baselines (WP10)

- PLM-00 documentation phase is complete as planning
- implementation classified into gates A (scaffold), B (early domain), C (financial engine), D (production)
- **PLM-D-00-10 Closed / Product Owner Accepted** — documentation baseline accepted
- Product Owner acceptance does **not** approve unresolved rates, formulas, legal compliance, or production readiness
- Product implementation is **deliberately paused** while ExItS scale architecture and remaining PLM business/policy decisions are finalized
- `feat/plm-01-scaffold` is an **unmerged parked** implementation branch and is **not** part of accepted mainline product state. Do not merge or delete it from this documentation package. Do not treat it as evidence to close PLM-D-00-03

## Operating-model, calculation, and operational open areas (do not invent)

Direction in WP03–WP10 docs does **not** close these. Tracked primarily under PLM-D-00-04, PLM-D-00-07 (remainder), PLM-D-00-08 (remainder), PLM-D-00-11, and D-P12-03:

- custom-role support (deferred; **not** MVP — PLM-D-00-06 Closed for MVP presets/grants)
- document legal sufficiency (PLM-D-00-11)
- exact legally mandated receipt/document format (PLM-D-00-11)
- numeric retention durations (PLM-D-00-11)
- notification provider integration (product channel direction resolved PLM-DOC-08; provider Open)
- accounting / GL integration
- exact traditional loan assessment criteria / approval limits
- exact Traditional mandatory application fields
- exact revision/reapproval workflow before disbursement
- exact Traditional document/condition checklist for disbursement
- default or maximum interest rates (not defined; never invent)
- exact penalty **rates**/amounts/caps as numbers (engine accepted; no defaults — PLM-DOC-03)
- restructuring calculations (**resolved PLM-DOC-06**)
- write-off product behavior (**resolved PLM-DOC-06**; GL projection open)
- duplicate-borrower detection
- required KYC fields
- Platform usage-charge transport (D-P12-03)
- regulatory / legal validation (PLM-D-00-11), including effective-cost/disclosure formula

## Loan policy subjects (do not invent)

Tracked under **PLM-D-00-08** unless noted. Remaining items (do **not** invent):

- loan types beyond the two origination *paths*
- restructuring calculations (**PLM-DOC-06**)
- write-off accounting treatment (**PLM-DOC-06** product behavior; GL projection open)
- Recovery treatment (**PLM-DOC-06**)
- full accounting/GL integration details (PLM-D-00-07 remainder)
- legal/regulatory operating rules (PLM-D-00-11)
- default or maximum interest rates / fee amounts / penalty amounts (never invent)

**Resolved in PLM-DOC-02** (not legal approval): MVP methods and formulas, rate bases, added vs deducted interest, fee model, oldest-due allocation, component order, partial/multiple/advance/overpayment, money precision, rounding (PLM-D-00-12 Closed).

**Resolved in PLM-DOC-03** (not legal approval): frequencies, collection calendar, first due dates, Following Valid Collection Day, month-end rule, DPD, missed-day counter, grace semantics, penalty types/bases/cap requirement, exception policies and defaults, maturity and post-maturity modes.

**Resolved in PLM-DOC-04** (not legal approval): Settlement Quote, settlement formula, Flat/Add-On earned vs unearned rebate, deducted-charge rebate credit, reducing-balance current-period accrual, partial principal prepayment, Reduce Term default, no MVP settlement/prepayment penalty, Refund Payable, payment/disbursement reversal boundaries, cash variance close/resolution, maker/checker and Owner Override (**PLM-D-00-13 Closed**), operational Loan subledger vs Cash Accountability vs GL boundary.

**Resolved in PLM-DOC-05** (not legal/security-production approval): role codes (`plm.owner`, `plm.manager`, `plm.cashier`, `plm.collector`), PLM Authorization Policy v1 grant catalog, default preset matrix, multiple-role union, scope model, workflow guards, data minimization, first-Owner bootstrap direction, last-Owner protection, no self-escalation, Platform recovery boundary, high-risk audit catalog (**PLM-D-00-06 Closed for MVP**).

**Resolved in PLM-DOC-06** (not legal approval): restructuring (same Loan, new schedule version, component treatment, Refinancing deferred), Write-Off classification and post-write-off behavior, Recovery Payment and allocation, Promise to Pay, Collection Case, collection conduct boundaries (**PLM-D-00-08 Closed for MVP Product business/calculation policy**).

**Resolved in PLM-DOC-07** (not legal approval): natural-person Borrower minimum, Traditional/Quick application minimums, manual assessment, approval scope without per-user limits, material reapproval, approval expiry, Disbursement readiness checklist, borrower acknowledgment content.

**Resolved in PLM-DOC-08** (not legal approval): authoritative document catalog, durable receipt identity, template versioning, account statement component breakdown, GROSS OUTSTANDING PRINCIPAL / PAST-DUE SCHEDULED AMOUNT / COLLECTION RATE / PAR-X formulas, PAR 1/7/30/60/90, aging buckets Current/1–7/8–30/31–60/61–90/91+, scope-filtered report catalog, Personal primary notification channel and optional SMS/email/push direction, delivery-does-not-change-financial-state, data classification PUBLIC/INTERNAL/CONFIDENTIAL/HIGHLY SENSITIVE, retention architecture (no numeric periods), audit coverage catalog, privacy/support boundaries.

**Resolved in PLM-DOC-09** (not legal approval; not implementation): MAUI limited field purpose; MVP online/server authority; offline read-only cache and offline drafts; offline final posting deferred; assignment-based routes without auto optimization; optional event-based GPS with policy/permission/disclosure and no continuous tracking; Branch Treasury concept; Cashier Session funded from treasury; collector float two-step Pending Receipt acknowledgment; Web/MAUI component sharing (**PLM-D-00-09 Closed**); future collector device security requirements only (**no implemented security claim**).

**Resolved in PLM-DOC-10** (not Platform implementation): Platform access context facts; Personal link/consent contract operations and facts; Personal-facing loan API operation groups; unlink/pending-offer/relink product-contract rules (**PLM-D-00-05 Closed for PLM behavior/contract**); usage metering event types and idempotency; tenant placement abstraction. **PLM-D-00-11** remains Open for post-unlink legal visibility basis.

Do **not** close remaining items by guessing.

## Instructions

- Prefer stable IDs (`R-…`, `D-…`, `PLM-D-…`).
- “Closed” requires repository or operator evidence plus explicit approval.
- Unresolved policy in approved docs must appear here as open decisions.
- Do not close PLM-D-00-04, PLM-D-00-11, or D-P12-03 without explicit approval. **R-091 Closed for Phase 13 scope.** **D-P12-05 Closed / satisfied for authentication honesty.** **PLM-D-00-03 Closed for approved layout.** **PLM-D-00-05 Closed for PLM behavior/contract.** **PLM-D-00-07 Closed for MVP Product operational financial model.** **PLM-D-00-08 Closed for MVP Product business/calculation policy.** **PLM-D-00-09 Closed.** Persistence schema and external accounting integration remain implementation work.
- PLM-D-00-01 is **Closed** (`pinoy-loan-manager`). PLM-D-00-02 is **Closed for logical database name** only. PLM-D-00-06 is **Closed for MVP** (role codes, grant catalog v1, default presets; custom roles deferred). PLM-D-00-09 is **Closed** (Web/MAUI component-sharing policy). PLM-D-00-10 is **Closed / Product Owner Accepted** (documentation baseline only). PLM-D-00-12 is **Closed** (To Even; PHP 2 dp; ≥8 intermediate). PLM-D-00-13 is **Closed** (maker/checker + controlled Owner Override).
- ADRs: [Decisions/ADR-001-product-identity-and-database-name.md](Decisions/ADR-001-product-identity-and-database-name.md) through [Decisions/ADR-020-usage-metering-and-tenant-placement-contracts.md](Decisions/ADR-020-usage-metering-and-tenant-placement-contracts.md).
