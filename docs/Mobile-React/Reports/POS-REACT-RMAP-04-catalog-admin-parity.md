# RMAP-04 — Catalog admin parity

## Status

**COMPLETE**

## Baseline

starting SHA: `e58a259e201df1d4a027116b820eb4f07c7f9d82` (post RMAP-03 docs)

## Contract review

| Area | Finding |
|------|---------|
| Backend | `CatalogEndpoints` — categories/products CRUD, deactivate/reactivate, image PUT multipart `file`, concurrency via `expectedUpdatedAtUtc` |
| Capability | `UtangCapability.ManageCatalog` / `store-catalog-manage` — PosRoleMatrix Owner/Admin/StoreManager (Cashier ViewCatalog only) |
| SKU/barcode | Org-scoped uniqueness; server rejects duplicates |
| Create defaults | API still requires `unitOfMeasure` + `sellingPrice`; React sends `Piece` + `0` (editors deferred to RMAP-05/06) |
| Images | Existing PUT/DELETE/GET image contract supported |
| MAUI | `/catalog*` editors — behavior reference only |
| React prior | Read-only catalog client for sell floor |
| Contradictions | None material |
| Owner decision | NO |

## Implementation

- `canManageCatalog` UI gate mirrors PosRoleMatrix (Owner/Admin/StoreManager); OrganizationAdministrator alone denied
- Routes `/catalog`, `/catalog/categories`, `/catalog/products/new`, `/catalog/products/:id/edit` behind `RequireManageCatalog` + workspace bind
- Product/category list + forms with search, loading/empty/error, concurrency 409 feedback, image upload on edit
- Deep-link safe: AutoSelect no longer redirects unbound `/catalog` to `/` before bind completes
- TanStack Query keys scoped by organization/branch

## Exclusions

- UOM / SellingMode / product units (RMAP-05)
- Today’s Prices (RMAP-06)
- Inventory (RMAP-07)
- Advanced catalog import jobs

## Tests

- Vitest: `canManageCatalog` Owner/Manager/Cashier/OrgAdmin; catalog client create defaults + 409
- Playwright: manager CRUD path; cashier denied; owner concurrency conflict; viewports 375/768/1024/1440
- Regression: RMAP-02R / RMAP-03 / sell-floor catalog cart

## Next

RMAP-05 — Base UOM + SellingMode + product units
