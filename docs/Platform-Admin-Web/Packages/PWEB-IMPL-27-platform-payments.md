# PWEB-IMPL-27 — Platform Payments + Paid-Subscription Invariant

**Package ID:** PWEB-IMPL-27  
**Title:** Platform Payments + Paid-Subscription Invariant  
**Starting dependency:** PWEB-IMPL-20 PASS  
**Contract classification:** **PROVEN_EXISTING** (API/domain); UI **MISSING** in React  
**Implementation:** NOT STARTED (planning only)

## 1. Objective

Make Platform Payments the authoritative React view of Platform SaaS payment records and document/enforce the paid-subscription invariant already present in domain/API:

> Every **paid** subscription activation/renewal must reference a qualifying successful payment record, except where architecture already defines free trial / zero-price / explicit audited complimentary paths.

Never fabricate a successful payment for a free trial.

## 2. Current repository evidence

- `PaymentEndpoints`: list (global + org), get, manual create, confirm/reject/void, activate-subscription  
- Domain: `SaaSPayment` link-once; paid create requires confirmed unused payment  
- Bare `POST .../subscriptions/{id}/activate` blocked with payment-required error  
- React: Payments nav UNDER_DEVELOPMENT (`react-implementation.ts`)  
- Org billing tab (PWEB-14) is read-only payments list for an org

## 3. Existing APIs / contracts found

| Operation | Route | Classification |
|---|---|---|
| List payments | `GET /api/v1/platform/payments` | PROVEN_EXISTING (requires filters) |
| Org payments | `GET .../organizations/{id}/payments` | PROVEN_EXISTING |
| Payment detail | `GET .../payments/{paymentId}` | PROVEN_EXISTING |
| List-by-subscription App query | Application only | PROVEN_PARTIAL (no dedicated API route) |
| Confirm / reject / void | `POST .../payments/{id}/confirm|reject|void` | PROVEN_EXISTING |
| Activate subscription from payment | `POST .../payments/{id}/activate-subscription` | PROVEN_EXISTING |
| `isTest` on SaaS payment DTO | — | **MISSING** (Online method is weak proxy; ProviderPayment.IsTest is separate) |

**Methods (`SaaSPaymentMethod`):** `Cash`, `BankTransfer`, `GCash`, `Online` — **no** Cash Deposit / Other Manual in code.

## 4. Display fields (only when returned)

Document UI to bind: organization, subscription link, plan (if present), purpose, amount, currency, method, reference, status, dates, recorded/confirmed actors, failure reason — **only as DTO provides**. Do not invent billing-period fields if absent.

## 5. Reconciliation views (read-only detection)

Document operator-facing **detection** (not auto-repair) for anomalies **if** queryable from existing APIs; otherwise mark APPLICATION-ONLY / BACKEND CONTRACT REQUIRED:

- Paid Active subscription without qualifying successful payment  
- Successful subscription payment without linked subscription  
- Duplicate payment for same billing event  
- Amount/currency mismatch  

**Do not fabricate or repair historical data automatically.**

## 6. Authorization

`ManageManualPayments` for payment endpoints (per current API). UI + route + API.

## 7. UI / route scope

- Global `/admin/payments` (+ detail if supported)  
- Keep Platform SaaS money boundary (not POS sales, not PLM collections)

## 8. Mutation / CSRF / audit / errors

This package may be **read-first**; mutations that belong to attestation are PWEB-28. If any confirm/void is included here, CSRF required. Prefer splitting: 27 = authoritative payments view + invariant documentation/tests; 28 = attestation.

## 9. Explicit exclusions

- Auto-repair  
- Invented payment methods  
- Fabricating trial payments  
- POS/PLM money UIs

## 10. Change allowances

Backend: only if Product Owner authorizes `isTest` DTO or reconciliation queries; else document gaps. DB none unless authorized. POS/PLM/Blazor unchanged.

## 11. Tests / evidence / commit

Invariant tests against API; React list/detail; CSRF if mutating; axe  
Evidence: `docs/Platform-Admin-Web/Reports/PWEB-IMPL-27-platform-payments.md`  
Commit: `feat(platform-web): add platform payments directory`

## 12. Stop conditions

`PWEB27_PAYMENT_CONTRACT_MISSING`; inventing methods; claiming auto-repair

## 13. Definition of PASS

Payments authoritative view live; paid-subscription invariant documented and tested against server; no fabricated payments; gaps classified honestly.
