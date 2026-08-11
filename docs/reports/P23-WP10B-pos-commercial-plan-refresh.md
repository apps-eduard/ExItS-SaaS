# P23-WP10B — POS Commercial Plan Refresh (Starter / Growth / Pro)

| Field | Value |
|---|---|
| Status | **Implemented** (commercial catalog + capacity enforcement; WP11 not started) |
| Phase | [Phase 23](../phases/phase-23-multi-business-entitlements-and-variable-quantity-selling.md) |
| Date | 2026-08-11 |
| Device Verified | **No** |
| Production Ready | **No** |

## Status

WP10B refreshes Pinoy Business POS commercial plans to **Starter / Growth / Pro**, adds **MaxActiveBusinessTypes** capacity (plan column + mirrored quantity-limit feature grant), seeds all **16** Philippine Business Type grants on published plan versions, remaps legacy Development plans onto Growth, and blocks over-capacity activation / downgrade without deleting merchant catalog or history.

## Old plan audit

| Plan key | Role before WP10B | WP10B handling |
|---|---|---|
| `starter` | MVP Starter | Retained / refreshed commercial package |
| `business` | Mid-tier (display “Business”) | **Not** a customer-facing default; remapped → `growth`; retired when unused |
| `pro` | MVP Pro | Retained / refreshed commercial package |
| `growth` | (new) | New default mid-tier |
| `local-validation-pos` | Local Validation provisional | Remap active-like → Growth; retire when unused |
| `start-business-pos` | Start-a-Business provisional | Remap active-like → Growth; retire when unused |

No combination-specific plans (Sari-Sari Plan, Bakery Plan, etc.) were introduced.

## Exact Starter / Growth / Pro definitions

| Plan | Key | Branches | Staff | POS devices | MaxActiveBusinessTypes | Credit | Adv. reports | Export | Trial | Monthly PHP* | Annual PHP* |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Starter | `starter` | 1 | 3 | 1 | **1** | no | no | no | 14d | 299 | 2990 |
| Growth | `growth` | 3 | 10 | 3 | **3** | yes | yes | yes | 14d | 699 | 6990 |
| Pro | `pro` | 10 | 30 | 10 | **6** | yes | yes | yes | no | 1499 | 14990 |

\*DEVELOPMENT/default placeholder prices — **not** final launch prices. Editable by Platform Admin.

## Feature mapping (existing codes only)

No feature codes were invented beyond the capacity grant `plan-max-active-business-types`.

**All tiers** grant baseline store boolean features already used by MVP POS (catalog, sales, void, dashboard, reports view, permissions, inventory, expenses, suppliers, shifts, returns, registers).

**Starter** additionally: capacity limits only (credit / advanced reports / export = false).

**Growth / Pro** additionally enable:

- `customer-credit-create` / `customer-credit-view` / `customer-credit-repay`
- `store-advanced-reports`
- `store-export`

Further product capability split (purchasing, Today’s Prices, etc.) is not represented as separate subscription feature codes today; tiers differ on capacities + the three commercial toggles above. Favor simple tiers rather than fabricating codes.

## Business Type grants

Default POS plan versions grant **all 16** Philippine POS Business Types from `PhilippineBusinessTypeSeeds` (Ensure creates missing rows).

Effective types remain:

**Primary ∪ (activations ∩ PlanVersion grants)** subject to **MaxActiveBusinessTypes**.

Primary always consumes one slot and cannot be deactivated.

## MaxActiveBusinessTypes enforcement

- Capacity lives on `Plan.MaxActiveBusinessTypes` and is mirrored as quantity-limit grant `plan-max-active-business-types` on published versions (same pattern as branches/staff/devices).
- `ActivateOrganizationBusinessType`: after grant check; if not already effective and `EffectiveCount >= MaxActiveBusinessTypes` → `application.business_type_activation.capacity_exceeded`.
- Deactivation frees a slot; ungranted types still denied (`BusinessTypeNotEntitled`).
- WP03 grant/entitlement enforcement is unchanged.

## Downgrade policy

**Block** schedule/preview when effective BT count exceeds target `MaxActiveBusinessTypes`.

- Error: `application.plan_change.business_type_capacity_blocked`
- Merchant products / history / activations are **not** deleted.
- Owner must deactivate optional types until within capacity, then retry.

## Old plan cleanup / seed behavior

`EnsureMvpPosPlans` (idempotent):

1. Ensure product exists; ensure required feature definitions (including max BT).
2. Ensure Philippine BTs; create Starter/Growth/Pro (activate + publish versions with grants + 16 BT grants).
3. If published version lacks BT grants / capacity grant / grant set differs → publish **new** version and rebind active-like subscriptions on that plan.
4. Remap active-like subscriptions from legacy codes → Growth.
5. Retire unused `local-validation-pos`, `start-business-pos`, `business` when no active-like subscriptions remain (never hard-delete subscription history).

Local Validation dataset assigns **Growth** (not Business).

## Migration impact

Additive only: `20260811200000_AddPlanMaxActiveBusinessTypes`

- Column: `platform.plans.max_active_business_types` `integer NOT NULL DEFAULT 1`
- Snapshot updated; unrelated schema untouched
- No production auto-`Migrate()`

## Admin UX

`Plans.razor` create/edit/detail/list show **Max active business types** beside branches / staff / devices. No full Plans redesign.

## Tests / results

Focused unit:

- `MvpPlanCommercialPackageTests` — Starter/Growth/Pro keys + capacities + BuildGrants
- `BusinessTypeCapacityAndDowngradeTests` — activation at capacity, idempotent primary, preview/downgrade block
- `OrganizationBusinessTypeEntitlementResolverTests` — capacity + deactivation frees slot
- `Wp11PricingPaymentsPlanChangeTests` — Growth mid-tier + BT downgrade block

Focused integration:

- `MvpPlanAndSubscriptionDisplayIntegrationTests.EnsureMvpPosPlans_seeds_starter_growth_pro_idempotently` — 3 plans, capacities, 16 BT grants, max-BT grant limits
- `OrganizationBusinessTypeEntitlementEnforcementTests` — grant + capacity + deactivate frees slot
- `MigrationTests` / Ensure idempotent commercial endpoint checks updated for Growth

Evidence (this machine): unit filter batches **passed**; targeted integration filter **7 passed / 0 failed**.

## Explicit exclusions

- **WP11** onboarding / multi-BT activation UX — **not started**
- Final launch pricing
- Invented feature codes / combination-specific plans
- Device verification

## Implementation commit hash

`18de6de54b1df6412408f6d799e3a8d4efd362be`

## Files (representative)

- Domain: `Plan.cs`, `MvpPosPlanCatalog.cs`, `FeatureCode.cs`
- Application: `EnsureMvpPosPlans.cs`, `CatalogUseCases.cs`, `CatalogQueries.cs`, `OrganizationBusinessTypeActivationUseCases.cs`, `PlanChangeUseCases.cs`, `InitializeLocalValidationDataset.cs`
- Infrastructure: `PlanRecord.cs`, `CatalogEntityMapper.cs`, migration `20260811200000_AddPlanMaxActiveBusinessTypes.cs`
- API/Admin: `CatalogEndpoints.cs`, `PlatformDtos.cs`, `Plans.razor`
- Tests: `MvpPlanCommercialPackageTests.cs`, `BusinessTypeCapacityAndDowngradeTests.cs`, entitlement/MVP plan/commercial test updates
- Docs: this report; Phase 23 + phases README WP10B row
