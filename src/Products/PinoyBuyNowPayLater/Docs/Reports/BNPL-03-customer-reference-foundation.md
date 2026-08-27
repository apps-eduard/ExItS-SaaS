# BNPL-03 — Customer / Reference Foundation + Initial Product Persistence

| Field | Value |
|---|---|
| Package | BNPL-03 |
| Status | **COMPLETE** |
| Branch | `feat/bnpl` |
| Baseline | `88c10c37ff62c0220e349c33bfd45483f1e39000` |

## Delivered

- Organization-scoped `BnplCustomer` aggregate (profile only; not financing eligibility)
- Optional Platform Personal public user id link (`EX-####-####`) — opaque, no cross-DB FK
- Optional Commerce/POS customer Guid link — opaque, no POS project dependency
- Idempotent create by stable `CustomerId` (compatible → converge; conflict → 409)
- Unique filtered indexes: org+personal link, org+commerce link
- `BnplDbContext` + schema `bnpl` + migration `InitialBnplCustomerFoundation`
- Logical database name: `ExItS_PinoyBuyNowPayLater` (BNPL-D-00-04)
- Capabilities: `bnpl.customer.read`, `bnpl.customer.manage` (BNPL-D-00-18 extended)
- API: `/api/v1/bnpl/customers` (+ personal-link / commerce-link) with branch header + access guard
- No production `Database.Migrate()` at API startup

## Effective access for customer staff ops

authenticated actor + org membership + BNPL entitlement + product assignment
+ `X-Bnpl-Branch-Id` allowed by branch scope + `bnpl.customer.read|manage`

Customer aggregate itself is **not** branch-bound (org-scoped). Branch scopes the staff operation.

## Explicit non-goals (confirmed absent)

FinancingApplication, FinancingPlan, Installment, Repayment, Settlement, CreditLimit, KYC, React client.

## Decisions

| ID | Status |
|---|---|
| BNPL-D-00-04 | Provisionally Approved / Implemented in BNPL-03 |
| BNPL-D-00-18 | Implemented in BNPL-02; extended in BNPL-03 with customer capabilities |
| BNPL-D-00-13 | Remains OPEN (no Personal self-service UX) |
| D-P12-03 | Not bypassed — default access provider remain fail-closed |

## Next

**BNPL-04** — Financing application + lifecycle
