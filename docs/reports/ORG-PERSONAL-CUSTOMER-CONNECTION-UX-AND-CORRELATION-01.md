# ORG-PERSONAL-CUSTOMER-CONNECTION-UX-AND-CORRELATION-01

| Field | Value |
|---|---|
| Status | Complete (correlation auth fix + connection UX) |
| Branch | `feat/pos-react-client` |
| Worktree | `ExItS-SaaS-pos-react-client` |
| Migration | **NONE** |

## Root cause (Paul Stand ↔ Toto Uy)

Platform Active link and POS correlation were **already healthy**:

- `LinkedCustomerAppUser` Active
- `BusinessCustomer.LinkedUserIdentityId` matches Toto
- Exactly one `POSCustomer` with matching `PlatformBusinessCustomerId`
- Utang sale `SALE-20260825-000001` / ₱205 on that POS customer

Personal UI showed **"Purchase history unavailable" / "Linked customer was not found."** because:

1. React Personal uses **HttpOnly Platform cookie** + **Bearer** product token for POS.
2. `LinkedCustomerPlatformAuthorizationClient` only forwarded `X-ExItS-Session-Token` / `PlatformSession` Authorization — **not** the Platform auth cookie.
3. Platform linked-merchant authorization therefore failed closed → POS returned 404 with that detail.
4. React mapped any statement 404 to a relationship-looking empty state.

**ROOT_CAUSE = SESSION_FORWARDING** (not missing POS correlation for this pair).

## Fix

- Forward Platform caller credentials (Cookie / PlatformSession / session header; never product Bearer) from POS → Platform in:
  - `LinkedCustomerPlatformAuthorizationClient`
  - `PersonalFeatureEntitlementClient`
- Resolve `.ExItS.Platform.Auth` cookie into PlatformSession when present.
- Separate **relationship state** from **data-load state** in Personal statement UI (Connected + history not ready / load error).
- Shared connection presentation helpers + Org/Personal status chips and copy (en + fil-PH; other locales keyed for parity).

## Non-goals preserved

- Business Utang remains independent of connection state.
- Organization must not see “customer blocked you” (neutral Unavailable).
- Device auth Local Validation pause unchanged.
- No migration; no duplicate customers.

## Live proof (Local Validation)

After POS API restart with fix:

- `Bearer` + Platform cookie via `:5177/pos-api` → statement **200**, outstanding **205**
- Activity → `SALE-20260825-000001` charge **205**
- Device policy still `enforcementEnabled=false`
