# Pinoy Loan Manager — Roadmap / Phase Plan

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)
> Phase names are for this product only — do not copy another product’s phases.

| Field | Value |
|---|---|
| Product | Pinoy Loan Manager |
| Current phase | PLM-00 Foundation & Product Decisions (documentation complete) |
| Current work package | **PLM-DOC-11** Final Documentation Consistency Review & Closeout |
| Status | **PLM MVP Product documentation 100% complete.** PLM-D-00-03/05/06/07/08/09/10/12/13 Closed as documented. PLM-D-00-04/11 and D-P12-03/R-091 Open external. Implementation **paused**. |

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
- Remaining schema/GL/write-off items, peso/percent **rates**
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

PLM-00 documentation phase is complete. **PLM-DOC-01** through **PLM-DOC-05** completed. **PLM-DOC-06** finalizes restructuring, Write-Off, Recovery, and collections closeout.

`feat/plm-01-scaffold` exists as an **unmerged parked** implementation branch. It is **not** part of accepted mainline product state. Do not merge or delete it from this documentation package. Do **not** use it as evidence to close **PLM-D-00-03**.

## Documentation finalization

| Package | Name | Status |
|---|---|---|
| PLM-DOC-01 | Product Identity, Borrower Identity & Personal Linking Finalization | **Completed** |
| PLM-DOC-02 | Financial Calculation, Fees & Payment Allocation Decisions | **Completed** |
| PLM-DOC-03 | Schedule Calendar, Delinquency, Penalties & Maturity Decisions | **Completed** |
| PLM-DOC-04 | Early Settlement, Refunds, Reversals, Cash Variance & Accounting Boundaries | **Completed** |
| PLM-DOC-05 | Roles, Grants, Workflow Authorization & Operational Security Finalization | **Completed** |
| PLM-DOC-06 | Restructuring, Write-Off, Recovery & Collections Closeout | **Completed** |
| PLM-DOC-07 | Borrower Onboarding, Application, Assessment, Approval & Disbursement Readiness | **Completed** |
| PLM-DOC-08 | Documents, Receipts, Reporting, Notifications, Privacy & Retention | **Completed** |
| PLM-DOC-09 | MAUI Field Operations, Offline Boundary, Routes, Device Security, Branch Treasury & UI Sharing | **Completed** |
| PLM-DOC-10 | Platform, Personal, and Commercial Contracts | **Completed** |
| PLM-DOC-11 | Final Documentation Consistency Review, Decision Closeout & Readiness Gates | **Completed** |

Product implementation: **Paused** pending final PR review/merge and explicit authorization.

Recommended next after documentation merge: **PLM-IMPLEMENTATION-00 — Fresh Scaffold and Architecture Reconciliation**. Do not start without Gate A.

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

### PLM-DOC-03 completed decisions

- UTC instants vs Branch local collection dates; Loan-snapshotted time zone
- MVP frequencies: Daily, Weekly, Biweekly, Semi-Monthly, Monthly
- Following Valid Collection Day; Same Day or Last Calendar Day
- Quick Loan and Traditional first-due defaults
- DPD vs cumulative unexcused missed scheduled-day counter
- grace `N` semantics (not retroactive; no Platform default)
- penalty types, excluded bases, required cap, no penalty-on-penalty / capitalization
- Quick Loan vs Traditional exception defaults; schedule version history
- waiver vs reversal vs exception
- maturity does not erase balance; post-maturity modes

### PLM-DOC-04 completed decisions

- full early settlement and partial principal prepayment supported
- no MVP settlement/prepayment penalty or hidden settlement fee
- Settlement Quote contract and Branch-local business-date validity default
- Flat/Add-On earned vs unearned finance charge; deducted-charge rebate credit
- reducing-balance current-period Actual-Days accrual; future interest not charged
- fee snapshot refundable/earned treatment; penalties remain unless waived/reversed
- Reduce Term default for principal prepayment
- no borrower wallet; Refund Payable for excess credit
- payment correction = full reversal + repost; Loan reversal ≠ Cash Refund
- office/Cashier-only cash refunds in MVP
- disbursement cancellation before release vs reversal after recovery
- Collector/Cashier close-with-variance; nonzero variance cannot be marked balanced
- maker/checker required when another eligible approver exists; controlled Owner Override (**PLM-D-00-13 Closed**)
- operational Loan subledger vs Cash Accountability ledger; PLM is not a complete GL

### PLM-DOC-05 completed decisions

