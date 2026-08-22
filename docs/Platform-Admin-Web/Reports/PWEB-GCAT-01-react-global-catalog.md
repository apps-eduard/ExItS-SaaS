# PWEB-GCAT-01 — React Global Catalog (Categories + Products)

**Status:** COMPLETE

## Delivered capability

React Platform Admin routes under `/admin/global-catalog/*` for global categories and global products backed by `/api/v1/platform/global-catalog`.

- Category list/detail/create/edit with server-side paging, filtering, sorting, parent hierarchy, and business-type scope
- Product list/detail/create/edit with server-side paging, filtering, sorting, business-type scope, lifecycle actions, and image upload/remove (thumb/medium preview)
- Permission gates: `viewGlobalCatalog` to read; `manageGlobalCategories` / `manageGlobalProducts` for mutations
- Unauthorized users see `ShellNotFoundPage`; mutation controls hidden without manage permissions
- Optimistic concurrency via `ExpectedUpdatedAtUtc` with 409 conflict messaging and detail refetch
- CSRF-protected mutations through existing Platform antiforgery bootstrap
- EN + fil-PH i18n under `globalCatalog.*`

## Routes

| Route | Screen |
| --- | --- |
| `/admin/global-catalog/categories` | Category list |
| `/admin/global-catalog/categories/new` | Create category |
| `/admin/global-catalog/categories/:categoryId` | Category detail |
| `/admin/global-catalog/categories/:categoryId/edit` | Edit category |
| `/admin/global-catalog/products` | Product list |
| `/admin/global-catalog/products/new` | Create product |
| `/admin/global-catalog/products/:productId` | Product detail |
| `/admin/global-catalog/products/:productId/edit` | Edit product |

Navigation registry hrefs for `PWEB-NAV-CATEGORIES` and `PWEB-NAV-GLOBAL-PRODUCTS` point to these routes.

## Explicit exclusions

- Business types admin CRUD (read-only lookup only)
- Templates, imports, and other global-catalog endpoints outside categories/products
- Blazor Admin parity for non-GCAT surfaces

## Build and test evidence

Run from `src/Platform/ExItS.Platform.Admin.Web`:

- `npm test`
- `npm run typecheck`
- `npm run lint`
- `npm run build`

## Security limitations

Development-stage UI; authorization is enforced client-side for UX gating and server-side on API mutations. Image upload accepts JPEG/PNG/WebP only; preview uses authenticated GET image variants (`thumb`, `medium`).

## Next work package

PWEB-GCAT-02 or adjacent catalog surfaces (business types, templates, imports) per portfolio plan.
