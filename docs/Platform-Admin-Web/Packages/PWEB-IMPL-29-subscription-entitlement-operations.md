# PWEB-IMPL-29 — Subscription + Entitlement Operations

**Package ID:** PWEB-IMPL-29  
**Title:** Subscription + Entitlement Operations  
**Starting dependency:** PWEB-IMPL-27 + PWEB-IMPL-28 (for payment-required paths)  
**Contract classification:** **PROVEN_PARTIAL**  
**Implementation:** NOT STARTED (planning only)

## 1. Objective

Implement controlled Platform commercial-state operations using **existing** subscription and entitlement domain/API — only operations that are proven. Payment-required paths must obey the PWEB-27 invariant.

## 2. Current repository evidence

- Org subscription/entitlement/billing read-only (PWEB-12/13/14)  
- SubscriptionEndpoints: grace/past-due/suspend/reactivate/cancel/expire; plan change; trials; paid create with `paymentId`  
- Bare activate route exists but use case enforces payment-required  
- Renew: Application + Local Validation simulate; **no** dedicated Admin `POST .../renew`  
- Entitlements: snapshots generate/reconcile; feature overrides create/revoke (`ManageEntitlementOverrides`)

## 3. Existing APIs / contracts found (representative)

| Area | Classification |
|---|---|
| List/get subscriptions | PROVEN_EXISTING |
| Trial create | PROVEN_EXISTING |
| Paid create with paymentId | PROVEN_EXISTING |
| Suspend / reactivate / cancel / expire / grace / past-due | PROVEN_EXISTING |
| Plan change / upgrade / downgrade / convert-trial / preview | PROVEN_EXISTING |
| Dedicated renew HTTP | **MISSING** / PROVEN_PARTIAL |
| Entitlement snapshot generate/reconcile | PROVEN_EXISTING |
| Feature override create/revoke | PROVEN_EXISTING |
| Bare activate without payment | Blocked by domain (**PROVEN_EXISTING** invariant) |

**At implementation time:** re-list exact routes from `SubscriptionEndpoints.cs` / `EntitlementEndpoints.cs` and only wire buttons that match.

## 4. Rules

- One product subscription must not mutate another product’s subscription  
- Subscription entitlement ≠ product-local role / POS/PLM operational permission  
- Entitlement overrides must be audited (server)  
- Payment-required commercial ops obey PWEB-27/28  

## 5. Authorization

- `ManageSubscriptions` for subscription commercial ops / snapshot generate  
- `ManageEntitlementOverrides` for overrides  
- BillingAdministrator lacks entitlement overrides in current catalog — UI must not imply otherwise

## 6. UI / route scope

- Global and/or org workspace subscription + entitlement surfaces currently under-development  
- Confirmation for destructive/commercial transitions  
- No inventing renew button without route (`BACKEND CONTRACT REQUIRED BEFORE IMPLEMENTATION`)

## 7. Mutation / CSRF / audit / errors

PWEB-20 CSRF; server audit; 401/403/404/409; payment-required errors must guide operator to Payments — not fabricate payment

## 8. Explicit exclusions

- Cross-product subscription mutation  
- Granting POS/PLM roles via entitlement  
- Auto historical repair  
- Undocumented renew UI

## 9. Change allowances

Backend only if renew (or other) Admin route is authorized as a gap fix; DB only if required. POS/PLM/Blazor unchanged.

## 10. Tests / evidence / commit

Each wired op; payment invariant; override audit path; CSRF; isolation between products  
Evidence: `docs/Platform-Admin-Web/Reports/PWEB-IMPL-29-subscription-entitlement-operations.md`  
Commit: `feat(platform-web): add subscription entitlement operations`

## 11. Stop conditions

`PWEB29_SUBSCRIPTION_MUTATION_CONTRACT_MISSING`; payment bypass; inventing ops

## 12. Definition of PASS

Only proven commercial ops exposed; invariant held; entitlement ≠ product-local auth; CSRF correct.
