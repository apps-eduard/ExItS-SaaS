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

Remaining calculation algorithms, rates, and legal validation stay open in [../product-definition.md](../product-definition.md) and [../risks-and-decisions.md](../risks-and-decisions.md) (PLM-D-00-08, PLM-D-00-11). Do not invent:

- interest method/formula or peso/percent rates
- amortization algorithms
- due-date generation
- payment allocation order
- penalty amounts or legal limits
- legal/regulatory operating rules
