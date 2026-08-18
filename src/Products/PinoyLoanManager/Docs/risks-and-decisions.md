# Pinoy Loan Manager — Risks and Decisions

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)
> Close items only with evidence. Do not invent answers for portfolio-open items.

| Field | Value |
|---|---|
| Product | Pinoy Loan Manager |
| Last updated | 2026-08-18 |

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
| PLM-D-00-06 | Decision | Loan roles and grants | Open / Product Owner Decision Required | PLM-03 and all operational WPs | Product owner | Authorization matrix intentionally empty | Owner-approved role/grant matrix; do not copy POS roles |
| PLM-D-00-07 | Decision | Operational financial model | Open / Product Owner Decision Required | Origination, payments, collections | Product owner | Ownership boundary only (not SaaS billing) | Owner-approved money/ledger model in this product |
| PLM-D-00-08 | Decision | Loan business/calculation rules | Open / Product Owner Decision Required | PLM-05 through PLM-10 | Product owner | Subjects listed; no formulas | Owner-approved policy for each rule area |
| PLM-D-00-09 | Decision | Web/MAUI component-sharing strategy | Open / Product Owner Decision Required | Client scaffold and PLM-13 | Architecture WP | Client direction recorded (Blazor Web + MAUI Blazor Hybrid) | Approved sharing/isolation approach; no client project until authorized |
| PLM-D-00-10 | Decision | Product documentation baseline completion / owner approval | Open / Product Owner Decision Required | Closing PLM-00 | Product owner | Canonical docs created in PLM-00-WP02 | Owner accepts baseline or lists required changes |

## Loan policy subjects (do not invent)

Tracked under **PLM-D-00-08**. None of the following is decided:

- interest method/formula
- amortization method
- loan types
- payment allocation order
- rounding rules
- grace periods
- penalties
- delinquency rules
- approval limits
- credit scoring
- collateral policy
- refinancing
- restructuring
- write-off
- collections policy
- legal/regulatory operating rules

## Instructions

- Prefer stable IDs (`R-…`, `D-…`, `PLM-D-…`).
- “Closed” requires repository or operator evidence plus explicit approval.
- Unresolved policy in approved docs must appear here as open decisions.
- Category indexes under `Docs/*/README.md` are not ADRs; ADRs belong in `Docs/Decisions/` when later authorized.
