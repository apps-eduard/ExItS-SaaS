# ADR-006 — Delinquency, penalty, and maturity policy

**Status:** Accepted product policy (PLM-DOC-03); not implemented
**Date:** 2026-08-19
**Decisions:** Delinquency/penalty/maturity product rules accepted; **PLM-D-00-08** Open / Partially Resolved; **PLM-D-00-11** Open; **PLM-D-00-12** remains Closed

> **Final status note:** **PLM-D-00-08 Closed for MVP Product business/calculation policy** at final review. See [PLM-decision-status-summary.md](PLM-decision-status-summary.md).

---

## Context

PLM needed Product Owner rules for Past Due, DPD, missed-day counting, grace, penalty tiers/bases/caps, exception vs waiver vs reversal, and post-maturity behavior. Prior docs recorded concepts without choosing bases or maturity modes. No default peso amounts or percentages are selected.

---

## Decision

1. Past Due begins after the effective due date ends in Branch local time with unpaid amount remaining. One missed scheduled-payment event per installment/due date. Partial payment does not erase remainder.
2. **DPD** = 0 if nothing is Past Due; otherwise current local date minus oldest effective unpaid due date (calendar days). Grace does not change raw DPD.
3. **Cumulative Unexcused Missed Scheduled Days** increments once per unexcused missed scheduled date, does not increment for excused events, and does **not** reset on catch-up. Waiver does not reset the counter. Distinct from DPD.
4. Grace `N` (zero or positive) is snapshotted. First `N` unexcused missed scheduled days are not penalty-eligible. Day `N+1` is the first penalty-eligible event. Grace is **not** retroactively penalized. Grace does not erase Past Due/DPD. No Platform default `N`.
5. Customer unavailable is **unexcused by default**. Collector records facts and may request review; cannot approve own exception.
6. MVP penalty types: fixed per eligible missed event; % of unpaid scheduled amount of the affected installment; % of total past-due scheduled amount. Bases exclude existing penalties, Platform usage, future installments, unrelated Loans. **No** % of future Outstanding Principal. **No** penalty-on-penalty.
7. Cap required when penalties enabled: fixed PHP total and/or % of Contract Principal (lower wins if both). No capitalization, compounding, or unlimited growth. One assessment per missed event, To-Even rounding, remaining cap applied. Idempotent future posting.
8. Exception (prevents/invalidates assessment), Waiver (valid but forgiven), and Reversal (should not have been assessed) remain distinct. Retroactive exception never silently deletes an assessment.
9. Maturity Date = final effective scheduled due date of the current schedule version. Maturity does not close the Loan or erase balance. Remaining outstanding → Matured Past Due; Settled only when Total Outstanding = 0.
10. Post-maturity grace `M` (no default) suppresses new post-maturity assessments only. Modes: no additional post-maturity penalty; continue missed-event policy; separate Daily or Weekly post-maturity policy sharing the same Loan cap. Payments after maturity use Interest → Principal → Fees → Penalties.

Canonical text: [../Product/delinquency-and-missed-payment-policy.md](../Product/delinquency-and-missed-payment-policy.md), [../Product/penalty-assessment-and-cap-policy.md](../Product/penalty-assessment-and-cap-policy.md), [../Product/maturity-and-post-maturity-policy.md](../Product/maturity-and-post-maturity-policy.md).

---

## Consequences

Collections configuration has an approved engine contract without default rates.

**Still open:** early-settlement future-interest, principal prepayment/recalculation, restructuring, write-off, legal/compliance (PLM-D-00-11).

No penalty engine, schema, or implementation is authorized by this ADR. No legal permissibility is claimed.
