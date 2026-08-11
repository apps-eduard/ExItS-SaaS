# P23-WP02 — Business Type subscription entitlement model

| Field | Value |
|---|---|
| Status | **Implemented** (domain + persistence only) |
| Phase | [Phase 23](../phases/phase-23-multi-business-entitlements-and-variable-quantity-selling.md) |
| Date | 2026-08-11 |

## Current architecture confirmed

- Commercial feature grants hang off **`PlanVersion`** (`platform.plan_version_feature_grants`), not `Plan`.
- Organizations keep immutable **`PrimaryBusinessTypeId`** on `platform.organizations`.
- Subscriptions bind an org to a **plan version**; no org→subscription column.
- Business Types live in **`catalog.business_types`** (dynamic, soft lifecycle).
- No prior plan↔BT grant or org add-on activation tables (WP01 gap).

## Exact entities / tables added

### Domain

| Type | Role |
|---|---|
| `PlanVersion.BusinessTypeGrants` | `IReadOnlyList<BusinessTypeId>` on version; draft replace via `ReplaceDraftBusinessTypeGrants` |
| `OrganizationBusinessTypeActivation` | Org add-on activation row (not primary) |

### Persistence

| Table | Schema | PK | FKs / indexes |
|---|---|---|---|
| `plan_version_business_type_grants` | `platform` | `(plan_version_id, business_type_id)` | Cascade ← `plan_versions`; Restrict → `catalog.business_types`; ix on `business_type_id` |
| `organization_business_type_activations` | `platform` | `(organization_id, business_type_id)` | Cascade ← `organizations`; Restrict → `catalog.business_types`; ix on `business_type_id`; column `activated_at_utc` |

Records: `PlanVersionBusinessTypeGrantRecord`, `OrganizationBusinessTypeActivationRecord`.  
Repository: `IOrganizationBusinessTypeActivationRepository` / `OrganizationBusinessTypeActivationRepository`.  
Plan versions load/save BT grants through existing `IPlanRepository` + `CatalogEntityMapper`.

## Effective entitlement semantics (reserved for WP03)

Conceptual (not implemented as resolver/API enforcement here):

`EffectiveBusinessTypes = { PrimaryBusinessTypeId } ∪ (activations ⊆ plan-version BusinessTypeGrants)`

WP02 stores grants and activations only. Server filtering / grant authorization at activate time is **WP03**.

## Domain invariants enforced in WP02

- Duplicate plan-version BT grant → `DuplicateBusinessTypeGrant`
- Duplicate org activation (domain set + repository) → `DuplicateBusinessTypeActivation`
- Activating the org **primary** BT via activation model → `PrimaryBusinessTypeActivationForbidden`
- Published/retired plan versions cannot change BT grants (same immutability as feature grants)
- FK existence of BusinessType / Organization enforced by database
- **Deferred to WP03/application:** activate only if current subscription grants the BT; reject Inactive/Archived BT on new grant/activate

## Migration

- Name: `20260811152507_AddPlanBusinessTypeGrantsAndOrgActivations`
- Additive only: creates the two tables above
- Preserves organizations, `primary_business_type_id`, plans/versions/feature grants, catalog/templates
- No data backfill required (empty grants/activations = pre-WP02 single-primary behavior)
- No production `Migrate()` at startup

## Backward compatibility

- Existing orgs with only `PrimaryBusinessTypeId` behave as before (no activation rows).
- Existing plan versions get empty `BusinessTypeGrants` collections.
- `PlanVersion.CreateDraft` optional `businessTypeGrants` defaults to empty — call sites unchanged.
- Downgrade/removal of grants/activations does **not** delete merchant products or history (no cascade from BT master; no such deletion logic added).

## Tests executed

| Suite | Filter / scope | Result |
|---|---|---|
| Unit | `PlanVersionBusinessTypeGrantTests` + `OrganizationBusinessTypeActivationTests` + `PlanAndTrialTests` | **15 passed** |
| Unit | `CommercialUseCaseTests` + `SubscriptionAndEntitlementTests` + `MvpPlanCommercialPackageTests` | **39 passed** |
| Integration | `PlanBusinessTypeEntitlementPersistenceTests` + `MigrationTests` | **4 passed** |
| Integration | `EntitlementPersistenceTests` + `CatalogPersistenceTests` | **21 passed** |

Covered: one/many plan BT grants; duplicate grant rejection (domain + DB unique); org activation persist; duplicate activation rejection; primary unchanged; new tables present after migrate.

## Known gaps / deferred

- Effective BT resolver + merchant API enforcement (**WP03**)
- Catalog/template filtering (**WP04**)
- Admin plan UI/DTOs to edit BT grants
- `EnsureMvpPosPlans` seeding of default BT packs
- Application-layer Active BT validation on grant/activate
- Subscription-grant check when activating add-ons
- Entitlement snapshot inclusion of effective BT codes

## Files changed

- Domain: `PlanVersion.cs`, `OrganizationBusinessTypeActivation.cs`, `DomainErrorCodes.cs`
- Application: `IOrganizationBusinessTypeActivationRepository.cs`
- Infrastructure: records, `PlatformDbContext`, `CatalogEntityMapper`, `PlanRepository`, activation repository, `DependencyInjection`, migration + snapshot
- Tests: `PlanVersionBusinessTypeGrantTests.cs`, `PlanBusinessTypeEntitlementPersistenceTests.cs`, `MigrationTests.cs`
- Docs: this report; Phase 23 WP02 status

## Commit hash

_Recorded after implementation commit._
