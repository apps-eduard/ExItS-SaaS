# Pinoy Loan Manager — Roadmap / Phase Plan

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)
> Phase names are for this product only — do not copy another product’s phases.

| Field | Value |
|---|---|
| Product | Pinoy Loan Manager |
| Current phase | PLM-00 Foundation & Product Decisions (documentation complete) |
| Current work package | PLM-00-WP10 complete; **product implementation paused** |
| Status | **PLM-D-00-10 Closed / Product Owner Accepted** (documentation baseline). Implementation is deliberately paused while ExItS scale architecture and remaining PLM business/policy decisions are finalized |

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
- Final grant identifiers, calculation formulas, peso/percent rates, rounding mode, component allocation order
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

PLM-00 documentation phase is complete. Product implementation is **not** currently authorized.

`feat/plm-01-scaffold` exists as an **unmerged parked** implementation branch. It is **not** part of accepted mainline product state. Do not merge or delete it from this documentation package. Do **not** use it as evidence to close **PLM-D-00-03**.

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

Before product implementation resumes, documentation still needs **final decisions** for (do **not** invent them in a scale-architecture package):

- product slug (PLM-D-00-01)
- database name (PLM-D-00-02)
- physical source/test/deploy layout on mainline (PLM-D-00-03; remains open)
- Personal/Borrower linking contract (PLM-D-00-04, PLM-D-00-05)
- final grants (PLM-D-00-06)
- financial model (PLM-D-00-07)
- interest calculation methods, rounding, payment allocation, fee model, penalty policies, excused-day schedule treatment, settlement (PLM-D-00-08, PLM-D-00-12)
- high-risk separation of duties (PLM-D-00-13)
- cash variance close policy
- refund/reversal workflow
- legal/compliance validation (PLM-D-00-11)
- Platform commercial-state transport dependencies (D-P12-03)

Portfolio: R-091 remains open. Scale architecture: [exits-scale-and-growth-architecture.md](../../../../docs/Product-Foundation/exits-scale-and-growth-architecture.md).

## Dependencies

| Dependency | Notes |
|---|---|
| Platform subscription for `pinoy-loan-manager` | Required; registration open (PLM-D-00-01) |
| Product-owner decisions | PLM-D-00-01 through PLM-D-00-09, PLM-D-00-11 through PLM-D-00-13 (PLM-D-00-10 closed) |
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
| PLM-D-00-08 | Pressure to invent Loan formulas/rates to “fill” templates | Keep rates/formulas Open; stop at owner decisions |
| PLM-D-00-06 | Hard-coding authorization to role names | Grants + scope; no implicit hierarchy |
| PLM-D-00-04 | Premature generic Platform relationship schema | Record intent only; no schema |
| PLM-D-00-05 | Auto-link from EX ID / QR | Consent required; resolution identifies only |
| R-091 | Claiming production-ready identity | Honest Dev/Testing vs Production language |
| D-P12-03 | Copying POS Dev commercial headers as PLM production design | Leave transport Open |
| PLM-D-00-03 | Treating parked scaffold as mainline | Leave PLM-D-00-03 open until authorized scaffold lands on `main` |

## Exact next package

**Finalize remaining PLM business and financial decisions** (and complete review of ExItS scale architecture) **before any product implementation**.

Do **not** start or merge PLM-01 from this documentation state.

## Phase closeout requirements

- [x] WP matrix complete (documentation)
- [x] Remaining debt honest
- [x] No invented unresolved policy
- [x] Closeout report filed ([Reports/PLM-00-foundation-closeout.md](Reports/PLM-00-foundation-closeout.md))
- [ ] Portfolio / phase status updated (outside this product Docs tree; not done here)
- [x] Product-owner acceptance of documentation baseline (PLM-D-00-10 Closed / Product Owner Accepted)
- [x] Implementation pause recorded (current Product Owner direction)
