# Pinoy Loan Manager — File Manifest

**Status:** PLM-DOC-11 complete; PLM-01 scaffold; Gates B–D3 complete; PLM-02A server access boundary complete
**Implementation present:** Product shell + React Client + online-first PWA + cookie Sign In + Personal account lifecycle + organization/product-access gate + fail-closed server access boundary; Platform `pinoy-loan-manager` catalog + current-session access API
**Current work package:** PLM-02 — lending domain (not started)

This file is the navigation map for future Cursor work. Load this product’s `Docs/` after the shared Product Foundation reference. Do not scan PinoyBusinessPOS implementation by default.

Shared contracts to load with this product:

- `.cursor/rules/exits-workflow.mdc`
- `.cursor/rules/exits-product-context.mdc`
- `docs/Product-Foundation/exits-product-foundation-reference.md`

---

## Canonical documents

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/product-definition.md` | Product identity, ownership, boundaries, exclusions | Foundation / Planning Only | No |
| `Docs/architecture.md` | Technical and data boundaries; Personal/Borrower intent | Foundation / Planning Only | No |
| `Docs/security.md` | Security, privacy, consent | Foundation / Planning Only | No |
| `Docs/authorization-matrix.md` | Access intersection; MVP preset matrix; grant catalog v1 | Accepted product policy (PLM-DOC-05) | No |
| `Docs/development-plan.md` | Delivery buckets PLM-00–PLM-14 | Foundation / Planning Only | No |
| `Docs/roadmap.md` | Current phase and work-package sequence | Foundation / Planning Only | No |
| `Docs/risks-and-decisions.md` | Open risks and decisions | Foundation / Planning Only | No |

## Operating-model direction (PLM-00-WP03)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/Product/lending-operating-model.md` | Origination paths, shared Loan core, roles, branch, PHP, Platform usage | Agreed direction / not a spec | No |
| `Docs/Product/quick-loan-model.md` | Templates, snapshot, eligibility, Personal flow | Agreed direction / not a spec | No |
| `Docs/Product/collector-cash-and-reconciliation.md` | Loan ledger vs collector cash | Agreed direction / not a spec | No |
| `Docs/Product/penalty-exception-and-waiver-model.md` | Penalty, exception, waiver, reversal, post-maturity | Agreed direction / not a spec | No |
| `Docs/Architecture/application-surface-model.md` | Platform Admin, Org Web, MAUI, Personal | Agreed direction / not a spec | No |

## Financial / lifecycle planning (PLM-00-WP04)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/Product/financial-calculation-baseline.md` | Money terms; pointer to PLM-DOC-02 policies | Planning baseline / not a spec | No |
| `Docs/Product/interest-and-finance-charge-policy.md` | MVP methods, formulas, treatments | Accepted product rules (PLM-DOC-02) | No |
| `Docs/Product/fees-and-net-proceeds-policy.md` | Fee bases/treatments; Net Proceeds | Accepted product rules (PLM-DOC-02) | No |
| `Docs/Product/payment-allocation-and-prepayment-policy.md` | Allocation, advance, overpayment | Accepted product rules (PLM-DOC-02) | No |
| `Docs/Product/early-settlement-and-principal-prepayment-policy.md` | Settlement Quote, rebate, principal prepayment | Accepted product rules (PLM-DOC-04) | No |
| `Docs/Product/money-precision-and-rounding-policy.md` | Decimal money; To Even; reconciliation | Accepted product rules (PLM-DOC-02) | No |
| `Docs/Product/payment-and-allocation-model.md` | Payments, posting notes, reversals, idempotency | Planning baseline / not a spec | No |
| `Docs/Product/schedule-maturity-and-settlement.md` | Schedule, calendar, maturity, settlement (index) | Planning baseline / not a spec | No |
| `Docs/Product/schedule-and-collection-calendar-policy.md` | Frequencies, calendar, first due, exceptions | Accepted product rules (PLM-DOC-03) | No |
| `Docs/Product/delinquency-and-missed-payment-policy.md` | Past Due, DPD, missed-day counter, grace | Accepted product rules (PLM-DOC-03) | No |
| `Docs/Product/penalty-assessment-and-cap-policy.md` | Tiers, bases, caps, waiver vs reversal | Accepted product rules (PLM-DOC-03) | No |
| `Docs/Product/maturity-and-post-maturity-policy.md` | Maturity and post-maturity modes | Accepted product rules (PLM-DOC-03) | No |
| `Docs/Product/loan-lifecycle-model.md` | Origination vs lifecycle vs delinquency | Planning baseline / not a spec | No |
| `Docs/Architecture/loan-ledger-and-balance-model.md` | Operational subledger and balances | Planning baseline / not a spec | No |
| `Docs/Architecture/operational-subledger-and-accounting-boundary.md` | Loan vs cash ledgers; not a complete GL | Accepted architecture policy (PLM-DOC-04) | No |

