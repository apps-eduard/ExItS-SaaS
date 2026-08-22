# Platform Admin Web — PWEB-IMPL-21 through PWEB-IMPL-30 Continuation Plan

**Status:** AUTHORITATIVE PLANNING (documentation only)  
**Branch:** `feat/platform-admin-web-v2`  
**Documentation baseline HEAD:** `06e5cc1cdcf927c4c2c61d0345b7c892669e363c`  
**Includes:** PWEB-20 CSRF foundation + CSRF compatibility gate  
**Implementation started:** NO  

This document is the master continuation plan for packages **PWEB-IMPL-21 … PWEB-IMPL-30**.  
Package specifications live under [`Packages/`](./Packages/).

---

## 1. Purpose

Define the next authorized implementation queue **after** PWEB-20, using only contracts proven in the current repository. This is **not** an implementation authorization for any single package until that package is explicitly started.

---

## 2. Completed foundation (frozen)

| Package | SHA (representative) | Outcome |
|---|---|---|
| PWEB-IMPL-16 | users directory | Read-only Platform users |
| PWEB-IMPL-17 | user detail | Read-only user + assignments |
| PWEB-IMPL-18 | product catalog | Read-only products |
| PWEB-IMPL-19 | `8cbd8ff0…` | Read-only product detail + plans |
| PWEB-IMPL-20 | `96a3acdf…` | CSRF-safe browser mutation foundation |
| Compat gate | `06e5cc1c…` | POS Platform HttpClient cookie isolation |

### CSRF model (must be reused by all mutation packages)

| Item | Value |
|---|---|
| Bootstrap | `GET /api/v1/platform/antiforgery/token` |
| Header | `X-XSRF-TOKEN` |
| Session cookie | `.ExItS.Platform.Auth` |
| Antiforgery cookie | `.ExItS.Platform.Antiforgery` |
| React storage | in-memory only |
| Protected methods | POST / PUT / PATCH / DELETE when session cookie present without `X-ExItS-Session-Token` |
| Blazor / header clients | exempt when `X-ExItS-Session-Token` present |

All React mutations **must** use the centralized `platform-http` mutation path. No ad-hoc `fetch` that bypasses antiforgery.

---

## 3. Hard architecture rules (continuation)

| Rule | Requirement |
|---|---|
| **A** | Platform Admin does **not** create customer organizations. Creation remains Personal → Start a Business (`POST /api/v1/personal/start-business`). Runtime `POST /api/v1/platform/organizations` is Testing-gated only — **PROHIBITED** in Admin UI. |
| **B** | Platform Admin does **not** create products at runtime. Products are provisioned via seed / migration / deployment. Runtime `POST /api/v1/platform/catalog/products` is Testing-gated only — **PROHIBITED** in Admin UI. |
| **C** | Platform does **not** own POS/PLM operational authorization. Subscription/entitlement ≠ product-local role. |
| **D** | Server remains authoritative. UI visibility is not authorization. |
| **E** | Do not mark Production Ready, Cutover Authorized, or Blazor retirement without PWEB-30 proof. |

---

## 4. Implementation order and dependencies

```text
20 PASS
 → 21 User Lifecycle + Session Control
 → 22 Roles + Permission Catalog
 → 23 User Role Assignments
 → 24 Existing Organization Lifecycle
 → 25 Existing Product Lifecycle
 → 26 Plans + Versions + Grants / Pricing
 → 27 Platform Payments + Paid-Subscription Invariant
 → 28 Manual Payment Attestation
 → 29 Subscription + Entitlement Operations
 → 30 Security / Compatibility / Cutover Blocker Closure
```

| Package | Depends on | Contract rollup |
|---|---|---|
| 21 | 20 | **PROVEN_PARTIAL** (lifecycle APIs exist; dedicated session list/revoke missing; self-protection missing) |
| 22 | 20 | **PROVEN_EXISTING** (roles, role-definitions, permission catalog) |
| 23 | 21, 22 | **PROVEN_EXISTING** (system + custom assignments; last-admin revoke guard) |
| 24 | 20 | **PROVEN_EXISTING** (suspend / reactivate / close; profile update) |
| 25 | 19, 20 | **PROVEN_EXISTING** (activate / deactivate / retire / rename; no create) |
| 26 | 19, 20, 25 | **PROVEN_PARTIAL** (plan/version/grant/pricing APIs exist; business-type grant Admin API gap) |
| 27 | 20 | **PROVEN_EXISTING** (payments + paid activation invariant); UI + `isTest` gaps partial |
| 28 | 27 | **PROVEN_PARTIAL** (manual cash/bank/GCash APIs exist; actor binding still free-text) |
| 29 | 27, 28 | **PROVEN_PARTIAL** (most subscription/entitlement ops exist; dedicated renew HTTP missing) |
| 30 | 21–29 as implemented | **UNRESOLVED** until proven (social URL + cross-client CSRF validation) |

Later packages **must not** start if their required API/domain contracts are `MISSING` or unresolved business semantics block safe UI. Prefer stop codes over inventing behavior.

---

## 5. Cross-project compatibility flags (open until PWEB-30)

These remain **open for final validation** even if earlier gates reduced risk:

| Flag | Value until PWEB-30 proves otherwise |
|---|---|
| `PLM_PWA_CSRF_COMPAT_REVIEW_REQUIRED` | **YES** |
| `POS_REACT_CSRF_COMPAT_REVIEW_REQUIRED` | **YES** |
| Social-auth `sessionToken` in return URL | **OPEN** (`BLOCKS_CUTOVER`) |

Do **not** modify POS React or PLM PWA in packages 21–29. PWEB-30 validates against then-current clients.

