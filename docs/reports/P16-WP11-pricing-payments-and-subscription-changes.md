# P16-WP11 — Pricing, Payments, and Subscription Changes

> **Status:** In Progress (validation underway)  
> **Phase:** 16 — Implementation Complete, Under Validation  
> **Next:** P16-WP12 — Not Started  
> **Commit SHA:** pending

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
- `LocalValidation:RunHostedSeed=false` allows integration tests to enable LV payments without hosted dataset pollution.

## Start a Business

- Personal Admin page `/admin/personal/start-business` collects org details, plan, billing cycle, trial/pay-now.
- Calls `POST /api/v1/personal/start-business` with `PlanKey`, `BillingCycle`, `StartAsTrial`, `PayNow`.
- **14-day trial fix:** `ResolveMvpPlanCatalogAsync` prefers plan-bound trial, then duration-matched trial (`DefaultTrialDays`), else creates `{PlanDisplayName} Trial` with plan grants.
- **Idempotency:** same slug + existing Active Owner membership resumes and returns existing org/subscription snapshot (no second org).
- Invalid/inactive `PlanKey` returns `PlanNotFound`.
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

### Downgrade usage conflicts

- `IOrganizationProductUsageReader` + composite: real Active staff from memberships; branch count **unavailable** across product boundary unless `activeBranchCount` override (Local Validation only).
- Preview DTO includes `ActiveStaffCount`, `ActiveBranchCount`, `BranchCountAvailable`, `BranchCountUnavailableReason`, usage conflicts, lost features.
- When branch count unavailable and no override: **no fabricated branch conflict**; unavailable reason shown in confirm text.
- Confirm text states existing data is not deleted and new over-limit creation is blocked after downgrade.
- Unresolved dependency: Platform does not yet receive active branch counts from Pinoy Business POS across the product boundary.

## Entitlement transitions

- Trial/paid activation and plan upgrades refresh entitlement snapshots.
- Downgrade schedules pending plan; data retained until effective apply.
- Expired trial / suspended subscription blocks launch; Product Instance data retained.
- Trial conversion: LV Succeeded → Active + period + agreed price snapshot + entitlement revision; Declined leaves Trialing; duplicate payment event idempotent.

## Local Validation reset (acceptance)

Command (operator must opt in; **not auto-run**):

```powershell
.\tools\Reset-LocalValidation.ps1 -ConfirmReset
```

- Requires `-ConfirmReset`, non-Production environment, Local Validation enabled in `.env.local-validation`.
- Wipes only `exits_local_validation_platform_db_data` and `exits_local_validation_pos_db_data`.
- Starts with `LocalValidation__SeedScope=PlatformAdministratorsOnly` (Olivia Mendoza + Rafael Torres only).
- Reference catalog/plans/features/roles recreated by migrate+seed; POS DB reset via product volume wipe + product migrate on start.
- Production guards unchanged (`LocalValidation:Enabled` forbidden; `Payments:Provider=LocalValidation` forbidden).

## Tests (Release evidence)

| Area | Location | Result |
|---|---|---|
| Unit — pricing, payments, plan change, branch-unavailable preview, Admin UI guards | `Wp11PricingPaymentsPlanChangeTests.cs` + `LocalValidationIdentityCatalogTests.cs` | **25 passed** |
| Integration — Start Business commercial + trial conversion + existing WP11 commercial | `ApiWp11*` | **9 passed** |
| Admin unit — WP11 UI source guards | `FullyQualifiedName~Wp11` | **25 passed** |

**Browser framework:** none present (no Playwright). UI validation covered by Admin Razor source guards + API integration tests. Interactive Admin acceptance remains manual.

## Manual acceptance checklist (remaining)

- [ ] After LV reset: only Olivia + Rafael present
- [ ] Register Personal user → Start a Business → Business Monthly → 14-day trial
- [ ] Subscription Trialing; Entitlement Enabled; Personal Account still separate
- [ ] Explicit switch to Organization scope
- [ ] Simulate successful payment → Active; AgreedPrice PHP 699.00
- [ ] Upgrade to Pro; Entitlement revision updates
- [ ] Schedule downgrade to Starter; confirm usage conflicts / branch-unavailable reason; no Product data deleted
- [ ] Simulate renewal failure → Past Due; then successful renewal → Active
- [ ] Fake-payment controls absent when Local Validation disabled
- [ ] Production rejects LocalValidation payment provider configuration
- [ ] Platform has no Personal/Organization account creation buttons

## Migration

- `20260803090000_AddPlanPricingAndSubscriptionCommercialSnapshot`

## Production guards

- `LocalValidation:Enabled=true` forbidden in Production (API startup)
- `Payments:Provider=LocalValidation` forbidden in Production (payment provider DI)
- Reset script refuses Production environment and Production-looking connection strings
- LV Test Payments UI/API gated to Local Validation only

## Status

```text
Phase 16 — Implementation Complete, Under Validation
P16-WP11 — In Progress
P16-WP12 — Not Started
```

Final acceptance is **not** claimed.
