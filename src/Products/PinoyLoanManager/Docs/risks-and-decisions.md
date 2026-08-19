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
| R-091 | Risk | Production authentication missing | Open | No production-secure identity | Portfolio auth roadmap | Real Platform auth shipped + evidenced |
| D-P12-03 | Decision | Commercial-state transport to products | Open / provisional | How product learns subscription/entitlements without Platform table reads | Commercial/integration WP | Approved contract + implementation; no direct Platform EF/SQL |
| D-P12-05 | Decision | Honest Dev/Testing vs Production language | Open (tied to R-091) | Risk of claiming production-secure identity | With R-091 | Dev/Testing shortcuts labeled; Production fail-closed |

## Product register

| ID | Type | Description | Current state | Impact | Owner / decision point | Evidence | Resolution criteria |
|---|---|---|---|---|---|---|---|
| PLM-D-00-01 | Decision | Product code/slug `pinoy-loan-manager` | **Closed** | Catalog, plans, independent subscription | Product owner | [ADR-001](Decisions/ADR-001-product-identity-and-database-name.md) | Code approved for future Platform catalog registration; catalog row not created here |
| PLM-D-00-02 | Decision | Logical database name `ExItS_PinoyLoanManager` | **Closed for logical name**; creation/placement deferred | Persistence, migrations, operations | Product owner + architecture WP | [ADR-001](Decisions/ADR-001-product-identity-and-database-name.md) | Name approved. Database, schema, connections, partitions, stamps, backups, and migrations remain deferred |
| PLM-D-00-03 | Decision | Physical source/test/deploy layout beside `Docs/` | Open / Product Owner Decision Required | PLM-01 scaffold | Architecture WP | **Planning target** recorded (`ExItS.PinoyLoanManager.*`); projects **not** on `main`. Parked unmerged branch `feat/plm-01-scaffold` is **not** accepted mainline state | Authorized scaffold WP creates projects on mainline; still no Product Foundation change |
| PLM-D-00-04 | Decision | Generic Platform cross-product relationship model | Open / Product Owner Decision Required | Personal as POS Customer, Loan Borrower, and future product-specific relationships | Platform architecture WP — do not design in PLM | Conceptual diagram only; product behavior in ADR-002 | Approved Platform contract/schema, not invented here |
| PLM-D-00-05 | Decision | Personal-to-Borrower linking mechanism | Open / Product Owner Decision Required | Optional link, consent, no auto-link from EX ID / QR | PLM-04 + Platform | Product behavior defined (PLM-DOC-01); Platform transport, contract, persistence, and integration **not** designed | Approved linking/consent **implementation** design |
| PLM-D-00-06 | Decision | Loan roles and grants | Open / Product Owner Decision Required | PLM-03 and all operational WPs | Product owner | Presets, grant **catalog intent**, scope, SoD recorded; **identifiers** not final | Owner-approved identifiers; no role-name hard-coding; no implicit hierarchy |
| PLM-D-00-07 | Decision | Operational financial model | **Open / Partially Resolved** | Origination, payments, collections, cash | Product owner | Terminology, fee model, net proceeds, allocation baseline, money precision (PLM-DOC-02). Schema/GL/settlement/write-off/cash refund still open | Owner-approved money/ledger **schema** in this product; remaining accounting workflows |
| PLM-D-00-08 | Decision | Loan business/calculation rules | **Open / Partially Resolved** | PLM-05 through PLM-10 | Product owner | Methods, formulas, treatments, fees, allocation (PLM-DOC-02). Calendar, penalties, excused days, early-settlement rebate, restructuring, write-off still open | Remaining policy areas approved; no invented rates |
| PLM-D-00-09 | Decision | Web/MAUI component-sharing strategy | Open / Product Owner Decision Required | Client scaffold and PLM-13 | Architecture WP | Surface split recorded (full Org Web vs limited MAUI vs Personal presentation) | Approved sharing/isolation approach; no client project until authorized |
| PLM-D-00-10 | Decision | Product documentation baseline completion / owner approval | **Closed / Product Owner Accepted** | Closing PLM-00 | Product owner | PLM-00 WP01–WP10 completed; GitHub branch reviewed; documentation baseline accepted. Product implementation is deliberately paused while ExItS scale architecture and remaining PLM business/policy decisions are finalized | Owner accepted documentation baseline. Remaining financial calendar/penalty/legal and production decisions remain open. Implementation is not currently authorized |
| PLM-D-00-11 | Decision | External legal/compliance validation | Open / Product Owner Decision Required | Production use | Product owner + external counsel | No rates/workflows claimed compliant. Effective-cost/EIR/APR formula, required documents, terminology, timing, rounding, and consumer presentation require qualified review | Written legal/compliance validation before Production |
| PLM-D-00-12 | Decision | Exact money rounding mode | **Closed** | Calculation engine | Product owner + accounting | [ADR-004](Decisions/ADR-004-rounding-fees-and-payment-allocation.md) | PHP 2 dp posted; ≥8 intermediate; midpoint To Even; final-installment reconciliation |
| PLM-D-00-13 | Decision | Small-org vs two-person high-risk approval | Open / Product Owner Decision Required | Operational SoD | Product owner | Multiple presets on one person allowed; high-risk self-approval still restricted where required | Explicit policy for which actions may never be self-approved |

## Accepted engineering / planning baselines (WP04)

