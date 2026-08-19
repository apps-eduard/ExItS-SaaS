# Pinoy Loan Manager — Product Definition

> Template: P12-WP03. Contract: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)
> Unresolved items → [risks-and-decisions.md](risks-and-decisions.md). Do not invent policy.

| Field | Value |
|---|---|
| Product name | Pinoy Loan Manager |
| Platform product code | `pinoy-loan-manager` (**Closed**, PLM-D-00-01) |
| Docs root | `src/Products/PinoyLoanManager/Docs/` |
| Status | PLM-00 baseline accepted (PLM-D-00-10); PLM-DOC-01–03 recorded; no implementation |
| Last updated | 2026-08-19 |
| Implementation present | No |

## Purpose and users

- Purpose: Independently subscribed ExItS product for **lending operations**. Two origination paths are agreed: Traditional Loan and Quick Loan. After disbursement both converge into one core Loan model. Detail: [Product/lending-operating-model.md](Product/lending-operating-model.md). Money terminology, MVP calculation methods, fee model, rounding (**PLM-D-00-12 Closed**), payment allocation, schedule calendar, delinquency, penalties, and maturity are recorded in PLM-DOC-02 and PLM-DOC-03. Default rates and penalty amounts remain undefined. Early-settlement rebate remains **Open** (PLM-D-00-08 remainder).
- Target organizations: Independently subscribed ExItS lending organizations. Multi-branch support is intended from the beginning; a single-branch organization may use one default branch.
- Target users / jobs: Organization staff via `plm.owner` / `plm.manager` / `plm.cashier` / `plm.collector` presets backed by explicit grants (**PLM Authorization Policy v1**; PLM-D-00-06 Closed for MVP). Borrowers use ExItS Personal as a presentation surface only. Do not hard-code authorization to role names. Do not copy PinoyBusinessPOS grant sets.

Pinoy Loan Manager is a **separate first-class ExItS SaaS product**, a sibling of PinoyBusinessPOS. It is not a POS module, POS feature, or POS database extension.

```text
ExItS Platform
├── PinoyBusinessPOS
├── Pinoy Loan Manager
└── future products
```

## Platform integration

| Concern | Owner | Notes |
|---|---|---|
| Identity / production auth | Platform | **DECISION:** R-091 open — do not claim production-secure auth. Keep Dev/Testing vs Production language honest (D-P12-05). |
| Organizations | Platform | Product will store organization id as a `Guid` reference / contract only. Field name **Status: Open / Product Owner Decision Required**. |
| Catalog / plans / subscription | Platform | **Required:** independent subscription for this product only. Product code `pinoy-loan-manager` is **approved** for future catalog registration (PLM-D-00-01 Closed). Catalog registration itself is not performed in this package. |
| Entitlements / commercial access | Platform facts | **DECISION:** D-P12-03 commercial-state transport — do not invent. Platform entitlement does not replace Loan product-local authorization. |
| SaaS billing payments | Platform | Never store product operational money in Platform SaaS billing. |
| Operational workflows / roles / money | **This product** | Not implemented. Role presets + grant catalog v1 recorded (**PLM-D-00-06 Closed for MVP**). Cashier Session and collector cash accountability recorded; ledger **schema** open (PLM-D-00-07 remainder). |

## Boundaries (checklist)

Recorded as **required intent**. Nothing below is implemented.

- [x] Independent product subscription (not shared with other products) — required intent
- [x] Separate logical database name `ExItS_PinoyLoanManager` (**PLM-D-00-02** Closed for name). Database, schema, connections, partitions, stamps, backups, and migrations are **not** created.
- [x] No direct Platform table reads; no cross-product FKs — required intent
- [x] Product-local roles and grants defined for MVP — **PLM Authorization Policy v1** (**PLM-D-00-06 Closed for MVP**)
- [ ] Operational money defined separately from SaaS billing — ownership, fee model, Net Proceeds, allocation, and precision recorded (PLM-DOC-02); subledger **schema** open (PLM-D-00-07 remainder)
- [x] Trusted org + product context enforced server-side — required intent; not implemented
- [x] PHI / sensitive data: default **none** unless explicitly authorized below
- [x] No customer-specific source forks (config only)

