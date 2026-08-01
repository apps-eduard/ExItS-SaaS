# P15-WP04 — Product Catalog and Plan CRUD (completion)

[Phase 15](../phases/phase-15-ant-design-platform-admin.md) | [Portfolio](../portfolio-progress.md) | [ADR-015](../decisions/ADR-015-antdesign-blazor-platform-admin.md)

## Status

**Complete.** Starting tip `444f19610a3790731c29287d918292485daecb09`. Final tip `17e7d3dee411c8c7929b9918e3051473cc4f0c8b` (feature `d0e2ad3bd211607b59e16278fbb94e4fc73589f3`). P15-WP05 not started.

## Field / lifecycle decisions (audit-first)

Reused existing P3 catalog domain; **no new pricing/feature engine**.

| Aggregate | Statuses (unchanged) | Fields reused |
|---|---|---|
| Product | Active / Inactive / Retired (no Draft) | Code, DisplayName, Status, timestamps |
| Plan | Draft / Active / Retired (**no Inactive**) | ProductCode, Code, DisplayName, Status, timestamps |
| PlanVersion | Draft / Published / Retired | BillingPeriod, TrialEligible, grants (read/manage via existing APIs; UI surfaces versions read-only) |

**Not added:** description, icon/logo, launch URL, list price/currency, display order (not in current model; deferred).

**New permission:** `platform.permission.manage_catalog` — PlatformAdministrator only (Billing/Support retain ViewPortfolio reads only).

## Routes

| Route | Capability |
|---|---|
| `/admin/products` | List/create/filter (ManageCatalog for mutate) |
| `/admin/products/{id}` | Detail tabs + rename/lifecycle |
| `/admin/plans` | Global plan list/create |
| `/admin/plans/{id}` | Detail + rename/activate/retire + versions |

## Endpoints (authz now enforced)

| Method | Path | Authz |
|---|---|---|
| GET | `/api/v1/platform/catalog/products` | ViewPortfolio (+ search/sort) |
| POST/PATCH/POST lifecycle | `/api/v1/platform/catalog/products…` | ManageCatalog |
| GET | `/api/v1/platform/catalog/plans` | ViewPortfolio (new global list) |
| GET | `/api/v1/platform/catalog/plans/{planId}` | ViewPortfolio |
| Nested plan mutations | `/products/{code}/plans…` | ManageCatalog |
| Features/trials/versions | existing nested routes | ViewPortfolio read / ManageCatalog mutate |

## Platform Admin capabilities

- Product CRUD metadata (create, rename) + activate/deactivate/retire
- Plan create under product, rename, activate, retire
- Server-side paging/search/filter/sort for products and plans
- View features, versions, trials on product detail; versions on plan detail
- Audit actions for catalog create/update/lifecycle

## Organization Admin

- No catalog list/create/lifecycle (403 without ViewPortfolio/ManageCatalog)
- Licensed product visibility remains via product-access surfaces (WP02), not platform catalog CRUD
- Cannot escalate to Platform Admin via membership

## Lifecycle / safety

- No hard DELETE endpoints; subscription FKs Restrict
- Inactive/retired product cannot start new trials (existing product Active check)
- Non-active plan cannot start new trials (`SubscriptionIneligible`) — added in this WP
- Retired records remain historical; rename blocked when retired
- Concurrency on rename via `expectedUpdatedAtUtc` → 409
- SaaS catalog ≠ POS operational catalog; no product-local roles

## Tests

- Unit: role catalog ManageCatalog denial; retired-plan trial denial
- Integration: product/plan search/lifecycle/concurrency/audit; Org Admin mutate/list denial; existing CatalogApiTests
- Admin architecture/display guards updated for Ant Products/Plans
- Full Release suite: **1288 passed / 0 failed / 0 skipped** (`ASPNETCORE_ENVIRONMENT=Testing`, `dotnet test ExItS.slnx -c Release`)
- Hardening: Staging Live Preview HTTP-auth test no longer requires a live Platform on `:8091`

## Residual gaps

- Plan version draft create/publish UI deferred (API remains)
- Feature/trial create UI deferred (API remains)
- Product description/icon/launch URL / plan list price not in domain
- Org Admin authorized read of licensed catalog summary still via product-access, not a dedicated catalog read surface
