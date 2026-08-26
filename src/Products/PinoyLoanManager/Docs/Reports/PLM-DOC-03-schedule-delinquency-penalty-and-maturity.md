# PLM-DOC-03 — Schedule Calendar, Delinquency, Penalties & Maturity

**Status:** Documentation package complete (planning only)
**Implementation present:** No
**Last updated:** 2026-08-19
**Branch:** `docs/plm-final-decisions`

Runtime / browser / device / database / production validation: **Not Applicable**.

---

## Scope

Finalize Pinoy Loan Manager product rules for schedule and due-date generation, collection calendars, MVP frequencies, missed-payment classification, DPD, allowable missed days, excused/unexcused events, penalty tiers/bases/assessment/caps, waiver and reversal, organization-wide suspensions, and maturity / post-maturity behavior.

Explicitly **out of scope:** code, database, migrations, APIs, UI, solution changes, parked scaffold, POS/Platform implementation, default penalty amounts/percentages/caps, legal compliance claims, early-settlement future-interest treatment, restructuring, write-off accounting, cash refunds (PLM-DOC-04).

---

## Accepted calendar rules

- UTC instants for financial events; Branch local time zone for due/collection-business dates; never device/browser TZ
- Daily: valid Collection Days only
- Non-daily: **Following Valid Collection Day** (forward only; cascade on collision; no silent merge)
- Monthly / Semi-Monthly Month End: **Same Day or Last Calendar Day**, then Following Valid Collection Day
- Quick Loan first due: next valid Collection Day **after** Disbursement (no same-day MVP)
- Traditional first due: one full frequency interval after Disbursement, then calendar; explicit override allowed before Disbursement
- Schedule version history required

Canonical: [../Product/schedule-and-collection-calendar-policy.md](../Product/schedule-and-collection-calendar-policy.md).

---

## Supported frequencies

Daily, Weekly, Biweekly, Semi-Monthly, Monthly. Custom cron/scripts deferred.

---

## Missed-day semantics

Past Due after local due day closes with unpaid remainder. One missed event per installment/due date. **DPD** uses oldest effective unpaid due date; grace does not change DPD. **Cumulative Unexcused Missed Scheduled Days** does not reset on catch-up and does not increment for excused events. Customer unavailable is unexcused by default.

Canonical: [../Product/delinquency-and-missed-payment-policy.md](../Product/delinquency-and-missed-payment-policy.md).

---

## Grace / tier semantics

`N` snapshotted; first `N` unexcused missed scheduled days are not penalty-eligible; day `N+1` is first eligible; grace is not retroactively penalized. Tiers: non-overlapping, ordered, no gaps after eligibility, positive whole-number boundaries. No default `N` or tier amounts.

---

## Penalty bases and caps

Types: fixed per eligible missed event; % unpaid scheduled amount of affected installment; % total past-due scheduled amount. Exclude existing penalties, Platform usage, future installments, unrelated Loans. **No** % of future Outstanding Principal. **No** penalty-on-penalty. Cap required (fixed PHP and/or % of Contract Principal; lower wins). No capitalization or compounding.

Canonical: [../Product/penalty-assessment-and-cap-policy.md](../Product/penalty-assessment-and-cap-policy.md).

---

## Exception policies

- Fixed Schedule / Penalty Suppression (Traditional **default**)
- Shift Future Due Dates (Quick Loan **default**)

Exception, Waiver, and Reversal remain distinct. Retroactive exception never silently deletes an assessment.

---

## Maturity / post-maturity

Maturity Date = final effective scheduled due date of the current version. Maturity does not erase balance. Remaining outstanding → Matured Past Due. Modes: no additional post-maturity penalty; continue missed-event policy; separate Daily/Weekly post-maturity policy sharing the Loan cap. Payments after maturity: Interest → Principal → Fees → Penalties.

Canonical: [../Product/maturity-and-post-maturity-policy.md](../Product/maturity-and-post-maturity-policy.md).

---

## Remaining open decisions

| ID | Remaining |
|---|---|
| PLM-D-00-08 | Early-settlement future-interest; principal prepayment/recalculation; restructuring; write-off |
| PLM-D-00-07 | Subledger schema; cash refund; settlement/GL/write-off accounting |
| PLM-D-00-11 | Legal/compliance, including penalty permissibility |
| PLM-DOC-04 | Early settlement, refunds, reversals, cash variance, accounting boundaries |

PLM-D-00-12 remains **Closed**. PLM-D-00-10 remains **Closed / Product Owner Accepted**. PLM-D-00-13 remains **Open**.

No default penalty amount, percentage, or cap was introduced.

---

## No-code / no-database statement

This package is **documentation only**. No `.cs`, `.csproj`, `ExItS.slnx`, migrations, database, API, UI, tests, POS, Platform implementation, or parked-scaffold changes.

Implementation remains **paused**. `feat/plm-01-scaffold` remains unmerged.

---

## Validation

Documentation only. `git diff --check` recorded at commit time. No implementation authorization. No legal compliance claim.

---

## Git evidence

Recorded in the PLM-DOC-03 commit on `docs/plm-final-decisions`. Parked scaffold `feat/plm-01-scaffold` @ `4ec9e96e9149cd8d014adde3d694872a6d5ef576` not modified.

---

## Exact next documentation package

**PLM-DOC-04 — Early Settlement, Refunds, Reversals, Cash Variance & Accounting Boundaries**

Do not start PLM-DOC-04 in this package. Implementation remains paused.