Additional isolation (required intent):

- no direct POS database reads
- no direct Platform reads of Loan operational tables
- OrganizationId crosses boundaries only as an identifier/contract
- approved APIs/contracts only

## Surfaces

| Surface | Ownership | Notes |
|---|---|---|
| API | Product | Intended. No API project authorized. Personal/Loan API shape remains open. |
| Platform Admin Web | Platform | Unified SaaS control plane. Must **not** become the normal borrower-loan operations UI. |
| Organization Web UI | Product | Full operational lending application (proposed Blazor Web). Web/MAUI sharing **Closed** (PLM-D-00-09); no client project until PLM-D-00-03 and owner authorization. |
| MAUI Hybrid UI | Product | Limited field / collector application. Not a duplicate of Organization Web. MVP online authority; offline cache/drafts only; final offline posting deferred. |
| ExItS Personal | Platform (presentation) | Customer/borrower experience. Not a separate borrower app. Loan operational data remains this product’s authority. |
| Reports | Product | Intended. Report contents **Status: Open / Product Owner Decision Required**. |

Surface detail: [Architecture/application-surface-model.md](Architecture/application-surface-model.md).

## Operational money

**Status: Open / Partially Resolved** for schema, GL integration, settlement/write-off accounting, and cash refund (PLM-D-00-07 remainder). Calculation methods, fees, allocation, and rounding are recorded in PLM-DOC-02.

Required / agreed direction:

- SaaS subscription / billing money remains Platform-owned (Organization → ExItS).
- Borrower Loan Charges remain Pinoy Loan Manager–owned (Borrower → lending organization) and must never become Platform `SaaSPayment*` records.
- Platform usage charge must **not** enter the borrower Loan subledger. Fee model: [Product/fees-and-net-proceeds-policy.md](Product/fees-and-net-proceeds-policy.md).
- Loan financial ledger and collector cash accountability are **separate facts**. See [Product/collector-cash-and-reconciliation.md](Product/collector-cash-and-reconciliation.md) and [Architecture/loan-ledger-and-balance-model.md](Architecture/loan-ledger-and-balance-model.md).
- Preferred Platform usage billable event: **LOAN DISBURSED** (not Loan Approved). Transport remains D-P12-03.
- Do not assume Principal = Net Proceeds = Total Scheduled Repayment. Terminology: [Product/financial-calculation-baseline.md](Product/financial-calculation-baseline.md).
- Authoritative money math: decimal, PHP 2 dp posted, ≥8 intermediate, midpoint To Even (**PLM-D-00-12 Closed**). [Product/money-precision-and-rounding-policy.md](Product/money-precision-and-rounding-policy.md).
- Entities, posting algorithms, and journal entries are **not** defined. Do not copy PinoyBusinessPOS money models.

## Product-local roles and grants (summary)

Role preset codes and grant catalog v1 recorded (**PLM-D-00-06 Closed for MVP**). Custom roles deferred.

Do **not** hard-code authorization to role names. Do **not** implement implicit role hierarchy. Do **not** copy PinoyBusinessPOS grant sets.

| Preset | Purpose | Key grants |
|---|---|---|
| Owner | Organization-level PLM administration | Broad org PLM grants (planning intent) |
| Manager | Lending operations and supervision | Broad operational grants; not ownership-admin by default |
| Cashier | Cash custody, float, remittance, office cash | Cash / office payment / disbursement / remittance |
| Collector | Assigned field collection and remittance | Assigned-scope field grants only |

Separation of duties, scope, and catalog: [Security/role-and-grant-baseline.md](Security/role-and-grant-baseline.md), [authorization-matrix.md](authorization-matrix.md).

