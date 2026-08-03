# P16-WP11 — Pricing, Payments, and Subscription Changes

> **Status:** In Progress (implementation complete; validation underway)  
> **Phase:** 16 — Implementation Complete, Under Validation  
> **Next:** P16-WP12 — Not Started  
> **Commit SHA:** `0f45e4c6ed499786326c2b0ea79c884eb7964651`

---

## Plan pricing

- `Plan` exposes `MonthlyPrice`, `AnnualPrice`, `CurrencyCode` separate from `SortOrder` (Display Order).
- MVP PHP defaults (Local Validation, configurable): Starter 299/2,990; Business 699/6,990 + 14-day trial; Pro 1,499/14,990.
- Platform Admin **Plans** list and create/commercial edit forms show pricing columns and fields.
- Catalog edits do not retroactively change existing subscription snapshots.

## Subscription snapshots

- `Subscription` stores `BillingCycle`, `AgreedPrice`, `CurrencyCode`, `PriceEffectiveFromUtc`, period bounds, and `PendingPlanId` / `PendingPlanEffectiveAtUtc` for scheduled downgrades.
- Platform Admin **Subscriptions** and Organization Commercial views show commercial snapshot columns.

## LV fake payments

- `IPaymentProvider` with `LocalValidationPaymentProvider` (test-only) and `ProviderPayment` persistence (`lvp_pay_*` references, `IsTest`).
- `POST /api/v1/platform/local-validation/payments/simulate` and `GET /api/v1/platform/local-validation/enabled`.
- Admin **Test Payments** (platform) and Organization Commercial billing section gated by `LocalValidationSignInService.IsAvailable` (non-Production + config).
- Production startup throws if `Payments:Provider=LocalValidation`.

## Start a Business

- Personal Admin page `/admin/personal/start-business` collects org details, plan, billing cycle, trial/pay-now.
- Calls `POST /api/v1/personal/start-business` with `PlanKey`, `BillingCycle`, `StartAsTrial`, `PayNow`.
- Creates Organization Account Profile + Organization + Owner membership + Org Subscription + Entitlement + Product Instance + explicit POS Owner; Personal Account unchanged.
- Success requires explicit Organization account profile selection (no “assign Plan to Personal Account” copy).

## Upgrade / downgrade

API (org-scoped, Platform `ManageSubscriptions` or org Owner):

| Method | Route |
|---|---|
| GET | `/api/v1/platform/organizations/{organizationId}/subscriptions/{subscriptionId}/plan-change-preview` |
| POST | `/api/v1/platform/organizations/{organizationId}/subscriptions/{subscriptionId}/upgrade` |
| POST | `/api/v1/platform/organizations/{organizationId}/subscriptions/{subscriptionId}/downgrade` |
| POST | `/api/v1/platform/organizations/{organizationId}/subscriptions/{subscriptionId}/apply-pending-plan` |

Admin Organization Commercial page: current plan, plan comparison, upgrade/downgrade with usage-conflict preview confirm, LV test payments.

## Entitlement transitions

- Trial/paid activation and plan upgrades refresh entitlement snapshots.
- Downgrade schedules pending plan; data retained until effective apply.
- Expired trial / suspended subscription blocks launch; Product Instance data retained.

## Tests

| Area | Location | Result |
|---|---|---|
| Unit — pricing, payments, plan change, usage conflicts, boundaries | `Wp11PricingPaymentsPlanChangeTests.cs` | 20 passed |
| Integration — Start Business, upgrade/downgrade guards, plan sort, LV enabled | `ApiWp11CommercialIntegrationTests.cs` (+ related commercial/Start Business filters) | 10 passed |

Scenarios covered (consolidated): plan pricing persistence, display order independence, agreed-price snapshot, monthly/annual selection, LV payment outcomes, idempotency, renewal extend/past-due, trial entitlement, expired trial, upgrade/downgrade scheduling, usage conflicts before downgrade, Start Business org scope, platform account-creation UI boundary, LV Production fail-closed, cross-org denial, list sort before pagination.

## Manual validation checklist

- [ ] Sign in as Personal user → Start a Business → Business Monthly → 14-day trial
- [ ] Subscription Trialing; Entitlement Enabled; Personal Account still separate
- [ ] Explicit switch to Organization scope
- [ ] Simulate successful payment → Active; AgreedPrice PHP 699.00
- [ ] Upgrade to Pro; Entitlement revision updates
- [ ] Schedule downgrade to Starter; confirm usage conflicts shown; no Product data deleted
- [ ] Simulate renewal failure → Past Due; then successful renewal → Active
- [ ] Fake-payment controls absent when Local Validation disabled
- [ ] Production rejects LocalValidation payment provider configuration
- [ ] Platform has no Personal/Organization account creation buttons

## Migration

- `20260803090000_AddPlanPricingAndSubscriptionCommercialSnapshot`

## Files changed (summary)

- Domain/Application/Infrastructure: Plan pricing, Subscription snapshots, payment provider, plan-change use cases, Start Business commercial path
- API: Subscription upgrade/downgrade/preview endpoints, LV payment simulate/enabled
- Admin: Plans, Subscriptions, OrganizationCommercial, PersonalStartBusiness, LocalValidationTestPayments, AdminNav
- Tests + architecture docs + this report
