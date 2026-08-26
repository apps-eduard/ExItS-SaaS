# PWEB-IMPL-25 — Existing Product Lifecycle Management

**Package ID:** PWEB-IMPL-25  
**Title:** Existing Product Lifecycle Management  
**Starting dependency:** PWEB-IMPL-19 + PWEB-IMPL-20  
**Contract classification:** **PROVEN_EXISTING**  
**Implementation:** NOT STARTED (planning only)

## 1. Objective

Add approved lifecycle controls for **existing** Platform catalog products (activate / deactivate / retire / rename). **Create Product is PROHIBITED** in Platform Admin UI.

## 2. Current repository evidence

- PWEB-18/19 read-only product list/detail  
- Mutations: activate / deactivate / retire / rename  
- Create product Testing-gated (`RuntimeProductCreationDisabled`)  
- Product aggregate metadata beyond display name: rename only (no description field on Product)

## 3. Existing APIs / contracts found

| Operation | Route | Classification |
|---|---|---|
| Activate | `POST .../catalog/products/{id}/activate` | PROVEN_EXISTING |
| Deactivate | `POST .../catalog/products/{id}/deactivate` | PROVEN_EXISTING |
| Retire | `POST .../catalog/products/{id}/retire` | PROVEN_EXISTING |
| Rename | `PATCH .../catalog/products/{id}/rename` | PROVEN_EXISTING (`DisplayName`, optional `ExpectedUpdatedAtUtc`) |
| Create product | `POST .../catalog/products` | PROVEN_PARTIAL / **PROHIBITED in UI** |

**Statuses:** `ProductStatus` = `Active | Inactive | Retired`  
**DTO:** `ProductDto(Id, Code, DisplayName, Status, CreatedAtUtc, UpdatedAtUtc)`

## 4. Interaction notes (document at implementation from server behavior)

- Plans under product remain catalog entities; do not invent cascade rules  
- Existing subscriptions/entitlements: surface server errors only; do not invent org-wide auto-changes  
- Future products: same APIs by id/code — no hardcoded POS/PLM registry

## 5. Authorization

`ManageCatalog` for mutations; reads remain `ViewPortfolio`

## 6. UI / route scope

- `/admin/products` and `/admin/products/:productId`  
- Lifecycle actions with confirmation  
- **No Create Product**

## 7. Mutation behavior

CSRF; refresh product + related plan list after success; concurrency via `ExpectedUpdatedAtUtc` when provided

## 8. Audit

Server audit

## 9. Security / CSRF

PWEB-20

## 10. Error states

401/403/404/409 invalid transition / concurrency

## 11. Concurrency / idempotency

Honor `ExpectedUpdatedAtUtc` on rename; re-fetch on conflict

## 12. A11y / i18n / responsive

Standard

## 13. Explicit exclusions

- **Create Product**  
- Product-operational POS/PLM data  
- Invented metadata fields  
- Hardcoded product codes in UI logic

## 14–17. Change allowances

Backend none expected; DB none; POS/PLM/Blazor unchanged

## 18. Tests required

Activate/deactivate/retire/rename; no create; CSRF; unknown status fallback; future arbitrary product id still works

## 19. Evidence path

`docs/Platform-Admin-Web/Reports/PWEB-IMPL-25-product-lifecycle.md`

## 20. Proposed commit message

`feat(platform-web): add product lifecycle controls`

## 21. Stop conditions

`PWEB25_PRODUCT_LIFECYCLE_CONTRACT_MISSING`; Create Product UI

## 22. Definition of PASS

Existing-product lifecycle only; Create Product absent; CSRF correct; no operational product data touched.
