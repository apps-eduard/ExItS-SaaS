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
| PLM-D-00-01 | Decision | Product code/slug final registration (`pinoy-loan-manager` proposed) | Open / Product Owner Decision Required | Catalog, plans, independent subscription | Product owner + Platform catalog WP | None | Slug registered; docs match |
| PLM-D-00-02 | Decision | Database name final approval (`ExItS_PinoyLoanManager` proposed); schema unset | Open / Product Owner Decision Required | Persistence, migrations, operations | Product owner + architecture WP | None | Name/schema approved; database still not created until an authorized persistence WP |
| PLM-D-00-03 | Decision | Physical source/test/deploy layout beside `Docs/` | **Closed** | PLM-01 scaffold | Architecture WP | Scaffold created `ExItS.PinoyLoanManager.{Domain,Application,Infrastructure,Api,ApiClient,Web}` plus unit tests; MAUI/LocalStore not created | Layout proven by scaffold. Preferred client path later closed by PLM-D-00-09 (React + PWA + Capacitor; MAUI not preferred). |
| PLM-D-00-04 | Decision | Generic Platform cross-product relationship model | Open / Product Owner Decision Required | Personal as POS Customer, Loan Borrower, and future BNPL Customer | Platform architecture WP — do not design in PLM | Conceptual diagram only | Approved Platform contract/schema, not invented here |
| PLM-D-00-05 | Decision | Personal-to-Borrower linking mechanism | Open / Product Owner Decision Required | Optional link, consent, no auto-link from EX ID / QR | PLM-04 + Platform | Lifecycle and unlink *intent* recorded; schema not designed | Approved linking/consent design |
| PLM-D-00-06 | Decision | Loan roles and grants | Open / Product Owner Decision Required | PLM-03 and all operational WPs | Product owner | Presets, grant **catalog intent**, scope, SoD recorded; **identifiers** not final | Owner-approved identifiers; no role-name hard-coding; no implicit hierarchy |
| PLM-D-00-07 | Decision | Operational financial model | Open / Product Owner Decision Required | Origination, payments, collections, cash | Product owner | Loan vs collector cash; Cashier Session *concept*; billable event DISBURSED; schema not designed | Owner-approved money/ledger schema in this product |
| PLM-D-00-08 | Decision | Loan business/calculation rules | Open / Product Owner Decision Required | PLM-05 through PLM-10 | Product owner | Treatment *modes*, partial payments, oldest-due *schedule* baseline recorded; **no** rates/formulas/component order | Owner-approved policy for each remaining rule area |
| PLM-D-00-09 | Decision | Organization/field client strategy (one React + PWA + Capacitor Android client; Web host/BFF) | **Closed / Product Owner Approved** | Client scaffold and later Capacitor/PWA gates | Product owner + PLM-01A | [Decisions/PLM-D-00-09-react-pwa-capacitor-client-strategy.md](Decisions/PLM-D-00-09-react-pwa-capacitor-client-strategy.md); [Reports/PLM-01A-react-pwa-capacitor-architecture-decision.md](Reports/PLM-01A-react-pwa-capacitor-architecture-decision.md) | One shared React/TypeScript client for Browser Web, PWA, and Capacitor Android; Web retained as host/BFF; MAUI preferred path superseded; Client not created in PLM-01A |
| PLM-D-00-10 | Decision | Product documentation baseline completion / owner approval | **Closed / Product Owner Accepted** | Closing PLM-00 | Product owner | PLM-00 WP01–WP10 completed; GitHub branch reviewed; Product Owner authorized progression to PLM-01 | Owner accepted documentation baseline. Financial, legal, and production decisions remain open. |
| PLM-D-00-11 | Decision | External legal/compliance validation | Open / Product Owner Decision Required | Production use | Product owner + external counsel | No rates/workflows claimed compliant | Written legal/compliance validation before Production |
| PLM-D-00-12 | Decision | Exact money rounding mode | Open / Product Owner Decision Required | Calculation engine | Product owner + accounting | Decimal money; boundaries recorded; midpoint algorithm **not** chosen | Explicit rounding-mode decision before engine implementation |
| PLM-D-00-13 | Decision | Small-org vs two-person high-risk approval | Open / Product Owner Decision Required | Operational SoD | Product owner | Multiple presets on one person allowed; high-risk self-approval still restricted where required | Explicit policy for which actions may never be self-approved |

## Accepted engineering / planning baselines (WP04)

These are **planning baselines**, not legal approval and not implementation. They do **not** close PLM-D-00-07 / PLM-D-00-08 formulas or rates.

- partial payments supported
- multiple payments supported
- deterministic allocation required
- oldest due obligation first as recommended **schedule-level** allocation baseline
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
- Principal, Net Proceeds, and Total Repayment are not assumed identical

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

## Accepted engineering / planning baselines (PLM-01A)

These supersede the **preferred client path** recorded in WP09. They do **not** create the React Client, Capacitor, PWA code, or LocalStore.