Access intersection (required, not implemented): trusted actor + trusted organization context + Platform product access + valid commercial state + required entitlement + active Loan product-local role + required Loan product-local grant + resource/workflow authorization.

Detail: [authorization-matrix.md](authorization-matrix.md).

## Privacy classification

| Class | Present? | Notes |
|---|---|---|
| PHI | No (default) | Not authorized. Do not add PHI unless a later package explicitly authorizes and designs for it. |
| PII | Expected later / not implemented | Borrower and related person data, if created later, will likely include PII. Retention and handling **Status: Open / Product Owner Decision Required**. |
| Financial operational | Intended later / not implemented | Loan operational financial records belong in this product when defined (PLM-D-00-07). |
| Other sensitive | **Status: Open / Product Owner Decision Required** | Consent records for optional Personal linking, if implemented later, would be sensitive. Schema not designed (PLM-D-00-04, PLM-D-00-05). |

## MVP inclusions

**Status: Open / Product Owner Decision Required.**

This package records:

- documentation workspace (PLM-00-WP01, completed)
- product definition and architecture baseline (PLM-00-WP02, completed)
- lending operating model and Quick Loan baseline (PLM-00-WP03, completed)
- financial calculation and loan lifecycle baseline (PLM-00-WP04, completed)
- authorization, cash control, and operational workflow baseline (PLM-00-WP05, completed)
- borrower, Personal linking, and Quick Loan publishing baseline (PLM-00-WP06, completed)
- traditional loan and origination workflow baseline (PLM-00-WP07, completed)
- reporting, documents, notifications, and customer-visibility baseline (PLM-00-WP08, completed)
- technical product layout and integration boundary (PLM-00-WP09, completed)
- foundation closeout and implementation readiness (PLM-00-WP10, completed)
- product identity and Personal linking (PLM-DOC-01, completed)
- financial calculation, fees, rounding, and payment allocation (PLM-DOC-02, completed)
- early settlement, refunds, reversals, cash variance, and accounting boundaries (PLM-DOC-04, completed)
- roles, grants, workflow authorization, and operational security (PLM-DOC-05, this package)

No loan MVP **implementation** is approved. Default rates, restructuring, write-off/recovery accounting, and legal validation remain open.

## Explicit exclusions

- Loan implementation, entities, calculations, workflows, and business rules
- .NET projects, tests, solution entries, migrations, Docker, and deployment
- Final Loan grant identifiers (PLM Authorization Policy v1 — **PLM-D-00-06 Closed for MVP**)
- Generic Platform cross-product relationship schema
- Copying PinoyBusinessPOS domain, grants, or financial models
- Exact interest **rates** (formulas/methods accepted in PLM-DOC-02), penalty **amounts**, restructuring/write-off accounting, legal/regulatory operating rules (PLM-D-00-08 remainder, PLM-D-00-11)
- Auto-approval of Quick Loans
- Treating any recorded rate, fee, penalty, or workflow as legally compliant
- Production authentication (R-091)
- Final Platform→product commercial-state transport (D-P12-03)

## Assumptions

- ExItS remains one Platform plus independently subscribed products.
- Pinoy Loan Manager remains operationally isolated from PinoyBusinessPOS.
- Documentation in this `Docs/` root is the product documentation authority (D-P12-02).
- Physical source/test/deploy layout beside `Docs/` has a recorded **planning target**; projects are not created (PLM-D-00-03).

## Unresolved decisions

