# P23-WP03 — Business Type entitlement enforcement

| Field | Value |
|---|---|
| Status | **Implemented** (resolver + server enforcement; WP04 UX not claimed) |
| Phase | [Phase 23](../phases/phase-23-multi-business-entitlements-and-variable-quantity-selling.md) |
| Date | 2026-08-11 |

## Resolver architecture

Authoritative Application service:

- `IOrganizationBusinessTypeEntitlementResolver` / `OrganizationBusinessTypeEntitlementResolver`
- Merchant gate: `MerchantCatalogEntitlementGate` (Platform Admin unrestricted via `ViewGlobalCatalog`)
- Activation use cases: `ActivateOrganizationBusinessType`, `DeactivateOrganizationBusinessType`, `GetOrganizationBusinessTypeEntitlement`

Subscription authority reused:

`ISubscriptionRepository.GetCurrentForOrganizationProductAsync` → require `Subscription.IsActiveLike` → `IPlanRepository.GetVersionByIdAsync` → `PlanVersion.BusinessTypeGrants`.

Default product: `pinoy-business-pos`.

## Exact effective-BT algorithm

1. Load organization (fail if missing).
2. Load current active-like subscription for product (fail closed if missing/inactive-like).
3. Load bound PlanVersion (fail if missing).
4. `granted = PlanVersion.BusinessTypeGrants`.
5. Load activation rows; `entitledActivations = activations ∩ granted` (stale rows kept in DB, excluded from effective).
6. Candidate set = `{ PrimaryBusinessTypeId? } ∪ entitledActivations`.
7. Load BusinessType entities:
   - **Primary** always included when present (legacy continuity), even if Inactive.
   - **Additional** activations included only when BusinessType status is **Active**.
8. Effective set = resulting list (codes included for API DTO).

## Activation rules

- Cannot activate duplicate / primary (domain + repository).
- Cannot activate type not in current plan-version grants → `BusinessTypeNotEntitled`.
- Cannot newly activate Inactive/Archived → `BusinessTypeInactive`.
- Auth on API: `EnsureCanManageOrganizationCommercialAsync` (Owner / Platform ManageSubscriptions).
- Deactivate removes activation row only (does not touch merchant catalog).

## Downgrade behavior

- Stale activation rows are **retained** (auditability).
- Resolver excludes activations outside current grants → discovery/import blocked for removed types.
- No automatic merchant product/history deletion.

## Fail-closed behavior

- Missing/inactive subscription → resolution failure; merchant discovery does not expand to all Platform catalog.
- Missing organization context on merchant routes → `OrganizationContextNotEligible`.
- Unentitled client BT filter → `BusinessTypeNotEntitled` (403).
- Empty effective set → empty discovery results (not unrestricted).
- Platform Admin with `ViewGlobalCatalog` remains unrestricted.

## API / parameter mismatch fixed

| Old (no-op) | Correct |
|---|---|
| `businessType` | `businessTypeCode` |
| `primaryBusinessTypeId` (templates) | `businessTypeId` |

Fixed clients:

- `MerchantCatalogDiscoveryClient`
- `PlatformMerchantCatalogClient`

Server still accepts legacy aliases on merchant list endpoints but **intersects** them with entitlement (cannot widen).

## Endpoints / use cases protected

| Surface | Enforcement |
|---|---|
| `GET /api/v1/catalog/business-types` | Effective BT list only |
| `GET /api/v1/catalog/templates` | Entitled filter / allowed set |
| `GET /api/v1/catalog/templates/{id}` (+ products) | Template primary ∈ effective |
| `GET /api/v1/catalog/products/search` | Entitled filter / allowed set |
| `GET /api/v1/catalog/products/{id}` | Product BT ∩ effective |
| `GET /api/v1/catalog/categories` | Entitled filter / allowed set |
| `GET .../organizations/{id}/business-type-entitlements` | View org |
| `POST/DELETE .../business-type-activations` | Manage commercial |

## Tests executed

| Suite | Result |
|---|---|
| Unit `OrganizationBusinessTypeEntitlementResolverTests` | **9 passed** |
| Unit WP02 grant tests (regression) | **5 passed** (with prior filter) |
| Integration `OrganizationBusinessTypeEntitlementEnforcementTests` | **1 passed** |
| Integration `PlanBusinessTypeEntitlementPersistenceTests` | **3 passed** (prior run green) |
| Maui `MerchantCatalogDiscoveryClientParameterTests` | **1 passed** |

## Known gaps / deferred (WP04+)

- Richer merchant catalog/template UI for multi-BT selection.
- Admin plan UI to edit BT grants / MVP plan seed packs.
- Entitlement snapshot inclusion of effective BT codes.
- POS import use-case double-check beyond Platform discovery gate (Platform get-by-id already entitlement-checked).
- HTTP WebApplicationFactory coverage of forged Pharmacy query (covered at gate/unit + activation integration).

## Files changed (high level)

- Application: resolver, gate, activation use cases, error codes, list filter params
- Infrastructure: multi-BT allowed filters on product/category/template repositories
- API: merchant discovery enforcement; org activation endpoints; DI
- POS clients: query param contract fix
- Tests + Phase 23 / this report

## Commit hash

_Recorded after implementation commit._
