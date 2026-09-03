# Product Truth: Pinoy Loan Manager

> Source: `src/Products/PinoyLoanManager/Docs/product-definition.md` and related docs.
> Implementation status as of 2026-09-03 code inspection.

---

## Product Identity

| Field | Value |
|---|---|
| Product name | Pinoy Loan Manager |
| Platform product code | `pinoy-loan-manager` (approved — PLM-D-00-01) |
| Status | **PLANNED** — Documentation complete (PLM-DOC-01 through PLM-DOC-11); implementation absent and paused |
| Implementation | API, Application, Domain, Infrastructure, Client, Web scaffold projects exist but contain no operational implementation |

---

## What the Product Is Intended to Be

From product documentation:

- Independently subscribed ExItS product for **lending operations**
- Two origination paths: Traditional Loan and Quick Loan; both converge into one core Loan model after disbursement
- Features documented: installment schedules, repayments, financing lifecycle, fee model (ADR-007), payment allocation, delinquency, penalties, early settlement, principal prepayment, restructuring, write-off, recovery
- Role presets: `plm.owner`, `plm.manager`, `plm.cashier`, `plm.collector` (PLM Authorization Policy v1 — PLM-D-00-06)
- Borrowers use ExItS Personal as presentation surface only
- Multi-branch support intended

**This is recorded intent only. Implementation is absent and paused.**

---

## Confirmed Capabilities

**None.** Implementation is absent.

---

## Marketing Classification

**PLANNED / Coming Soon**

- Do not present as available.
- Safe messaging: "Lending management for Filipino organizations. Coming soon."
- Loan product codes and financial product details must not be marketed until product resumes and is confirmed.

---

## Prohibited Claims

- Do not imply SEC or BSP licensing or compliance.
- Do not describe specific interest rates, fee structures, or loan terms.
- Do not present as available.
- Do not promise a release date.
