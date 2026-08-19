# Pinoy Loan Manager — Product Definition

> Template: P12-WP03. Contract: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)
> Unresolved items → [risks-and-decisions.md](risks-and-decisions.md). Do not invent policy.

| Field | Value |
|---|---|
| Product name | Pinoy Loan Manager |
| Platform product code | `pinoy-loan-manager` (**Closed**, PLM-D-00-01) |
| Docs root | `src/Products/PinoyLoanManager/Docs/` |
| Status | PLM MVP Product planning documentation complete (PLM-DOC-01 through PLM-DOC-11); **PLM-D-00-10 Closed / Product Owner Accepted**; implementation absent and paused |
| Last updated | 2026-08-19 |
| Implementation present | No |

## Purpose and users

- Purpose: Independently subscribed ExItS product for **lending operations**. Two origination paths are agreed: Traditional Loan and Quick Loan. After disbursement both converge into one core Loan model. Detail: [Product/lending-operating-model.md](Product/lending-operating-model.md). Money terminology, MVP calculation methods, fee model, rounding (**PLM-D-00-12 Closed**), payment allocation, schedule calendar, delinquency, penalties, maturity, early settlement, and principal prepayment are recorded (PLM-DOC-02–04, **ADR-007**). Default organization **rates** and penalty **amounts** remain undefined (organization-configured). Restructuring, Write-Off, and Recovery product rules are **Closed for MVP** (PLM-DOC-06).
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
| Identity / production auth | Platform | **R-091 Closed for Phase 13 scope.** Residual MFA/step-up/SSO/email and portfolio Production readiness remain separate gates. **D-P12-05 Closed / satisfied for authentication honesty.** |
| Organizations | Platform | Product will store organization id as a `Guid` reference / contract only. Field name **Status: Open / Product Owner Decision Required**. |
| Catalog / plans / subscription | Platform | **Required:** independent subscription for this product only. Product code `pinoy-loan-manager` is **approved** for future catalog registration (PLM-D-00-01 Closed). Catalog registration itself is not performed in this package. |
| Entitlements / commercial access | Platform facts | **DECISION:** D-P12-03 commercial-state transport — do not invent. Platform entitlement does not replace Loan product-local authorization. |
| SaaS billing payments | Platform | Never store product operational money in Platform SaaS billing. |
| Operational workflows / roles / money | **This product** | Not implemented. Role presets + grant catalog v1 recorded (**PLM-D-00-06 Closed for MVP**). Operational financial model **Closed for MVP Product policy** (**PLM-D-00-07**); persistence, journal/export, and external GL are implementation work |

## Boundaries (checklist)

Recorded as **required intent**. Nothing below is implemented.

- [x] Independent product subscription (not shared with other products) — required intent
- [x] Separate logical database name `ExItS_PinoyLoanManager` (**PLM-D-00-02** Closed for name). Database, schema, connections, partitions, stamps, backups, and migrations are **not** created.
- [x] No direct Platform table reads; no cross-product FKs — required intent
- [x] Product-local roles and grants defined for MVP — **PLM Authorization Policy v1** (**PLM-D-00-06 Closed for MVP**)
- [x] Operational money boundary defined for MVP Product policy — ownership, fee model, Net Proceeds, allocation, precision, subledger vs cash accountability, settlement/refund/variance recorded (**PLM-D-00-07 Closed for MVP**); persistence/schema/journal/export/GL remain implementation work
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
| API | Product | Intended. No API project authorized. Personal-facing loan API contract **accepted** (**ADR-019**); transport **D-P12-03 Open** |
| Platform Admin Web | Platform | Unified SaaS control plane. Must **not** become the normal borrower-loan operations UI. |
| Organization Web UI | Product | Full operational lending application (proposed Blazor Web). Web/MAUI sharing **Closed** (PLM-D-00-09); layout **Closed** (PLM-D-00-03); no client project until Gate A and owner authorization. |
| MAUI Hybrid UI | Product | Limited field / collector application. Not a duplicate of Organization Web. MVP online authority; offline cache/drafts only; final offline posting deferred. |
| ExItS Personal | Platform (presentation) | Customer/borrower experience. Not a separate borrower app. Loan operational data remains this product’s authority. |
| Reports | Product | Intended. MVP report formulas, aging buckets, and KPI definitions **accepted** (**ADR-015**); [Product/reporting-kpi-and-aging-policy.md](Product/reporting-kpi-and-aging-policy.md) |

Surface detail: [Architecture/application-surface-model.md](Architecture/application-surface-model.md).

## Operational money

