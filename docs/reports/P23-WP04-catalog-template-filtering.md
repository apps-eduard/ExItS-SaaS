# P23-WP04 — Catalog / Template entitlement filtering

| Field | Value |
|---|---|
| Status | **Implemented** (merchant discovery + import semantics; WP05+ not claimed) |
| Phase | [Phase 23](../phases/phase-23-multi-business-entitlements-and-variable-quantity-selling.md) |
| Date | 2026-08-11 |
| Device Verified | **No** |
| Production Ready | **No** |

## Status

WP04 completes Business-Type-entitled Global Catalog and Template discovery/import behavior on top of the authoritative WP03 resolver/gate (`IOrganizationBusinessTypeEntitlementResolver` / `MerchantCatalogEntitlementGate`). No second entitlement system was introduced.

## Visibility semantics

Merchant-visible Platform resources = resources applicable to **at least one effective Business Type**, where effective types come **only** from the WP03 resolver.

| Resource | Rule |
|---|---|
| Global product | `BusinessTypeIds ∩ effective ≠ ∅` |
| Global category | `BusinessTypeIds ∩ effective ≠ ∅` (untagged categories are not exposed under an entitlement filter) |
| Catalog template | `PrimaryBusinessTypeId ∈ effective` |
| Template product links | Template must be entitled **and** each linked Active global product must intersect effective |

Examples (effective = SariSari + MiniGrocery stand-in for multi-BT):

- Product tagged SariSari → visible
- Product tagged MiniGrocery → visible
- Product tagged SariSari + Bakery → visible (intersection)
- Product tagged Pharmacy only → hidden
- Multi-tagged product returned **once** (DB `.Any()` filter; count before skip/take)

### No-filter semantics

Omitted merchant BT query ⇒ results constrained to the **full effective set**, never unrestricted Platform catalog.

Explicit entitled BT query ⇒ narrows within effective.

Explicit unentitled BT query ⇒ `BusinessTypeNotEntitled` (403) via WP03 gate.

### Direct-ID enforcement

- `GET /api/v1/catalog/templates/{id}` and `/products` — template primary must be entitled; product links filtered per product BT.
- `GET /api/v1/catalog/products/{id}` — product BT ∩ effective (WP03).

Platform Admin with `ViewGlobalCatalog` remains unrestricted on merchant routes.

## Import enforcement

| Path | Boundary |
|---|---|
| Template import (`ImportTemplateBatch`) | `GetPublishedTemplate` (403/404 → not found) + **re-check every** `GlobalProductId` via entitled `GetActiveProduct(s)` before queueing |
| Selected products (`ImportSelectedProducts`) | Live product GET only; unentitled/forged IDs omitted; all-denied → no job |
| Platform client | Maps HTTP **403 Forbidden** to null (same as 404) so forge attempts do not throw as “platform unavailable” |

Downgrade: existing merchant products remain; new discovery/import of removed BT is denied at Platform GET time.

Provenance unchanged: `PlatformTemplateId` → GlobalTemplate; `PlatformGlobalProductId` → GlobalCatalog; otherwise MerchantCreated. No auto-push of later global/template edits into imported merchant rows.

## Duplicate / pagination

- Product/category list filters use EF `.Any(...)` (no join fan-out duplicates).
- `TotalCount` is computed on the filtered query **before** `Skip`/`Take`.
- Template merchant product filter orders by `SortOrder`, then `GlobalProductId`.

## Canonical API parameters

| Canonical | Compatibility-only alias |
|---|---|
| `businessTypeCode` | `businessType` |
| `businessTypeId` | `primaryBusinessTypeId` (templates) |

Canonical clients (`MerchantCatalogDiscoveryClient`, `PlatformMerchantCatalogClient`) send only canonical names. Server aliases remain entitlement-intersected and cannot widen access.

Merchant `GET /api/v1/catalog/business-types` returns effective types only, with `isPrimary` when the org primary is present (minimal DTO additive field for WP11).

## Backward compatibility

- Single-primary orgs unchanged (effective = `{Primary}`).
- Existing imported merchant products retained.
- No schema migration for WP04.
- No rewrite/delete of templates or global products.

## Migration / schema impact

**None.** WP04 is application/API/client/test/docs only.

## Tests + results

| Suite | Result |
|---|---|
| Integration `MerchantCatalogEntitlementFilteringTests` | **4 passed** |
| Unit `ImportTemplateBatchTests` (+ entitlement re-check) | **6 passed** (suite filter) |
| Unit `ImportSelectedProductsEntitlementTests` | **2 passed** |
| Maui `MerchantCatalogDiscoveryClientParameterTests` | **1 passed** |
| Unit `OrganizationBusinessTypeEntitlementResolverTests` (regression) | **9 passed** |

## Files changed (high level)

- Platform Application: template product entitlement filter; merchant BT `IsPrimary`
- Platform API: merchant discovery template product filtering; alias docs
- POS Application: `ImportTemplateBatch` live entitlement re-check; canonical client param names
- POS Api / ApiClient: Forbidden→null; `businessTypeCode` / `businessTypeId` contracts
- Tests + Phase 23 / this report

## Known gaps deferred to WP05+

- SellingMode / weighted UX / Today’s Prices
- Onboarding multi-BT activation UX (WP11)
- Offline price-snapshot fidelity (WP08)
- Admin plan UI for BT grant packs / VegetableVendor seed packs
- Full HTTP WebApplicationFactory forge matrix (covered via gate + query filters + POS import unit tests)

## Implementation commit hash

`e4bcac5345925d8992f77d98baa29e4e230cc0a9`