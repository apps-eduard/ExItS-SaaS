# PWEB-IMPL-18 — Platform Product Catalog

**Status:** COMPLETE

**Branch:** `feat/platform-admin-web-v2`

**Message:** `feat(platform-web): add product catalog`

## Screen

Read-only `/admin/products` backed by `GET /api/v1/platform/catalog/products` with `status`, `search`, `sortBy`, `sortDesc`, `page`, and `pageSize`.

Reuses the same commercial catalog client and product source as Organizations → By Product (`product-catalog-client.ts`). No hardcoded POS/PLM registry or second static product list.

Rows link to `/admin/products/:productId` for PWEB-19. Authorization: `viewPortfolio`; unauthorized fail-closed. No create/activate/deactivate/retire controls.

## Evidence

`docs/Platform-Admin-Web/Reports/impl-18-product-catalog/`

## Visual approval

**AWAITING PRODUCT OWNER + CHATGPT**
