# PLATFORM-WEB-PA-COM-06 — SaaS Billing + Manual Payment + Paid Activation UI

**Package:** PA-COM-06  
**Status:** COMPLETE (awaiting Product Owner / ChatGPT review)  
**Application:** `src/Platform/ExItS.Platform.Admin.Web`  
**Branch:** `feat/platform-admin-pa-com-06`  
**Worktree:** `C:\Users\speed\Desktop\ExItS-SaaS-PlatformWeb-PA-COM-01`  
**Starting HEAD:** `1ee685a653661c92dd8da476533ddc97afbf2990`  

`PA_COM_01=APPROVED` · `PA_COM_04=APPROVED` · `PA_COM_06_AUTHORIZED=YES`  
Do **not** start PA-COM-05 from this report.

---

## 1. Scope delivered

| Area | Result |
|---|---|
| Organization → Billing | Summary cards + SaaS payment list with status filter/pagination |
| Record manual payment | `POST /api/v1/platform/payments/manual` → **PendingConfirmation** (catalog plan price, method, reference, paid date) |
| Confirm / Reject / Void | Canonical payment mutations with confirmation dialogs; reject/void require backend reason |
| Paid activation | **No misleading Activate subscription button.** Uses `POST .../payments/{id}/activate-subscription` after confirmed payment |
| Trial → paid | Operator path: record → confirm → **Activate from payment** for trialing subscription |
| No-subscription paid | **Subscribe with payment**: record → confirm → `POST .../organizations/{id}/subscriptions` with `paymentId` |
| Paid upgrade | PA-COM-04 upgrade still returns `payment_required`; operator completes billing workflow on Billing page |
| Local Validation | **LOCAL VALIDATION — Simulate payment** only when `localValidationToolsEnabled === true` |
| Real payment gateway | **NOT IMPLEMENTED** (manual + Local Validation only) |
| SAAS vs POS | Copy + captions distinguish subscription billing from POS sales |
| Agent 3 auth/runtime | **NOT modified** |
| POS React | **NOT modified** |
| Platform → POS direct calls | **None** |

---

## 2. Backend contracts used (PA-COM-01 clients)

| Action | Route |
|---|---|
| List org payments | `GET /api/v1/platform/organizations/{organizationId}/payments` |
| Record manual | `POST /api/v1/platform/payments/manual` |
| Confirm | `POST /api/v1/platform/payments/{paymentId}/confirm` |
| Reject | `POST /api/v1/platform/payments/{paymentId}/reject` |
| Void | `POST /api/v1/platform/payments/{paymentId}/void` |
| Activate from payment | `POST /api/v1/platform/payments/{paymentId}/activate-subscription` |
| Create paid subscription | `POST /api/v1/platform/organizations/{organizationId}/subscriptions` |
| Local Validation simulate | `POST /api/v1/platform/local-validation/payments/simulate` |

Payment statuses: `PendingConfirmation | Confirmed | Rejected | Voided` (no invented states).

Manual methods: `Cash | BankTransfer | GCash` (domain enum).

Plan prices displayed from catalog `monthlyPrice` / `annualPrice` + `currencyCode`. Seed values (e.g. 299/699/1499 PHP) are **development placeholders**, not launch pricing.

---

## 3. Permissions

| Capability | Permission |
|---|---|
| View/list payments | `platform.permission.manage_manual_payments` (backend; fail-closed 403 without it) |
| Record / confirm / reject / void | `platform.permission.manage_manual_payments` |
| Activate from payment / subscribe with payment | **Both** `manage_manual_payments` and `platform.permission.manage_subscriptions` in UI; server authoritative on 403 |

---

## 4. Canonical paid flows

### 4.1 Record ≠ confirmed

Manual record creates **PendingConfirmation**. Confirm is a separate operator action unless using the combined subscribe-with-payment wizard (record + confirm + create in sequence).

### 4.2 Trial → paid (manual)

1. Record SaaS payment (catalog amount)  
2. Confirm payment  
3. **Activate from payment** → `ConfirmPaymentAndActivateSubscription` (trialing → active paid period)

Does **not** call misleading `POST .../subscriptions/{id}/activate`.

### 4.3 No subscription → paid

**Subscribe with payment** when organization has no POS subscription record:

1. Select active catalog plan (live price)  
2. Record + confirm payment  
3. `createPaidSubscription` with confirmed `paymentId` + published plan version

### 4.4 Paid upgrade (Growth → Pro)

1. Subscription page: Change plan → upgrade → backend `payment_required` (PA-COM-04)  
2. Billing: record + confirm payment for target plan catalog price  
3. Complete upgrade via billing activation path or apply pending plan per backend state (operator uses Billing + Subscription together)

No browser-side proration.

---

## 5. Tests / quality gates

| Gate | Result |
|---|---|
| Vitest | 55 files, **329** tests PASS |
| ESLint | PASS |
| Typecheck | PASS |
| Production build | PASS |
| Playwright billing spec | Not re-run in this session (existing `organization-billing.spec.ts` baseline) |

New coverage: `OrganizationBillingLifecycle.test.tsx`, `billing-lifecycle.test.ts`.

---

## 6. Files changed (React)

- `OrganizationBillingLifecycle.tsx` (new)
- `OrganizationBillingPage.tsx` (delegates to lifecycle)
- `billing-lifecycle.ts` / `billing-lifecycle.test.ts` (new)
- `use-commercial-mutations.ts` (payment + paid subscription hooks)
- `use-authorization.tsx` (`actorIdentifier` for audit actor fields)
- `auth-fixtures.ts` (payment mutation stubs)
- `messages.ts` (billing i18n)
- `OrganizationBillingLifecycle.test.tsx` (new; replaces page test)

---

## 7. Known gaps preserved (not PA-COM-06 scope)

- Plan-version retire HTTP missing  
- Admin renew route missing  
- Draft business-type grants forced null  
- MaxActiveStaff invite enforcement missing  

---

## 8. Flags

```
PA_COM_06=AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW

SAAS_MANUAL_PAYMENT_UI=PASS
PAYMENT_CONFIRM_UI=PASS
PAYMENT_REJECT_UI=PASS
PAYMENT_VOID_UI=PASS

PAID_SUBSCRIPTION_ACTIVATION=PASS
TRIAL_TO_PAID_FLOW=PASS
PAID_UPGRADE_FLOW=PASS (operator path via Billing after PA-COM-04 payment_required)
NO_SUBSCRIPTION_PAID_FLOW=PASS

LOCAL_VALIDATION_PAYMENT_SIMULATION=PASS
REAL_PAYMENT_GATEWAY=NOT_IMPLEMENTED

SAAS_PAYMENT_POS_PAYMENT_SEPARATION=PASS
PLATFORM_TO_POS_DIRECT_CALLS=NO

AGENT_3_RUNTIME_AUTH_FILES_MODIFIED=NO
POS_REACT_MODIFIED=NO

PA_COM_05_AUTHORIZED=NO
MERGE_TO_PLATFORM_ADMIN_V2=NO
MERGE_TO_MAIN=NO
PRODUCTION_CUTOVER=NO
```

**HARD STOP** — do not start PA-COM-05.
