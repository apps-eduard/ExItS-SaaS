# Pinoy Loan Manager — Schedule and Collection Calendar Policy

**Status:** Accepted product policy (PLM-DOC-03); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Due-date generation, collection calendars, MVP frequencies, first due date, and schedule-exception treatment. Not a schedule engine, timezone library, or legally validated calendar.

**Canonical companions:** [delinquency-and-missed-payment-policy.md](delinquency-and-missed-payment-policy.md), [penalty-assessment-and-cap-policy.md](penalty-assessment-and-cap-policy.md), [maturity-and-post-maturity-policy.md](maturity-and-post-maturity-policy.md). ADR: [../Decisions/ADR-005-schedule-calendar-and-exception-treatment.md](../Decisions/ADR-005-schedule-calendar-and-exception-treatment.md). Index: [schedule-maturity-and-settlement.md](schedule-maturity-and-settlement.md).

Early settlement, principal prepayment recalculation, restructuring, and write-off remain later packages.

---

## Date and time authority

- Authoritative financial event timestamps are stored as **UTC instants**.
- Due dates and collection-business dates are evaluated using the applicable **Organization / Branch local time zone**.
- Every Branch used for Loan operations must eventually have an explicit valid time-zone configuration.
- Never infer the time zone from a device or browser.
- One Loan **snapshots** its applicable Branch / time-zone context.
- Moving a user or Collector to another time zone must **not** alter Loan due dates.

Do not select an implementation library or database type here.

---

## Schedule snapshot (mandatory)

Every Loan schedule must snapshot enough information to be unambiguous:

- disbursement date
- first due date
- frequency
- installment count
- collection-day calendar
- non-collection-day adjustment rule
- end-of-month rule where applicable
- exception / schedule-shift policy
- time-zone identity
- schedule policy version

Term labels alone are insufficient.

“1 month” is not enough without installment count and calendar rules.  
“Daily” does not automatically mean every calendar day.

Changing a Template/Product later must not silently mutate an existing Loan schedule. Schedule shifts create a **new schedule version**. Original versions remain historically visible.

---

## MVP payment frequencies

| Frequency | Meaning |
|---|---|
| **Daily** | One installment on each valid Collection Day |
| **Weekly** | One installment every 7 calendar days from the first due date, then Following Valid Collection Day |
| **Biweekly** | One installment every 14 calendar days from the first due date, then Following Valid Collection Day |
| **Semi-Monthly** | Two configured schedule anchors per month |
| **Monthly** | One installment on the snapshotted day-of-month rule, then month-end and Following Valid Collection Day as applicable |

Other / custom frequencies remain **deferred**. Do **not** authorize arbitrary cron expressions or organization-provided scripts.

### Semi-Monthly configuration

Supported configuration:

- one configured day from 1 through 28
- plus either another configured later day from 2 through 28, **or** Month End

The second anchor must occur after the first.

---

## Collection Calendar

Each organization/branch must eventually define a Collection Calendar. The concept includes:

- enabled weekdays
- organization holidays
- branch closure dates
- declared collection-suspension dates
- approved exceptional closure periods

**Daily Loans:** only valid Collection Days create ordinary scheduled installments.

**Non-daily Loans:** a calculated due date that falls on a non-collection day uses Following Valid Collection Day.

---

## Non-collection-day adjustment (approved)

**Following Valid Collection Day.**

If a Weekly, Biweekly, Semi-Monthly, or Monthly due date falls on a non-collection day:

- move its **effective** due date forward to the next valid Collection Day
- retain the original calculated date in schedule-generation evidence
- never move it backward automatically
- never combine two installments silently

If shifting creates a collision with another installment:

- cascade the later installment forward to the next valid Collection Day
- preserve installment order
- preserve schedule version / evidence

---

## Month-end rule (approved)

**Same Day or Last Calendar Day.**

For Monthly schedules:

- if the configured day exists in the month, use it
- otherwise use that month’s final calendar day
- then apply Following Valid Collection Day if necessary

Examples (calendar facts, not rates):

- day 31 in April begins from April 30
- day 30/31 in February begins from February’s last day
- leap year follows the actual calendar

For Semi-Monthly Month End: use the actual final calendar day, then Following Valid Collection Day.

---

## First due date

The actual first due date must always be visible and snapshotted. The borrower must see it before accepting / disbursement.

**Quick Loan default**

- first due date = next valid Collection Day **after** Disbursement
- same-day first installment is **not** supported in MVP

**Traditional Loan**

- first due date is generated from the approved Loan Product rule
- default generation = one full selected frequency interval after Disbursement
- the generated date then uses the applicable collection calendar

An authorized approval process may set a different explicit first due date before Disbursement where the Loan Product allows it.

---

## Installment financial state vs collection attempt

Conceptual installment **financial** states (not a finalized enum):

- Future
- Due
- Partially Paid
- Paid
- Past Due

Do **not** mix collection-attempt outcomes into the financial state.

A **Collection Attempt** is separate factual history. Possible outcomes include:

- Paid
- Partially Paid
- Customer Unavailable
- Insufficient Funds
- Payment Refused
- Location Closed
- Service/Collector Failure
- Weather/Emergency
- No Contact
- Other documented outcome

Do not finalize code enums.

Past Due timing: [delinquency-and-missed-payment-policy.md](delinquency-and-missed-payment-policy.md).

---

## Schedule-exception policies

An approved Excused Event uses the Loan’s snapshotted schedule-exception policy.

**A. Fixed Schedule / Penalty Suppression**

- original effective due dates remain
- no penalty for the excused event
- schedule does not shift automatically

**B. Shift Future Due Dates**

- affected unpaid due date moves to the next valid Collection Day
- subsequent unpaid future due dates shift/cascade while preserving order
- installment count is preserved
- a new schedule version is created
- original schedule remains historically visible

### Product defaults (snapshotted)

| Origination | Default exception policy |
|---|---|
| Quick Loan | Shift Future Due Dates (preserve promised collection opportunities for organization/service-caused disruptions) |
| Traditional Loan | Fixed Schedule / Penalty Suppression |

An organization may choose the other approved policy in the source Template/Product before publication/use. Changing the Template later does not alter an existing Loan.

Excused vs unexcused classification: [delinquency-and-missed-payment-policy.md](delinquency-and-missed-payment-policy.md). Organization-wide suspension: [penalty-assessment-and-cap-policy.md](penalty-assessment-and-cap-policy.md).

---

## Legal / compliance boundary

No calendar, frequency, or first-due rule is claimed legally sufficient. External qualified review remains required before Production (PLM-D-00-11). This package does not invent Philippine regulations.

---

## Explicit non-goals

- Time-zone library / database type
- Custom cron / organization scripts
- Same-day Quick Loan first installment in MVP
- Final code enums
- Early-settlement formula
- Implementation of a schedule engine