## Authorization / cash / operational workflow (PLM-00-WP05)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/Security/role-and-grant-baseline.md` | Index to PLM Authorization Policy v1 | Accepted product policy (PLM-DOC-05) | No |
| `Docs/Security/authorization-grant-catalog.md` | Exact MVP grant identifiers | Accepted product policy (PLM-DOC-05) | No |
| `Docs/Security/default-role-preset-policy.md` | Role codes and default preset assignments | Accepted product policy (PLM-DOC-05) | No |
| `Docs/Security/resource-scope-and-data-minimization-policy.md` | Scope types and data minimization | Accepted product policy (PLM-DOC-05) | No |
| `Docs/Security/privileged-access-and-owner-recovery-policy.md` | Owner bootstrap, last-Owner protection, recovery | Accepted product policy (PLM-DOC-05) | No |
| `Docs/Product/workflow-authorization-policy.md` | Workflow-state authorization guards | Accepted product policy (PLM-DOC-05) | No |
| `Docs/Product/daily-operational-workflow.md` | Common operating day, assignments, offline boundary | Planning baseline / not a spec | No |
| `Docs/Product/cashier-and-collector-control-model.md` | Cashier Session, float, remittance, cash availability | Planning baseline; float ack in PLM-DOC-09 policy | No |
| `Docs/Product/branch-treasury-and-float-acknowledgment-policy.md` | Branch Treasury; Pending Receipt float acknowledgment | Accepted product rules (PLM-DOC-09) | No |
| `Docs/Product/collector-route-and-location-policy.md` | Routes; optional event GPS; no continuous tracking | Accepted product rules (PLM-DOC-09) | No |
| `Docs/Product/disbursement-and-payment-controls.md` | Office/field disbursement and cash payment | Planning baseline / not a spec | No |
| `Docs/Product/exception-reversal-and-variance-workflow.md` | Exceptions, waivers, reversals vs cash refund, variance | Planning baseline / not a spec | No |
| `Docs/Product/reversal-refund-and-correction-policy.md` | Payment reversal, Refund Payable, cash refund | Accepted product rules (PLM-DOC-04) | No |
| `Docs/Product/cash-variance-and-session-close-policy.md` | Expected vs actual; close-with-variance | Accepted product rules (PLM-DOC-04) | No |
| `Docs/Product/borrower-onboarding-and-verification-policy.md` | Natural-person Borrower minimum | Accepted product rules (PLM-DOC-07) | No |
| `Docs/Product/traditional-application-and-assessment-policy.md` | Traditional application + assessment | Accepted product rules (PLM-DOC-07) | No |
| `Docs/Product/quick-loan-eligibility-and-approval-policy.md` | Quick Loan request minimum | Accepted product rules (PLM-DOC-07) | No |
| `Docs/Product/approval-revision-and-disbursement-readiness-policy.md` | Approval, reapproval, readiness | Accepted product rules (PLM-DOC-07) | No |
| `Docs/Reports/PLM-DOC-07-onboarding-application-and-approval.md` | PLM-DOC-07 closeout | Documentation closeout | No |
| `Docs/Decisions/ADR-013-borrower-onboarding-and-application-minimums.md` | Borrower/application minimums | Accepted (PLM-DOC-07) | No |
| `Docs/Decisions/ADR-014-assessment-approval-and-disbursement-readiness.md` | Assessment, approval, readiness | Accepted (PLM-DOC-07) | No |
| `Docs/Product/write-off-and-recovery-policy.md` | Write-Off, Recovery, component tracking | Accepted product rules (PLM-DOC-06) | No |
| `Docs/Product/collections-case-and-promise-to-pay-policy.md` | PTP, Collection Case, conduct boundaries | Accepted product rules (PLM-DOC-06) | No |
| `Docs/Reports/PLM-DOC-06-restructuring-write-off-recovery-and-collections.md` | PLM-DOC-06 closeout | Documentation closeout | No |
| `Docs/Decisions/ADR-011-restructuring-refinancing-and-hardship.md` | Restructuring; Refinancing deferred | Accepted product policy (PLM-DOC-06) | No |
| `Docs/Decisions/ADR-012-write-off-recovery-and-collections-case-policy.md` | Write-Off, Recovery, collections case | Accepted product policy (PLM-DOC-06) | No |
| `Docs/Security/collector-device-security-policy.md` | Future collector device requirements; not implemented | Accepted future-requirements policy (PLM-DOC-09) | No |
| `Docs/Reports/PLM-DOC-09-mobile-field-treasury-and-ui-boundaries.md` | PLM-DOC-09 closeout | Documentation closeout | No |
| `Docs/Decisions/ADR-017-mobile-offline-route-and-device-policy.md` | Mobile, offline, route, device | Accepted (PLM-DOC-09) | No |
| `Docs/Decisions/ADR-018-branch-treasury-float-and-ui-sharing-policy.md` | Treasury, float ack, UI sharing; **PLM-D-00-09 Closed** | Accepted (PLM-DOC-09) | No |