These are **planning baselines**, not legal approval and not implementation. PLM-DOC-02 **partially resolves** PLM-D-00-07 / PLM-D-00-08 and **closes** PLM-D-00-12. Remaining calendar, penalty, settlement-rebate, schema, and legal items stay open. No default rates.

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

These are **planning baselines**, not legal approval and not implementation. They do **not** close grant identifiers, custom roles, Cashier close-with-variance policy, cash-refund workflow, or offline posting design.

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

These are **planning baselines**, not legal approval, KYC sufficiency, or implementation. They do **not** close PLM-D-00-04 / PLM-D-00-05 schema.

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

- organization dashboard indicators are operational, not finalized KPI formulas
- reporting covers Loans, collections, operational financials, cash operations, borrowers, and audit
- PAR / accounting formulas are not defined here
- documents may be issued from snapshotted terms
- posted payment has durable receipt identity independent of print success
- notifications must not roll back posted financial events
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
- MAUI online/server-authoritative initially; offline financial posting not authorized
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

Direction in WP03–WP10 docs does **not** close these. Tracked primarily under PLM-D-00-04, PLM-D-00-05, PLM-D-00-06, PLM-D-00-07 (remainder), PLM-D-00-08 (remainder), PLM-D-00-11, PLM-D-00-13, and D-P12-03:

- exact grant identifiers
- custom-role support / version
- whether high-risk actions require two distinct human users for all organization sizes (PLM-D-00-13)
- exact Cashier Session closing rules with unresolved variance
- exact collector acknowledgement mechanism for float
- cash vault / branch treasury architecture
- exact cash refund workflow
- exact payment reversal approval threshold
- exact disbursement cancellation / reversal workflow
- mobile offline financial behavior
- route planning / GPS requirements
- collector device security
- receipt numbering / format
- report KPI / PAR formulas
- document legal sufficiency
- notification provider / channel selection
- audit retention schedule
- accounting / GL integration
- exact traditional loan assessment criteria / approval limits
- exact Traditional mandatory application fields
- exact revision/reapproval workflow before disbursement
- exact Traditional document/condition checklist for disbursement
- default or maximum interest rates (not defined; never invent)
- exact penalty basis / rates / caps (**PLM-DOC-03**)
- exact schedule behavior after excused collection day (**PLM-DOC-03**)
- exact future-interest treatment on early settlement
- restructuring calculations
- write-off accounting treatment
- due-date generation details (**PLM-DOC-03**)
- Personal / Loan API shape
- who may initiate unlink
- pending Quick Loan offer treatment after unlink
- historical Personal visibility after unlink
- re-linking and consent-history rules
- duplicate-borrower detection
- required KYC fields
- Platform usage-charge transport (D-P12-03)
- regulatory / legal validation (PLM-D-00-11), including effective-cost/disclosure formula

## Loan policy subjects (do not invent)

Tracked under **PLM-D-00-08** unless noted. Remaining items (do **not** invent):

- due-date generation (**PLM-DOC-03**)
- loan types beyond the two origination *paths*
- exact future-interest treatment on early settlement
- exact schedule behavior after excused collection day (**PLM-DOC-03**)
- grace-period **N** (concept recorded; not a Platform default)
- penalty basis/rates/caps (concepts recorded; no amount/rate — **PLM-DOC-03**)
- restructuring calculations
- write-off accounting treatment
- full accounting/GL integration (PLM-D-00-07 remainder)
- legal/regulatory operating rules (PLM-D-00-11)
- default or maximum interest rates / fee amounts (never invent)

**Resolved in PLM-DOC-02** (not legal approval): MVP methods and formulas, rate bases, added vs deducted interest, fee model, oldest-due allocation, component order, partial/multiple/advance/overpayment, money precision, rounding (PLM-D-00-12 Closed).

Do **not** close remaining items by guessing. Do **not** claim legal compliance.

## Instructions

- Prefer stable IDs (`R-…`, `D-…`, `PLM-D-…`).
- “Closed” requires repository or operator evidence plus explicit approval.
- Unresolved policy in approved docs must appear here as open decisions.
- Do not close PLM-D-00-03, PLM-D-00-04 through PLM-D-00-09, PLM-D-00-11, PLM-D-00-13, D-P12-03, R-091, or D-P12-05 without explicit approval. PLM-D-00-07 and PLM-D-00-08 remain **Open / Partially Resolved** — do not mark them Closed.
- PLM-D-00-01 is **Closed** (`pinoy-loan-manager`). PLM-D-00-02 is **Closed for logical database name** only. PLM-D-00-10 is **Closed / Product Owner Accepted** (documentation baseline only). PLM-D-00-12 is **Closed** (To Even; PHP 2 dp; ≥8 intermediate).
- ADRs: [Decisions/ADR-001-product-identity-and-database-name.md](Decisions/ADR-001-product-identity-and-database-name.md), [Decisions/ADR-002-borrower-personal-cardinality-and-consent.md](Decisions/ADR-002-borrower-personal-cardinality-and-consent.md), [Decisions/ADR-003-supported-interest-and-schedule-methods.md](Decisions/ADR-003-supported-interest-and-schedule-methods.md), [Decisions/ADR-004-rounding-fees-and-payment-allocation.md](Decisions/ADR-004-rounding-fees-and-payment-allocation.md).