| ID | Question | Blocks |
|---|---|---|
| PLM-D-00-01 | Final product code / slug | **Closed** — `pinoy-loan-manager` |
| PLM-D-00-02 | Logical database name vs physical creation | **Closed for name** — `ExItS_PinoyLoanManager`; creation/schema/placement deferred |
| PLM-D-00-03 | Physical source / test / deploy layout (planning target recorded; projects not created) | Scaffold (PLM-01) |
| PLM-D-00-04 | Generic Platform cross-product relationship model | Personal multi-product participation |
| PLM-D-00-05 | Personal-to-Borrower linking mechanism (product behavior defined; Platform transport/schema open) | Borrower identity implementation (PLM-04) |
| PLM-D-00-06 | Loan roles and grants | **Closed for MVP** — PLM Authorization Policy v1 | Authorization (PLM-03) |
| PLM-D-00-07 | Operational financial model (methods/fees/allocation/settlement/refund/variance/ledger boundary recorded; schema/GL/write-off open) | Origination, payments, collections |
| PLM-D-00-08 | Loan business / calculation rules (MVP methods, calendar/penalty engine, settlement/prepayment recorded; restructuring/write-off open) | Product configuration through collections |
| PLM-D-00-09 | Web / MAUI component-sharing strategy | **Closed** (PLM-DOC-09) |
| PLM-D-00-10 | Product documentation baseline completion / owner approval | **Closed / Product Owner Accepted** |
| PLM-D-00-11 | External legal/compliance validation before Production | Production use |
| PLM-D-00-12 | Exact money rounding mode | **Closed** — PHP 2 dp; ≥8 intermediate; To Even; final-installment reconciliation |
| PLM-D-00-13 | Small-org vs two-person high-risk approval | **Closed** — maker/checker when another eligible approver exists; controlled Owner Override for sole eligible Owner |
| D-P12-03 | Commercial-state transport | Product access enforcement |
| R-091 | Production authentication | Production readiness |
| D-P12-05 | Honest Dev/Testing vs Production language | Tied to R-091 |

## Document links

