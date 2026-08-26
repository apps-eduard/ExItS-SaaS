# Pinoy Loan Manager — Restructuring and Hardship Policy

**Status:** Accepted product policy (PLM-DOC-06); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

MVP restructuring under the same Loan identity, component treatment, Restructuring Policy configuration, and separation from Refinancing. Not legal approval or schema design.

**Canonical companions:** [maturity-and-post-maturity-policy.md](maturity-and-post-maturity-policy.md), [payment-allocation-and-prepayment-policy.md](payment-allocation-and-prepayment-policy.md), [interest-and-finance-charge-policy.md](interest-and-finance-charge-policy.md), [loan-lifecycle-model.md](loan-lifecycle-model.md). Authorization: [workflow-authorization-policy.md](workflow-authorization-policy.md), [../Security/authorization-grant-catalog.md](../Security/authorization-grant-catalog.md). ADR: [../Decisions/ADR-011-restructuring-refinancing-and-hardship.md](../Decisions/ADR-011-restructuring-refinancing-and-hardship.md).

**No default numeric limits, rates, or DPD thresholds are selected.**

---

## Purpose

Restructuring allows an organization to revise repayment terms for an existing Loan when the borrower faces hardship or when collection recovery requires a revised schedule, without erasing historical agreement, schedule, payment, or penalty history.

---

## Eligible Loan conditions

Restructuring is supported for:

- **Active** Loans
- **Past Due** Loans
- **Matured Past Due** Loans

Restructuring is **not** supported for:

- Draft / Rejected / Cancelled requests
- **Settled** Loans
- Loans whose Disbursement was fully reversed
- already **Written-Off** Loans (unless a future recovery-settlement policy explicitly allows it)

---

## Required process

Restructuring must require:

1. borrower request or documented hardship case
2. updated assessment
3. proposed revised terms
4. borrower acceptance
5. authorized approval (`plm.loan-requests.approve` or future restructuring-specific grant where defined)
6. **maker/checker** for the financial-term change when another eligible approver exists, or controlled Owner Override per ADR-008
7. disclosure of revised schedule and totals to the borrower
8. audit history

Restructuring must **never** silently edit the original Loan history.

---

## Same Loan / new schedule version

MVP restructuring remains under the **same Loan identity**.

It creates:

- **Restructuring Agreement**
- revised terms snapshot
- **new schedule version**
- explicit effective date
- audit event

Must preserve:

- original agreement
- original schedule
- original payments
- original penalties
- prior balances
- prior schedule versions

Do not delete or overwrite historical terms.

---

## Component treatment at restructuring

At restructuring:

- **Outstanding Principal** remains Principal
- Do **not** capitalize into Principal: penalties, fees, unpaid interest/finance charge
- Outstanding interest, fees, and penalties remain **separate components**
- They may be **retained**, **rescheduled as separate components**, **waived**, or **reversed** only according to authorized policy
- Do **not** create interest on penalties
- Do **not** create interest on fees
- Future finance charge is calculated on the approved restructured Principal using one of the PLM-DOC-02 approved methods

---

## Restructuring Policy

A **Restructuring Policy** may configure:

- eligible Loan conditions
- minimum/maximum DPD
- maximum number of restructures per Loan
- permitted new term/frequency
- permitted calculation method
- permitted rate range
- required documentation
- approval requirements
- whether existing penalties/fees must be paid, retained, or waived

Do not choose default numeric limits or rates.

**One Loan may have only one active restructuring request at a time.**

Changing a policy later does **not** change an existing restructured Loan.

---

## Refinancing (deferred)

**Refinancing** is separate from **Restructuring**.

| | Restructuring | Refinancing |
|---|---|---|
| Loan identity | Same Loan | New Loan |
| Cash Disbursement | No new cash Disbursement | May create new Disbursement |
| Prior Loan | Revised schedule/terms | Settled/replaced through explicit transactions; old Loan remains historically visible |

Refinancing is **deferred beyond MVP** unless later explicitly approved.

Do not call restructuring refinancing.

---

## Hardship and collection linkage

A hardship request may trigger or accompany restructuring review. Collection Case records may reference hardship/restructuring review. See [collections-case-and-promise-to-pay-policy.md](collections-case-and-promise-to-pay-policy.md).

---

## Honesty gates

| Claim | Allowed? |
|---|---|
| Restructuring product rules are approved for MVP planning | Yes |
| Restructuring is implemented | **No** |
| Default DPD or rate limits are defined | **No** |
| Legally sufficient disclosure or collections practice | **No** (PLM-D-00-11 Open) |
