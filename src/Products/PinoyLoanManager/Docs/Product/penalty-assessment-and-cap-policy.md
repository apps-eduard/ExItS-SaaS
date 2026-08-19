# Pinoy Loan Manager — Penalty Assessment and Cap Policy

**Status:** Accepted product policy (PLM-DOC-03); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Penalty tiers, calculation types, assessment, caps, safety rules, organization-wide suspension, retroactive exception, waiver, and reversal. Not a default price list or legally validated collections policy.

**Canonical companions:** [delinquency-and-missed-payment-policy.md](delinquency-and-missed-payment-policy.md), [schedule-and-collection-calendar-policy.md](schedule-and-collection-calendar-policy.md), [maturity-and-post-maturity-policy.md](maturity-and-post-maturity-policy.md). Rounding: [money-precision-and-rounding-policy.md](money-precision-and-rounding-policy.md). ADR: [../Decisions/ADR-006-delinquency-penalty-and-maturity-policy.md](../Decisions/ADR-006-delinquency-penalty-and-maturity-policy.md).

**No default peso penalty amount, percentage, or cap is defined.** The product must be capable of disabling penalties entirely.

---

## Penalty tiers

Support configurable cumulative missed-day tiers. Example ranges only (not defaults): counts 1–5, 6–10, 11+.

Each tier must define:

- inclusive starting count
- optional inclusive ending count
- penalty calculation type
- amount or rate
- assessment behavior
- policy / version

Tier ranges must not overlap, be ordered, not contain gaps once penalty eligibility begins, and use positive whole-number boundaries.

---

## MVP penalty calculation types

**A.** Fixed amount per penalty-eligible missed event

**B.** Percentage of unpaid scheduled amount for the affected installment

**C.** Percentage of total past-due scheduled amount

Percentage bases include only unpaid scheduled contractual components:

- principal
- scheduled finance charge / interest
- scheduled fees

Penalty calculation bases must **exclude**:

- existing penalties
- waived penalties
- Platform usage charges
- unrelated future installments
- unrelated Loans
- unexplained balances

Do **not** support percentage of total future Outstanding Principal as an MVP penalty basis.

Do **not** support penalty on penalty.

---

## Penalty assessment

For each newly penalty-eligible unexcused missed event:

1. identify the cumulative unexcused missed count
2. identify the applicable snapshotted tier
3. calculate using that tier’s method and basis
4. round using To-Even money policy ([money-precision-and-rounding-policy.md](money-precision-and-rounding-policy.md))
5. apply the remaining penalty cap
6. post one explicit Penalty Assessment event
7. preserve reason, date, tier, basis, inputs, actor/system source, and policy version

Do **not** reassess the same missed event twice. Future posting must be **idempotent**.

---

## Penalty cap

Every penalty-enabled Loan Template / Product must define a cap.

**A.** Fixed PHP total penalty cap  
**B.** Percentage of Contract Principal total penalty cap

A policy may define both. If both are defined: **effective cap = the lower resulting amount**.

Total effective penalty assessments for the Loan must not exceed the cap after considering reversals.

Waived amounts do not authorize creating penalties beyond the original cap without a future explicit policy.

No default cap amount or percentage is selected. Legal/compliance review remains required (PLM-D-00-11).

A template with penalties enabled but no valid cap **cannot be published**.

---

## Safety rules

- penalty-on-penalty = **prohibited**
- penalty capitalization into Principal = **prohibited**
- compound penalties = **prohibited**
- unlimited penalty growth = **prohibited**
- penalty cap = **required** when penalties are enabled
- one missed event cannot produce duplicate assessments
- penalties are separate from Principal, interest, and fees
- penalties remain separately visible in Loan balance breakdown and receipts

---

## Organization-wide collection suspension

Owner / Manager with the future required grant may declare a collection suspension scoped by:

- Organization
- Branch
- area / route where supported
- date or date range
- affected Collectors where relevant
- reason
- exception treatment
- approver
- effective time

Examples: heavy rain, flooding, declared emergency, branch closure, organization service outage.

The future system must identify affected scheduled obligations **deterministically**. Do not silently apply a global exception to unrelated Organizations or Branches.

Schedule treatment uses the Loan’s snapshotted exception policy: [schedule-and-collection-calendar-policy.md](schedule-and-collection-calendar-policy.md).

---

## Retroactive exception

A retroactive exception must **never** silently delete an existing Penalty Assessment.

If an approved exception is applied after a penalty was assessed:

- preserve the original assessment
- create an explicit Penalty Reversal where the penalty becomes invalid
- create / update the Excused Event record
- create a new schedule version if the selected policy shifts due dates
- record reason and approval

Exact time limits for retroactive requests remain future organization / legal policy.

---

## Penalty Waiver vs Penalty Reversal

Keep these **distinct**.

**Waiver:** the penalty was validly assessed, but an authorized approver forgives all or part of it.

- original Penalty Assessment remains visible
- waiver may be partial or full
- reason is required
- Owner / Manager grant required
- Collector may request but cannot approve
- Cashier has no default waiver-approval authority
- waiver creates a separate financial event
- waiver does not change Principal
- waiver does not reset missed-day history
- waiver does not make the original missed event excused

**Reversal:** the original penalty should not have been assessed.

- original assessment remains visible
- reversal references the original assessment
- reason and authorization are required
- reversal changes the effective penalty balance
- may result from duplicate posting, incorrect tier/basis, approved retroactive exception, or system/operator error

Waiver and Reversal must remain distinct in reporting and audit.

---

## Invalid penalty configuration

Future validation must reject:

- negative grace days
- negative post-maturity grace days
- zero / negative tier boundaries
- overlapping tiers
- tier gaps after penalty eligibility starts
- negative fixed penalty
- negative percentage rate
- percentage penalty without a valid basis
- penalties enabled without a cap
- negative cap
- penalty basis including existing penalty
- penalty-on-penalty configuration
- reducing / raising Principal through penalty
- duplicate tier identities
- unsupported assessment cadence
- schedule-shift policy missing where exceptions are enabled

Do not define legal maximums here.

---

## Legal / compliance boundary

Do **not** claim that any configurable penalty is legally permitted. **PLM-D-00-11 remains Open.**

Before Production, qualified review must validate permissible penalty types, rates, caps, grace treatment, disclosures, waiver/reversal practices, post-maturity charges, collection practices, customer communications, and applicable consumer/lending rules.

---

## Explicit non-goals

- Default peso amounts, percentages, or caps
- Penalty-on-penalty
- Capitalization into Principal
- Percentage of future Outstanding Principal as MVP basis
- Legal maximums
- Implementation of a penalty engine