- role codes: `plm.owner`, `plm.manager`, `plm.cashier`, `plm.collector`
- PLM Authorization Policy v1 grant catalog (`plm.<resource>.<action>`)
- no wildcard grant; no implicit hierarchy; no custom roles in MVP
- default Owner/Manager/Cashier/Collector preset matrix (**PLM-D-00-06 Closed for MVP**)
- multiple active preset assignments; grant union with scope preserved
- Organization / Branch / Assigned Work / Own Session scopes
- server-side resource filtering and role-based data minimization
- workflow-state authorization guards
- first-Owner bootstrap direction; last-Owner protection; no self-escalation
- Platform emergency Owner recovery boundary (not implemented)
- high-risk action catalog; maker/checker retained; future step-up auth direction

### PLM-DOC-08 completed decisions

- authoritative document catalog and template versioning
- durable receipt identity for Payment, Disbursement, Cash Refund, Settlement, Principal Prepayment, Recovery
- account statement component breakdown
- GROSS OUTSTANDING PRINCIPAL, PAST-DUE SCHEDULED AMOUNT, COLLECTION RATE, PAR-X formulas
- PAR 1 / 7 / 30 / 60 / 90 and aging buckets Current / 1–7 / 8–30 / 31–60 / 61–90 / 91+
- scope-filtered operational report catalog
- Personal primary notification channel; optional SMS/email/push direction
- delivery does not change financial state
- data classification PUBLIC / INTERNAL / CONFIDENTIAL / HIGHLY SENSITIVE
- retention architecture (policy-driven; no invented numeric periods)
- audit coverage catalog and privacy/support boundaries

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
- remaining financial model (PLM-D-00-07 remainder: schema, journal/export, Write-Off/Recovery accounting, external GL)
- remaining loan policy (PLM-D-00-08 remainder: restructuring, Write-Off, Recovery)
- legal/compliance validation (PLM-D-00-11)
- Platform commercial-state transport dependencies (D-P12-03)
- custom roles (deferred; not MVP)

Portfolio: R-091 remains open. Scale architecture: [exits-scale-and-growth-architecture.md](../../../../docs/Product-Foundation/exits-scale-and-growth-architecture.md).

## Dependencies

| Dependency | Notes |
|---|---|
| Platform subscription for `pinoy-loan-manager` | Required; **code approved** (PLM-D-00-01); catalog registration not done |
| Product-owner decisions | PLM-D-00-03, PLM-D-00-04, PLM-D-00-07, PLM-D-00-11 (PLM-D-00-01 Closed; PLM-D-00-02 Closed for name; PLM-D-00-05 Closed for PLM behavior/contract; PLM-D-00-06 Closed for MVP; PLM-D-00-09 Closed; PLM-D-00-10 closed; PLM-D-00-12 Closed; PLM-D-00-13 Closed; PLM-D-00-07 / PLM-D-00-08 Open / Partially Resolved) |
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
| PLM-D-00-08 | Pressure to invent Loan **rates** or remaining restructuring/write-off policy | Keep rates Open; settlement/prepayment accepted in PLM-DOC-04 |
| PLM-D-00-06 | Pressure to invent custom roles or wildcard grants | MVP presets/grants closed in PLM-DOC-05; custom roles deferred |
| PLM-D-00-04 | Premature generic Platform relationship schema | Record intent only; no schema |
| PLM-D-00-05 | Treating PLM contract close as Platform implementation | Product contract in PLM-DOC-10; Platform transport/schema still external (**PLM-D-00-04**, **D-P12-03**) |
| R-091 | Claiming production-ready identity | Honest Dev/Testing vs Production language |
| D-P12-03 | Copying POS Dev commercial headers as PLM production design | Leave transport Open |
| PLM-D-00-03 | Treating parked scaffold as mainline | Leave PLM-D-00-03 open until authorized scaffold lands on `main` |

## Exact next package

**No further PLM-DOC packages are defined.** Await explicit Product Owner authorization before **PLM-01** implementation or additional documentation. Portfolio **D-P12-03**, **PLM-D-00-04**, and **R-091** remain open.

Do **not** start or merge PLM-01 without authorization. Implementation remains paused.

## Phase closeout requirements

- [x] WP matrix complete (documentation)
- [x] Remaining debt honest
- [x] No invented unresolved policy
- [x] Closeout report filed ([Reports/PLM-00-foundation-closeout.md](Reports/PLM-00-foundation-closeout.md))
- [ ] Portfolio / phase status updated (outside this product Docs tree; not done here)
- [x] Product-owner acceptance of documentation baseline (PLM-D-00-10 Closed / Product Owner Accepted)
- [x] Implementation pause recorded (current Product Owner direction)
