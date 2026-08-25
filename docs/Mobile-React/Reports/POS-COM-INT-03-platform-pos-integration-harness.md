# POS-COM-INT-03 — Platform→POS Commercial Integration Harness

**Package:** POS-COM-INT-03  
**Branch:** `feat/pos-react-client`  
**Scope:** POS React commercial UX + Platform→POS real integration harness (Agent 1)  
**Status:** AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW

---

## 1. Summary

COM-INT-03 wires shared commercial error UX across protected POS React flows and adds a **REAL_PLATFORM_STATE_E2E** harness that exercises the commercial spine below Platform Admin React:

`MVP plan catalog → subscription/trial → entitlement snapshot → Platform introspection → POS bearer middleware → POS endpoint authorization`

Header-simulation tests from COM-INT-01/02 remain as **HEADER_SIMULATION_TEST** regression coverage.

---

## 2. Platform plans and authoritative catalog

| Plan key | Max POS devices | Customer credit | Advanced reports | Export |
|----------|-----------------|-----------------|------------------|--------|
| `starter` | 1 | disabled | disabled | disabled |
| `growth` | 3 | enabled | enabled | enabled |
| `pro` | 10 | enabled | enabled | enabled |

Source: `MvpPosPlanCatalog` / `EnsureMvpPosPlans.BuildGrants` (Platform domain).

Ordering/delivery (`store-customer-ordering`, `store-delivery-orders`) are granted on all MVP BasicStore plans (V1 configuration).

---

## 3. REAL vs HEADER simulation

| Area | Classification | Location |
|------|----------------|----------|
| Growth device limit (3 + 4th blocked) | **REAL_PLATFORM_STATE_E2E** | `PosPlatformCommercialSpineIntegrationTests` |
| Growth→Pro upgrade capacity | **REAL_PLATFORM_STATE_E2E** | same |
| Suspend / reactivate POS authorization | **REAL_PLATFORM_STATE_E2E** | same |
| Starter vs Growth feature grants | **REAL_PLATFORM_STATE_E2E** | same |
| Ordering / delivery grants | **REAL_PLATFORM_STATE_E2E** | same |
| Cross-org isolation | **REAL_PLATFORM_STATE_E2E** | same |
| Introspection refresh after suspend | **REAL_PLATFORM_STATE_E2E** | same |
| Strict bearer / header grants | **HEADER_SIMULATION_TEST** | `PosCommercialIntegrationReadinessTests` |

Harness setup:

- Dual PostgreSQL Testcontainers (Platform + POS), migrations applied
- `EnsureMvpPosPlans` catalog seed
- Personal register/login → `/api/v1/personal/start-business` (trial, POS owner role, Sari-Sari business type)
- Session grant → Platform access token → POS strict bearer (`CommercialValidation:Strict=true`)
- POS introspection `HttpClient` wired to in-process Platform TestServer handler (no flaky localhost HTTP)

---

## 4. Platform defect fixed (canonical, small)

**Trial definition resolution in `StartBusinessUseCases.ResolveMvpPlanCatalogAsync`**

Previously, when no trial existed for the requested plan, code fell back to *any* active trial with the same duration. After a Growth org was created first, Starter start-business could attach Growth’s trial definition, leaking Growth grants into Starter introspection (cross-org advanced-report false positive).

**Fix:** resolve trial **only** by matching `TrialDefinition.PlanId` to the selected plan; create a plan-specific trial when missing.

**Introspection owner role preservation (`AccessTokenUseCases`)**

When org owner has `ProductLocalRoleGranted` but introspection took the organization-management branch, `mappedPosRole` was cleared and POS mutations 403’d. Introspection/bind now preserve mapped POS roles when a product-local role grant exists.

---

## 5. POS runtime hardening

| Change | Purpose |
|--------|---------|
| `PosPlatformBearerMiddleware` resets commercial accessor each bearer request | Prevents stale scoped commercial state between sequential bearer calls in tests/runtime |
| `PlatformTokenIntrospectionClient.ClearCacheForTests()` | Test-safe cache invalidation between spine scenarios (~45s TTL documented) |
| Platform introspection role preservation | Owner POS checkout/mutations authorized when role grant exists |

