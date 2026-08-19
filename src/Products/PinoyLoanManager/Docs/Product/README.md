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
| [penalty-exception-and-waiver-model.md](penalty-exception-and-waiver-model.md) | Penalty, exception, waiver, reversal, post-maturity |
| [financial-calculation-baseline.md](financial-calculation-baseline.md) | Money terminology; pointer to PLM-DOC-02 policies |
| [interest-and-finance-charge-policy.md](interest-and-finance-charge-policy.md) | MVP methods, formulas, interest treatments |
| [fees-and-net-proceeds-policy.md](fees-and-net-proceeds-policy.md) | Fee bases/treatments; Net Proceeds |
| [payment-allocation-and-prepayment-policy.md](payment-allocation-and-prepayment-policy.md) | Oldest-due allocation; component order |
| [money-precision-and-rounding-policy.md](money-precision-and-rounding-policy.md) | Decimal money; To Even; reconciliation |
| [payment-and-allocation-model.md](payment-and-allocation-model.md) | Payments, posting notes, reversals |
| [schedule-maturity-and-settlement.md](schedule-maturity-and-settlement.md) | Schedule, calendar, maturity, settlement |
| [loan-lifecycle-model.md](loan-lifecycle-model.md) | Origination vs lifecycle vs delinquency |
| [daily-operational-workflow.md](daily-operational-workflow.md) | Common operating day, assignments, offline boundary |
| [cashier-and-collector-control-model.md](cashier-and-collector-control-model.md) | Cashier Session, float, remittance, cash availability |
| [disbursement-and-payment-controls.md](disbursement-and-payment-controls.md) | Office/field disbursement and cash payment |
| [exception-reversal-and-variance-workflow.md](exception-reversal-and-variance-workflow.md) | Exceptions, waivers, reversals vs cash refund, variance |
| [borrower-model.md](borrower-model.md) | PLM-owned Borrower; may exist without Personal |
| [borrower-identity-and-duplicate-policy.md](borrower-identity-and-duplicate-policy.md) | Ownership, cardinality, duplicate handling |
| [personal-borrower-linking.md](personal-borrower-linking.md) | Optional consent-based linking |
| [personal-linking-lifecycle-and-visibility.md](personal-linking-lifecycle-and-visibility.md) | Link lifecycle, MVP flow, unlink/relink, visibility |
| [quick-loan-publishing-and-eligibility.md](quick-loan-publishing-and-eligibility.md) | Publishing audiences; eligibility ≠ approval |
| [borrower-groups-and-targeting.md](borrower-groups-and-targeting.md) | Organization-owned groups |
| [traditional-loan-model.md](traditional-loan-model.md) | Traditional origination path |
| [loan-application-and-approval.md](loan-application-and-approval.md) | Application, approval, rejection |
| [loan-product-configuration.md](loan-product-configuration.md) | Reusable Loan Product configuration |
| [disbursement-readiness-model.md](disbursement-readiness-model.md) | Pre-release checks |
| [reporting-baseline.md](reporting-baseline.md) | Dashboard and operational reporting |
| [loan-documents-and-receipts.md](loan-documents-and-receipts.md) | Documents and receipts |
| [notification-model.md](notification-model.md) | Notifications |
| [personal-loan-experience.md](personal-loan-experience.md) | Personal Loan area |

Remaining default **rates**, calendar/penalty/settlement items, grant identifiers, and legal validation stay open in [../product-definition.md](../product-definition.md) and [../risks-and-decisions.md](../risks-and-decisions.md) (PLM-D-00-06, PLM-D-00-08 remainder, PLM-D-00-11, PLM-D-00-13). Do not invent:

- peso/percent **rates** as defaults
- penalty amounts or legal limits
- legal/regulatory operating rules
