# Pinoy Loan Manager — Product Documentation

Authoritative product docs for **Pinoy Loan Manager** (`pinoy-loan-manager`).

Always load with:

1. `.cursor/rules/exits-workflow.mdc`
2. `.cursor/rules/exits-product-context.mdc`
3. `docs/Product-Foundation/exits-product-foundation-reference.md` (repo path)
4. Docs in this folder
5. The active work-package prompt/report
6. Files required for the task only

**Status:** PLM-00 documentation baseline accepted (PLM-D-00-10); PLM-DOC-01–05 recorded; implementation paused
**Implementation present:** No
**Documentation root:** `src/Products/PinoyLoanManager/Docs/` (D-P12-02)

Pinoy Loan Manager is a **separate first-class ExItS SaaS product**, a sibling of PinoyBusinessPOS, not a POS module, feature, or database extension.

---

## Canonical documents

| Doc | Description |
|---|---|
| [product-definition.md](product-definition.md) | Purpose, ownership, boundaries, exclusions |
| [architecture.md](architecture.md) | System, data, Personal/Borrower, and client boundaries |
| [security.md](security.md) | Security, privacy, consent |
| [authorization-matrix.md](authorization-matrix.md) | Access layers; MVP preset matrix (**PLM-D-00-06 Closed for MVP**) |
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
| [Architecture/application-surface-model.md](Architecture/application-surface-model.md) | Platform Admin, Org Web, MAUI, Personal |
| [Product/financial-calculation-baseline.md](Product/financial-calculation-baseline.md) | Money terms; pointer to PLM-DOC-02 policies |
| [Product/interest-and-finance-charge-policy.md](Product/interest-and-finance-charge-policy.md) | MVP methods, formulas, interest treatments |
| [Product/fees-and-net-proceeds-policy.md](Product/fees-and-net-proceeds-policy.md) | Fee bases/treatments; Net Proceeds; Platform charge separation |
| [Product/payment-allocation-and-prepayment-policy.md](Product/payment-allocation-and-prepayment-policy.md) | Oldest-due allocation; component order; advance/overpayment |
| [Product/early-settlement-and-principal-prepayment-policy.md](Product/early-settlement-and-principal-prepayment-policy.md) | Settlement Quote, rebate, principal prepayment |
| [Product/reversal-refund-and-correction-policy.md](Product/reversal-refund-and-correction-policy.md) | Payment reversal, Refund Payable, cash refund |
| [Product/cash-variance-and-session-close-policy.md](Product/cash-variance-and-session-close-policy.md) | Expected vs actual cash; close-with-variance |
| [Product/disbursement-cancellation-and-reversal-policy.md](Product/disbursement-cancellation-and-reversal-policy.md) | Cancel before release; reverse after recovery |
| [Product/money-precision-and-rounding-policy.md](Product/money-precision-and-rounding-policy.md) | Decimal money; To Even; schedule reconciliation |
| [Product/payment-and-allocation-model.md](Product/payment-and-allocation-model.md) | Partial payments, posting notes, reversals, idempotency |
| [Product/schedule-maturity-and-settlement.md](Product/schedule-maturity-and-settlement.md) | Schedule, calendar, maturity, settlement (index) |
| [Product/schedule-and-collection-calendar-policy.md](Product/schedule-and-collection-calendar-policy.md) | Frequencies, collection calendar, first due, exceptions |
| [Product/delinquency-and-missed-payment-policy.md](Product/delinquency-and-missed-payment-policy.md) | Past Due, DPD, missed-day counter, grace |
| [Product/penalty-assessment-and-cap-policy.md](Product/penalty-assessment-and-cap-policy.md) | Tiers, bases, caps, waiver vs reversal |
| [Product/maturity-and-post-maturity-policy.md](Product/maturity-and-post-maturity-policy.md) | Maturity Date, Matured Past Due, post-maturity modes |
| [Product/loan-lifecycle-model.md](Product/loan-lifecycle-model.md) | Origination vs lifecycle vs delinquency |
| [Architecture/loan-ledger-and-balance-model.md](Architecture/loan-ledger-and-balance-model.md) | Operational subledger and balance components |
| [Architecture/operational-subledger-and-accounting-boundary.md](Architecture/operational-subledger-and-accounting-boundary.md) | Loan vs cash ledgers; PLM is not a complete GL |
| [Security/role-and-grant-baseline.md](Security/role-and-grant-baseline.md) | Index to PLM Authorization Policy v1 |
| [Security/authorization-grant-catalog.md](Security/authorization-grant-catalog.md) | Exact MVP grant identifiers |
| [Security/default-role-preset-policy.md](Security/default-role-preset-policy.md) | Role codes and default preset assignments |
| [Security/resource-scope-and-data-minimization-policy.md](Security/resource-scope-and-data-minimization-policy.md) | Scope types and data minimization |
| [Security/privileged-access-and-owner-recovery-policy.md](Security/privileged-access-and-owner-recovery-policy.md) | Owner bootstrap, last-Owner protection, recovery |
| [Product/workflow-authorization-policy.md](Product/workflow-authorization-policy.md) | Workflow-state authorization guards |
| [Product/daily-operational-workflow.md](Product/daily-operational-workflow.md) | Common operating day, assignments, offline boundary |
| [Product/cashier-and-collector-control-model.md](Product/cashier-and-collector-control-model.md) | Cashier Session, float, remittance, cash availability |
| [Product/disbursement-and-payment-controls.md](Product/disbursement-and-payment-controls.md) | Office/field disbursement and cash payment |
| [Product/exception-reversal-and-variance-workflow.md](Product/exception-reversal-and-variance-workflow.md) | Exceptions, waivers, reversals vs cash refund, variance |
| [Product/borrower-model.md](Product/borrower-model.md) | PLM-owned Borrower; may exist without Personal |
| [Product/borrower-identity-and-duplicate-policy.md](Product/borrower-identity-and-duplicate-policy.md) | Borrower ownership, cardinality, duplicate handling |
| [Product/personal-borrower-linking.md](Product/personal-borrower-linking.md) | Optional consent-based linking; unlink does not delete history |
| [Product/personal-linking-lifecycle-and-visibility.md](Product/personal-linking-lifecycle-and-visibility.md) | Link lifecycle, MVP flow, unlink/relink, visibility |
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
| [Architecture/source-and-project-layout.md](Architecture/source-and-project-layout.md) | Future project tree; not created |
| [Architecture/api-and-contract-boundary.md](Architecture/api-and-contract-boundary.md) | API / Personal / Platform contracts |
| [Architecture/persistence-and-database-boundary.md](Architecture/persistence-and-database-boundary.md) | Separate database isolation |
| [Architecture/mobile-offline-boundary.md](Architecture/mobile-offline-boundary.md) | Online-first MAUI; offline posting deferred |
| [Architecture/mobile-and-offline-operating-model.md](Architecture/mobile-and-offline-operating-model.md) | MAUI purpose; MVP authority; cache/drafts |
| [Architecture/web-maui-component-sharing-policy.md](Architecture/web-maui-component-sharing-policy.md) | Web/MAUI sharing; **PLM-D-00-09 Closed** |
| [Security/collector-device-security-policy.md](Security/collector-device-security-policy.md) | Future collector device requirements |
| [Architecture/platform-commercial-integration.md](Architecture/platform-commercial-integration.md) | Commercial/identity contracts index; D-P12-03 open |
| [Architecture/platform-access-context-contract.md](Architecture/platform-access-context-contract.md) | Required Platform context facts (PLM-DOC-10) |
| [Architecture/personal-link-and-consent-contract.md](Architecture/personal-link-and-consent-contract.md) | Link/consent contract; **PLM-D-00-05 Closed** |
| [Architecture/personal-facing-loan-api-contract.md](Architecture/personal-facing-loan-api-contract.md) | Personal customer operations (PLM-DOC-10) |
| [Architecture/platform-usage-metering-contract.md](Architecture/platform-usage-metering-contract.md) | LOAN DISBURSED usage events (PLM-DOC-10) |
| [Architecture/tenant-placement-and-routing-contract.md](Architecture/tenant-placement-and-routing-contract.md) | Tenant placement abstraction (PLM-DOC-10) |
| [Reports/PLM-DOC-10-platform-personal-and-commercial-contracts.md](Reports/PLM-DOC-10-platform-personal-and-commercial-contracts.md) | PLM-DOC-10 Platform, Personal, commercial contracts |
| [Decisions/ADR-019-platform-personal-contract-requirements.md](Decisions/ADR-019-platform-personal-contract-requirements.md) | Platform/Personal contracts; PLM-D-00-05 Closed |
| [Decisions/ADR-020-usage-metering-and-tenant-placement-contracts.md](Decisions/ADR-020-usage-metering-and-tenant-placement-contracts.md) | Usage metering and tenant placement |
| [Reports/PLM-00-foundation-closeout.md](Reports/PLM-00-foundation-closeout.md) | PLM-00 closeout and implementation gates |
| [Reports/PLM-DOC-01-product-identity-and-personal-linking.md](Reports/PLM-DOC-01-product-identity-and-personal-linking.md) | PLM-DOC-01 identity and Personal linking finalization |
| [Reports/PLM-DOC-02-financial-calculation-and-allocation.md](Reports/PLM-DOC-02-financial-calculation-and-allocation.md) | PLM-DOC-02 calculation, fees, rounding, allocation |
| [Reports/PLM-DOC-03-schedule-delinquency-penalty-and-maturity.md](Reports/PLM-DOC-03-schedule-delinquency-penalty-and-maturity.md) | PLM-DOC-03 calendar, delinquency, penalty, maturity |
| [Reports/PLM-DOC-04-settlement-reversals-variance-and-accounting.md](Reports/PLM-DOC-04-settlement-reversals-variance-and-accounting.md) | PLM-DOC-04 settlement, reversals, variance, accounting |
| [Decisions/ADR-007-early-settlement-and-prepayment-policy.md](Decisions/ADR-007-early-settlement-and-prepayment-policy.md) | Early settlement and principal prepayment |
| [Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md](Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md) | Reversals, refunds, variance, GL boundary; PLM-D-00-13 Closed |
| [Reports/PLM-DOC-05-authorization-and-operational-security.md](Reports/PLM-DOC-05-authorization-and-operational-security.md) | PLM-DOC-05 roles, grants, workflow security |
| [Decisions/ADR-009-role-codes-grant-catalog-and-default-presets.md](Decisions/ADR-009-role-codes-grant-catalog-and-default-presets.md) | Role codes and grant catalog; PLM-D-00-06 Closed |
| [Decisions/ADR-010-resource-scope-workflow-security-and-owner-recovery.md](Decisions/ADR-010-resource-scope-workflow-security-and-owner-recovery.md) | Scope, workflow security, Owner recovery |
| [Decisions/ADR-005-schedule-calendar-and-exception-treatment.md](Decisions/ADR-005-schedule-calendar-and-exception-treatment.md) | Calendar, frequencies, exception defaults |
| [Decisions/ADR-006-delinquency-penalty-and-maturity-policy.md](Decisions/ADR-006-delinquency-penalty-and-maturity-policy.md) | DPD, penalties, maturity |
| [Decisions/ADR-001-product-identity-and-database-name.md](Decisions/ADR-001-product-identity-and-database-name.md) | Product code and logical database name |
| [Decisions/ADR-002-borrower-personal-cardinality-and-consent.md](Decisions/ADR-002-borrower-personal-cardinality-and-consent.md) | Borrower/Personal cardinality and consent |
| [Decisions/ADR-003-supported-interest-and-schedule-methods.md](Decisions/ADR-003-supported-interest-and-schedule-methods.md) | MVP interest/schedule methods |
| [Decisions/ADR-004-rounding-fees-and-payment-allocation.md](Decisions/ADR-004-rounding-fees-and-payment-allocation.md) | Rounding, fees, allocation |
| [Reports/PLM-final-documentation-closeout.md](Reports/PLM-final-documentation-closeout.md) | PLM-DOC-11 final closeout |
| [implementation-gates.md](implementation-gates.md) | Implementation gates A–E |