## Borrower / Personal / publishing (PLM-00-WP06)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/Product/borrower-model.md` | PLM-owned Borrower; independent of POS/Personal | Planning baseline / not a spec | No |
| `Docs/Product/borrower-identity-and-duplicate-policy.md` | Ownership, cardinality, duplicate handling | Accepted product rules (PLM-DOC-01) | No |
| `Docs/Product/personal-borrower-linking.md` | Optional consent-based linking and unlink | Planning baseline / not a spec | No |
| `Docs/Product/personal-linking-lifecycle-and-visibility.md` | Link lifecycle, MVP flow, unlink/relink, visibility | Accepted product rules (PLM-DOC-01) | No |
| `Docs/Product/quick-loan-publishing-and-eligibility.md` | Publishing audiences; eligibility ≠ approval | Planning baseline / not a spec | No |
| `Docs/Product/borrower-groups-and-targeting.md` | Organization-owned groups | Planning baseline / not a spec | No |
| `Docs/Architecture/personal-integration-boundary.md` | Personal vs PLM authority | Planning baseline / not a spec | No |

## Traditional origination (PLM-00-WP07)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/Product/traditional-loan-model.md` | Traditional origination path; converges after disbursement | Planning baseline / not a spec | No |
| `Docs/Product/loan-application-and-approval.md` | Application capture, approval, rejection, term changes | Planning baseline / not a spec | No |
| `Docs/Product/loan-product-configuration.md` | Reusable Loan Product (not a Loan) | Planning baseline / not a spec | No |
| `Docs/Product/disbursement-readiness-model.md` | Pre-release checks; approval ≠ disbursement | Planning baseline / not a spec | No |

## Reporting / documents / notifications (PLM-00-WP08)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/Product/reporting-baseline.md` | Dashboard and operational reporting areas | Planning baseline / not a spec | No |
| `Docs/Product/document-and-receipt-policy.md` | Document types, identity, receipts, statements | Accepted product rules (PLM-DOC-08) | No |
| `Docs/Product/reporting-kpi-and-aging-policy.md` | KPI formulas, PAR, aging, report catalog | Accepted product rules (PLM-DOC-08) | No |
| `Docs/Product/notification-and-delivery-policy.md` | Channels, events, delivery safety | Accepted product rules (PLM-DOC-08) | No |
| `Docs/Product/loan-documents-and-receipts.md` | Documents, snapshot, durable receipts | Planning baseline; superseded by PLM-DOC-08 policy | No |
| `Docs/Product/notification-model.md` | Personal and staff notification intent | Planning baseline; superseded by PLM-DOC-08 policy | No |
| `Docs/Product/personal-loan-experience.md` | Personal Loan area; distinct from P2P | Planning baseline / not a spec | No |
| `Docs/Security/audit-and-history-baseline.md` | High-risk history; not editable notes | Planning baseline / not a spec | No |
| `Docs/Security/privacy-retention-and-audit-policy.md` | Classification, retention, audit, privacy | Accepted product policy (PLM-DOC-08) | No |
| `Docs/Reports/PLM-DOC-08-documents-reporting-privacy-and-notifications.md` | PLM-DOC-08 closeout | Documentation closeout | No |
| `Docs/Decisions/ADR-015-documents-receipts-and-reporting-policy.md` | Documents, receipts, reporting | Accepted (PLM-DOC-08) | No |
| `Docs/Decisions/ADR-016-notification-privacy-retention-and-audit-policy.md` | Notification, privacy, retention, audit | Accepted (PLM-DOC-08) | No |

