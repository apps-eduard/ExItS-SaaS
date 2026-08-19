# Pinoy Loan Manager — Product Documentation

Authoritative product docs for **Pinoy Loan Manager** (`pinoy-loan-manager`, proposed).

Always load with:

1. `.cursor/rules/exits-workflow.mdc`
2. `.cursor/rules/exits-product-context.mdc`
3. `docs/Product-Foundation/exits-product-foundation-reference.md` (repo path)
4. Docs in this folder
5. The active work-package prompt/report
6. Files required for the task only

**Status:** PLM-00 accepted; PLM-01 product shell scaffolded; PLM-01A client architecture approved (PLM-D-00-09)
**Implementation present:** Product shell only — no lending domain; React Client not created
**Documentation root:** `src/Products/PinoyLoanManager/Docs/` (D-P12-02)

Pinoy Loan Manager is a **separate first-class ExItS SaaS product**, a sibling of PinoyBusinessPOS, not a POS module, feature, or database extension.

---

## Canonical documents

| Doc | Description |
|---|---|
| [product-definition.md](product-definition.md) | Purpose, ownership, boundaries, exclusions |
| [architecture.md](architecture.md) | System, data, Personal/Borrower, and client boundaries |
| [security.md](security.md) | Security, privacy, consent |
| [authorization-matrix.md](authorization-matrix.md) | Access layers; role presets and grant **intent**; identifiers open |
| [development-plan.md](development-plan.md) | Delivery buckets and testing expectations |
| [roadmap.md](roadmap.md) | Phases and work packages |
| [risks-and-decisions.md](risks-and-decisions.md) | Open risks and decisions |
| [FILE-MANIFEST.md](FILE-MANIFEST.md) | Path inventory |

Agreed operating-model direction (not implementation specs):

| Doc | Description |
|---|---|
| [Product/lending-operating-model.md](Product/lending-operating-model.md) | Origination paths, shared Loan core, roles, branch, PHP, Platform usage |
| [Product/quick-loan-model.md](Product/quick-loan-model.md) | Templates, snapshot, eligibility, Personal flow |
| [Product/collector-cash-and-reconciliation.md](Product/collector-cash-and-reconciliation.md) | Loan ledger vs collector cash |
| [Product/penalty-exception-and-waiver-model.md](Product/penalty-exception-and-waiver-model.md) | Penalty, exception, waiver, reversal, post-maturity |
| [Architecture/application-surface-model.md](Architecture/application-surface-model.md) | Platform Admin, Org Web/PWA, Capacitor Android, Personal |
| [Product/financial-calculation-baseline.md](Product/financial-calculation-baseline.md) | Money terms, interest-treatment modes, precision |
| [Product/payment-and-allocation-model.md](Product/payment-and-allocation-model.md) | Partial payments, oldest-due, reversals, idempotency |
| [Product/schedule-maturity-and-settlement.md](Product/schedule-maturity-and-settlement.md) | Schedule, calendar, maturity, settlement |
| [Product/loan-lifecycle-model.md](Product/loan-lifecycle-model.md) | Origination vs lifecycle vs delinquency |
| [Architecture/loan-ledger-and-balance-model.md](Architecture/loan-ledger-and-balance-model.md) | Operational subledger and balance components |
| [Security/role-and-grant-baseline.md](Security/role-and-grant-baseline.md) | Owner/Manager/Cashier/Collector presets; grant catalog intent |
| [Product/daily-operational-workflow.md](Product/daily-operational-workflow.md) | Common operating day, assignments, offline boundary |
| [Product/cashier-and-collector-control-model.md](Product/cashier-and-collector-control-model.md) | Cashier Session, float, remittance, cash availability |
| [Product/disbursement-and-payment-controls.md](Product/disbursement-and-payment-controls.md) | Office/field disbursement and cash payment |
| [Product/exception-reversal-and-variance-workflow.md](Product/exception-reversal-and-variance-workflow.md) | Exceptions, waivers, reversals vs cash refund, variance |
| [Product/borrower-model.md](Product/borrower-model.md) | PLM-owned Borrower; may exist without Personal |
| [Product/personal-borrower-linking.md](Product/personal-borrower-linking.md) | Optional consent-based linking; unlink does not delete history |
| [Product/quick-loan-publishing-and-eligibility.md](Product/quick-loan-publishing-and-eligibility.md) | Publishing audiences; eligibility ≠ approval |
| [Product/borrower-groups-and-targeting.md](Product/borrower-groups-and-targeting.md) | Organization-owned groups; no built-in mandatory groups |
| [Architecture/personal-integration-boundary.md](Architecture/personal-integration-boundary.md) | Personal vs PLM authority; no table access |
| [Product/traditional-loan-model.md](Product/traditional-loan-model.md) | Traditional origination; same engine after disbursement |
| [Product/loan-application-and-approval.md](Product/loan-application-and-approval.md) | Application, approval snapshot, rejection |
| [Product/loan-product-configuration.md](Product/loan-product-configuration.md) | Reusable Loan Product configuration |
| [Product/disbursement-readiness-model.md](Product/disbursement-readiness-model.md) | Pre-release checks; approval ≠ disbursement |
| [Product/reporting-baseline.md](Product/reporting-baseline.md) | Dashboard and operational reporting |
| [Product/loan-documents-and-receipts.md](Product/loan-documents-and-receipts.md) | Documents, snapshot, durable receipts |
| [Product/notification-model.md](Product/notification-model.md) | Personal and staff notifications |
| [Product/personal-loan-experience.md](Product/personal-loan-experience.md) | Personal Loan area; distinct from P2P |
| [Security/audit-and-history-baseline.md](Security/audit-and-history-baseline.md) | High-risk history |
| [Architecture/source-and-project-layout.md](Architecture/source-and-project-layout.md) | Physical layout; Client future; LocalStore deferred |
| [Architecture/react-pwa-capacitor-client.md](Architecture/react-pwa-capacitor-client.md) | Shared React + PWA + Capacitor client architecture (PLM-D-00-09) |
| [Architecture/api-and-contract-boundary.md](Architecture/api-and-contract-boundary.md) | API / Personal / Platform contracts |
| [Architecture/persistence-and-database-boundary.md](Architecture/persistence-and-database-boundary.md) | Separate database isolation |
| [Architecture/mobile-offline-boundary.md](Architecture/mobile-offline-boundary.md) | Online-first; LocalStore not authorized |
| [Architecture/platform-commercial-integration.md](Architecture/platform-commercial-integration.md) | Commercial/identity contracts; D-P12-03 open |
| [Reports/PLM-00-foundation-closeout.md](Reports/PLM-00-foundation-closeout.md) | PLM-00 closeout and implementation gates |
| [Reports/PLM-01-product-scaffold-and-isolation.md](Reports/PLM-01-product-scaffold-and-isolation.md) | PLM-01 scaffold and isolation evidence |
| [Reports/PLM-01A-react-pwa-capacitor-architecture-decision.md](Reports/PLM-01A-react-pwa-capacitor-architecture-decision.md) | PLM-01A client architecture decision |
| [Decisions/PLM-D-00-09-react-pwa-capacitor-client-strategy.md](Decisions/PLM-D-00-09-react-pwa-capacitor-client-strategy.md) | ADR: one React + PWA + Capacitor client |
| [Validation/PLM-00-readiness-checklist.md](Validation/PLM-00-readiness-checklist.md) | Docs-only readiness checklist |