**Cache / refresh behavior (live):** Platform Admin subscription changes propagate to POS after authoritative introspection refresh (new access token, cache expiry ~45s, or session refresh). Not instant.

---

## 6. React commercial UX

Shared module: `src/access/pos-commercial-errors.ts`

| User-safe state | i18n key |
|-----------------|----------|
| Product access unavailable | `commercial.productUnavailable` |
| Subscription suspended | `commercial.subscriptionSuspended` |
| Feature not in plan | `commercial.notIncludedInPlan` |
| Commercial state unavailable | `commercial.accessUnavailable` |
| Device limit | `devices.capacity.limitReached` |

Wired via `describePosApiError` / `mapCommercialAccessErrorKey` into checkout, Utang repay, classic/operational reports, customer ordering, device registration, workspace bind errors.

Session grant commercial posture reuses `resolveCommercialAccessState()` on existing grant facts (no second commercial store).

Export: no export button; `store-export` remains contract-only (`PRODUCT_FEATURE_NOT_IMPLEMENTED`).

---

## 7. Evidence matrix (REAL_PLATFORM_STATE_E2E)

| # | Scenario | Evidence |
|---|----------|----------|
| 1 | Growth introspection grants | Platform `/auth/introspect` enabledFeatureCodes include credit, advanced reports, ordering, delivery |
| 2 | Starter introspection exclusions | Same endpoint excludes credit, advanced, export |
| 3 | Growth device #1–3 register | Platform device registration PASS |
| 4 | Growth device #4 | `PosDeviceCapacityExceeded` |
| 5 | Growth→Pro upgrade | Platform upgrade API → 4th device PASS |
| 6 | Suspend | introspection `Suspended`; POS catalog/sale/credit denied |
| 7 | Reactivate | introspection trialing/active; POS catalog restored |
| 8 | Starter cash/GCash | POS checkout Created |
| 9 | Starter Utang | POS checkout Forbidden (CreateCredit gate) |
| 10 | Starter advanced report | POS operational report Forbidden |
| 11 | Growth Utang | customer create + Utang checkout Created |
| 12 | Cross-org devices | Growth 3 devices does not block Starter 1-device cap |
| 13 | Cross-org features | Starter introspection lacks advanced; POS Forbidden after Growth POS call |
| 14 | Introspection refresh | New access token after suspend reflects `Suspended` without polling |

---

## 8. Test gates (Release)

| Gate | Result |
|------|--------|
| `PosPlatformCommercialSpineIntegrationTests` (16) | PASS |
| `PosCommercialIntegrationReadinessTests` (19) | PASS |
| `PosDeviceConcurrentRegistrationIntegrationTests` (2) | PASS |
| `PosOfflinePriceAuthorityApiTests` (RMAP-21) (12) | PASS |
| Vitest Client (560) | PASS |
| Client `typecheck` / `build` | PASS |

---

## 9. Files (material)

**React UX:** `pos-commercial-errors.ts`, checkout/reports/ordering/utang/device pages, i18n locales, `workspace-bind-error.ts`

**Harness:** `PosPlatformSpineFixture.cs`, `PosPlatformCommercialSpineIntegrationTests.cs`, `Support/PlatformCommercialSpineSupport.cs`, `Support/PosSpinePosApiHelpers.cs`

**Platform/POS fixes:** `StartBusinessUseCases.cs`, `AccessTokenUseCases.cs`, `PosPlatformBearerMiddleware.cs`, `PlatformTokenIntrospectionClient.cs`

---

## 10. Known gaps

- No dedicated global product-level commercial banner component (session/readiness paths cover bind-level denial).
- Export product feature not implemented (by design).
- POS introspection cache TTL (~45s) means live Admin changes are not instant on POS.

---

## 11. COM-INT-02 note

Cross-org Starter grant leakage was a **Platform trial-resolution defect**, not a POS authorization bypass. COM-INT-02 strict/header conclusions remain valid; REAL harness now proves plan isolation with authoritative Platform state.
