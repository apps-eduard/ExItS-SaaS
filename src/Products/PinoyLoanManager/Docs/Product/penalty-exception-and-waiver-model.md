# Pinoy Loan Manager — Penalty, Exception, and Waiver Model

**Status:** Planning index; penalty engine accepted in PLM-DOC-03
**Implementation present:** No
**Last updated:** 2026-08-19

Pointer to accepted penalty, exception, waiver, and reversal rules. Not a default price list or legally validated collections policy.

**Canonical PLM-DOC-03 policies:**

- [delinquency-and-missed-payment-policy.md](delinquency-and-missed-payment-policy.md)
- [penalty-assessment-and-cap-policy.md](penalty-assessment-and-cap-policy.md)
- [maturity-and-post-maturity-policy.md](maturity-and-post-maturity-policy.md)
- [schedule-and-collection-calendar-policy.md](schedule-and-collection-calendar-policy.md)

ADR: [../Decisions/ADR-006-delinquency-penalty-and-maturity-policy.md](../Decisions/ADR-006-delinquency-penalty-and-maturity-policy.md). Workflow: [exception-reversal-and-variance-workflow.md](exception-reversal-and-variance-workflow.md).

**No default peso amount, percentage, grace `N`, or cap is defined.** The product must be able to disable penalties entirely.

---

## Concepts (accepted)

- grace allowance (snapshotted `N`; not retroactive)
- cumulative unexcused missed scheduled days (does not reset on catch-up)
- excused vs unexcused events
- configurable non-overlapping tiers
- MVP calculation types and excluded bases
- required penalty cap when penalties are enabled
- post-maturity modes
- waiver vs reversal vs exception (distinct)
- collection exception vs collector Collection Attempt

Customer unavailable is **unexcused by default**. Collector records facts; cannot approve own exception or waiver.

---

## Collection exception vs waiver vs reversal

| Concept | Meaning |
|---|---|
| **Collection exception** | Qualifying event prevents penalty assessment (or invalidates it via reversal if already posted) |
| **Penalty waiver** | Penalty was **validly assessed**; authorized staff forgives all or part. History remains. Missed-day counter is not reset |
| **Penalty reversal** | Penalty was **incorrectly assessed** and is corrected through an auditable reversal |

Never silently delete historical penalty records. Retroactive exception never silently deletes an assessment.

---

## Safety (accepted)

Penalty-on-penalty **prohibited**. Capitalization into Principal **prohibited**. Unlimited growth **prohibited**. Cap **required** when penalties enabled.

---

## Legal / compliance boundary

No penalty configuration is claimed legally permitted. **PLM-D-00-11 remains Open.** This package does not invent Philippine lending regulations.

---

## Explicit non-goals

- Hard-coded global penalty
- Platform-default days, peso amounts, or percentages
- Silent deletion of penalty history
- Collector self-approval of waivers
- Legal maximums