Category folders below are indexes only. They must not become a second source of truth.

---

## Category indexes

| Directory | Purpose |
|---|---|
| [Product/](Product/README.md) | **WHAT** — points to [product-definition.md](product-definition.md) and operating-model docs |
| [Architecture/](Architecture/README.md) | **HOW** — points to [architecture.md](architecture.md), surfaces, and ledger/balance model |
| [Security/](Security/README.md) | Access and privacy — points to [security.md](security.md), [authorization-matrix.md](authorization-matrix.md), and [Security/role-and-grant-baseline.md](Security/role-and-grant-baseline.md) |
| [Decisions/](Decisions/README.md) | Future ADRs — register is [risks-and-decisions.md](risks-and-decisions.md) |
| [Phases/](Phases/README.md) | Sequencing — points to [roadmap.md](roadmap.md) and [development-plan.md](development-plan.md) |
| [Reports/](Reports/README.md) | Completed work-package evidence |
| [Validation/](Validation/README.md) | Owner/device/browser/calculation evidence |
| [Operations/](Operations/README.md) | Deployment and production operations |

Do not scatter Pinoy Loan Manager documentation into the repository-root `docs/` tree unless the content is genuinely portfolio-wide.

---

## Identity

| Item | Value | Status |
|---|---|---|
| Display name | Pinoy Loan Manager | Recorded |
| Repository directory | `PinoyLoanManager` | Recorded |
| Product code / slug | `pinoy-loan-manager` | **Closed** (PLM-D-00-01) |
| Logical database | `ExItS_PinoyLoanManager` | **Closed for name** (PLM-D-00-02); not created |

