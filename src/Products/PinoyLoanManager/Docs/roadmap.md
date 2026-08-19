# Pinoy Loan Manager — Roadmap / Phase Plan

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)
> Phase names are for this product only — do not copy another product’s phases.

| Field | Value |
|---|---|
| Product | Pinoy Loan Manager |
| Current phase | PLM-01 Product Scaffold & Isolation (complete); PLM-01A client architecture (this package) |
| Current work package | PLM-CLIENT-GATE-C Browser + PWA foundation |
| Status | PLM-00 accepted; PLM-01 scaffolded; PLM-01A approved; Gate B and Gate C complete; PLM-02 not started |

## Phase objective

Create the isolated Pinoy Loan Manager product shell (projects, solution registration, architecture guards) without lending business functionality, persistence, Platform catalog registration, or a native field client. PLM-01A records the approved React + PWA + Capacitor client architecture without creating that client.

## Scope

### Included

- Domain / Application / Infrastructure / Api / ApiClient / Web project scaffold
- Solution registration and isolation tests
- Intentional deferral of LocalStore; MAUI preferred path superseded (PLM-D-00-09)
- PLM-01 evidence report
- PLM-01A client architecture ADR (documentation only; Client not created)

### Excluded

- Loan / Quick Loan / Borrower domain implementation
- Database, DbContext, migrations, connection strings, secrets
- Platform catalog / subscriptions / entitlements / Personal linking
- Authorization implementation
- MAUI / Android workload / Capacitor
- Final grant identifiers, calculation formulas, peso/percent rates, rounding mode
- Production authentication (R-091)
- Final commercial-state transport (D-P12-03)
- Starting PLM-02 from this package

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

PLM-00 documentation phase is complete. PLM-01 scaffold is complete. PLM-01A is the current documentation package.

## Planning buckets (later phases)

| Phase | Status |
|---|---|
| PLM-01 Product Scaffold & Isolation | **Complete** (shell) |
| PLM-01A React / PWA / Capacitor client architecture | **Complete** (documentation; Gate A) |
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
| Product-owner decisions | PLM-D-00-02, PLM-D-00-04 through PLM-D-00-08, PLM-D-00-11 through PLM-D-00-13 (PLM-D-00-03, PLM-D-00-09, and PLM-D-00-10 closed) |
| D-P12-03 / R-091 / D-P12-05 | Portfolio-open; do not invent |

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
- [x] Isolation contract preserved in scaffold (no POS refs; no Platform Infrastructure; no EF/migrations)
- [x] Docs match scaffold (no lending implementation claimed)
- [ ] Tests green on `feat/plm-01-scaffold` (recorded in PLM-01 report; not merged to main)

## Risks

| ID | Risk | Mitigation |
|---|---|---|
| PLM-D-00-08 | Pressure to invent Loan formulas/rates to “fill” templates | Keep rates/formulas Open; stop at owner decisions |
| PLM-D-00-06 | Hard-coding authorization to role names | Grants + scope; no implicit hierarchy |
| PLM-D-00-04 | Premature generic Platform relationship schema | Record intent only; no schema |
| PLM-D-00-05 | Auto-link from EX ID / QR | Consent required; resolution identifies only |
| R-091 | Claiming production-ready identity | Honest Dev/Testing vs Production language |
| D-P12-03 | Copying POS Dev commercial headers as PLM production design | Leave transport Open |

## Frontend delivery track (cross-cutting)

Does **not** replace the core business roadmap above. Detail: [Architecture/react-pwa-capacitor-client.md](Architecture/react-pwa-capacitor-client.md).

| Gate | Status |
|---|---|
| PLM-CLIENT-GATE A Architecture decision | **Complete** (PLM-01A) |
| PLM-CLIENT-GATE B React scaffold | **Complete** |
| PLM-CLIENT-GATE C Browser/PWA foundation | **Complete** |
| PLM-CLIENT-GATE D Auth + org/product access | Not started |
| PLM-CLIENT-GATE E First lending slice + visual review | Not started |
| PLM-CLIENT-GATE F Responsive/field workflows | Not started |
| PLM-CLIENT-GATE G Capacitor Android shell | Not started |
| PLM-CLIENT-GATE H Physical Android validation | Not started |
| PLM-CLIENT-GATE I Performance/reliability assessment | Not started |
| PLM-CLIENT-GATE J Production readiness/cutover | Not started |

Offline financial operation remains **PLM-13**.

## Exact next package

**STOPPED AFTER PLM-CLIENT-GATE-C.** Do not start Gate D, Capacitor, auth, or PLM-02 from this package.

Recommended later order when separately authorized: **PLM-CLIENT-GATE D** or **PLM-02 Identity / Organization / Product Access** (do not start either here).

PLM-02 still consumes Platform actor/org/product access without Platform table reads. Do not invent D-P12-03.

## Phase closeout requirements

- [x] WP matrix complete (documentation)
- [x] Remaining debt honest
- [x] No invented unresolved policy
- [x] Closeout report filed ([Reports/PLM-00-foundation-closeout.md](Reports/PLM-00-foundation-closeout.md))
- [ ] Portfolio / phase status updated (outside this product Docs tree; not done here)
- [x] Product-owner acceptance (PLM-D-00-10 Closed / Product Owner Accepted)
