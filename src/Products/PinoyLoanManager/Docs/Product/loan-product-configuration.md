# Pinoy Loan Manager — Loan Product Configuration

**Status:** Planning / product-rule baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

A **Loan Product** is a reusable Traditional origination configuration. It is **not** itself a Loan.

Quick Loan uses **templates** ([quick-loan-model.md](quick-loan-model.md)). Traditional origination may use Loan Products. After disbursement both become operational Loans in one financial core.

Related: [traditional-loan-model.md](traditional-loan-model.md), [loan-application-and-approval.md](loan-application-and-approval.md), [financial-calculation-baseline.md](financial-calculation-baseline.md), [interest-and-finance-charge-policy.md](interest-and-finance-charge-policy.md), [fees-and-net-proceeds-policy.md](fees-and-net-proceeds-policy.md).

---

## Potential configurable categories

A Loan Product **may** eventually include:

- name
- amount constraints
- term constraints
- frequency
- approved interest / calculation policy
- fees
- penalty policy
- eligibility
- approval requirements
- active / archive state

Do **not** establish actual rates, peso limits, or fee amounts.

Interest-treatment **modes** and MVP methods: [interest-and-finance-charge-policy.md](interest-and-finance-charge-policy.md). Calendar, first due, and penalty engine: [schedule-and-collection-calendar-policy.md](schedule-and-collection-calendar-policy.md), [penalty-assessment-and-cap-policy.md](penalty-assessment-and-cap-policy.md). Settlement/prepayment: [early-settlement-and-principal-prepayment-policy.md](early-settlement-and-principal-prepayment-policy.md). Changing a Loan Product later must never recalculate an already submitted, approved, or disbursed Loan silently. Default rates and penalty amounts remain undefined. Remaining PLM-D-00-08 items (restructuring, write-off) stay open.

---

## Snapshot

When an application is approved, agreed terms must be snapshotted. Later Loan Product edits must not silently mutate:

- submitted applications
- approved applications
- disbursed Loans
- existing schedules

---

## Explicit non-goals

- Built-in Platform loan types
- Hard-coded rates
- Treating a Loan Product as a posted Loan
- Schema design
