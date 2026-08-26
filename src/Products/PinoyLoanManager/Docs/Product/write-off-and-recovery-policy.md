# Pinoy Loan Manager — Write-Off and Recovery Policy

**Status:** Accepted product policy (PLM-DOC-06); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Write-Off classification, post-write-off charge behavior, component tracking, Recovery Payment allocation, and recovery completion. Not external GL integration or legal forgiveness.

**Canonical companions:** [payment-allocation-and-prepayment-policy.md](payment-allocation-and-prepayment-policy.md), [../Architecture/operational-subledger-and-accounting-boundary.md](../Architecture/operational-subledger-and-accounting-boundary.md), [../Architecture/loan-ledger-and-balance-model.md](../Architecture/loan-ledger-and-balance-model.md). Authorization: [workflow-authorization-policy.md](workflow-authorization-policy.md). ADR: [../Decisions/ADR-012-write-off-recovery-and-collections-case-policy.md](../Decisions/ADR-012-write-off-recovery-and-collections-case-policy.md).

**No default DPD threshold for Write-Off is selected.**

---

## Write-Off definition

**Write-Off** is an authorized operational/accounting classification.

Write-Off does **NOT**:

- delete the Loan
- erase the debt automatically
- erase Disbursement
- erase Payments
- erase Borrower history
- create a new Loan
- imply legal forgiveness

---

## Write-Off requirements

Write-Off requires:

- eligible Loan condition (organization policy; no Platform default DPD threshold)
- documented reason
- effective date
- balance/component snapshot at effective date
- authorized high-risk approval (maker/checker or controlled Owner Override)
- audit

Eligible approvers follow PLM Authorization Policy v1 and ADR-008.

---

## Post-Write-Off charges (MVP)

After the Write-Off effective date:

- stop new finance-charge accrual
- stop new fee assessment
- stop new penalty assessment
- preserve all existing effective balances
- preserve prior waivers/reversals

Do **not** capitalize written-off components.

---

## Written-off components

Record written-off components separately:

- **Written-Off Principal**
- **Written-Off Interest/Finance Charge**
- **Written-Off Fees**
- **Written-Off Penalties**

Do not collapse them into one unexplained number.

Operational reporting and future accounting projection must be able to explain every component.

Concrete persistence schema and external journal/export remain implementation work (PLM-D-00-07 remainder).

---

## Recovery Payment

A payment received after Write-Off is a **Recovery Payment**.

Recovery:

- receives its own Payment identity and receipt
- updates the Loan operational subledger
- updates cash accountability for cash channels
- does **not** delete the Write-Off event
- does **not** silently restore the Loan to Active
- is separately reportable

---

## Recovery allocation

Recovery allocation uses **oldest obligation first**, then component order:

**Interest → Principal → Fees → Penalties**

unless a future legally/accounting-required policy replaces it.

No new post-write-off charges are created.

Ordinary PLM-DOC-02 allocation applies to written-off component buckets as operational subledger facts, not as new scheduled obligations.

---

## Recovery completion

When all written-off outstanding components reach zero:

- Collection condition may become **Recovered / Closed After Write-Off**
- Do **not** mark the original Write-Off as never having happened
- Do **not** delete recovery history

---

## Reporting separation

Written-Off Loans are excluded from active PAR denominator and reported separately. See [reporting-kpi-and-aging-policy.md](reporting-kpi-and-aging-policy.md) (PLM-DOC-08).

---

## Honesty gates

| Claim | Allowed? |
|---|---|
| Write-Off/Recovery product behavior is approved for MVP planning | Yes |
| Write-Off/Recovery is implemented | **No** |
| External GL or statutory accounting treatment is defined | **No** |
| Legal forgiveness or compliance | **No** (PLM-D-00-11 Open) |