| Doc | Path |
|---|---|
| Architecture | [architecture.md](architecture.md) |
| Application surfaces | [Architecture/application-surface-model.md](Architecture/application-surface-model.md) |
| Lending operating model | [Product/lending-operating-model.md](Product/lending-operating-model.md) |
| Quick Loan | [Product/quick-loan-model.md](Product/quick-loan-model.md) |
| Collector cash | [Product/collector-cash-and-reconciliation.md](Product/collector-cash-and-reconciliation.md) |
| Penalty / exception / waiver | [Product/penalty-exception-and-waiver-model.md](Product/penalty-exception-and-waiver-model.md) |
| Financial calculation | [Product/financial-calculation-baseline.md](Product/financial-calculation-baseline.md) |
| Interest / finance charge | [Product/interest-and-finance-charge-policy.md](Product/interest-and-finance-charge-policy.md) |
| Fees / net proceeds | [Product/fees-and-net-proceeds-policy.md](Product/fees-and-net-proceeds-policy.md) |
| Payment allocation / prepayment | [Product/payment-allocation-and-prepayment-policy.md](Product/payment-allocation-and-prepayment-policy.md) |
| Early settlement / principal prepayment | [Product/early-settlement-and-principal-prepayment-policy.md](Product/early-settlement-and-principal-prepayment-policy.md) |
| Reversal / refund / correction | [Product/reversal-refund-and-correction-policy.md](Product/reversal-refund-and-correction-policy.md) |
| Cash variance / session close | [Product/cash-variance-and-session-close-policy.md](Product/cash-variance-and-session-close-policy.md) |
| Disbursement cancellation / reversal | [Product/disbursement-cancellation-and-reversal-policy.md](Product/disbursement-cancellation-and-reversal-policy.md) |
| Money precision / rounding | [Product/money-precision-and-rounding-policy.md](Product/money-precision-and-rounding-policy.md) |
| Payment / allocation (posting notes) | [Product/payment-and-allocation-model.md](Product/payment-and-allocation-model.md) |
| Schedule / maturity / settlement | [Product/schedule-maturity-and-settlement.md](Product/schedule-maturity-and-settlement.md) |
| Schedule / collection calendar | [Product/schedule-and-collection-calendar-policy.md](Product/schedule-and-collection-calendar-policy.md) |
| Delinquency / missed payment | [Product/delinquency-and-missed-payment-policy.md](Product/delinquency-and-missed-payment-policy.md) |
| Penalty assessment / cap | [Product/penalty-assessment-and-cap-policy.md](Product/penalty-assessment-and-cap-policy.md) |
| Maturity / post-maturity | [Product/maturity-and-post-maturity-policy.md](Product/maturity-and-post-maturity-policy.md) |
| Loan lifecycle | [Product/loan-lifecycle-model.md](Product/loan-lifecycle-model.md) |
| Loan ledger / balances | [Architecture/loan-ledger-and-balance-model.md](Architecture/loan-ledger-and-balance-model.md) |
| Operational subledger / accounting boundary | [Architecture/operational-subledger-and-accounting-boundary.md](Architecture/operational-subledger-and-accounting-boundary.md) |
| Role / grant baseline | [Security/role-and-grant-baseline.md](Security/role-and-grant-baseline.md) |
| Daily operational workflow | [Product/daily-operational-workflow.md](Product/daily-operational-workflow.md) |
| Cashier / collector control | [Product/cashier-and-collector-control-model.md](Product/cashier-and-collector-control-model.md) |
| Disbursement / payment controls | [Product/disbursement-and-payment-controls.md](Product/disbursement-and-payment-controls.md) |
| Exception / reversal / variance | [Product/exception-reversal-and-variance-workflow.md](Product/exception-reversal-and-variance-workflow.md) |
| Borrower model | [Product/borrower-model.md](Product/borrower-model.md) |
| Borrower identity / duplicates | [Product/borrower-identity-and-duplicate-policy.md](Product/borrower-identity-and-duplicate-policy.md) |
| Personal / Borrower linking | [Product/personal-borrower-linking.md](Product/personal-borrower-linking.md) |
| Linking lifecycle / visibility | [Product/personal-linking-lifecycle-and-visibility.md](Product/personal-linking-lifecycle-and-visibility.md) |
| Quick Loan publishing / eligibility | [Product/quick-loan-publishing-and-eligibility.md](Product/quick-loan-publishing-and-eligibility.md) |
| Borrower groups | [Product/borrower-groups-and-targeting.md](Product/borrower-groups-and-targeting.md) |
| Personal integration boundary | [Architecture/personal-integration-boundary.md](Architecture/personal-integration-boundary.md) |
| Traditional Loan | [Product/traditional-loan-model.md](Product/traditional-loan-model.md) |
| Application / approval | [Product/loan-application-and-approval.md](Product/loan-application-and-approval.md) |
| Loan Product configuration | [Product/loan-product-configuration.md](Product/loan-product-configuration.md) |
| Disbursement readiness | [Product/disbursement-readiness-model.md](Product/disbursement-readiness-model.md) |
| Reporting | [Product/reporting-baseline.md](Product/reporting-baseline.md) |
| Documents / receipts | [Product/loan-documents-and-receipts.md](Product/loan-documents-and-receipts.md) |
| Notifications | [Product/notification-model.md](Product/notification-model.md) |
| Personal Loan experience | [Product/personal-loan-experience.md](Product/personal-loan-experience.md) |
| Audit / history | [Security/audit-and-history-baseline.md](Security/audit-and-history-baseline.md) |
| Source / project layout | [Architecture/source-and-project-layout.md](Architecture/source-and-project-layout.md) |
| API / contract boundary | [Architecture/api-and-contract-boundary.md](Architecture/api-and-contract-boundary.md) |
| Persistence / database boundary | [Architecture/persistence-and-database-boundary.md](Architecture/persistence-and-database-boundary.md) |
| Mobile / offline boundary | [Architecture/mobile-offline-boundary.md](Architecture/mobile-offline-boundary.md), [Architecture/mobile-and-offline-operating-model.md](Architecture/mobile-and-offline-operating-model.md) |
| Web / MAUI sharing | [Architecture/web-maui-component-sharing-policy.md](Architecture/web-maui-component-sharing-policy.md) |
| Branch treasury / float acknowledgment | [Product/branch-treasury-and-float-acknowledgment-policy.md](Product/branch-treasury-and-float-acknowledgment-policy.md) |
| Collector route / location | [Product/collector-route-and-location-policy.md](Product/collector-route-and-location-policy.md) |
| Collector device security (future) | [Security/collector-device-security-policy.md](Security/collector-device-security-policy.md) |
| Platform commercial integration | [Architecture/platform-commercial-integration.md](Architecture/platform-commercial-integration.md) |
| Foundation closeout | [Reports/PLM-00-foundation-closeout.md](Reports/PLM-00-foundation-closeout.md) |
| PLM-DOC-01 | [Reports/PLM-DOC-01-product-identity-and-personal-linking.md](Reports/PLM-DOC-01-product-identity-and-personal-linking.md) |
| PLM-DOC-02 | [Reports/PLM-DOC-02-financial-calculation-and-allocation.md](Reports/PLM-DOC-02-financial-calculation-and-allocation.md) |
| PLM-DOC-03 | [Reports/PLM-DOC-03-schedule-delinquency-penalty-and-maturity.md](Reports/PLM-DOC-03-schedule-delinquency-penalty-and-maturity.md) |
| PLM-DOC-04 | [Reports/PLM-DOC-04-settlement-reversals-variance-and-accounting.md](Reports/PLM-DOC-04-settlement-reversals-variance-and-accounting.md) |
| ADR-007 | [Decisions/ADR-007-early-settlement-and-prepayment-policy.md](Decisions/ADR-007-early-settlement-and-prepayment-policy.md) |
| ADR-008 | [Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md](Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md) |
| ADR-009 | [Decisions/ADR-009-role-codes-grant-catalog-and-default-presets.md](Decisions/ADR-009-role-codes-grant-catalog-and-default-presets.md) |
| ADR-010 | [Decisions/ADR-010-resource-scope-workflow-security-and-owner-recovery.md](Decisions/ADR-010-resource-scope-workflow-security-and-owner-recovery.md) |
| PLM-DOC-05 | [Reports/PLM-DOC-05-authorization-and-operational-security.md](Reports/PLM-DOC-05-authorization-and-operational-security.md) |
| ADR-005 | [Decisions/ADR-005-schedule-calendar-and-exception-treatment.md](Decisions/ADR-005-schedule-calendar-and-exception-treatment.md) |
| ADR-006 | [Decisions/ADR-006-delinquency-penalty-and-maturity-policy.md](Decisions/ADR-006-delinquency-penalty-and-maturity-policy.md) |
| ADR-003 | [Decisions/ADR-003-supported-interest-and-schedule-methods.md](Decisions/ADR-003-supported-interest-and-schedule-methods.md) |
| ADR-004 | [Decisions/ADR-004-rounding-fees-and-payment-allocation.md](Decisions/ADR-004-rounding-fees-and-payment-allocation.md) |
| Readiness checklist | [Validation/PLM-00-readiness-checklist.md](Validation/PLM-00-readiness-checklist.md) |
| Security | [security.md](security.md) |
| Authorization | [authorization-matrix.md](authorization-matrix.md) |
| Grant catalog | [Security/authorization-grant-catalog.md](Security/authorization-grant-catalog.md) |
| Workflow authorization | [Product/workflow-authorization-policy.md](Product/workflow-authorization-policy.md) |
| Development plan | [development-plan.md](development-plan.md) |
| Roadmap | [roadmap.md](roadmap.md) |
| Risks / decisions | [risks-and-decisions.md](risks-and-decisions.md) |
| Manifest | [FILE-MANIFEST.md](FILE-MANIFEST.md) |
| Deployment | `deployment-notes.md` (not created; optional until packaging) |
