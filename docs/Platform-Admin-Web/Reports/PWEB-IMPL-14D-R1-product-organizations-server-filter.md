# PWEB-IMPL-14D-R1 — Server-authoritative product organization filter

**Status:** COMPLETE

**Branch:** `feat/platform-admin-web-v2`

**Commit:** recorded after push

**Message:** `feat(platform): filter organizations by product`

## Canonical organization ↔ product rule

An organization uses a Platform product when it has a **Platform Subscription** for that catalog `ProductCode` (`Subscription.OrganizationId` + `Subscription.ProductCode`).

This is the existing organization-level commercial aggregate (`GetCurrentForOrganizationProductAsync`, commercial summary subscriptions). User-level `ProductAccessAssignment` is **not** used.

## API

`GET /api/v1/platform/organizations?productCode=<catalogProductCode>`

Optional. Existing callers without `productCode` are unchanged. Invalid or unknown catalog codes return 400. List authorization is unchanged (`ViewPortfolio` or `ManageOrganizations`).

Filtering is a translatable `EXISTS` on subscriptions. `totalCount` is computed after the product filter. Search, status, sort, and paging compose on the filtered set.

## React

Sanitized UI `?product=<code>` maps to `productCode` only after authorized catalog sanitation. Arbitrary URL products do not issue a product-specific organization request.

## Evidence

Screenshots: `docs/Platform-Admin-Web/Reports/impl-14d-r1-product-organizations-server-filter/`

Platform unit tests: 1012 passed. Targeted PostgreSQL integration: `ApiOrganizationProductFilterTests` passed (org A=X, B=Y, C=X+Y; search/status/sort/page composition; invalid/unknown product 400; authorization unchanged). React: typecheck/lint/format PASS; vitest 219 passed; Playwright 96 passed; container smoke 3 passed. Full `ExItS.slnx` Release build reports the existing MAUI Android SDK path error only.

## N+1

ABSENT — one organization list query with server-side `EXISTS`; no commercial-summary-per-row.

## Visual approval

**AWAITING PRODUCT OWNER + CHATGPT**
