# Pinoy Loan Manager — Early Settlement and Principal Prepayment Policy

**Status:** Accepted product policy (PLM-DOC-04); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Full early settlement, Settlement Quotes, unearned finance-charge rebate, and explicit partial principal prepayment. Not a settlement engine, default price list, or legally validated disclosure.

**Canonical companions:** [payment-allocation-and-prepayment-policy.md](payment-allocation-and-prepayment-policy.md), [reversal-refund-and-correction-policy.md](reversal-refund-and-correction-policy.md), [interest-and-finance-charge-policy.md](interest-and-finance-charge-policy.md), [fees-and-net-proceeds-policy.md](fees-and-net-proceeds-policy.md), [money-precision-and-rounding-policy.md](money-precision-and-rounding-policy.md). ADR: [../Decisions/ADR-007-early-settlement-and-prepayment-policy.md](../Decisions/ADR-007-early-settlement-and-prepayment-policy.md).

Restructuring and write-off remain later / remaining PLM-D-00-08 items. **No MVP settlement or prepayment penalty is approved.**

---

## Prepayment principle

A borrower may request **full early settlement** or **partial principal prepayment** before contractual maturity.

PLM MVP must **not** impose:

- an early settlement penalty
- a principal-prepayment penalty
- a hidden settlement fee

This is a Product policy baseline. It does **not** replace required legal/compliance review (PLM-D-00-11).

Any future legally permitted administrative charge would require an explicit future Product Owner decision, clear disclosure, legal validation, and a separately versioned policy. No such charge is approved in MVP.

---

## Ordinary advance payment vs principal prepayment

| Concept | Meaning |
|---|---|
| **Ordinary advance payment** | Follows PLM-DOC-02 allocation. Satisfies future scheduled obligations chronologically. Does **not** automatically regenerate the schedule or reduce contracted Flat/Add-On finance charge |
| **Principal prepayment** | Explicit borrower-requested action. Not inferred from excess. Applies an approved amount to outstanding principal. Requires recalculation/rebate. Creates a **new schedule version**. Prior schedule remains historically visible |

A payment must not silently switch from advance payment to principal prepayment.

---

## Settlement Quote

Full early settlement requires a Settlement Quote. Generating or accepting a quote does **not** settle the Loan.

A Settlement Quote must contain:

- Quote ID, Organization, Branch, Borrower, Loan
- quote effective local date
- generated UTC timestamp
- valid-through local date/time
- calculation method/version and settlement policy/version
- Outstanding Principal
- earned/accrued finance charge
- unearned finance-charge rebate/credit
- applicable fees; refundable/waived fees
- assessed penalties; penalty waivers/reversals
- other approved adjustments
- final Settlement Amount
- disclosure notes
- status

Conceptual quote statuses (not finalized enums): Draft, Issued, Expired, Invalidated, Consumed, Cancelled.

### Validity (MVP default)

**Until the end of its stated Branch-local business date.**

A Loan Product may allow an explicit later valid-through date where components are stable, validity is stated, the policy is snapshotted, and no intervening event invalidates the quote. Do **not** select a universal maximum number of validity days.

Invalidate the quote after material events: payment, payment reversal, fee/penalty assessment/waiver/reversal, schedule revision, principal prepayment, restructuring, or other balance-changing adjustment. Recalculate expired or invalid quotes.

### Consumption

Consumed only when a valid settlement payment is posted, quote requirements are met, Total Outstanding reaches zero, and settlement posting succeeds authoritatively.

A partial payment against a quote does **not** silently mark the quote consumed. Future posting must be **idempotent**.

---

## Full settlement formula

```text
Settlement Amount
  = Outstanding Principal
  + Earned/Accrued Unpaid Finance Charge
  + Applicable Outstanding Fees
  + Valid Outstanding Penalties
  + Other Approved Debit Adjustments
  − Unearned Finance-Charge Rebate
  − Refundable/Unearned Fee Credits
  − Approved Waivers
  − Other Approved Credit Adjustments
```

Floor final Settlement Amount at **PHP 0.00**. Any excess approved credit becomes **Refund Payable** ([reversal-refund-and-correction-policy.md](reversal-refund-and-correction-policy.md)). Every component must be separately explainable.

No new settlement penalty is allowed. Valid outstanding penalties remain unless waived/reversed. Early settlement does not automatically erase a validly assessed penalty.

---

## Flat / Add-On — added to repayment

Settlement includes outstanding principal, earned and unpaid finance charge, applicable non-refundable/earned fees, assessed penalties, and other valid outstanding items.

Excludes / rebates: unearned future finance charge; future refundable/unearned fees according to snapshot policy; approved waivers and credits.

Unearned finance charge must **not** remain collectible after full early settlement.

### Finance-charge earning schedule (MVP)

Every Flat/Add-On Loan maintains a reproducible finance-charge earning schedule:

- each installment has a snapshotted finance-charge component
- components with effective due dates **on or before** the quote effective date are **earned**
- components with effective due dates **after** the quote effective date are **unearned**
- paid amounts, waivers, and reversals are considered separately
- prior schedule versions remain visible

Legal/compliance review must confirm whether a different or minimum statutory rebate formula is required before Production (PLM-D-00-11).

---

## Flat / Add-On — deducted from proceeds

The finance charge was collected through proceeds deduction at Disbursement. The Loan must still maintain a finance-charge earning schedule for rebate purposes.

At early settlement:

- earned portion remains earned
- unearned future portion becomes a **Finance-Charge Rebate Credit**
- the rebate credit is applied transparently against Settlement Amount
- the charge must not be collected twice
- the rebate is a separate auditable financial event

If valid rebate/credits exceed remaining Settlement Amount: floor at PHP 0.00; excess becomes Refund Payable; not a hidden borrower wallet.

---

## Reducing-balance settlement

Settlement includes Outstanding Principal, unpaid due interest, current-period accrued interest through the quote effective date, applicable fees/penalties, and approved adjustments.

Future interest after the quote effective date is **not** charged. No precomputed future finance-charge rebate is needed because future reducing interest has not yet accrued.

### Current-period accrual (Actual-Days-in-Current-Period)

```text
P_open     = opening Outstanding Principal of the current installment period
i          = snapshotted periodic decimal rate
PeriodStart = previous effective due date, or Disbursement Date for the first period
PeriodEnd   = next effective due date
PeriodDays  = calendar-day difference between PeriodStart and PeriodEnd
ElapsedDays = calendar-day difference between PeriodStart and quote effective date,
              bounded from 0 through PeriodDays

CurrentPeriodInterestRaw = P_open × i × ElapsedDays / PeriodDays
```

Apply decimal precision and To-Even rounding at the documented posting boundary. Do not double-count already due interest. Do not accrue beyond PeriodEnd under this formula. Use Branch-local financial dates. Preserve inputs in the quote. Do not infer a new rate.

---

## Fees at early settlement

Every fee snapshot must state whether it is refundable, non-refundable once assessed, earned over schedule periods, or waived only through authorization.

| Treatment | MVP settlement |
|---|---|
| Upfront deducted | Earned at Disbursement unless snapshot explicitly marks it refundable |
| Financed | Earned/unearned follows snapshot; unearned refundable portion becomes a settlement credit |
| Scheduled | Fees whose effective due dates have not arrived may be removed from the settlement obligation when marked refundable/unearned; already due/earned fees remain unless waived/reversed |

Do not silently change original fee terms.

---

## Partial principal prepayment — common rules

Before applying Principal Prepayment:

1. satisfy all Past Due using accepted payment allocation
2. satisfy Current Due where required by the Loan policy
3. identify the explicit Principal Prepayment amount
4. validate it does not exceed Outstanding Principal
5. calculate any associated finance-charge rebate
6. post principal reduction and rebate as **separate** financial effects
7. generate a new schedule version
8. preserve the original schedule

Principal Prepayment receives its own transaction/reference identity.

### Reducing-balance

Apply the explicit prepayment to Outstanding Principal after current-period accrued interest. Recalculate future interest from the reduced principal. Default MVP schedule treatment = **Reduce Term** (keep contractual installment amount as close as possible; reduce remaining installment count; reconcile final installment with To-Even rounding).

**Reduce Installment / Keep Maturity** is deferred unless separately approved later.

### Flat / Add-On

```text
Prepayment Ratio = Principal Prepayment / Outstanding Principal Before Prepayment
Finance-Charge Rebate Raw = Total Unearned Future Finance Charge × Prepayment Ratio
```

Round with To-Even. Reduce Outstanding Principal by the explicit prepayment. Reduce future finance-charge obligation by the rebate. Default schedule treatment = **Reduce Term**. Preserve prior schedule; create a new version.

For deducted-interest Loans, the calculated rebate is a **credit** because the charge was already collected. Excess credit follows Refund Payable rules.

---

## No negative balances

Must not silently produce negative Outstanding Principal, Settlement Amount, fee balance, penalty balance, or unexplained borrower credit. Use a zero floor where appropriate, explicit Refund Payable, and explicit corrections/reversals. No general borrower wallet in MVP.

---

## Invalid operations

Future validation must reject:

- settlement using an expired or invalid quote
- quote consumption twice
- negative Settlement Amount
- prepayment greater than Outstanding Principal
- inferred principal prepayment from unexplained excess
- unexplained borrower credit / silent negative balances

---

## Legal / compliance boundary

No rebate, quote, or prepayment rule is claimed legally sufficient. **PLM-D-00-11 remains Open.** This package does not invent Philippine regulations.

---

## Explicit non-goals

- MVP settlement/prepayment penalty
- Borrower wallet
- Reduce Installment / Keep Maturity
- Restructuring / write-off
- Statutory rebate formula (legal review)
- Implementation of a settlement engine