## Technical layout / integration (PLM-00-WP09)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/Architecture/source-and-project-layout.md` | Physical layout; Client Gate B scaffold | PLM-01 + Gate B | Product shell + React Client |
| `Docs/Architecture/react-pwa-capacitor-client.md` | Shared React + PWA + Capacitor architecture | Accepted (PLM-D-00-09); Gate B scaffold | React foundation |
| `Docs/Architecture/api-and-contract-boundary.md` | API consumers; Personal contracts | Planning target / not a spec | No |
| `Docs/Architecture/persistence-and-database-boundary.md` | Separate DB isolation | Planning target / not a spec | No |
| `Docs/Architecture/mobile-offline-boundary.md` | Online-first; LocalStore not authorized | Planning target / not a spec | Online-first PWA |
| `Docs/Architecture/mobile-and-offline-operating-model.md` | MVP authority; cache/drafts; deferred posting | Accepted architecture policy (PLM-DOC-09) | No |
| `Docs/Architecture/web-maui-component-sharing-policy.md` | Web/MAUI sharing; **PLM-D-00-09 Closed** | Accepted architecture policy (PLM-DOC-09) | No |
| `Docs/Architecture/platform-commercial-integration.md` | Platform contracts; D-P12-03 open | Planning target / not a spec | Platform access API |
| `Docs/Decisions/PLM-D-00-09-react-pwa-capacitor-client-strategy.md` | ADR: one React + PWA + Capacitor client | Accepted / Product Owner Approved | React Client |
| `Docs/Reports/PLM-01A-react-pwa-capacitor-architecture-decision.md` | PLM-01A evidence | Architecture decision complete | No |
| `Docs/Reports/PLM-CLIENT-GATE-B-react-client-scaffold.md` | Gate B React Client scaffold | Complete after validation | React foundation |
| `Docs/Reports/PLM-CLIENT-GATE-C-browser-pwa-foundation.md` | Gate C Browser + PWA foundation | Complete after validation | Online-first PWA |
| `Docs/Reports/PLM-CLIENT-GATE-D0-browser-auth-transport.md` | Gate D0 browser session auth transport | Complete after validation | Same-origin `/platform-api` + cookie policy |
| `Docs/Reports/PLM-CLIENT-GATE-D1-mobile-sign-in-session.md` | Gate D1 Sign In + session UI | Complete after validation | Cookie Sign In |
| `Docs/Reports/impl-gate-d2-account-lifecycle/` | Gate D2 screenshots | Complete after validation | Account lifecycle |
| `Docs/Reports/PLM-CLIENT-GATE-D3-organization-product-access.md` | Gate D3 org discovery + product access gate | Complete after validation | Fail-closed workspace gate |
| `Docs/Reports/impl-gate-d3-organization-product-access/` | Gate D3 screenshots | Complete after validation | Org/product-access gate |
| `Docs/Reports/PLM-PWA-H1-cache-storage-security.md` | PWA cache/storage security proof | Complete after validation | NetworkOnly APIs |
| `Docs/Reports/PLM-PWA-H2-install-update-lifecycle.md` | PWA install/update lifecycle | Complete after validation | User-triggered refresh |
| `Docs/Reports/PLM-PWA-H3-connectivity-fail-closed.md` | Fail-closed connectivity UX | Complete after validation | No financial offline |
| `Docs/Reports/PLM-PWA-H4-production-preview-reliability.md` | Production-preview PWA reliability | Complete after validation | Evidence before lending |
| `Docs/Reports/PLM-PWA-H5-csrf-compatibility.md` | CSRF compatibility with Platform auth | Complete after validation | Antiforgery transport |
| `Docs/Reports/PLM-02A-server-access-boundary-foundation.md` | Server fail-closed access boundary | PLM-02A complete | No lending/persistence |