**Closed for MVP Product operational financial model** (**PLM-D-00-07**). Calculation methods, fees, allocation, rounding, settlement, refund, variance, and subledger boundaries are recorded (PLM-DOC-02–04). Persistence schema, journal/export, and external GL integration are **implementation work**, not unresolved MVP Product policy.

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
| PII | Expected later / not implemented | Borrower and related person data will likely include PII. Retention and handling architecture **accepted** (**ADR-016**); numeric legal retention periods remain **PLM-D-00-11** |
| Financial operational | Intended later / not implemented | Loan operational financial records belong in this product when implemented (**PLM-D-00-07 Closed for MVP policy**) |
| Other sensitive | Consent records for optional Personal linking | Sensitive when implemented. **PLM-D-00-05 Closed** for PLM contract; **PLM-D-00-04** external Platform schema |

## MVP inclusions

**PLM MVP Product planning documentation is complete** (PLM-DOC-01 through PLM-DOC-11). No loan MVP **implementation** is approved. Default organization **rates** remain undefined; legal sufficiency and numeric legal limits remain **PLM-D-00-11**.

Final decision register and closeout: [Reports/PLM-final-documentation-closeout.md](Reports/PLM-final-documentation-closeout.md), [Decisions/PLM-decision-status-summary.md](Decisions/PLM-decision-status-summary.md).

## Explicit exclusions

- Loan implementation, entities, calculations, workflows, and business rules
- .NET projects, tests, solution entries, migrations, Docker, and deployment
- Final Loan grant identifiers (PLM Authorization Policy v1 — **PLM-D-00-06 Closed for MVP**)
- Generic Platform cross-product relationship schema
- Copying PinoyBusinessPOS domain, grants, or financial models
- Exact interest **rates** (formulas/methods accepted; organization-configured rates subject to **PLM-D-00-11**)
- Legal/regulatory operating rules and numeric legal limits (**PLM-D-00-11** Open)
- Auto-approval of Quick Loans
- Treating any recorded rate, fee, penalty, or workflow as legally compliant
- Platform integration contracts (**D-P12-03** Open)
- External legal/compliance validation (**PLM-D-00-11** Open)
- Final Platform→product commercial-state transport (D-P12-03)

## Assumptions

- ExItS remains one Platform plus independently subscribed products.
- Pinoy Loan Manager remains operationally isolated from PinoyBusinessPOS.
- Documentation in this `Docs/` root is the product documentation authority (D-P12-02).
- Physical source/test/deploy layout has a **Closed** approved target (**PLM-D-00-03**); projects are not implemented on main

## Unresolved decisions

| ID | Question | Blocks |
|---|---|---|
| PLM-D-00-01 | Final product code / slug | **Closed** — `pinoy-loan-manager` |
| PLM-D-00-02 | Logical database name vs physical creation | **Closed for name** — `ExItS_PinoyLoanManager`; creation/schema/placement deferred |
| PLM-D-00-03 | Physical source / test / deploy layout | **Closed for approved target architecture/layout** — implementation absent on main; Gate A required |
| PLM-D-00-04 | Generic Platform cross-product relationship model | **Open / External Platform dependency** |
| PLM-D-00-05 | Personal-to-Borrower linking mechanism | **Closed for PLM behavior/contract requirements** — Platform transport/schema external |
| PLM-D-00-06 | Loan roles and grants | **Closed for MVP** — PLM Authorization Policy v1 |
| PLM-D-00-07 | Operational financial model | **Closed for MVP Product operational financial model** — persistence/journal/export/GL are implementation work |
| PLM-D-00-08 | Loan business / calculation rules | **Closed for MVP Product business/calculation policy** — organization rates subject to **PLM-D-00-11** |
| PLM-D-00-09 | Web / MAUI component-sharing strategy | **Closed** (PLM-DOC-09) |
| PLM-D-00-10 | Product documentation baseline completion / owner approval | **Closed / Product Owner Accepted** |
| PLM-D-00-11 | External legal/compliance validation before Production | Production use |
| PLM-D-00-12 | Exact money rounding mode | **Closed** — PHP 2 dp; ≥8 intermediate; To Even; final-installment reconciliation |
| PLM-D-00-13 | Small-org vs two-person high-risk approval | **Closed** — maker/checker when another eligible approver exists; controlled Owner Override for sole eligible Owner |
| D-P12-03 | Commercial-state transport | Product access enforcement |
| R-091 | Production authentication | **Closed for Phase 13 scope** — see [PLM-decision-status-summary.md](Decisions/PLM-decision-status-summary.md) |
| D-P12-05 | Honest Dev/Testing vs Production language | **Closed / satisfied for authentication honesty** |

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
