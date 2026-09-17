# POS-BUSINESS-CUSTOMER-CREDIT-POLICY-UI-01

## TASK
POS-BUSINESS-CUSTOMER-CREDIT-POLICY-UI-01 — Business Customer Credit & Payment Terms (+ People 404 UX fix)

## START_SHA
`6c28ffd74f7b98a67bce0481bc8b2b747c119960`

## FINAL_SHA
`1a5873054aebcbc74d757c90f7ac1067f05066ab`

## BRANCH
`feat/organization`

## STATUS
PASS (targeted .NET unit + React vitest + `tsc --noEmit`; full `npm run build` / `tsc -b` still blocked by pre-existing unrelated inventory/shifts errors)

## PEOPLE_CREDIT_POLICY_404_ROOT_CAUSE

Observed UI: People customer detail showed raw `POS API request failed (404)` inside Credit policy.

Likely causes (code-side):

1. **Route trailing-slash hardening** — `MapGet("/")` on the credit-policy group could miss `/credit-policy` without a trailing slash depending on hosting; now maps both `""` and `"/"`.
2. **Raw error UX** — any real 404/network failure was rendered as the PosApiError message string (not a friendly load/retry state).
3. **Operational** — unapplied `AddCustomerCreditPolicies` migration or an API binary without the endpoints also surfaces as HTTP 404 to the client.

Contract preserved/confirmed: valid customer with **no policy row** returns **200** + `status: NotConfigured` (never `CustomerCreditPolicyNotFound` on GET). GET also applies branch visibility fail-closed (`CustomerNotFound` when inaccessible).

## BUSINESS CREDIT

New seller-owned aggregate (no POSCustomer stub):

| Table / type | Role |
|---|---|
| `BusinessCustomerCreditPolicy` | SellerOrganizationId + BuyerOrganizationId (+ ConnectionId) |
| `BusinessCustomerCreditPolicyChange` | Append-only audit |

States: NotConfigured → PendingApproval → Approved → Disabled (same semantics as People).

API (canonical connection routing):

- `GET/PUT /api/v1/pos/connected-suppliers/business-customers/{connectionId}/credit-policy`
- `POST .../approve`, `.../disable`
- `GET .../history`

Outstanding for B2B is **0** until a B2B credit ledger exists (`B2B_UTANG_LEDGER_IMPLEMENTED=NO`). Checkout Utang for B2B not wired (`B2B_UTANG_CHECKOUT_IMPLEMENTED=NO`).

## FLAGS

| Flag | Value |
|---|---|
| PEOPLE_NOT_CONFIGURED_RETURNS_200 | YES |
| BUSINESS_CREDIT_POLICY_UI | YES |
| BUSINESS_CREDIT_POLICY_STORAGE | YES |
| BUSINESS_POLICY_AUDIT | YES |
| BUSINESS_POLICY_APPROVAL | YES |
| BUSINESS_POLICY_LIMIT | YES |
| BUSINESS_POLICY_TERM | YES |
| BUSINESS_POLICY_HISTORY | YES |
| BUSINESS_POSCUSTOMER_DUPLICATE_CREATED | NO |
| CROSS_ORG_FAIL_CLOSED | YES |
| B2B_UTANG_LEDGER_IMPLEMENTED | NO |
| B2B_UTANG_CHECKOUT_IMPLEMENTED | NO |
| N_PLUS_ONE | NO (detail-only load) |
| MIGRATION | `20260910180000_AddBusinessCustomerCreditPolicies` |

## VALIDATION

- Application / Infrastructure / Api Release: OK
- Unit: BusinessCustomerCreditPolicy + CustomerCreditPolicy — **23 passed**
- React `tsc --noEmit`: OK
- Vitest Business/People credit sections + BusinessCustomerIdentity — **10 passed**
