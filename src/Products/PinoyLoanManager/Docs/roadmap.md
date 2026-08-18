# Pinoy Loan Manager — Roadmap / Phase Plan

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)
> Phase names are for this product only — do not copy another product’s phases.

| Field | Value |
|---|---|
| Product | Pinoy Loan Manager |
| Current phase | PLM-00 Foundation & Product Decisions |
| Current work package | PLM-00-WP06 Borrower, Personal Linking & Quick Loan Publishing Baseline |
| Status | Draft — planning only |

## Phase objective

Establish product documentation, architecture boundaries, Personal/Borrower intent, operating-model direction, financial-lifecycle planning, authorization/cash-control workflows, borrower/publishing rules, and an honest open-decision register. No Loan implementation.

## Scope

### Included

- Canonical Product Foundation documents under `Docs/`
- Isolation and Personal/Borrower planning rules
- Agreed origination, surface, role-preset, Quick Loan, collector-cash, and penalty direction (WP03)
- Financial terminology, interest-treatment modes, payment/schedule/lifecycle/ledger planning (WP04)
- Grant-based authorization intent, Cashier Session, collector cash, disbursement/payment/exception/variance workflows (WP05)
- Borrower model, Personal linking/consent lifecycle, Quick Loan publishing/eligibility, borrower groups (WP06)
- Planning buckets PLM-00 through PLM-14

### Excluded

- Code, projects, database objects, migrations, APIs, UI, Docker, deployment, solution changes
- Final grant identifiers, calculation formulas, peso/percent rates, rounding mode, component allocation order
- Generic Platform relationship schema
- Required KYC field lists
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
| PLM-00-WP06 | Borrower, Personal Linking & Quick Loan Publishing Baseline | Current | PLM-00-WP05 |
| PLM-00-WP07 | Traditional Loan & Origination Workflow Baseline | Next proposed | PLM-00-WP06 |

Later phases (PLM-01 … PLM-14) have no authorized work packages yet. No implementation phase is authorized yet. WP07 remains documentation / product planning.

## Planning buckets (later phases)

| Phase | Status |
|---|---|
| PLM-01 Product Scaffold & Isolation | Not started |
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

## Dependencies

| Dependency | Notes |
|---|---|
| Platform subscription for `pinoy-loan-manager` | Required; registration open (PLM-D-00-01) |
| Product-owner decisions | PLM-D-00-02 through PLM-D-00-13 |
| D-P12-03 / R-091 / D-P12-05 | Portfolio-open; do not invent |

## Acceptance criteria (phase)

- [x] Documentation workspace exists (WP01)
- [x] Canonical definition/architecture/security/plan docs exist (WP02)
- [x] Lending operating model and Quick Loan baseline recorded (WP03)
- [x] Financial calculation and loan lifecycle baseline recorded (WP04)
- [x] Authorization, cash control, and operational workflow baseline recorded (WP05)
- [x] Borrower, Personal linking, and Quick Loan publishing baseline recorded (this WP)
- [ ] Product-owner approval of documentation baseline (PLM-D-00-10)
- [ ] Isolation contract preserved in any later implementation (separate DB; no Platform table reads; product-local roles)
- [ ] Docs match implementation (no implementation yet)
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

## Exact next package

**PLM-00-WP07 — Traditional Loan & Origination Workflow Baseline** (do not begin until this WP is committed)

Documentation / product planning only. Deepen Traditional application, Loan Product configuration, approval snapshot, and disbursement readiness without implementing code or claiming legal compliance.

## Phase closeout requirements

- [ ] WP matrix complete
- [ ] Remaining debt honest
- [ ] No invented unresolved policy
- [ ] Closeout report filed
- [ ] Portfolio / phase status updated
