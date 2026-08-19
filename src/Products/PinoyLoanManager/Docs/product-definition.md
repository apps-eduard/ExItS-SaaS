# Pinoy Loan Manager — Product Definition

> Template: P12-WP03. Contract: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)
> Unresolved items → [risks-and-decisions.md](risks-and-decisions.md). Do not invent policy.

| Field | Value |
|---|---|
| Product name | Pinoy Loan Manager |
| Platform product code | `pinoy-loan-manager` (proposed — **Status: Open / Product Owner Decision Required**, PLM-D-00-01) |
| Docs root | `src/Products/PinoyLoanManager/Docs/` |
| Status | PLM-00 accepted; PLM-01 shell scaffolded; PLM-01A client architecture approved; no lending implementation |
| Last updated | 2026-08-19 |
| Implementation present | Product shell only — React Client not created |

## Purpose and users

- Purpose: Independently subscribed ExItS product for **lending operations**. Two origination paths are agreed: Traditional Loan and Quick Loan. After disbursement both converge into one core Loan model. Detail: [Product/lending-operating-model.md](Product/lending-operating-model.md). Money terminology, interest-treatment *modes*, lifecycle vs delinquency, and ledger vs cash are recorded in WP04. Exact formulas, rates, rounding mode, and component allocation order remain **Open** (PLM-D-00-08, PLM-D-00-12).
- Target organizations: Independently subscribed ExItS lending organizations. Multi-branch support is intended from the beginning; a single-branch organization may use one default branch.
- Target users / jobs: Organization staff via Owner / Manager / Cashier / Collector **presets** backed by explicit grants (identifiers still open — PLM-D-00-06). Borrowers use ExItS Personal as a presentation surface only. Do not hard-code authorization to role names. Do not copy PinoyBusinessPOS grant sets.

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
| Catalog / plans / subscription | Platform | **Required:** independent subscription for this product only. Catalog registration of `pinoy-loan-manager` is not done (PLM-D-00-01). |
| Entitlements / commercial access | Platform facts | **DECISION:** D-P12-03 commercial-state transport — do not invent. Platform entitlement does not replace Loan product-local authorization. |
| SaaS billing payments | Platform | Never store product operational money in Platform SaaS billing. |
| Operational workflows / roles / money | **This product** | Not implemented. Role presets + grant **intent** recorded; identifiers open (PLM-D-00-06). Cashier Session and collector cash accountability recorded; schema open (PLM-D-00-07). |

## Boundaries (checklist)

Recorded as **required intent**. Nothing below is implemented.

- [x] Independent product subscription (not shared with other products) — required intent
- [ ] Separate database `ExItS_PinoyLoanManager` / schema **Status: Open / Product Owner Decision Required** (PLM-D-00-02) — proposed name only; not created
- [x] No direct Platform table reads; no cross-product FKs — required intent
- [ ] Product-local roles and grants defined — presets and grant **intent** recorded; identifiers **Open / Product Owner Decision Required** (PLM-D-00-06)
- [ ] Operational money defined separately from SaaS billing — ownership boundary and ledger-vs-cash direction recorded; schema open (PLM-D-00-07)
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
| Organization Web UI | Product | Full operational lending application via the shared React Client (Browser/PWA). Client project not created (PLM-CLIENT-GATE B). |
| Capacitor Android | Product | Same React Client in a thin native host; role-optimized subset later. Not created. MAUI is not the preferred path (PLM-D-00-09). |
| ExItS Personal | Platform (presentation) | Customer/borrower experience. Not a separate borrower app. Loan operational data remains this product’s authority. |
| Reports | Product | Intended. Report contents **Status: Open / Product Owner Decision Required**. |

Surface detail: [Architecture/application-surface-model.md](Architecture/application-surface-model.md).

## Operational money

**Status: Open / Product Owner Decision Required** for schema, component allocation order, exact formulas, and GL integration (PLM-D-00-07, PLM-D-00-08).

Required / agreed direction:

- SaaS subscription / billing money remains Platform-owned (Organization → ExItS).
- Borrower Loan Charges remain Pinoy Loan Manager–owned (Borrower → lending organization) and must never become Platform `SaaSPayment*` records.
- Loan financial ledger and collector cash accountability are **separate facts**. See [Product/collector-cash-and-reconciliation.md](Product/collector-cash-and-reconciliation.md) and [Architecture/loan-ledger-and-balance-model.md](Architecture/loan-ledger-and-balance-model.md).
- Preferred Platform usage billable event: **LOAN DISBURSED** (not Loan Approved). Transport remains D-P12-03.
- Do not assume Principal = Net Proceeds = Total Repayment. Terminology: [Product/financial-calculation-baseline.md](Product/financial-calculation-baseline.md).
- Authoritative money math: decimal, not binary floating-point; exact rounding mode open (PLM-D-00-12).
- Entities, posting algorithms, and journal entries are **not** defined. Do not copy PinoyBusinessPOS money models.

## Product-local roles and grants (summary)

Role **presets** and grant **intent** recorded; identifiers **Open / Product Owner Decision Required** (PLM-D-00-06).

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
- foundation closeout and implementation readiness (PLM-00-WP10, this package)

No loan MVP **implementation** is approved. Calculation algorithms, peso/percent rates, rounding mode, and legal validation remain open.

## Explicit exclusions

