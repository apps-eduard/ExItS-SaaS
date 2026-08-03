# P16-WP11 — Pricing, Payments, and Subscription Changes

> **Status:** In Progress (validation underway)  
> **Phase:** 16 — Implementation Complete, Under Validation  
> **Next:** P16-WP12 — Not Started  
> **Commit SHA:** `7b2566a4fa49b07cbc711d64712cb6fc40e8520d`

---

## Commercial catalog defects fixed (2026-08-03)

### Root cause A — Personal Start a Business “No plans available”

- `PersonalStartBusiness.razor` called `GetPlansAsync` → `GET /api/v1/platform/catalog/plans`, which requires `PlatformPermission.ViewPortfolio`.
- Personal account profiles lack `ViewPortfolio` → API returned **403**; UI treated any non-success as an empty plan list.

**Fix:** Authenticated commercial catalog `GET /api/v1/commercial/plans?productCode=pinoy-business-pos` (any authenticated account class; no portfolio/org/subscription gate). Calls `EnsureMvpPosPlans` before listing Active MVP plans. Admin UI uses `GetCommercialPlansAsync` with loading / retryable error / true empty states; form validates org name, slug, and billing cycle before submit.

### Root cause B — Organization Current Plan “An unexpected error occurred”

- Route `/admin/organizations/{id}/commercial` loaded `GetOrganizationCommercialSummaryAsync` → `GET /api/v1/platform/admin/organizations/{id}/commercial-summary`.
- Application DTO used `PlatformOrganizationDto` + `SaaSPaymentDto`; Admin expected `OrganizationDto` + `PaymentDto`. Section loads (subscriptions / payments / entitlements) could throw and surface as **500** via the global exception handler (“An unexpected error occurred.”).

**Fix:** Null-safe section loads in `GetOrganizationCommercialSummaryAsync`; subscription mapping skips enrichment failures; API maps commercial-summary JSON to Admin-compatible shape; dedicated `GET /api/v1/platform/organizations/{organizationId}/current-plan?productCode=pinoy-business-pos` for Current Plan UI; `OrganizationCommercial.razor` uses current-plan + commercial plans, null-safe subscription rendering, Choose a Plan when no subscription / Trialing / Expired, and retry on load errors.

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
| `Wp11PricingPaymentsPlanChangeTests` | **34 passed** (incl. commercial endpoint source guard) |
| `ApiWp11*` integration | **19 passed** (incl. `ApiWp11CommercialCatalogAndCurrentPlanTests`) |
| Admin `~Wp11` | **25 passed** |

Covers: Starter/Pro trial rejected; Business 14-day trial; one trial/org/product; convert to Starter/Business/Pro; failed conversion preserves Trialing; expired subscribe; idempotent conversion; no payment on trial start; agreed-price snapshot; Production LV guard; UI source guards; personal commercial plans 200; EnsureMvpPosPlans idempotent; org empty current-plan/commercial-summary 200; Admin DTO deserialize; cross-org denial.

## Manual Local Validation evidence (2026-08-03)

Against running LV stack (`localhost:8091`):

| Check | Result |
|---|---|
| `GET /api/v1/commercial/plans` (Olivia / Maria) | **200** — Starter 299/2990 trial=false; Business 699/6990 trial=true/14; Pro 1499/14990 trial=false |
| `GET /api/v1/platform/catalog/plans` (Maria org owner) | **403** (expected — ViewPortfolio required) |
| `GET .../organizations/{abc}/current-plan` (Olivia + Maria) | **200** — Trialing subscription mapped; no 500 |
| `GET .../commercial-summary` (Olivia) | **200** |
| Empty org `current-plan` | **200** — `currentSubscription=null`, `availablePlans=3` |

Browser UI: Admin watch should pick up `PersonalStartBusiness` / `OrganizationCommercial` changes; confirm plan cards and Current Plan render without the prior empty/error states after hard refresh if needed.

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
