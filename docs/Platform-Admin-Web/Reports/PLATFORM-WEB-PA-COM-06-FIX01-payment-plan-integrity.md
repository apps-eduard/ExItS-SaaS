# PLATFORM-WEB-PA-COM-06-FIX01 — Payment→Plan Integrity + Manual Paid Upgrade

**Package:** PA-COM-06-FIX01  
**Status:** COMPLETE (awaiting Product Owner / ChatGPT review)  
**Branch:** `feat/platform-admin-pa-com-06`  

---

## Server funding rule

**CONFIRMED_PAYMENT ≠ SUFFICIENT_PAYMENT**

Confirmation records operator verification of a manual payment reference. Funding eligibility is validated separately when a payment is used to:

- create a paid subscription
- activate / reactivate from trial or non-active states
- upgrade an active subscription

Canonical validator: `SaaSPaymentFundingValidation.ValidatePlanFunding`

- `payment.Amount == plan.PriceForCycle(billingCycle)`
- `payment.CurrencyCode == plan.CurrencyCode` (exact match; no overpayment wallet)

Error codes: `application.payment.amount_mismatch`, `application.payment.currency_mismatch`, `application.payment.period_mismatch`

---

## Manual paid upgrade (Active Growth → Pro)

**Do not** use `activate-subscription` for active subscriptions.

`POST /api/v1/platform/payments/{paymentId}/upgrade-subscription`

Body: `{ subscriptionId, targetPlanId, billingCycle }`

Use case: `UpgradeSubscriptionFromConfirmedPayment` — same subscription id, plan upgraded, payment linked, entitlement snapshot regenerated. No `IPaymentProvider`.

---

## React handoff

Subscription upgrade `payment_required` → navigate to Billing with `upgradeSubscriptionId`, `targetPlanId`, `billingCycle` (no price in URL). Billing reloads catalog price; **Complete upgrade** calls upgrade endpoint.

---

## Billing cycle

UI selects Monthly/Annual when catalog supports both. Server validates `PriceForCycle` and paid period via `SubscriptionBillingPeriods.ComputePaidPeriod`.
