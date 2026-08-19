# Pinoy Loan Manager — Fees and Net Proceeds Policy

**Status:** Accepted product policy (PLM-DOC-02); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Structured borrower-facing fee model, fee treatments, Net Proceeds, snapshot/disclosure, and Platform usage-charge separation. Not a fee catalog, default price list, or legally validated disclosure.

**Canonical companions:** [interest-and-finance-charge-policy.md](interest-and-finance-charge-policy.md), [money-precision-and-rounding-policy.md](money-precision-and-rounding-policy.md), [payment-allocation-and-prepayment-policy.md](payment-allocation-and-prepayment-policy.md). ADR: [../Decisions/ADR-004-rounding-fees-and-payment-allocation.md](../Decisions/ADR-004-rounding-fees-and-payment-allocation.md).

No default fee amount or percentage is defined in this package.

---

## Terminology

| Term | Meaning |
|---|---|
| Upfront Fee | A disclosed fee deducted when money is released |
| Financed Fee | A disclosed fee added to the repayment obligation |
| Scheduled Fee | A disclosed fee assigned to one or more scheduled obligations |
| Net Proceeds | The cash/value actually released to the borrower |
| Total Scheduled Repayment | The total contractual amount expected under the original schedule |
| Outstanding Charges | Interest/finance charges, fees, or other approved non-principal obligations not yet satisfied |
| Penalty | A separately assessed late/delinquency charge ([penalty-assessment-and-cap-policy.md](penalty-assessment-and-cap-policy.md)) |

---

## Fee definition

A fee definition must include:

- organization
- name / display label
- description / purpose
- calculation basis
- amount or rate
- timing / treatment
- refundable / waivable behavior where approved
- active / archive state
- policy / version identity

Each fee must use **exactly one** approved treatment for the Loan snapshot unless a future policy explicitly supports component splitting.

---

## MVP calculation bases

| Basis | Meaning |
|---|---|
| Fixed Amount | A disclosed peso amount |
| Percentage of Contract / Face Principal | A disclosed percentage of `P` |

Do **not** support arbitrary code or formulas.

Do **not** calculate a fee as a percentage of:

- another fee
- a penalty
- a Platform usage charge
- an unexplained balance

unless a future separately approved policy explicitly requires it.

---

## Fee treatments

| Treatment | Effect |
|---|---|
| **Upfront Deducted** | Deducted from proceeds at disbursement |
| **Financed** | Added to Total Scheduled Repayment and distributed/scheduled according to terms |
| **Scheduled** | Assigned explicitly to one or more schedule obligations |

---

## Net Proceeds

Depends on interest treatment. Canonical formulas: [interest-and-finance-charge-policy.md](interest-and-finance-charge-policy.md).

**Added To Repayment:**

```text
Net Proceeds = P − Upfront Deducted Fees
```

**Deducted From Proceeds:**

```text
Net Proceeds = P − Total Finance Charge − Upfront Deducted Fees
```

Do not hide deductions. A Loan must not be represented merely as “Borrow PHP 3,000” when the customer actually receives less.

---

## Disclosure before submission and disbursement

The borrower/customer-facing calculation must disclose at minimum:

- Contract Principal
- Total Finance Charge
- each fee (name, purpose, amount, treatment)
- total deductions
- Net Proceeds
- installment amount(s)
- installment count
- payment frequency
- Total Scheduled Repayment

Show every borrower-facing fee **separately**. Do not hide all fees inside one unexplained “Other Charges” amount.

Full customer display list: [interest-and-finance-charge-policy.md](interest-and-finance-charge-policy.md).

Do **not** claim this display satisfies legal disclosure requirements until qualified review (PLM-D-00-11).

---

## Snapshot

Once a request/application is submitted:

- fee definitions and calculated amounts are snapshotted
- later template/product fee changes do not silently change that request
- approved/disbursed Loans retain their original fee terms
- fee name, purpose, amount, and treatment remain reproducible

---

## Platform usage charge is not a borrower fee

| Charge | Direction |
|---|---|
| ExItS Platform Usage Charge | Organization → ExItS Platform |
| Borrower-facing Loan Fee | Borrower → Lending Organization |

The Platform usage charge must **not** automatically:

- reduce borrower Net Proceeds
- increase Loan repayment
- appear as borrower interest
- appear as a borrower fee
- enter the Loan subledger

If an organization creates an independently justified borrower-facing fee, that fee must use this disclosed PLM fee model and must **not** pretend to be the ExItS Platform charge.

Preferred Platform usage billable event remains **LOAN DISBURSED**. Transport remains D-P12-03.

---

## Fee changes, waivers, refunds, and reversals

Do **not** silently edit a posted fee.

Future concepts (not designed as schema here):

- Fee Assessment
- Fee Waiver
- Fee Reversal
- Fee Refund / Cash Movement where applicable

The original fee remains historically visible.

A financial fee reversal is **not** automatically proof that physical cash was returned. Cash refund remains a separate correlated physical-cash action.

Exact grant/approval thresholds remain future authorization work (PLM-D-00-06).

---

## Invalid configuration (financial)

Future validation must reject invalid configurations, including:

- negative principal
- zero or negative installment count
- negative rate
- percentage fee without a valid basis
- deducted interest/fees greater than or equal to proceeds where policy forbids zero/negative Net Proceeds
- reducing-balance method combined with deducted-interest treatment
- rate basis missing
- incompatible payment frequency and periodic-rate basis
- schedule totals that do not reconcile
- Net Proceeds below allowed minimum
- unsupported calculation method

Exact business limits remain organization/product/legal decisions. No default peso minimum is invented here.

---

## Legal / compliance boundary

No fee, deduction, or Net Proceeds presentation in this document is claimed legally compliant. External qualified legal/compliance review remains required before Production (PLM-D-00-11). This package does not invent Philippine regulations.

---

## Explicit non-goals

- Default fee amounts or percentages
- Treating Platform usage as a borrower fee
- Penalty rates/amounts (engine accepted; no defaults)
- Silent mutation of posted fees
- Schema / enum design
- Implementation of a fee engine
