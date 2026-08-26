# Product

**Purpose:** Authoritative **WHAT** — loan-product behavior and business rules.
**Canonical document:** [../product-definition.md](../product-definition.md)
**Status:** Foundation / planning only
**Implementation present:** No

Do not treat this folder as a second product definition. Detailed agreed **direction** (not implementation specs):

| Doc | Subject |
|---|---|
| [lending-operating-model.md](lending-operating-model.md) | Origination paths, shared Loan core, role presets, branch, PHP, Platform usage |
| [quick-loan-model.md](quick-loan-model.md) | Quick Loan templates, snapshot, eligibility, Personal flow |
| [collector-cash-and-reconciliation.md](collector-cash-and-reconciliation.md) | Loan ledger vs collector cash |
| [penalty-exception-and-waiver-model.md](penalty-exception-and-waiver-model.md) | Penalty, exception, waiver, reversal (index) |
| [schedule-and-collection-calendar-policy.md](schedule-and-collection-calendar-policy.md) | Frequencies, collection calendar, first due, exceptions |
| [delinquency-and-missed-payment-policy.md](delinquency-and-missed-payment-policy.md) | Past Due, DPD, missed-day counter, grace |
| [penalty-assessment-and-cap-policy.md](penalty-assessment-and-cap-policy.md) | Tiers, bases, caps, waiver vs reversal |
| [maturity-and-post-maturity-policy.md](maturity-and-post-maturity-policy.md) | Maturity Date, Matured Past Due, post-maturity modes |
| [financial-calculation-baseline.md](financial-calculation-baseline.md) | Money terminology; pointer to PLM-DOC-02 policies |
| [interest-and-finance-charge-policy.md](interest-and-finance-charge-policy.md) | MVP methods, formulas, interest treatments |
| [fees-and-net-proceeds-policy.md](fees-and-net-proceeds-policy.md) | Fee bases/treatments; Net Proceeds |
| [payment-allocation-and-prepayment-policy.md](payment-allocation-and-prepayment-policy.md) | Oldest-due allocation; component order |
| [early-settlement-and-principal-prepayment-policy.md](early-settlement-and-principal-prepayment-policy.md) | Settlement Quote, rebate, principal prepayment |
| [reversal-refund-and-correction-policy.md](reversal-refund-and-correction-policy.md) | Payment reversal, Refund Payable, cash refund |
| [cash-variance-and-session-close-policy.md](cash-variance-and-session-close-policy.md) | Expected vs actual; close-with-variance |
| [disbursement-cancellation-and-reversal-policy.md](disbursement-cancellation-and-reversal-policy.md) | Cancel before release; reverse after recovery |
| [money-precision-and-rounding-policy.md](money-precision-and-rounding-policy.md) | Decimal money; To Even; reconciliation |
| [payment-and-allocation-model.md](payment-and-allocation-model.md) | Payments, posting notes, reversals |
| [schedule-maturity-and-settlement.md](schedule-maturity-and-settlement.md) | Schedule, calendar, maturity, settlement |
| [loan-lifecycle-model.md](loan-lifecycle-model.md) | Origination vs lifecycle vs delinquency |
| [daily-operational-workflow.md](daily-operational-workflow.md) | Common operating day, assignments, offline boundary |
| [cashier-and-collector-control-model.md](cashier-and-collector-control-model.md) | Cashier Session, float, remittance, cash availability |
| [branch-treasury-and-float-acknowledgment-policy.md](branch-treasury-and-float-acknowledgment-policy.md) | Branch Treasury; Pending Receipt float acknowledgment |
| [collector-route-and-location-policy.md](collector-route-and-location-policy.md) | Routes; optional event GPS; no continuous tracking |
| [disbursement-and-payment-controls.md](disbursement-and-payment-controls.md) | Office/field disbursement and cash payment |
| [exception-reversal-and-variance-workflow.md](exception-reversal-and-variance-workflow.md) | Exceptions, waivers, reversals vs cash refund, variance |
| [borrower-onboarding-and-verification-policy.md](borrower-onboarding-and-verification-policy.md) | Natural-person Borrower minimum |
| [traditional-application-and-assessment-policy.md](traditional-application-and-assessment-policy.md) | Traditional application + assessment |
| [quick-loan-eligibility-and-approval-policy.md](quick-loan-eligibility-and-approval-policy.md) | Quick Loan request minimum |
| [approval-revision-and-disbursement-readiness-policy.md](approval-revision-and-disbursement-readiness-policy.md) | Approval, reapproval, readiness |
| [borrower-identity-and-duplicate-policy.md](borrower-identity-and-duplicate-policy.md) | Ownership, cardinality, duplicate handling |
| [personal-borrower-linking.md](personal-borrower-linking.md) | Optional consent-based linking |
| [personal-linking-lifecycle-and-visibility.md](personal-linking-lifecycle-and-visibility.md) | Link lifecycle, MVP flow, unlink/relink, visibility |
| [quick-loan-publishing-and-eligibility.md](quick-loan-publishing-and-eligibility.md) | Publishing audiences; eligibility ≠ approval |
| [borrower-groups-and-targeting.md](borrower-groups-and-targeting.md) | Organization-owned groups |
| [traditional-loan-model.md](traditional-loan-model.md) | Traditional origination path |
| [loan-application-and-approval.md](loan-application-and-approval.md) | Application, approval, rejection |
| [loan-product-configuration.md](loan-product-configuration.md) | Reusable Loan Product configuration |
| [disbursement-readiness-model.md](disbursement-readiness-model.md) | Pre-release checks |
| [workflow-authorization-policy.md](workflow-authorization-policy.md) | Workflow-state authorization guards |
| [reporting-baseline.md](reporting-baseline.md) | Dashboard and operational reporting |
| [document-and-receipt-policy.md](document-and-receipt-policy.md) | Document types, identity, receipts, statements |
| [reporting-kpi-and-aging-policy.md](reporting-kpi-and-aging-policy.md) | KPI formulas, PAR, aging, report catalog |
| [notification-and-delivery-policy.md](notification-and-delivery-policy.md) | Channels, events, delivery safety |
| [loan-documents-and-receipts.md](loan-documents-and-receipts.md) | Documents and receipts (planning baseline) |
| [notification-model.md](notification-model.md) | Notifications (planning baseline) |
| [restructuring-and-hardship-policy.md](restructuring-and-hardship-policy.md) | Restructuring, hardship; Refinancing deferred |
| [write-off-and-recovery-policy.md](write-off-and-recovery-policy.md) | Write-Off, Recovery |
| [collections-case-and-promise-to-pay-policy.md](collections-case-and-promise-to-pay-policy.md) | PTP, Collection Case |

Remaining default **rates**, restructuring/write-off, and legal validation stay open in [../product-definition.md](../product-definition.md) and [../risks-and-decisions.md](../risks-and-decisions.md) (PLM-D-00-08 remainder, PLM-D-00-11). Grant catalog v1 is closed for MVP (PLM-D-00-06). Do not invent:

- peso/percent **rates** or penalty **amounts** as defaults
- legal/regulatory operating rules