---

## 6. Package index

| ID | Title | Spec |
|---|---|---|
| PWEB-IMPL-21 | Platform User Lifecycle + Session Control | [Packages/PWEB-IMPL-21-platform-user-lifecycle.md](./Packages/PWEB-IMPL-21-platform-user-lifecycle.md) |
| PWEB-IMPL-22 | Platform Roles + Permission Catalog Management | [Packages/PWEB-IMPL-22-platform-roles-permissions.md](./Packages/PWEB-IMPL-22-platform-roles-permissions.md) |
| PWEB-IMPL-23 | Platform User Role Assignments | [Packages/PWEB-IMPL-23-platform-user-role-assignments.md](./Packages/PWEB-IMPL-23-platform-user-role-assignments.md) |
| PWEB-IMPL-24 | Existing Organization Lifecycle Management | [Packages/PWEB-IMPL-24-organization-lifecycle.md](./Packages/PWEB-IMPL-24-organization-lifecycle.md) |
| PWEB-IMPL-25 | Existing Product Lifecycle Management | [Packages/PWEB-IMPL-25-product-lifecycle.md](./Packages/PWEB-IMPL-25-product-lifecycle.md) |
| PWEB-IMPL-26 | Plans + Plan Versions + Grants / Pricing Management | [Packages/PWEB-IMPL-26-plans-versions-grants.md](./Packages/PWEB-IMPL-26-plans-versions-grants.md) |
| PWEB-IMPL-27 | Platform Payments + Paid-Subscription Invariant | [Packages/PWEB-IMPL-27-platform-payments.md](./Packages/PWEB-IMPL-27-platform-payments.md) |
| PWEB-IMPL-28 | Manual Payment Attestation | [Packages/PWEB-IMPL-28-manual-payment-attestation.md](./Packages/PWEB-IMPL-28-manual-payment-attestation.md) |
| PWEB-IMPL-29 | Subscription + Entitlement Operations | [Packages/PWEB-IMPL-29-subscription-entitlement-operations.md](./Packages/PWEB-IMPL-29-subscription-entitlement-operations.md) |
| PWEB-IMPL-30 | Security / Compatibility / Cutover Blocker Closure | [Packages/PWEB-IMPL-30-security-compatibility-cutover.md](./Packages/PWEB-IMPL-30-security-compatibility-cutover.md) |

---

## 7. Global stop codes

| Code | When |
|---|---|
| `PWEB21_USER_MUTATION_CONTRACT_MISSING` | Required lifecycle/session contract absent or ambiguous |
| `PWEB22_PLATFORM_RBAC_CONTRACT_MISSING` | Role/permission catalog contract absent |
| `PWEB23_ASSIGNMENT_CONTRACT_MISSING` | Assign/revoke contract absent |
| `PWEB24_ORG_LIFECYCLE_CONTRACT_MISSING` | Org lifecycle contract absent |
| `PWEB25_PRODUCT_LIFECYCLE_CONTRACT_MISSING` | Product lifecycle contract absent |
| `PWEB26_PLAN_MUTATION_CONTRACT_MISSING` | Plan/version/grant mutation contract absent |
| `PWEB27_PAYMENT_CONTRACT_MISSING` | Payment / invariant contract absent |
| `PWEB28_MANUAL_PAYMENT_CONTRACT_MISSING` | Manual attestation contract absent or unsafe |
| `PWEB29_SUBSCRIPTION_MUTATION_CONTRACT_MISSING` | Subscription/entitlement mutation contract absent |
| `PWEB30_CUTOVER_BLOCKERS_OPEN` | Cutover claimed while blockers remain open |

---

## 8. Explicit global non-goals (21–30)

- Create Organization in Platform Admin UI  
- Create Product in Platform Admin UI  
- POS operational data / POS product-local roles  
- PLM operational lending / rates / fees  
- Automatic historical payment/subscription data repair  
- Blazor Admin retirement  
- Production Ready / Cutover Authorized declarations before PWEB-30 proof  
- Implementation inside this documentation task  

---

## 9. Evidence sources used

- `src/Platform/ExItS.Platform.Api` (Identity, Authorization, Organizations, Catalog, Payments, Subscriptions, Entitlements, ExternalAuth, Antiforgery)  
- `src/Platform/ExItS.Platform.Application` / `Domain`  
- `src/Platform/ExItS.Platform.Admin.Web` (React routes, `platform-http`, nav implementation)  
- `docs/Platform-Admin-Web/` reports PWEB-IMPL-16…20, api-capability-matrix, navigation-registry, implementation-status  

---

## 10. Next authorized implementation package

**PWEB-IMPL-21** — only after Product Owner starts that package explicitly.  
This documentation commit does **not** start implementation.

## 11. Commercial E2E overlay (PA-COM) — 2026-08-22

A separate documentation-only track covers React Platform Admin **commercial/subscription** readiness for Platform Admin → POS E2E:

- [commercial-subscription-implementation-plan.md](./commercial-subscription-implementation-plan.md)
- [Reports/PLATFORM-WEB-COMMERCIAL-READINESS-AUDIT-01.md](./Reports/PLATFORM-WEB-COMMERCIAL-READINESS-AUDIT-01.md)

PA-COM-02…06 overlap PWEB-IMPL-25…29. Prefer the PA-COM execution order for commercial E2E (01 → 04 → 06 → 05…) because seed Starter/Growth/Pro already exist. PWEB-21…24 are **not** prerequisites for that spine.

`PA_COM_01_AUTHORIZED=NO`. Do not start PA-COM-01 from this continuation-plan file.
