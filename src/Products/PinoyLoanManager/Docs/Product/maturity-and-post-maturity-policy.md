# Pinoy Loan Manager — Maturity and Post-Maturity Policy

**Status:** Accepted product policy (PLM-DOC-03); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Maturity Date, Matured Past Due, post-maturity grace, post-maturity penalty modes, and payments after maturity. Not a settlement-quote specification or legally validated collections policy.

**Canonical companions:** [schedule-and-collection-calendar-policy.md](schedule-and-collection-calendar-policy.md), [delinquency-and-missed-payment-policy.md](delinquency-and-missed-payment-policy.md), [penalty-assessment-and-cap-policy.md](penalty-assessment-and-cap-policy.md). Allocation: [payment-allocation-and-prepayment-policy.md](payment-allocation-and-prepayment-policy.md). ADR: [../Decisions/ADR-006-delinquency-penalty-and-maturity-policy.md](../Decisions/ADR-006-delinquency-penalty-and-maturity-policy.md).

Early-settlement future-interest treatment, formal settlement-quote workflow, restructuring, and write-off remain later packages. **No default post-maturity amount or rate is selected.**

---

## Maturity Date

**Maturity Date** is the final **effective** scheduled due date in the current schedule version.

On the maturity date:

- the final scheduled obligation becomes Due
- the Loan does **not** close merely because the date was reached

After the maturity local day closes:

If **Total Outstanding = 0**: the Loan may become **Settled**.

If **Total Outstanding > 0**:

- Collection Condition becomes **Matured Past Due**
- remaining obligations remain collectible
- no balance is forgiven
- no automatic new Loan is created
- no automatic restructuring occurs
- no charge is capitalized into Principal

---

## Post-maturity grace

A Loan Template / Product may configure **Post-Maturity Grace Days = M**.

- `M` is zero or a positive whole number
- raw DPD and Matured Past Due status remain accurate
- post-maturity penalty assessment is suppressed during the grace period
- existing valid pre-maturity penalties remain
- grace is snapshotted into the Loan

No Platform-wide numeric default is selected.

---

## Post-maturity penalty modes

**A. No additional post-maturity penalty**

Existing obligations and previously assessed penalties remain collectible, but no new maturity-specific penalty is added.

**B. Continue missed-event policy**

The existing missed-event / tier policy continues for qualifying scheduled events according to its rules.

**C. Separate post-maturity policy**

After the post-maturity grace period, a separate policy may assess at an explicit cadence:

- Daily, or
- Weekly

Supported calculation types:

- fixed amount per assessment interval
- percentage of Total Past-Due Scheduled Amount

The post-maturity basis excludes existing penalties, Platform usage charges, and unrelated future / non-Loan amounts.

All penalty assessments share the same **effective total Loan penalty cap**.

---

## Post-maturity duplicate safety

A post-maturity assessment requires a unique assessment interval identity. Examples:

- Loan + local date for Daily cadence
- Loan + defined week period for Weekly cadence

The same interval must not be charged twice. Future implementation must be **idempotent**.

---

## Customer payment after maturity

Payments after maturity use the same accepted payment-allocation policy:

- oldest due obligation first
- Interest → Principal → Fees → Penalties

A payment does not automatically waive penalties or restructure the Loan.

When Total Outstanding reaches zero:

- the Loan may proceed to Settled
- settlement event / history must be recorded
- the Loan is **not** deleted

Exact formal settlement quote behavior remains for a later package (**PLM-DOC-04**).

---

## Legal / compliance boundary

No post-maturity charge is claimed legally permitted. **PLM-D-00-11 remains Open.** External qualified review remains required before Production. This package does not invent Philippine regulations.

---

## Explicit non-goals

- Default `M` or post-maturity rates
- Automatic forgiveness or new Loan at maturity
- Early-settlement unearned-interest formula
- Restructuring / write-off accounting
- Implementation of a maturity engine
