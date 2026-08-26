# Pinoy Loan Manager — Traditional Loan Model

**Status:** Planning / product-rule baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Traditional Loan is one origination **experience**. After successful disbursement it uses the **same** operational Loan / financial engine as Quick Loan.

Related: [loan-application-and-approval.md](loan-application-and-approval.md), [loan-product-configuration.md](loan-product-configuration.md), [disbursement-readiness-model.md](disbursement-readiness-model.md), [lending-operating-model.md](lending-operating-model.md), [quick-loan-model.md](quick-loan-model.md).

---

## Two origination channels

| Channel | Typical start | After disbursement |
|---|---|---|
| Traditional Loan | Staff-assisted application against a Loan Product | Same Loan core |
| Quick Loan | Published template + customer request | Same Loan core |

Do **not** design two independent financial engines. Snapshot, schedule, subledger, payments, penalties, collections, settlement, audit, and reports converge after disbursement.

Traditional Loan MVP may use **Flat / Add-On** or **Reducing-Balance Equal-Installment**. Reducing-balance supports Added To Repayment only. Canonical: [interest-and-finance-charge-policy.md](interest-and-finance-charge-policy.md). First due default and exception default: [schedule-and-collection-calendar-policy.md](schedule-and-collection-calendar-policy.md).

Exact Traditional assessment criteria, agreement artifacts, and approval **limits** remain **OPEN**.

---

## Conceptual application flow

Not a finalized enum:

```text
Borrower
  → Draft Application
  → Submitted
  → Under Review
  → Approved / Rejected
  → Awaiting Disbursement
  → Disbursed
  → Active Loan
```

Support **cancellation** and **expiry** concepts where appropriate. Cancelled / expired applications remain historically visible. Do not convert them into deletion.

---

## Legal / compliance boundary

No Traditional application, product, or origination workflow in this document is claimed legally compliant. External qualified legal/compliance review remains required before Production (PLM-D-00-11). This package does not invent Philippine regulations.

---

## Explicit non-goals

- Duplicate Traditional vs Quick financial cores
- Auto-approval
- Final enum names
- Actual interest rates or peso limits
