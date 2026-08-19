# Pinoy Loan Manager — Payment Allocation and Prepayment Policy

**Status:** Accepted product policy (PLM-DOC-02); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Deterministic payment allocation, partial/multiple/advance payments, overpayment, and reversal. Not a posting engine, cash-refund workflow, or legally validated collections policy.

**Canonical companions:** [interest-and-finance-charge-policy.md](interest-and-finance-charge-policy.md), [fees-and-net-proceeds-policy.md](fees-and-net-proceeds-policy.md), [money-precision-and-rounding-policy.md](money-precision-and-rounding-policy.md), [penalty-assessment-and-cap-policy.md](penalty-assessment-and-cap-policy.md), [maturity-and-post-maturity-policy.md](maturity-and-post-maturity-policy.md), [early-settlement-and-principal-prepayment-policy.md](early-settlement-and-principal-prepayment-policy.md), [reversal-refund-and-correction-policy.md](reversal-refund-and-correction-policy.md). ADR: [../Decisions/ADR-004-rounding-fees-and-payment-allocation.md](../Decisions/ADR-004-rounding-fees-and-payment-allocation.md). Operational posting notes: [payment-and-allocation-model.md](payment-and-allocation-model.md).

Penalty **amounts** remain undefined. Settlement and principal-prepayment calculation: [early-settlement-and-principal-prepayment-policy.md](early-settlement-and-principal-prepayment-policy.md).

---

## Terminology

| Term | Meaning |
|---|---|
| Current Due | Amount due for the current obligation/date |
| Past Due | Unpaid obligations whose due dates have passed |
| Outstanding Principal | Contract principal not yet satisfied |
| Outstanding Charges | Interest/finance charges, fees, or other approved non-principal obligations not yet satisfied |
| Penalty | Separately assessed late/delinquency charge ([penalty-assessment-and-cap-policy.md](penalty-assessment-and-cap-policy.md)) |
| Total Outstanding | All currently outstanding contractual components |
| Settlement Amount | Amount required by a valid settlement quote to fully settle the Loan |

---

## Schedule allocation order (approved)

**Oldest due obligation first.**

The payment engine processes:

1. past-due installments from oldest due date forward
2. current due installment
3. future installments only when allowed by advance-payment policy

Within an installment, use its remaining unpaid components.

A newer installment must not be satisfied while an older due installment is silently left unpaid, unless a future explicitly approved allocation policy allows it.

---

## Component allocation order (MVP, approved)

Within the applicable oldest due obligation:

1. Due Interest / Finance Charge
2. Due Principal
3. Due Scheduled Fees
4. Due Penalties

This is the MVP **contract** allocation policy.

Record that:

- the policy is snapshotted/versioned for each Loan
- the policy is deterministic
- customer-facing terms must disclose the applicable allocation behavior
- changing a template later does not change an existing Loan
- legal/accounting review remains required before Production (PLM-D-00-11)

Do **not** make the allocation order organization-editable in MVP.

A future separately approved version may support additional allocation policies.

Zero or nonexistent components are skipped.

---

## Deducted-interest allocation

For a deducted-interest Loan, the deducted finance charge was satisfied at disbursement.

It must **not** appear as outstanding scheduled interest.

Ordinary payment allocation proceeds through the components that actually remain due (for example principal, scheduled/financed fees, penalties) according to the snapshotted component order.

Do not allocate against a finance-charge component that was already satisfied by proceeds deduction.

---

## Partial payments

**Supported.**

A valid payment smaller than the amount due is posted. Apply it through oldest due obligation first, then component order. Remaining unpaid amount stays due/past due.

Do not reject a valid partial payment solely because it is less than the installment amount.

---

## Multiple payments

**Supported.**

A borrower may make several payments against the same Loan or installment.

Each payment:

- receives its own transaction identity
- is independently auditable
- uses the same deterministic allocation policy
- contributes to the remaining component balances

Do not merge separate posted payments silently.

---

## Advance payment

When all past/current due obligations are satisfied, an additional payment may be applied to future scheduled obligations in chronological order.

For Flat / Add-On Loans:

- Advance payment does **not** automatically reduce the already contracted total finance charge.
- It satisfies future schedule obligations early.
- It does **not** silently regenerate the schedule.

The borrower should be able to see which future installments were satisfied.

---

## Principal prepayment

Principal prepayment is **not** the same as ordinary advance installment payment.

Do **not** implicitly convert an excess payment into principal prepayment.

Canonical policy: [early-settlement-and-principal-prepayment-policy.md](early-settlement-and-principal-prepayment-policy.md). Explicit request, rebate/recalculation, new schedule version, Reduce Term default.

For MVP ordinary excess within valid scheduled obligations remains an **advance payment**.

---

## Early settlement

Full early settlement requires a **Settlement Quote**. Canonical formula, method-specific rebate/accrual, quote validity, and consumption: [early-settlement-and-principal-prepayment-policy.md](early-settlement-and-principal-prepayment-policy.md). ADR: [../Decisions/ADR-007-early-settlement-and-prepayment-policy.md](../Decisions/ADR-007-early-settlement-and-prepayment-policy.md).

No MVP settlement/prepayment penalty. Legal/accounting review remains required before Production (PLM-D-00-11).

---

## True overpayment

No general borrower wallet or stored-credit balance in MVP.

A payment greater than the current valid collectible or settlement amount must not silently create unexplained customer credit.

Future operational behavior:

- block the excess before final posting where possible
- show the maximum valid collectible amount
- require explicit correction where the entered amount is wrong

If physical cash was already accepted:

- preserve the original cash facts
- use an explicit refund/cash-correction workflow
- do not mutate the posted payment silently

Exact cash-refund workflow: [reversal-refund-and-correction-policy.md](reversal-refund-and-correction-policy.md).

---

## Payment reversal

Payment reversal does not delete the original payment.

Conceptually:

```text
Original Payment
+ Authorized Reversal Event
+ New Correct Payment where needed
```

Loan payment reversal and physical cash refund are separate correlated actions. Canonical: [reversal-refund-and-correction-policy.md](reversal-refund-and-correction-policy.md).

Grant identifiers: `plm.payments.reversal-request`, `plm.payments.reversal-approve` per [authorization-grant-catalog.md](../Security/authorization-grant-catalog.md).

---

## Versioning

Every Loan snapshots the payment-allocation policy and version. Future engine changes must not silently alter historical Loans.

---

## Legal / compliance boundary

No allocation order, advance-payment, or overpayment rule in this document is claimed legally compliant. External qualified legal/compliance review remains required before Production (PLM-D-00-11). This package does not invent Philippine regulations.

---

## Explicit non-goals

- Organization-editable allocation order in MVP
- Automatic conversion of excess into principal prepayment
- Borrower wallet / stored credit
- Settlement/prepayment penalty in MVP
- Penalty rates/amounts (engine accepted; no defaults)
- Implementation of a posting engine
