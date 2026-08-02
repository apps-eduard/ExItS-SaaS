# P16-WP09 — Product Access and Navigation Integration

| Field | Value |
|---|---|
| Status | **Complete** |
| Starting commit | `2b8ee718454c3ba33a6755b796696342defec2a4` (after P16-WP08 tip-hash) |
| Feature commit | `9ae47bc635eb30b357c6f8317c9025ad850e054e` |
| Date | 2026-08-02 |

## Scope completed

- Enabled-product discovery for Organization sessions (`GET /api/v1/organizations/{orgId}/enabled-products`).
- Product launch authorization (`POST .../products/{productCode}/launch`) and Admin Launch Product navigation to enabled-products (no operational POS nav in Organization Administration).
- Product-local role assignment/revoke APIs and Admin UI; WP08 provisional `product_local_role_grants` become lifecycle-aware (Active/Revoked) with Owner/Manager/Cashier/Viewer catalog.
- Entitlement versus role separation: commercial evaluate remains entitlement/assignment; `EvaluateProductAuthorization` requires entitlement + product-local role for `CanOperate` / product entry / launch.
- POS boundary: Personal sessions denied org product routes; Platform mapped role codes sync into POS DB when bearer introspection carries `MappedPosRoleCode` and no POS assignment exists (Owner→Owner, Manager→StoreManager, Cashier→Cashier, Viewer→ReportingUser).
- Migration `AddProductLocalRoleGrantLifecycle`.
- Unit + integration coverage for discovery, launch denial, entitlement vs role.

## Files changed (high level)

- Domain: `ProductLocalRoleGrant` lifecycle + `ProductLocalRoleCodes` POS mapping
- Application: `DiscoverEnabledProducts`, `EvaluateProductAuthorization`, `AssignProductLocalRole`, `RevokeProductLocalRole`; access-token issue/introspect require operable authorization
- Infrastructure: grant repository expansion; migration `AddProductLocalRoleGrantLifecycle`
- API: `ProductNavigationEndpoints`; membership helper `EnsureActiveOrganizationMemberAsync`
- Admin: `OrganizationEnabledProducts.razor`, Launch Product nav, API client DTOs/methods
- POS: bearer introspect role fields; `PosRoleResolutionMiddleware` Platform→POS sync
- Tests: `ProductAuthorizationAndDiscoveryTests`, `ApiProductAccessNavigationTests`

## Schema and migration changes

Migration `AddProductLocalRoleGrantLifecycle`:

| Change | Purpose |
|---|---|
| `status`, `revoked_at_utc`, `revoked_by_user_identity_id`, `reason` | Lifecycle for product-local role grants |
| Drop `ux_product_local_role_grants_org_user_product_role` | Replaced by active-only uniqueness |
| Add `ux_product_local_role_grants_active_org_user_product` (filtered `status = 'Active'`) | One active role per org/user/product |

WP03–WP08 tables remain intact. No Phase 14 changes. No POS schema migration (Platform→POS sync is runtime).

## API / UI summary

| Method | Route | Notes |
|---|---|---|
| GET | `/api/v1/organizations/{orgId}/enabled-products` | Org member discovery |
| GET | `/api/v1/organizations/{orgId}/product-authorization` | Entitlement vs role flags |
| GET/POST | `/api/v1/organizations/{orgId}/product-local-roles` | List / assign |
| POST | `/api/v1/organizations/{orgId}/product-local-roles/{grantId}/revoke` | Revoke role |
| POST | `/api/v1/organizations/{orgId}/products/{productCode}/launch` | Launch gate → product-entry |

Admin UI: Organization shell **Launch Product** → `/admin/organizations/{id}/enabled-products` (discovery, launch, role assignment). Commercial product-access page unchanged (entry eligibility only).

## Exit criteria

| Criterion | Evidence |
|---|---|
| Subscribed product appears for the Organization | Discovery lists POS after Start a Business |
| Unauthorized staff cannot operate | Authorization `canOperate=false` without role; launch 403 |
| Removing entitlement disables Organization access | Commercial deny → `CanOperate` false (existing evaluate) |
| Removing product role disables individual access | Revoke role → launch 403 / `product_local_role_missing` |
| Platform role grants no POS operation | Product entry requires product-local role, not Platform RBAC |
| Regression suite passes | Unit 342 / Integration 174 |

## Audit coverage

- `platform.enabled_products.discovered`
- `platform.product_authorization.checked`
- `platform.product.launched`
- `platform.product_local_role.granted` / `platform.product_local_role.revoked`

## Tests added

- `ProductAuthorizationAndDiscoveryTests` — role mapping, entitlement vs role, discovery launch gate, revoke
- `ApiProductAccessNavigationTests` — discovery/launch, role separation, Personal scope denial, revoke blocks launch

## Build / test evidence

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Testing"
dotnet build src/Platform/ExItS.Platform.Api/ExItS.Platform.Api.csproj -c Release
dotnet test tests/ExItS.Platform.UnitTests/ExItS.Platform.UnitTests.csproj -c Release
dotnet test tests/ExItS.Platform.IntegrationTests/ExItS.Platform.IntegrationTests.csproj -c Release
```

- Platform unit: **342 passed**, 0 failed, 0 skipped
- Platform integration: **174 passed**, 0 failed, 0 skipped
- Admin unit: **67 passed**, 0 failed, 0 skipped
- Build: Platform API Release — 0 warnings, 0 errors

## Explicit exclusions

- Full POS operational shell navigation inside Admin (by design — product-owned)
- Continuous Platform↔POS role bi-directional sync after first POS assignment (POS DB authoritative once present)
- Phase 14 production closeout
- WP10 security/privacy hardening and Phase 16 closeout
- WP03–WP08 feature SHAs unchanged

## Prior feature SHAs preserved

| WP | Feature SHA |
|---|---|
| WP03 | `3454a7e6caa0d307d03a03d91abe7250ccad96a1` |
| WP04 | `17f53e204243844b86602eaf12369495ffd8db01` |
| WP05 | `4b7b4d5c223bf4e293248881df14c970e76e80d1` |
| WP06 | `6f85bd3fb324a93fc8eadf2f82426be0178b064e` |
| WP07 | `ae39e9f7084f44c6c5a9a5e598767fc91987feae` |
| WP08 | `cb3f3585e07e6b0865df1a40175b9f5b99a22a78` |

## Explicit next work package

**P16-WP10** — Security, Privacy, UX Hardening, and Closeout.

## Production blockers

Unchanged. Phase 14 not modified. App remains **not production-ready**.