## Foundation closeout (PLM-00-WP10)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `Docs/Reports/PLM-00-foundation-closeout.md` | PLM-00 vision, gates, next phase | Planning closeout | No |
| `Docs/Reports/PLM-DOC-01-product-identity-and-personal-linking.md` | PLM-DOC-01 identity and Personal linking | Documentation closeout | No |
| `Docs/Reports/PLM-DOC-02-financial-calculation-and-allocation.md` | PLM-DOC-02 calculation, fees, rounding, allocation | Documentation closeout | No |
| `Docs/Reports/PLM-DOC-03-schedule-delinquency-penalty-and-maturity.md` | PLM-DOC-03 calendar, delinquency, penalty, maturity | Documentation closeout | No |
| `Docs/Reports/PLM-DOC-04-settlement-reversals-variance-and-accounting.md` | PLM-DOC-04 settlement, reversals, variance, accounting | Documentation closeout | No |
| `Docs/Reports/PLM-DOC-05-authorization-and-operational-security.md` | PLM-DOC-05 roles, grants, workflow security | Documentation closeout | No |
| `Docs/Decisions/ADR-009-role-codes-grant-catalog-and-default-presets.md` | Role codes and grant catalog; PLM-D-00-06 Closed | Accepted product policy (PLM-DOC-05) | No |
| `Docs/Decisions/ADR-010-resource-scope-workflow-security-and-owner-recovery.md` | Scope, workflow security, Owner recovery | Accepted product policy (PLM-DOC-05) | No |
| `Docs/implementation-gates.md` | Gate A–E before implementation resume | Accepted (PLM-DOC-11) | No |
| `Docs/Reports/PLM-final-documentation-closeout.md` | PLM-DOC-11 final closeout | Documentation closeout | No |
| `Docs/Decisions/PLM-decision-status-summary.md` | Final decision status summary | Accepted (PLM-DOC-11) | No |
| `Docs/Validation/PLM-final-documentation-readiness-checklist.md` | Final docs readiness checklist | Accepted (PLM-DOC-11) | No |

## Workspace indexes (PLM-00-WP01, updated in WP02–WP10)

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `src/Products/PinoyLoanManager/` | Product workspace root | PLM-01 + Client Gates + PLM-02A | Product shell + React Client |
| `src/Products/PinoyLoanManager/Docs/` | Authoritative product documentation root (D-P12-02) | PLM-DOC-11 complete | Documentation |
| `Docs/Reports/PLM-01-product-scaffold-and-isolation.md` | PLM-01 scaffold evidence | Scaffold complete | Product shell |
| `Docs/Reports/PLM-D3-PRE-product-registration-self-access.md` | Product code + access API prerequisite | PLM-D3-PRE complete | Platform prerequisite |

---

## Implemented projects (PLM-01 shell + Client Gates + PLM-02A)

| Item | Status |
|---|---|
| `ExItS.PinoyLoanManager.Domain` | Created — access types; no lending entities |
| `ExItS.PinoyLoanManager.Application` | Created — operational access guard; no lending use cases |
| `ExItS.PinoyLoanManager.Infrastructure` | Created — no EF/Npgsql/DbContext |
| `ExItS.PinoyLoanManager.Api` | Created — health + fail-closed access boundary |
| `ExItS.PinoyLoanManager.ApiClient` | Created — marker only |
| `ExItS.PinoyLoanManager.Web` | Created — identity shell only |
| `ExItS.PinoyLoanManager.Client` | Created — Gates B–D3 React + PWA + auth + org/product-access gate |
| `tests/ExItS.PinoyLoanManager.UnitTests` | Created — scaffold + access boundary tests |
| `ExItS.slnx` PLM entries | Registered |

---

## Not present (intentionally)

| Item | Reason |
|---|---|
| `ExItS.PinoyLoanManager.Maui` | Superseded by React + PWA + Capacitor (PLM-D-00-09) |
| `ExItS.PinoyLoanManager.LocalStore` | Not justified until offline lending is authorized |
| Database / migration folders | Persistence not authorized (PLM-D-00-02 remains open) |
| Docker / deploy implementation | Not authorized |
| Lending domain entities/use cases | PLM-02 not started |
| `Docs/deployment-notes.md` | Optional until packaging |
