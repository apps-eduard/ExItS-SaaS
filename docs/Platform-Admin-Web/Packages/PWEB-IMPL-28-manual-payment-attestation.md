# PWEB-IMPL-28 — Manual Payment Attestation

**Package ID:** PWEB-IMPL-28  
**Title:** Manual Payment Attestation  
**Starting dependency:** PWEB-IMPL-27  
**Contract classification:** **PROVEN_PARTIAL**  
**Implementation:** NOT STARTED (planning only)

## 1. Objective

Authorized Platform Billing operator attests an **actual received** manual payment under Platform Administration → Payments, then drives confirm → subscription activation/renewal → entitlement update → audit as a **server transactional** flow where contracts provide it.

## 2. Current repository evidence

- `POST /api/v1/platform/payments/manual` — `CreateManualSaaSPayment`  
- Manual methods allowed: **Cash**, **BankTransfer**, **GCash** (Online rejected for manual create)  
- Confirm / reject / void endpoints  
- `POST .../payments/{id}/activate-subscription` → confirm+activate+link  
- Actor fields (`ConfirmedBy` etc.) still plain strings in API — **PROVEN_PARTIAL** (development-stage actor binding concern)

## 3. Existing APIs / contracts found

| Operation | Route | Classification |
|---|---|---|
| Record manual | `POST .../payments/manual` | PROVEN_EXISTING |
| Confirm | `POST .../payments/{id}/confirm` | PROVEN_EXISTING |
| Reject | `POST .../payments/{id}/reject` | PROVEN_EXISTING |
| Void | `POST .../payments/{id}/void` | PROVEN_EXISTING |
| Confirm + activate subscription | `POST .../payments/{id}/activate-subscription` | PROVEN_EXISTING |
| Session-bound attested-by actor | — | **PROVEN_PARTIAL** (`BACKEND CONTRACT REQUIRED` to replace free-text actors for production-grade attestation) |

**Do not invent:** Cash Deposit, Other Manual Payment (not in `SaaSPaymentMethod`).

## 4. Transactional expectation

Prefer server endpoints that keep payment + subscription + entitlement consistent. If any required step fails, UI must not claim partial commercial success. Prefer `activate-subscription` composite where that is the canonical path.

## 5. Authorization

`ManageManualPayments` (+ any additional server checks). BillingAdministrator has this in catalog; still fail closed on 403.

## 6. UI / route scope

- Only under Payments (`/admin/payments`)  
- Explicit confirmation; busy/idempotent submit; duplicate reference errors surfaced  
- CSRF required

## 7. Audit / security / errors

Server audit; PWEB-20 CSRF; 401/403/409 duplicate reference / already used payment / validation

## 8. Explicit exclusions

- Online/provider payment pretending to be manual  
- Invented methods  
- Attestation outside Payments  
- Ignoring actor-binding gap for cutover claims

## 9. Change allowances

Backend may be required to bind ConfirmedBy to session user before treating attestation as production-ready; DB only if that change needs it and is authorized. POS/PLM/Blazor unchanged.

## 10. Tests / evidence / commit

Manual create+confirm+activate; duplicate reference; CSRF; unauthorized; refresh  
Evidence: `docs/Platform-Admin-Web/Reports/PWEB-IMPL-28-manual-payment-attestation.md`  
Commit: `feat(platform-web): add manual payment attestation`

## 11. Stop conditions

`PWEB28_MANUAL_PAYMENT_CONTRACT_MISSING`; unsafe free-text actor accepted as “done” without Product Owner risk acceptance

## 12. Definition of PASS

Manual attestation UI uses only proven methods/endpoints; commercial state remains consistent per server; CSRF correct; actor-binding gap either closed or explicitly accepted as non-cutover.
