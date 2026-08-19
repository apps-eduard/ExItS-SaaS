# Pinoy Loan Manager — Delinquency and Missed Payment Policy

**Status:** Accepted product policy (PLM-DOC-03); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

When an installment becomes Past Due, Days Past Due (DPD), collection condition, excused vs unexcused events, and the cumulative unexcused missed-day counter. Not a reporting-aging specification or legally validated collections policy.

**Canonical companions:** [schedule-and-collection-calendar-policy.md](schedule-and-collection-calendar-policy.md), [penalty-assessment-and-cap-policy.md](penalty-assessment-and-cap-policy.md), [maturity-and-post-maturity-policy.md](maturity-and-post-maturity-policy.md). ADR: [../Decisions/ADR-006-delinquency-penalty-and-maturity-policy.md](../Decisions/ADR-006-delinquency-penalty-and-maturity-policy.md). Lifecycle split: [loan-lifecycle-model.md](loan-lifecycle-model.md).

No default grace-day count is selected.

---

## When an installment becomes Past Due

An installment becomes **Past Due** after its **effective** due date ends in the applicable Branch local time and an unpaid amount remains.

If the installment is fully satisfied before the local day closes:

- it does not create an unexcused missed-payment event

If any positive scheduled amount remains unpaid after the local due day:

- it becomes Past Due
- one missed scheduled-payment event is evaluated

A partial payment does not erase the unpaid remainder.

Do **not** create more than one missed scheduled-payment event for the same installment / due date.

---

## Days Past Due (DPD)

DPD is a derived reporting / collection measure.

If no installment is Past Due: **DPD = 0**.

Otherwise:

```text
DPD = current applicable local date − oldest effective unpaid due date
```

measured in **calendar days**.

Grace allowance does **not** change raw DPD. Grace controls penalty eligibility, not whether the obligation is late.

If an approved schedule shift changes the effective due date:

- DPD uses the current effective schedule version
- the original due date remains historically visible

Do **not** define final reporting aging buckets in this package.

---

## Loan collection condition

Keep collection condition **separate** from Loan lifecycle. Conceptual conditions (not finalized enums):

- Current
- Past Due
- Matured Past Due
- Settled

A Loan can be Lifecycle = **Active** and Collection Condition = **Past Due**.

Reaching Past Due does **not** create a new Loan.

Maturity: [maturity-and-post-maturity-policy.md](maturity-and-post-maturity-policy.md).

---

## Excused vs unexcused events

**Excused missed event**

A qualifying organization / service / external event prevented the normal collection opportunity or justified an approved exception. Examples:

- severe weather
- flooding
- declared emergency
- branch closure
- organization-wide collection suspension
- organization-caused Collector / service failure
- manager-approved exceptional circumstance

**Unexcused missed event**

The scheduled amount remained unpaid without an approved exception. Examples:

- customer unavailable
- insufficient funds
- payment refused
- customer avoided collection

**Customer unavailable is unexcused by default.**

It may be submitted for Manager / Owner exception review when supported by a documented exceptional reason.

Collector records facts and may request review. Collector **cannot** approve their own exception.

---

## Cumulative Unexcused Missed Scheduled Days

Loan-level counter supporting the Product Owner’s tiered missed-day model. **Separate from raw DPD.**

Rules:

- increments once for each unexcused missed scheduled-payment date
- does not increment for excused events
- does not increment more than once for one installment / due date
- does **not** reset merely because the borrower later catches up
- remains part of Loan history
- an incorrect count is corrected through audited correction / reversal
- penalty waiver does **not** erase or reset the historical counter

---

## Grace / allowable missed days

Each Loan Template / Product may configure:

**Allowed Unexcused Missed Days = N**

- `N` may be zero or a positive whole number
- the first `N` cumulative unexcused missed scheduled days do **not** produce a penalty assessment
- day `N+1` becomes the first penalty-eligible missed event
- grace days are **not** retroactively penalized later
- grace allowance does **not** erase Past Due status or DPD
- grace allowance is snapshotted into the Loan

No Platform-wide numeric default is selected.

Penalty assessment after grace: [penalty-assessment-and-cap-policy.md](penalty-assessment-and-cap-policy.md).

---

## Legal / compliance boundary

No DPD, grace, or missed-event rule is claimed legally compliant. External qualified review remains required before Production (PLM-D-00-11). This package does not invent Philippine regulations.

---

## Explicit non-goals

- Reporting aging buckets
- Default `N` grace days
- Automatic excuse for customer unavailable
- Final enum names
- Implementation of a delinquency engine