---

## Ownership (summary)

Platform owns identity, organizations, memberships, catalog, plans, subscriptions, entitlements, SaaS billing, Platform administration, and Platform audit.

Pinoy Loan Manager will own borrower records, loan-domain state, operational financial state, product-local authorization, product database/migrations, API, Web UI, MAUI UI, reports, and product audit/history.

Isolation: independent subscription; separate database; no cross-product FKs; no direct POS or Platform table reads; OrganizationId as identifier only; approved contracts/APIs only; SaaS billing ≠ Loan operational money.

Authoritative text: [product-definition.md](product-definition.md) and [architecture.md](architecture.md).

---

## Personal / Borrower

ExItS Personal is Platform-owned and product-neutral. POS Customer ≠ Loan Borrower. Linking is optional, consent-required, and never auto-activated from EX ID / QR resolution. Personal is a presentation surface; Loan operational data remains this product’s authority. Authoritative text: [architecture.md](architecture.md), [Product/borrower-model.md](Product/borrower-model.md), [Product/borrower-identity-and-duplicate-policy.md](Product/borrower-identity-and-duplicate-policy.md), [Product/personal-borrower-linking.md](Product/personal-borrower-linking.md), [Product/personal-linking-lifecycle-and-visibility.md](Product/personal-linking-lifecycle-and-visibility.md), [Architecture/personal-integration-boundary.md](Architecture/personal-integration-boundary.md).

---

## Client direction (proposed)

Organization Web: Blazor Web (full operations). MAUI Blazor Hybrid: limited field/collector application. Platform Admin: SaaS control plane only. Web/MAUI sharing **Closed** (PLM-D-00-09); no client project until PLM-D-00-03 and owner authorization.

---

## Explicit exclusions

No implementation exists. Default interest **rates** and penalty **amounts** are not defined. **PLM-D-00-06 Closed for MVP** (grant catalog v1). Restructuring and write-off/recovery remain open (PLM-D-00-08 remainder). Do not copy PinoyBusinessPOS grants or money models. No recorded workflow is claimed legally compliant or production-security certified (PLM-D-00-11, R-091).
