# POS-UTANG-CHECKOUT-CUSTOMER-DIRECTORY-FIX-02

## TASK
POS-UTANG-CHECKOUT-CUSTOMER-DIRECTORY-FIX-02 — Unify Sell → Checkout customer directory across Cash / GCash / Utang

## START_SHA
`0aa702cd4aabfa4a7b9820865514ca3459db1e4c`

## FINAL_SHA
_(recorded after commit)_

## BRANCH
`feat/organization`

## STATUS
PASS (targeted Application/unit + React tests; full `npm run build` / `tsc -b` still blocked by pre-existing unrelated inventory/shifts TS errors)

## ROOT_CAUSE

Cash/GCash and Utang used **different customer load paths**:

| Payment | Idle directory behavior |
|---|---|
| Cash / GCash (`kind=all`) | Prefer **Business** via `checkout-search kind=Business` (and Managers also used management `listCustomers` for people) |
| Utang | Forced `checkout-search kind=Customer` (people only) |

Additionally, switching payment to Utang **cleared** a selected B2B customer, and blank `kind=All` was rejected client- and (previously) server-side. The same Active person/B2B row therefore looked “missing” on Utang even when credit policy was unrelated.

Credit-policy Approved-only filtering was **not** the directory bug; policy remains an eligibility overlay + server gate.

## After

- **One** checkout-safe directory: `GET /api/v1/pos/customers/checkout-search` for Cash, GCash, and Utang
- Blank search valid for `All` / `Customer` / `Business` (first page ≤ 20)
- Batched credit projection on person rows (`CreditStatus`, limit, outstanding, available, term) — no N+1
- Utang shows compact credit status; ineligible customers stay **visible** but Confirm blocked
- Selection preserved across Cash ↔ GCash ↔ Utang (B2B kept selected on Utang with clear blocked reason)
- CreateSale least privilege unchanged; CheckoutSale / CreateCreditEntry credit-policy revalidation unchanged

## Flags

| Flag | Value |
|---|---|
| CHECKOUT_DIRECTORY_SHARED | YES |
| BLANK_SEARCH_SUPPORTED | YES |
| CASHIER_FULL_CUSTOMER_PERMISSION_REQUIRED | NO |
| CUSTOMER_HIDDEN_WHEN_CREDIT_NOT_APPROVED | NO |
| UTANG_FINAL_SERVER_REVALIDATION | YES |
| CREDIT_POLICY_BYPASSED | NO |
| CREDIT_LIMIT_BYPASSED | NO |
| TERM_BYPASSED | NO |
| CROSS_ORG_FAIL_CLOSED | YES |
| BRANCH_SCOPE_PRESERVED | YES |
| N_PLUS_ONE | NO |
| B2B_UTANG_ADDED | NO |
| OFFLINE_UTANG_ADDED | NO |

## Previous package

Builds on `POS-CUSTOMER-CREDIT-POLICY-APPROVAL-AUDIT-01` (`docs/Mobile-React/Reports/POS-CUSTOMER-CREDIT-POLICY-APPROVAL-AUDIT-01.md`). Does not weaken policy authorize, advisory locks, or Approved + limit gates.

## Validation evidence

- Application / Infrastructure / Api **Release** build: OK
- Unit: CheckoutBusinessCustomerSearch + CustomerCreditPolicy + CreditEntry + BusinessUtang filters — **34 passed**
- React `tsc --noEmit`: OK
- Vitest (directory / option / compact / shared / credit-policy / personal picker): **34 passed**
- React `npm run build` (`tsc -b`): FAIL — pre-existing errors in Inventory/LowStock/Shifts/Registers (not introduced by this task)

## Known residuals

- Full React `tsc -b` / `npm run build` still red on unrelated pages
- Integration `PosCheckoutCustomerSearchApiTests` updated for blank All; suite needs Testcontainers + migrated DB to execute in CI locally
- Manual Scenario A–C on a live API still recommended after restart/migrate
