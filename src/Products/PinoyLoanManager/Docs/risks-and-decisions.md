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
| PLM-D-00-03 | Decision | Physical source/test/deploy layout beside `Docs/` | Open / Product Owner Decision Required | PLM-01 scaffold | Architecture WP | Convention observation only (`ExItS.PinoyLoanManager.<Layer>` is not approved) | Layout ADR or authorized scaffold WP |
| PLM-D-00-04 | Decision | Generic Platform cross-product relationship model | Open / Product Owner Decision Required | Personal as POS Customer, Loan Borrower, and future BNPL Customer | Platform architecture WP — do not design in PLM | Conceptual diagram only | Approved Platform contract/schema, not invented here |
| PLM-D-00-05 | Decision | Personal-to-Borrower linking mechanism | Open / Product Owner Decision Required | Optional link, consent, no auto-link from EX ID / QR | PLM-04 + Platform | Intent recorded in architecture.md | Approved linking/consent design |
| PLM-D-00-06 | Decision | Loan roles and grants | Open / Product Owner Decision Required | PLM-03 and all operational WPs | Product owner | Owner/Manager/Cashier/Collector **presets** and separation-of-duty baseline recorded; grant codes not defined | Owner-approved role/grant matrix; do not hard-code auth to role names; do not copy POS grants |
| PLM-D-00-07 | Decision | Operational financial model | Open / Product Owner Decision Required | Origination, payments, collections, cash | Product owner | Loan vs collector cash; append-only subledger *principles*; billable event DISBURSED; schema not designed | Owner-approved money/ledger schema in this product |
| PLM-D-00-08 | Decision | Loan business/calculation rules | Open / Product Owner Decision Required | PLM-05 through PLM-10 | Product owner | Treatment *modes*, partial payments, oldest-due *schedule* baseline recorded; **no** rates/formulas/component order | Owner-approved policy for each remaining rule area |
| PLM-D-00-09 | Decision | Web/MAUI component-sharing strategy | Open / Product Owner Decision Required | Client scaffold and PLM-13 | Architecture WP | Surface split recorded (full Org Web vs limited MAUI vs Personal presentation) | Approved sharing/isolation approach; no client project until authorized |
| PLM-D-00-10 | Decision | Product documentation baseline completion / owner approval | Open / Product Owner Decision Required | Closing PLM-00 | Product owner | Canonical docs from WP01–WP04 | Owner accepts baseline or lists required changes |
| PLM-D-00-11 | Decision | External legal/compliance validation | Open / Product Owner Decision Required | Production use | Product owner + external counsel | No rates/workflows claimed compliant | Written legal/compliance validation before Production |
| PLM-D-00-12 | Decision | Exact money rounding mode | Open / Product Owner Decision Required | Calculation engine | Product owner + accounting | Decimal money; boundaries recorded; midpoint algorithm **not** chosen | Explicit rounding-mode decision before engine implementation |

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

## Operating-model and calculation open areas (do not invent)

Direction in WP03/WP04 docs does **not** close these. Tracked primarily under PLM-D-00-06, PLM-D-00-07, PLM-D-00-08, PLM-D-00-11, PLM-D-00-12, and D-P12-03:

- exact traditional loan workflow
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
- full accounting / GL integration
- due-date generation details
- detailed role / grant matrix
- collector offline behavior
- Personal / Loan API shape
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
- Do not close PLM-D-00-01 through PLM-D-00-12, D-P12-03, R-091, or D-P12-05 without explicit approval.
- Category indexes under `Docs/*/README.md` are not ADRs; ADRs belong in `Docs/Decisions/` when later authorized.