- Loan implementation, entities, calculations, workflows, and business rules
- .NET projects, tests, solution entries, migrations, Docker, and deployment
- Final Loan grant identifiers (presets and grant **intent** recorded)
- Generic Platform cross-product relationship schema
- Copying PinoyBusinessPOS domain, grants, or financial models
- Exact interest/amortization algorithms, peso or percent rates, rounding mode, due-date generation, component payment allocation order, early-settlement unearned interest, penalty amounts, legal/regulatory operating rules (PLM-D-00-08, PLM-D-00-12, and related open areas)
- Auto-approval of Quick Loans
- Treating any recorded rate, fee, penalty, or workflow as legally compliant
- Production authentication (R-091)
- Final Platform→product commercial-state transport (D-P12-03)

## Assumptions

- ExItS remains one Platform plus independently subscribed products.
- Pinoy Loan Manager remains operationally isolated from PinoyBusinessPOS.
- Documentation in this `Docs/` root is the product documentation authority (D-P12-02).
- Physical source/test/deploy layout beside `Docs/` is proven for the PLM-01 shell (PLM-D-00-03). Future React Client path is recorded and **not created** (PLM-D-00-09).

## Unresolved decisions

| ID | Question | Blocks |
|---|---|---|
| PLM-D-00-01 | Final product code / slug registration | Platform catalog, subscription bootstrap |
| PLM-D-00-02 | Final database name / schema | Persistence, migrations |
| PLM-D-00-03 | Physical source / test / deploy layout | **Closed** (PLM-01 scaffold; MAUI/LocalStore deferred) |
| PLM-D-00-04 | Generic Platform cross-product relationship model | Personal multi-product participation |
| PLM-D-00-05 | Personal-to-Borrower linking mechanism (lifecycle intent recorded; schema open) | Borrower identity design (PLM-04) |
| PLM-D-00-06 | Loan roles and grants (presets + grant intent recorded; identifiers open) | Authorization (PLM-03) |
| PLM-D-00-07 | Operational financial model (ledger vs cash; subledger principles recorded; schema open) | Origination, payments, collections |
| PLM-D-00-08 | Loan business / calculation rules (modes recorded; formulas/rates open) | Product configuration through collections |
| PLM-D-00-09 | Organization/field client strategy | **Closed / Product Owner Approved** — one React + PWA + Capacitor client; Web host/BFF |
| PLM-D-00-10 | Product documentation baseline completion / owner approval | **Closed / Product Owner Accepted** |
| PLM-D-00-11 | External legal/compliance validation before Production | Production use |
| PLM-D-00-12 | Exact money rounding mode | Calculation engine |
| PLM-D-00-13 | Small-org vs two-person high-risk approval | Operational SoD |
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
| Payment / allocation | [Product/payment-and-allocation-model.md](Product/payment-and-allocation-model.md) |
| Schedule / maturity / settlement | [Product/schedule-maturity-and-settlement.md](Product/schedule-maturity-and-settlement.md) |
| Loan lifecycle | [Product/loan-lifecycle-model.md](Product/loan-lifecycle-model.md) |
| Loan ledger / balances | [Architecture/loan-ledger-and-balance-model.md](Architecture/loan-ledger-and-balance-model.md) |
| Role / grant baseline | [Security/role-and-grant-baseline.md](Security/role-and-grant-baseline.md) |
| Daily operational workflow | [Product/daily-operational-workflow.md](Product/daily-operational-workflow.md) |
| Cashier / collector control | [Product/cashier-and-collector-control-model.md](Product/cashier-and-collector-control-model.md) |
| Disbursement / payment controls | [Product/disbursement-and-payment-controls.md](Product/disbursement-and-payment-controls.md) |
| Exception / reversal / variance | [Product/exception-reversal-and-variance-workflow.md](Product/exception-reversal-and-variance-workflow.md) |
| Borrower model | [Product/borrower-model.md](Product/borrower-model.md) |
| Personal / Borrower linking | [Product/personal-borrower-linking.md](Product/personal-borrower-linking.md) |
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
| React / PWA / Capacitor client | [Architecture/react-pwa-capacitor-client.md](Architecture/react-pwa-capacitor-client.md) |
| PLM-D-00-09 ADR | [Decisions/PLM-D-00-09-react-pwa-capacitor-client-strategy.md](Decisions/PLM-D-00-09-react-pwa-capacitor-client-strategy.md) |
| PLM-01A report | [Reports/PLM-01A-react-pwa-capacitor-architecture-decision.md](Reports/PLM-01A-react-pwa-capacitor-architecture-decision.md) |
| API / contract boundary | [Architecture/api-and-contract-boundary.md](Architecture/api-and-contract-boundary.md) |
| Persistence / database boundary | [Architecture/persistence-and-database-boundary.md](Architecture/persistence-and-database-boundary.md) |
| Mobile / offline boundary | [Architecture/mobile-offline-boundary.md](Architecture/mobile-offline-boundary.md) |
| Platform commercial integration | [Architecture/platform-commercial-integration.md](Architecture/platform-commercial-integration.md) |
| Foundation closeout | [Reports/PLM-00-foundation-closeout.md](Reports/PLM-00-foundation-closeout.md) |
| Readiness checklist | [Validation/PLM-00-readiness-checklist.md](Validation/PLM-00-readiness-checklist.md) |
| Security | [security.md](security.md) |
| Authorization | [authorization-matrix.md](authorization-matrix.md) |
| Development plan | [development-plan.md](development-plan.md) |
| Roadmap | [roadmap.md](roadmap.md) |
| Risks / decisions | [risks-and-decisions.md](risks-and-decisions.md) |
| Manifest | [FILE-MANIFEST.md](FILE-MANIFEST.md) |
| Deployment | `deployment-notes.md` (not created; optional until packaging) |
