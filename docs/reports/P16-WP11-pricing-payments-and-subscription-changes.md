# P16-WP11 — Pricing, Payments, and Subscription Changes

> **Status:** In Progress (validation underway)  
> **Phase:** 16 — Implementation Complete, Under Validation  
> **Next:** P16-WP12 — Not Started  
> **Commit SHA:** `b70f807b883d4b44d6910bc310b00bbd755f7b5a`

---

## Final trial policy (authoritative)

| Plan | Trial | Subscribe |
|---|---|---|
| **Starter** | No free trial (`TrialAllowed=false`) | Subscribe Now only |
| **Business** | 14-day free trial, no card, no Platform approval | Start Free Trial or Subscribe Now |
| **Pro** | No separate free trial | Subscribe Now; UI offers Try Business Free First |

- One trial per Organization per Product; expired or converted trials cannot be restarted.
- During or after Business trial, Owner may convert/subscribe to **Starter**, **Business**, or **Pro**.
- **No automatic charge** at trial expiry.
- No cash SaaS payment workflow; Local Validation fake online outcomes only.

## Plan pricing

- `Plan`: `TrialAllowed`, `DefaultTrialDays`, `MonthlyPrice`, `AnnualPrice`, `CurrencyCode`, `SortOrder` (Display Order).
- MVP PHP defaults: Starter 299/2,990; Business 699/6,990; Pro 1,499/14,990.
- Catalog price edits do not reprice existing subscription snapshots.

## Start a Business UI

- Plan cards with business-friendly labels (no keys/GUIDs).
- Business visually recommended; Starter/Pro show Subscribe Now; Pro offers Try Business Free First (highlights Business card).
- Trial start creates **no** payment record.

## Trial conversion

- `POST .../subscriptions/{id}/convert-trial` — target any active Plan + billing cycle + LV payment.
- Succeeded → Active, agreed price snapshot, billing period, entitlement revision.
- Declined/Failed → stays Trialing (if trial active); plan unchanged; failed payment retained.
- Duplicate idempotency key → no duplicate activation/period/entitlement transition.

## Trial expiry

- Trialing → Expired (existing expire path); no payment; Product data retained; Owner sees Choose a Plan.

## Local Validation

- Fake payment controls only when LV enabled; Production rejects `Payments:Provider=LocalValidation`.
- No card number/CVV/expiry inputs.
- Reset: `.\tools\Reset-LocalValidation.ps1 -ConfirmReset` → Olivia + Rafael only.

## Tests (Release evidence)

| Suite | Result |
|---|---|
| `Wp11PricingPaymentsPlanChangeTests` | **33 passed** |
| `ApiWp11*` integration | **11 passed** |
| Admin `~Wp11` | **25 passed** |

Covers: Starter/Pro trial rejected; Business 14-day trial; one trial/org/product; convert to Starter/Business/Pro; failed conversion preserves Trialing; expired subscribe; idempotent conversion; no payment on trial start; agreed-price snapshot; Production LV guard; UI source guards.

## Manual acceptance checklist (remaining)

- [ ] LV reset → only Olivia + Rafael
- [ ] Personal → Start a Business → Business Start Free Trial (14 days, no payment record)
- [ ] Starter/Pro Start Free Trial not offered / API rejects
- [ ] Convert trial to Starter / Business / Pro via LV Succeeded
- [ ] Declined conversion leaves Trialing
- [ ] Expire trial without charge → Choose a Plan → subscribe
- [ ] Upgrade/downgrade on Active; failed upgrade leaves plan unchanged
- [ ] LV controls absent when LV disabled

## Status

```text
Phase 16 — Implementation Complete, Under Validation
P16-WP11 — In Progress
P16-WP12 — Not Started
```

Final acceptance is **not** claimed.