Category folders below are indexes only. They must not become a second source of truth.

---

## Category indexes

| Directory | Purpose |
|---|---|
| [Product/](Product/README.md) | **WHAT** — points to [product-definition.md](product-definition.md) and operating-model docs |
| [Architecture/](Architecture/README.md) | **HOW** — points to [architecture.md](architecture.md), surfaces, and ledger/balance model |
| [Security/](Security/README.md) | Access and privacy — points to [security.md](security.md), [authorization-matrix.md](authorization-matrix.md), and [Security/role-and-grant-baseline.md](Security/role-and-grant-baseline.md) |
| [Decisions/](Decisions/README.md) | ADRs — register is [risks-and-decisions.md](risks-and-decisions.md) |
| [Phases/](Phases/README.md) | Sequencing — points to [roadmap.md](roadmap.md) and [development-plan.md](development-plan.md) |
| [Reports/](Reports/README.md) | Completed work-package evidence |
| [Validation/](Validation/README.md) | Owner/device/browser/calculation evidence |
| [Operations/](Operations/README.md) | Deployment and production operations |

Do not scatter Pinoy Loan Manager documentation into the repository-root `docs/` tree unless the content is genuinely portfolio-wide.

---

## Identity (proposed)

| Item | Value | Status |
|---|---|---|
| Display name | Pinoy Loan Manager | Recorded |
| Repository directory | `PinoyLoanManager` | Recorded |
| Product code / slug | `pinoy-loan-manager` | Open (PLM-D-00-01) |
| Future database | `ExItS_PinoyLoanManager` | Open (PLM-D-00-02) |

---

## Ownership (summary)

Platform owns identity, organizations, memberships, catalog, plans, subscriptions, entitlements, SaaS billing, Platform administration, and Platform audit.

Pinoy Loan Manager will own borrower records, loan-domain state, operational financial state, product-local authorization, product database/migrations, API, Organization Web/PWA/Capacitor client presentation, reports, and product audit/history.

Isolation: independent subscription; separate database; no cross-product FKs; no direct POS or Platform table reads; OrganizationId as identifier only; approved contracts/APIs only; SaaS billing ≠ Loan operational money.

Authoritative text: [product-definition.md](product-definition.md) and [architecture.md](architecture.md).

---

## Personal / Borrower

ExItS Personal is Platform-owned and product-neutral. POS Customer ≠ Loan Borrower. Linking is optional, consent-required, and never auto-activated from EX ID / QR resolution. Personal is a presentation surface; Loan operational data remains this product’s authority. Authoritative text: [architecture.md](architecture.md), [Product/borrower-model.md](Product/borrower-model.md), [Product/personal-borrower-linking.md](Product/personal-borrower-linking.md), [Architecture/personal-integration-boundary.md](Architecture/personal-integration-boundary.md).

---

## Client direction (approved — PLM-D-00-09)

One shared React + TypeScript client for Browser Web, installable PWA, and Capacitor Android. `ExItS.PinoyLoanManager.Web` is the future ASP.NET Core host/BFF (current PLM-01 identity shell remains scaffold only). MAUI is superseded as the preferred path. The React Client project does **not** exist yet. Platform Admin: SaaS control plane only. Detail: [Architecture/react-pwa-capacitor-client.md](Architecture/react-pwa-capacitor-client.md).

---

## Explicit exclusions

PLM-01 created an isolated product shell only. Lending, borrower, authorization, persistence, and Platform catalog work are not implemented. Exact loan calculation algorithms and peso/percent rates are not defined (PLM-D-00-08). Grant identifiers remain open (PLM-D-00-06). Do not copy PinoyBusinessPOS grants or money models. No recorded workflow is claimed legally compliant (PLM-D-00-11).
