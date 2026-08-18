# Pinoy Loan Manager — Loan Product Configuration

**Status:** Planning / product-rule baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

A **Loan Product** is a reusable Traditional origination configuration. It is **not** itself a Loan.

Quick Loan uses **templates** ([quick-loan-model.md](quick-loan-model.md)). Traditional origination may use Loan Products. After disbursement both become operational Loans in one financial core.

Related: [traditional-loan-model.md](traditional-loan-model.md), [loan-application-and-approval.md](loan-application-and-approval.md), [financial-calculation-baseline.md](financial-calculation-baseline.md).

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

Interest-treatment **modes** remain those recorded in [financial-calculation-baseline.md](financial-calculation-baseline.md). Exact formulas remain **OPEN** (PLM-D-00-08).

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
