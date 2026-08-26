# PWEB-IMPL-26 — Plans + Plan Versions + Grants / Pricing Management

**Package ID:** PWEB-IMPL-26  
**Title:** Plans + Plan Versions + Grants / Pricing Management  
**Starting dependency:** PWEB-IMPL-19 + PWEB-IMPL-20 (+ PWEB-25 recommended)  
**Contract classification:** **PROVEN_PARTIAL**  
**Implementation:** NOT STARTED (planning only)

## 1. Objective

Convert PWEB-19 read-only plan surfaces into authorized **Platform SaaS** commercial configuration (plans, versions, feature grants, pricing metadata) using proven catalog APIs. Separate from POS retail pricing and PLM rates/fees.

## 2. Current repository evidence

- Read: plans list/detail + product plans (PWEB-19)  
- Mutations: create plan, rename, commercial package, activate/deactivate/retire, draft version, upsert feature grant, publish version  
- Business-type grants: Domain/seed exist; Admin API draft path does not expose BT grant mutation/DTO → **PROVEN_PARTIAL**  
- Retire plan version: Domain only → **MISSING** API

## 3. Existing APIs / contracts found

| Operation | Route | Classification |
|---|---|---|
| Create plan | `POST .../catalog/products/{productCode}/plans` | PROVEN_EXISTING |
| Rename plan | `PATCH .../plans/{planId}/rename` | PROVEN_EXISTING |
| Update commercial/pricing | `PATCH .../plans/{planId}/commercial` | PROVEN_EXISTING |
| Activate/deactivate/retire plan | `POST .../plans/{planId}/activate|deactivate|retire` | PROVEN_EXISTING |
| Create draft version | `POST .../plans/{planId}/versions/draft` | PROVEN_EXISTING |
| Upsert feature grant | `PUT .../versions/{versionNumber}/feature-grants/{featureCode}` | PROVEN_EXISTING |
| Publish version | `POST .../versions/{versionNumber}/publish` | PROVEN_EXISTING |
| Business-type grant Admin API | — | **PROVEN_PARTIAL / MISSING** for Admin UI |
| Retire version HTTP | — | **MISSING** |

**Pricing fields only when present on DTOs/requests:** e.g. `MonthlyPrice`, `AnnualPrice`, `CurrencyCode`, limits/trial flags as returned/accepted by API — **do not invent**.

**Enums:** `PlanStatus` (`Draft|Active|Retired|Inactive`); `PlanVersionStatus` (`Draft|Published|Retired`); `BillingPeriod` (`None|Monthly|Yearly`)

## 4. Authorization

`ManageCatalog` for mutations; `ViewPortfolio` for reads

## 5. UI / route scope

- `/admin/plans`, `/admin/plans/:planId`, product detail plan sections  
- Show/edit only proven fields  
- Explicit copy: Platform SaaS pricing ≠ POS SKU ≠ PLM interest/fees

## 6. Mutation / audit / CSRF / errors

PWEB-20 CSRF; server audit; 401/403/404/409; no fabricated prices

## 7. Concurrency

Use server version/expected fields where provided; re-fetch on conflict

## 8. Explicit exclusions

- Inventing currencies/intervals/grant codes  
- Business-type grant UI until Admin API proven (`BACKEND CONTRACT REQUIRED BEFORE IMPLEMENTATION`)  
- POS/PLM operational pricing  
- Retire-version UI without route

## 9. Change allowances

Backend: only if Product Owner authorizes closing BT-grant Admin API gap; else stop or ship without that UI. DB: none unless authorized. POS/PLM/Blazor unchanged.

## 10. Tests / evidence / commit

Tests: plan lifecycle, commercial update, draft/grant/publish, CSRF, no invented fields  
Evidence: `docs/Platform-Admin-Web/Reports/PWEB-IMPL-26-plans-versions-grants.md`  
Commit: `feat(platform-web): add plan version grant management`

## 11. Stop conditions

`PWEB26_PLAN_MUTATION_CONTRACT_MISSING`; inventing pricing; BT-grant UI without API

## 12. Definition of PASS

Authorized plan/version/feature-grant/pricing management for **proven** endpoints; Platform SaaS boundary clear; gaps documented not faked.
