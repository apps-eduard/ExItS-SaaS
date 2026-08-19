# Pinoy Loan Manager — Quick Loan Model

**Status:** Agreed product direction (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Quick Loan is one origination path. After disbursement it uses the **same core Loan model** as Traditional Loan. See [lending-operating-model.md](lending-operating-model.md).

This is not an implementation specification. No interest rate, fee, or legal permissibility is claimed.

---

## Template model

Organizations may create **many** Quick Loan Templates. Names below are examples only — **not** built-in product types:

- Quick Loan 30 Days
- Weekly Quick Loan
- Employee Quick Loan
- VIP Quick Loan
- Emergency Quick Loan

A template should eventually support configurable categories such as the following. No default formula is invented beyond configuration support.

### General

- name
- description
- active / draft / published status

### Loanable amount

- minimum amount
- maximum amount
- optional amount increment

### Term

- term duration
- number of installments where applicable

### Payment frequency

Examples may include daily, weekly, biweekly, semi-monthly, monthly. No frequency is mandated as a Platform default.

### Interest

Quick Loan MVP uses **Flat / Add-On Finance Charge only**. Reducing-balance amortization is **not** supported for Quick Loan MVP.

Supported interest treatments: deducted from proceeds, or added to repayment. Deducted finance charge is satisfied at disbursement and must not also be scheduled as unpaid interest.

Canonical formulas, rate bases, snapshot, and disclosure: [interest-and-finance-charge-policy.md](interest-and-finance-charge-policy.md), [fees-and-net-proceeds-policy.md](fees-and-net-proceeds-policy.md). Terminology: [financial-calculation-baseline.md](financial-calculation-baseline.md).

Do **not** hard-code a specific rate. No default rate is defined. Do **not** claim legal permissibility of any configuration (PLM-D-00-11).

### Payment / schedule

- schedule generated from **snapshot** terms (calendar: [schedule-and-collection-calendar-policy.md](schedule-and-collection-calendar-policy.md))
- first due date default = next valid Collection Day after Disbursement; same-day first installment not MVP
- customer must see the calculation and actual first due date before submitting
- default exception policy = Shift Future Due Dates

### Penalty policy

Template-specific configurable policy. No default amount. Canonical: [penalty-assessment-and-cap-policy.md](penalty-assessment-and-cap-policy.md), [penalty-exception-and-waiver-model.md](penalty-exception-and-waiver-model.md).

### Eligibility

Configurable eligibility rules (see Publishing / eligibility below).

### Publishing

Audience targeting (not a global ExItS-user broadcast).

### Disbursement

- office / cashier
- collector
- future channels (not implemented)

### Collection

Field collection behavior as configured; collector cash rules: [collector-cash-and-reconciliation.md](collector-cash-and-reconciliation.md).

---

## Template versioning / snapshot (mandatory design intent)

Once a customer submits a Quick Loan Request, the system must **snapshot** the financial and operational terms used for that request.

Changing the source Quick Loan Template later must **not** silently change:

- submitted requests
- approved requests
- disbursed loans
- existing schedules
- interest
- fees
- penalties
- eligibility decision basis
- other material agreed terms

Future implementation should use explicit versioning / snapshot semantics. Schema is **not** designed in this package.

---

## Publishing and eligibility

Publishing and eligibility detail: [quick-loan-publishing-and-eligibility.md](quick-loan-publishing-and-eligibility.md), [borrower-groups-and-targeting.md](borrower-groups-and-targeting.md). Personal linking: [personal-borrower-linking.md](personal-borrower-linking.md).

Publishing audiences:

- All Eligible Linked Borrowers of the organization
- Borrower Group
- Selected Borrower(s)

Do **not** use a meaning equivalent to “all ExItS users globally”.

A user being a **POS Customer** alone must never make them a Loan Borrower. Personal / Borrower relationship remains separate and consent-based. See [../architecture.md](../architecture.md).

Eligibility should eventually allow configurable rules such as:

- maximum concurrent active Quick Loans
- no overdue loan
- completed-loan history
- borrower group
- outstanding exposure
- other future organization rules

**Engineering baseline:** default maximum active Quick Loans = **1 per borrower per organization**. This must be configurable by approved eligibility policy / template.

**Manual organization approval** remains the initial / default approval model. Do **not** implement auto-approval yet.

---

## Personal customer flow

ExItS Personal is the customer / borrower **presentation** surface. Pinoy Loan Manager remains authoritative for Loan operational data. Surfaces: [../Architecture/application-surface-model.md](../Architecture/application-surface-model.md).

```text
Organization publishes offer
        ↓
Eligible linked borrower sees it in ExItS Personal
        ↓
Customer opens offer
        ↓
Customer chooses amount within allowed range
        ↓
System calculates and displays terms
        ↓
Customer reviews (minimum display: [interest-and-finance-charge-policy.md](interest-and-finance-charge-policy.md)):
  - requested amount
  - calculation method, rate, rate basis
  - total finance charge and each fee
  - total deductions and Net Proceeds
  - installment amount(s), count, frequency
  - first due date, maturity date
  - Total Scheduled Repayment
  - payment-allocation policy summary
        ↓
Customer submits
        ↓
Pending Approval
        ↓
Approved / Rejected
```

Approved does **not** mean Disbursed.

```text
Approved
  → Awaiting Disbursement
  → Disbursed
  → Active
```

Office/cashier or collector may release funds according to the snapshotted disbursement configuration and product-local grants.

---

## Explicit non-goals

- Built-in named loan products as Platform types
- Auto-approval
- Silent mutation of submitted/approved/disbursed terms when a template is edited
- Specific interest rates, peso amounts, or legal claims
- Personal becoming a second loan ledger
