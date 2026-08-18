# Pinoy Loan Manager — Schedule, Maturity, and Settlement

**Status:** Planning / product-rule baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Schedule generation, collection calendar, excused-day treatment, maturity, advance payment, and early settlement. Not a schedule-engine specification.

Related: [financial-calculation-baseline.md](financial-calculation-baseline.md), [payment-and-allocation-model.md](payment-and-allocation-model.md), [loan-lifecycle-model.md](loan-lifecycle-model.md), [penalty-exception-and-waiver-model.md](penalty-exception-and-waiver-model.md).

---

## Schedule generation

The future schedule engine should create **explicit installments**.

Each installment conceptually includes:

- sequence
- due date
- scheduled principal component if applicable
- scheduled interest / charge component if applicable
- scheduled fees if applicable
- scheduled total
- paid amount
- remaining amount
- state

Possible state concepts (not a finalized enum):

- Future
- Due
- Partially Paid
- Paid
- Past Due

Once a Loan is disbursed, the schedule must be based on the Loan’s **snapshotted** terms. No template edit may silently change it.

Term vs installment count: [financial-calculation-baseline.md](financial-calculation-baseline.md). Rounding residuals should reconcile into the contractual total, typically via the final applicable installment.

---

## Collection calendar

Do **not** assume every calendar day is a valid collection day.

Future template / organization policy may need to consider:

- daily collection
- weekends
- holidays
- organization closure
- collection suspension
- branch-specific operating days
- first payment date
- end-of-month behavior

These remain configurable / business decisions.

Do **not** silently skip or extend schedules without an explicit policy.

---

## Excused collection day

Preserve the WP03 distinction: **organization / service-caused event ≠ borrower-caused nonpayment**.

Example organization / service events:

- severe rain / flood
- declared collection suspension
- collector unavailable due to organization issue
- branch closure
- emergency

The future system must preserve:

- original due date
- original scheduled amount
- exception reason
- who declared / approved the exception
- effective penalty treatment
- effective schedule treatment

Do **not** rewrite history. Detail: [penalty-exception-and-waiver-model.md](penalty-exception-and-waiver-model.md).

---

## Schedule extension policy

An organization / template may eventually choose an approved schedule treatment for qualifying excused collection days.

Possible models (neither is a Platform-wide default):

**A. Fixed maturity**

The original maturity date remains unchanged. The event may suppress penalties without extending the schedule.

**B. Collection-day extension**

A qualifying organization / service exception shifts affected collection obligations so the borrower still receives the intended number of collection opportunities.

**Engineering recommendation:** the system must support explicit schedule adjustment / **version history** rather than mutating old due dates without evidence.

Exact behavior after an excused collection day remains **OPEN**.

---

## Maturity

**Maturity Date** = the contractual expected end of the payment schedule.

Reaching maturity does **not** automatically close the Loan.

| Remaining obligation | Direction |
|---|---|
| Balance = 0 | Loan may proceed to **Settled** |
| Balance > 0 | Loan remains collectible and becomes **Matured / Past Due** according to policy |

Do **not** silently:

- forgive balance
- extend term
- capitalize charges
- create a new loan

Post-maturity penalty concepts (no invented rate): [penalty-exception-and-waiver-model.md](penalty-exception-and-waiver-model.md). Lifecycle vs collection condition: [loan-lifecycle-model.md](loan-lifecycle-model.md).

---

## Early / advance payment

Support the concepts:

- Advance Payment
- Partial Prepayment
- Early Settlement

Financial treatment must stay **explicit**. A borrower may pay before scheduled due dates.

The engine must know whether the applicable **snapshotted** Loan policy:

- simply pays future scheduled obligations
- reduces principal
- changes future interest
- creates a settlement adjustment
- leaves schedule unchanged

Do **not** guess this. Exact treatment depends on the snapshotted Loan policy and remains **OPEN** where not yet decided.

---

## Early settlement

Future system should support a **Settlement Quote**. Conceptually it should show clearly:

```text
Outstanding Principal
+ accrued / contractual applicable interest
+ applicable fees
+ applicable penalties
− approved waivers
− credits / adjustments
= Settlement Amount
```

Exact treatment of **future / unearned interest** remains **OPEN / Product Owner + Legal/Accounting Validation Required**.

A settlement quote should eventually include:

- generated time
- valid-through time / date
- component breakdown
- policy / version used

Do **not** implement in this package.

---

## Restructuring

Do **not** silently edit an existing schedule.

If restructuring is supported later:

- original Loan terms / history remain visible
- original schedule remains historically visible
- authorized restructuring action is recorded
- replacement / revised schedule receives explicit version / history
- actor / reason / approval are audited

Exact financial rules remain **OPEN**.

---

## Write-off

Write-off is **not** equivalent to deleting a Loan.

If supported:

- original disbursement remains
- payment history remains
- balances / history remain explainable
- write-off event is audited
- later recovery must be representable if policy permits

Exact accounting / legal semantics remain **OPEN**.

---

## Explicit non-goals

- Platform default for excused-day extension vs fixed maturity
- Final installment-state enum / schema
- Early-settlement unearned-interest formula
- Restructuring or write-off accounting rules
