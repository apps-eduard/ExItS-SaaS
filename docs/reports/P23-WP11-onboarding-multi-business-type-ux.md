# P23-WP11 — Onboarding Multi-Business-Type UX

| Field | Value |
|---|---|
| Status | **Implemented** (merchant onboarding + post-onboarding BT management; WP12 not started) |
| Phase | [Phase 23](../phases/phase-23-multi-business-entitlements-and-variable-quantity-selling.md) |
| Date | 2026-08-12 |
| Device Verified | **No** |
| Production Ready | **No** |

## Status

WP11 wires merchant onboarding and Organization settings to the Phase 23 commercial model (WP10B Starter/Growth/Pro + MaxActiveBusinessTypes + WP03 activation APIs). No schema migration.

## Final onboarding flow

1. Personal → Explore POS (plan cards from commercial API)
2. Start Business → **Primary Business Type** cards (display names) → business details → selected plan summary
3. Confirm creates organization + subscription (existing transactional Start Business use case)
4. Growth/Pro: optional **Add business types** step (`/onboarding/business-types`) before device registration
5. Starter (MaxActiveBusinessTypes = 1): skip additional BT step
6. Main Branch (created with Start Business) → POS device registration → POS PIN
7. Optional starter template (`/catalog/import?onboarding=1`, WP04-filtered) or Skip
8. POS ready

## Primary Business Type behavior

- Required; exactly one selection; display names from Platform Active types
- Loaded via `GET /api/v1/personal/onboarding/business-types` (Personal scope; not org-entitlement filtered)
- **General Retail / Other is not auto-selected**
- Primary counts toward MaxActiveBusinessTypes and cannot be deactivated

## Starter / Growth / Pro presentation

Explore POS and Start Business show API-driven limits:

- price (admin-configured; not hard-coded)
- branches, staff, POS devices
- **MaxActiveBusinessTypes**
- credit / advanced reports / export when enabled

Merchant `CommercialPlanDto` now includes `MaxActiveBusinessTypes`.

## Additional Business Type UX

- After Start Business when plan capacity &gt; 1
- Shows granted options with Primary locked
- Capacity copy: “{n} of {max} business types active” / “You can add up to {remaining} more”
- Activate / deactivate via existing WP03 endpoints; server remains authoritative
- Skip allowed

Post-onboarding: `/org/business-types` (Owner) from Org Summary / Subscription.

## MaxActiveBusinessTypes behavior

Unchanged from WP10B enforcement:

- Primary occupies one slot
- Activation denied at capacity / ungranted
- Deactivation of optional types frees capacity

Entitlement DTO enriched for UX: `MaxActiveBusinessTypes`, `EffectiveCount`, `RemainingCapacity`, `BusinessTypes[]` (id/code/name/flags).

## Template selection behavior

Unchanged WP04: published templates whose PrimaryBusinessTypeId ∈ effective types. Single-template onboarding import retained; Skip remains. No multi-template import subsystem.

## Online / offline behavior

BT activation, plan selection, and template onboarding remain online-required. POS sales offline unchanged.

## Authorization

- Activate/deactivate: `EnsureCanManageOrganizationCommercialAsync` (Owner / Platform ManageSubscriptions)
- Cashier cannot manage; UI Owner gate + server enforcement
- Cross-org denied by selected-organization commercial authz

## Downgrade behavior

WP10B block retained. Merchant-facing messaging improved:

`Deactivate N optional business type(s) before switching to {Plan}. Merchant catalog and history are not deleted.`

Surfaced in plan-change preview conflicts and Org Subscription / Business Types hints. No silent deactivation / no product deletion.

## Tests / results

| Suite | Result |
|---|---|
| Unit `BusinessTypeCapacityAndDowngradeTests` (+ UX capacity/primary tests) | **passed** (included in filter batch) |
| Unit `OrganizationBusinessTypeEntitlementResolverTests` | **passed** |
| Unit `Wp11PricingPaymentsPlanChangeTests` + `MvpPlanCommercialPackageTests` | **48 passed** |
| MAUI `Wp11OnboardingMultiBusinessTypeUxTests` + `PersonalPageGuardTests` | **18 passed** |
| Admin / Platform Api Release build | **succeeded** |

## Migration impact

**None.** Uses WP10B schema.

## Known / deferred

- Merchant in-app plan change UI still Admin-centric; MAUI shows capacity + downgrade hint only
- Multi-template import in one onboarding pass deferred
- **WP12 not started**
- Device Verified = No
- Production Ready = No

## Implementation commit hash

`fdc5a9868f71c558b0b04b2ee627fc2fce3dba02`

## Files (representative)

- Platform: `OrganizationBusinessTypeActivationUseCases.cs`, `PersonalEndpoints.cs`, `PlanChangeUseCases.cs`
- POS: `PlatformAccessModels.cs`, `PlatformAccessClient.cs`, `StartBusiness.razor`, `OnboardingActivateBusinessTypes.razor`, `OrgBusinessTypes.razor`, `NavigationGate.cs`
- Admin: `PersonalStartBusiness.razor`, `PlatformDtos.StartBusinessRequest`
- Docs: this report; Phase 23 WP11 Done
