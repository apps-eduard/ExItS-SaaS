# Pinoy Loan Manager — Roadmap / Phase Plan

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)
> Phase names are for this product only — do not copy another product’s phases.

| Field | Value |
|---|---|
| Product | Pinoy Loan Manager |
| Current phase | PLM-00 Foundation & Product Decisions (documentation complete) |
| Current work package | **PLM-DOC-02** Financial Calculation, Fees & Payment Allocation Decisions |
| Status | **PLM-D-00-10 Closed / Product Owner Accepted**. PLM-D-00-01 Closed. PLM-D-00-02 Closed for logical name. PLM-D-00-12 Closed. PLM-D-00-07 / PLM-D-00-08 Open / Partially Resolved. Implementation remains paused. |

## Phase objective

Establish product documentation, architecture boundaries, Personal/Borrower intent, operating-model, financial-lifecycle, authorization/cash-control, origination, reporting, technical layout, and an honest open-decision register. No Loan implementation on `main`.

## Scope

### Included

- Canonical Product Foundation documents under `Docs/`
- Isolation and Personal/Borrower planning rules
- Operating-model, financial, authorization, cash, origination, reporting, and technical-boundary planning (WP01–WP09)
- Foundation closeout and implementation-readiness gates (WP10)
- Planning buckets PLM-00 through PLM-14

### Excluded

- Code, projects, database objects, migrations, APIs, UI, Docker, deployment, solution changes **on main**
- Merging parked `feat/plm-01-scaffold` (unmerged; not accepted mainline state)
- Final grant identifiers, remaining calculation calendar/penalty/settlement items, peso/percent **rates**
- Generic Platform relationship schema
- Production authentication (R-091) unless a later phase explicitly delivers it
- Final commercial-state transport (D-P12-03) unless explicitly authorized

## Work packages (current phase)

| WP | Name | Status | Depends on |
|---|---|---|---|
| PLM-00-WP01 | Product Documentation Workspace | Completed | none |
| PLM-00-WP02 | Product Definition & Architecture Baseline | Completed | PLM-00-WP01 |
| PLM-00-WP03 | Lending Operating Model & Quick Loan Baseline | Completed | PLM-00-WP02 |
| PLM-00-WP04 | Financial Calculation & Loan Lifecycle Baseline | Completed | PLM-00-WP03 |
| PLM-00-WP05 | Authorization, Cash Control & Operational Workflow Baseline | Completed | PLM-00-WP04 |
| PLM-00-WP06 | Borrower, Personal Linking & Quick Loan Publishing Baseline | Completed | PLM-00-WP05 |
| PLM-00-WP07 | Traditional Loan & Origination Workflow Baseline | Completed | PLM-00-WP06 |
| PLM-00-WP08 | Reporting, Notifications, Documents & Customer Visibility Baseline | Completed | PLM-00-WP07 |
| PLM-00-WP09 | Technical Product Layout & Integration Boundary | Completed | PLM-00-WP08 |
| PLM-00-WP10 | Foundation Closeout & Implementation Readiness | Completed | PLM-00-WP09 |

PLM-00 documentation phase is complete. **PLM-DOC-01** finalized product identity, Borrower identity, and Personal linking. **PLM-DOC-02** finalizes MVP calculation, fees, rounding, and payment allocation.

`feat/plm-01-scaffold` exists as an **unmerged parked** implementation branch. It is **not** part of accepted mainline product state. Do not merge or delete it from this documentation package. Do **not** use it as evidence to close **PLM-D-00-03**.

## Documentation finalization

| Package | Name | Status |
|---|---|---|
| PLM-DOC-01 | Product Identity, Borrower Identity & Personal Linking Finalization | **Completed** |
| PLM-DOC-02 | Financial Calculation, Fees & Payment Allocation Decisions | **This package** |
| PLM-DOC-03 | Schedule Calendar, Delinquency, Penalties & Maturity Decisions | Proposed next |

### PLM-DOC-01 completed decisions

- product name, product code `pinoy-loan-manager`, logical database name `ExItS_PinoyLoanManager`
- Borrower ownership
- Personal/Borrower cardinality
- organization-initiated MVP linking
- consent lifecycle
- unlink behavior
- relinking safety
- duplicate-handling baseline
- Personal data minimization

### PLM-DOC-02 completed decisions

- Quick Loan MVP: Flat / Add-On only
- Traditional MVP: Flat / Add-On and Reducing-Balance Equal-Installment
- flat per-term and per-period formulas; reducing-balance formula
- added vs deducted interest; deducted charge not scheduled twice
- PHP decimal money; 2 dp posted; ≥8 intermediate; midpoint To Even; final-installment reconciliation (**PLM-D-00-12 Closed**)
- fee bases and treatments; Platform usage charge not a borrower fee
- oldest-due allocation; component order Interest → Principal → Fees → Penalties
- partial, multiple, and advance payments; no inferred principal prepayment; no borrower wallet

Implementation remains paused. The parked scaffold remains unmerged.

## Planning buckets (later phases)