- one shared React + TypeScript client for Browser Web, installable PWA, and Capacitor Android
- `ExItS.PinoyLoanManager.Client` is the future path and **does not exist yet**
- `ExItS.PinoyLoanManager.Web` is retained as ASP.NET Core host/BFF; not a second lending UI
- Blazor Organization Web + MAUI Blazor Hybrid is **superseded** as the preferred implementation path
- MAUI is not part of the approved preferred path; existing scaffold projects are not deleted
- LocalStore is not authorized
- PWA is online-first; no authoritative financial or auth cache; no financial Background Sync initially
- Capacitor is a thin native host; Android first; iOS later only by separate authorization
- server remains authoritative
- ExItS Personal remains the separate borrower presentation surface
- PinoyBusinessPOS remains strictly isolated; POS React must not be copied
- organization desktop visual reference: Stripe Dashboard + Linear/Vercel principles; phone: Wise/Revolut-style; ExItS tokens authoritative
- frontend gates A–J are a cross-cutting track; PLM-02 remains the next **business** phase when authorized
- this proving-ground architecture does not authorize POS migration

## Accepted engineering / planning baselines (WP09)

These are **planning targets from PLM-00**, not created projects. Client-path items below are **superseded** by PLM-01A / PLM-D-00-09.

- future layout: `ExItS.PinoyLoanManager.{Domain,Application,Infrastructure,Api,ApiClient,Web}` under `src/Products/PinoyLoanManager/` plus `Docs/`
- future React Client: `ExItS.PinoyLoanManager.Client` (not created)
- LocalStore only if/when justified
- Domain persistence-independent; Application must not reference Infrastructure
- no project may reference POS
- separate proposed database `ExItS_PinoyLoanManager` (not created)
- Personal uses PLM APIs, never PLM tables
- field/PWA online/server-authoritative initially; offline financial posting not authorized
- D-P12-03 remains open; no shared DB integration

## Accepted engineering / planning baselines (WP10)

- PLM-00 documentation phase is complete as planning
- implementation classified into gates A (scaffold), B (early domain), C (financial engine), D (production)
- **PLM-D-00-10 Closed / Product Owner Accepted** — documentation baseline accepted; progression to PLM-01 authorized
- Product Owner acceptance does **not** approve unresolved rates, formulas, legal compliance, or production readiness
- PLM-01 is the authorized next phase after this acceptance record

## Operating-model, calculation, and operational open areas (do not invent)

Direction in WP03–WP10 docs does **not** close these. Tracked primarily under PLM-D-00-04, PLM-D-00-05, PLM-D-00-06, PLM-D-00-07, PLM-D-00-08, PLM-D-00-11, PLM-D-00-12, PLM-D-00-13, and D-P12-03:

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
- exact financial calculation decisions remaining from WP04
- exact traditional loan assessment criteria / approval limits
- exact Traditional mandatory application fields
- exact revision/reapproval workflow before disbursement
- exact Traditional document/condition checklist for disbursement
- exact interest formula(s) supported for MVP
- exact rate / rate precision
- exact rounding mode (PLM-D-00-12)
- exact fee model
- component payment allocation order (penalty / fee / interest / principal)
- exact penalty basis / rates / caps
- exact schedule behavior after excused collection day
- exact future-interest treatment on early settlement
- restructuring calculations
- write-off accounting treatment
- due-date generation details
- Personal / Loan API shape
- who may initiate unlink
- pending Quick Loan offer treatment after unlink
- historical Personal visibility after unlink
- re-linking and consent-history rules
- duplicate-borrower detection
- required KYC fields
- Platform usage-charge transport (D-P12-03)
- regulatory / legal validation (PLM-D-00-11)

## Loan policy subjects (do not invent)

Tracked under **PLM-D-00-08** unless noted. None of the following is decided as a formula, rate, or legal rule:

- exact interest method/formula for MVP
- amortization method
- rate configuration/precision
- rounding mode (PLM-D-00-12)
- due-date generation
- loan types beyond the two origination *paths*
- component payment allocation order
- exact fee model
- exact future-interest treatment on early settlement
- exact schedule behavior after excused collection day
- grace-period **N** (concept recorded; not a Platform default)
- penalty basis/rates/caps (concepts recorded; no amount/rate)
- restructuring calculations
- write-off accounting treatment
- full accounting/GL integration
- legal/regulatory operating rules

Partial-payment *support*, oldest-due *schedule-level* allocation, and no-MVP-wallet overpayment handling are recorded as engineering baselines, not as legal rules.

Do **not** close remaining items by guessing. Do **not** claim legal compliance.

## Instructions

- Prefer stable IDs (`R-…`, `D-…`, `PLM-D-…`).
- “Closed” requires repository or operator evidence plus explicit approval.
- Unresolved policy in approved docs must appear here as open decisions.
- Do not close PLM-D-00-01, PLM-D-00-02, PLM-D-00-04 through PLM-D-00-08, PLM-D-00-11 through PLM-D-00-13, D-P12-03, R-091, or D-P12-05 without explicit approval.
- PLM-D-00-03 is **Closed** (scaffold layout). PLM-D-00-09 is **Closed / Product Owner Approved** (React + PWA + Capacitor client). PLM-D-00-10 is **Closed / Product Owner Accepted** (documentation baseline only).
- ADRs belong in `Docs/Decisions/`. Category indexes under `Docs/*/README.md` are not ADRs.