| Phase | Status |
|---|---|
| PLM-01 Product Scaffold & Isolation | **Paused** — not currently authorized on mainline |
| PLM-02 Identity / Organization / Product Access | Not started |
| PLM-03 Loan Product Authorization | Not started |
| PLM-04 Borrower Foundation | Not started |
| PLM-05 Loan Product Configuration | Not started |
| PLM-06 Loan Application / Approval | Not started |
| PLM-07 Origination / Disbursement | Not started |
| PLM-08 Schedule / Calculation Engine | Not started |
| PLM-09 Payment Posting | Not started |
| PLM-10 Collections / Delinquency | Not started |
| PLM-11 Reporting / Documents | Not started |
| PLM-12 Security / Audit / Privacy | Not started |
| PLM-13 Offline / Mobile Field Capabilities | Not started |
| PLM-14 Production Validation / Closeout | Not started |

## Remaining documentation before implementation resumes

Before product implementation resumes, documentation still needs **final decisions** for (do **not** invent them here):

- physical source/test/deploy layout on mainline (PLM-D-00-03; remains open)
- Platform relationship contract/schema (PLM-D-00-04) and linking transport (PLM-D-00-05)
- final grants (PLM-D-00-06)
- remaining financial model (PLM-D-00-07 remainder: schema, GL, settlement/write-off accounting, cash refund)
- remaining loan policy (PLM-D-00-08 remainder: calendar, penalties, excused days, early-settlement rebate, restructuring, write-off)
- high-risk separation of duties (PLM-D-00-13)
- cash variance close policy
- refund/reversal workflow
- legal/compliance validation (PLM-D-00-11)
- Platform commercial-state transport dependencies (D-P12-03)

Portfolio: R-091 remains open. Scale architecture: [exits-scale-and-growth-architecture.md](../../../../docs/Product-Foundation/exits-scale-and-growth-architecture.md).

## Dependencies

| Dependency | Notes |
|---|---|
| Platform subscription for `pinoy-loan-manager` | Required; **code approved** (PLM-D-00-01); catalog registration not done |
| Product-owner decisions | PLM-D-00-03 through PLM-D-00-09, PLM-D-00-11, PLM-D-00-13 (PLM-D-00-01 Closed; PLM-D-00-02 Closed for name; PLM-D-00-10 closed; PLM-D-00-12 Closed; PLM-D-00-07 / PLM-D-00-08 Open / Partially Resolved) |
| D-P12-03 / R-091 / D-P12-05 | Portfolio-open; do not invent |
| ExItS scale architecture | Documented on `docs/exits-scale-foundation`; implementation of stamps/shards not required to resume docs work |

## Acceptance criteria (phase)

- [x] Documentation workspace exists (WP01)
- [x] Canonical definition/architecture/security/plan docs exist (WP02)
- [x] Lending operating model and Quick Loan baseline recorded (WP03)
- [x] Financial calculation and loan lifecycle baseline recorded (WP04)
- [x] Authorization, cash control, and operational workflow baseline recorded (WP05)
- [x] Borrower, Personal linking, and Quick Loan publishing baseline recorded (WP06)
- [x] Traditional loan and origination workflow baseline recorded (WP07)
- [x] Reporting, documents, notifications, and customer-visibility baseline recorded (WP08)
- [x] Technical product layout and integration boundary recorded (WP09)
- [x] Foundation closeout and readiness checklist recorded (WP10)
- [x] Product-owner approval of documentation baseline (PLM-D-00-10 Closed / Product Owner Accepted)
- [ ] Remaining business/financial/legal decisions listed above
- [ ] Isolation contract preserved in any later implementation (separate DB; no Platform table reads; product-local roles)
- [ ] Docs match implementation (no implementation on `main`)
- [ ] Tests green; `main = origin/main` (not applicable until implementation and authorized push)

## Risks

| ID | Risk | Mitigation |
|---|---|---|
| PLM-D-00-08 | Pressure to invent Loan **rates** or remaining calendar/penalty policy | Keep rates/penalties Open; stop at owner decisions; methods already accepted in PLM-DOC-02 |
| PLM-D-00-06 | Hard-coding authorization to role names | Grants + scope; no implicit hierarchy |
| PLM-D-00-04 | Premature generic Platform relationship schema | Record intent only; no schema |
| PLM-D-00-05 | Auto-link from EX ID / QR | Consent required; resolution identifies only |
| R-091 | Claiming production-ready identity | Honest Dev/Testing vs Production language |
| D-P12-03 | Copying POS Dev commercial headers as PLM production design | Leave transport Open |
| PLM-D-00-03 | Treating parked scaffold as mainline | Leave PLM-D-00-03 open until authorized scaffold lands on `main` |

## Exact next package

**PLM-DOC-03 — Schedule Calendar, Delinquency, Penalties & Maturity Decisions**

Do **not** start PLM-DOC-03 in this package. Implementation remains paused. Do **not** start or merge PLM-01.

## Phase closeout requirements

- [x] WP matrix complete (documentation)
- [x] Remaining debt honest
- [x] No invented unresolved policy
- [x] Closeout report filed ([Reports/PLM-00-foundation-closeout.md](Reports/PLM-00-foundation-closeout.md))
- [ ] Portfolio / phase status updated (outside this product Docs tree; not done here)
- [x] Product-owner acceptance of documentation baseline (PLM-D-00-10 Closed / Product Owner Accepted)
- [x] Implementation pause recorded (current Product Owner direction)
