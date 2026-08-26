# PLATFORM-WEB-COMMERCIAL-READINESS-AUDIT-01

**Status:** COMPLETE (documentation only)  
**Date:** 2026-08-22  
**Program:** React Platform Admin commercial / subscription readiness  
**Implementation:** NOT STARTED  
**Production cutover:** NO  

## Evidence baseline (do not treat as stale)

| Source | Branch | HEAD SHA | Role |
|---|---|---|---|
| React Platform Admin + Platform API | `docs/platform-admin-commercial-readiness` (from `feat/platform-admin-web-v2`) | `525bae3633fb7fde1bbc9b855435a05f5f616c09` | Primary audit target |
| POS React worktree (read-only inspection) | `feat/pos-react-client` | `42d487d42c0d9e3e6592cafcb2259c24655dbb23` | Platform→POS enforcement evidence |
| Companion `ExItS-SaaS` checkout | `docs/pinoy-service-pro-foundation` | `3647e7af84d574fa4065804beee1203a556e0105` | Not used as Platform source of truth |

This audit inspected actual React, Platform API/Application/Domain, and POS React commercial code. It does **not** assume prior planning SHAs.

**Target UI:** `src/Platform/ExItS.Platform.Admin.Web` (React + TypeScript).  
**Blazor Admin** (`src/Platform/ExItS.Platform.Admin`) is reference-only. Do not copy its visual design.

**Hard stop:** this document does not authorize PA-COM-01 or any production code change.

---

## 1. Verdict

React Platform Admin can **inspect** catalog products, plans, and an organization’s commercial state. It cannot **operate** the commercial spine.

| Question | Answer |
|---|---|
| Ready for full Platform Admin → POS commercial E2E today? | **NO** |
| Why | React commercial mutations are absent. Seed plans and Platform APIs exist, but an operator cannot start trial, subscribe, upgrade, suspend, or reactivate from React Admin. Local Validation payment simulation has no React UX. POS device-limit APIs exist, but the Admin-driven lifecycle that feeds them is not wired. |
| React commercial completeness | **~30%** (inspection ~70%; mutations ~0%) |
| Backend commercial contracts | **~85%** available; remaining gaps are listed below |
| Production Ready | **NO** |

Related planning (`PWEB-IMPL-21`…`30`) remains documentation-only and is **not** started. This commercial track (`PA-COM-*`) is a dependency-ordered overlay for subscription/E2E readiness. It does not silently replace identity packages 21–23.

---

## 2. What is already implemented (React)

All of the following are **GET-only** unless noted.

| Surface | Route | Evidence |
|---|---|---|
| Product list | `/admin/products` | `ProductsPage.tsx`, `GET /api/v1/platform/catalog/products` |
| Product detail | `/admin/products/:productId` | `ProductDetailPage.tsx`, `GET .../catalog/products/{id}` |
| Plans nested under product | product detail | `GET .../catalog/products/{productCode}/plans` |
| Plan list | `/admin/plans` | `PlansPage.tsx`, `GET .../catalog/plans` |
| Plan detail (identity, pricing, limits, 3 feature flags, trial fields) | `/admin/plans/:planId` | `PlanDetailPage.tsx` |
| Organizations list (optional `?product=`) | `/admin/organizations` | PWEB-IMPL-07 / 14D |
| Organization workspace | `/admin/organizations/:organizationId` | Overview, Branches, People, Products, Subscription, Entitlements, Billing, Activity |
| Org products/access | `.../products` | commercial-summary `latestEntitlements` |
| Org subscriptions (filter/status display) | `.../subscription` | `GET .../organizations/{id}/subscriptions` |
| Org entitlements (snapshot list + grant expand) | `.../entitlements` | `GET .../products/{code}/entitlements/snapshots` |
| Org billing (payment list) | `.../billing` | `GET .../organizations/{id}/payments` |
| Org activity | `.../activity` | `GET .../organizations/{id}/audit` |
| CSRF mutation **transport** | (no commercial use yet) | PWEB-IMPL-20: `GET /antiforgery/token` + `X-XSRF-TOKEN` in `platform-http.ts` |
| Auth login/logout | `/admin/login` | only app `POST`s today |

There are **zero** commercial TanStack `useMutation` hooks.

---

## 3. What is read-only

Every commercial page that exists is read-only by design of PWEB-07…19.

Tests explicitly assert the absence of mutation controls, including:

- no Create Product (`e2e/products-plans.spec.ts`)
- no Activate/Cancel on org subscription (`OrganizationSubscriptionsPage.test.tsx`)
- no Confirm on billing (`OrganizationBillingPage.test.tsx`)
- no Override/Reconcile on entitlements (`OrganizationEntitlementsPage.test.tsx`)

Known statuses are **displayed/filtered**, not transitioned:

- Subscription: `Trialing`, `Active`, `GracePeriod`, `PastDue`, `Suspended`, `Cancelled`, `Expired`
- Payment: `PendingConfirmation`, `Confirmed`, `Rejected`, `Voided`
- Product: `Active`, `Inactive`, `Retired`
- Plan list filter omits `Draft` even though domain `PlanStatus` includes Draft

---

## 4. What is missing (React)

| Area | Missing operator capability |
|---|---|
| Product | rename, activate, deactivate, retire, feature-definition UI, product audit |
| Plan | create, edit commercial package, lifecycle, versions draft/publish, grant editor |
| Plan display | `store-customer-ordering` / `store-delivery-orders` (not on `CatalogPlan`) |
| Organization subscription | start trial, subscribe/pay-now, convert trial, upgrade, downgrade, preview, grace, past due, suspend, reactivate, cancel, expire, apply pending plan |
| Entitlements | generate snapshot, reconcile, create/revoke feature override |
| Billing | record/confirm/reject/void manual payment; activate-from-payment; Local Validation simulate |
| Global nav destinations | `/admin/subscriptions`, `/admin/entitlements`, `/admin/payments`, `/admin/local-validation/test-payments` — nav IDs exist, React status `UNDER_DEVELOPMENT`, **no routes** |
| Permissions in React constants | `platform.permission.manage_catalog` is **not** in `PLATFORM_PERMISSIONS` |

**Create Product in Platform Admin UI remains PROHIBITED.** Runtime `POST /catalog/products` is Testing-gated (`RuntimeProductCreationDisabled`).

**Create Organization in Platform Admin UI remains PROHIBITED.** Creation stays Personal → Start a Business.

---

## 5. Backend gaps

Most commercial HTTP contracts already exist. Documented gaps (domain/app without a safe Admin HTTP, or intentional blocks):

| Gap | Classification | Evidence |
|---|---|---|
| PlanVersion.Retire | BACKEND GAP (no HTTP) | Domain `PlanVersion.Retire`; no `MapPost` retire on versions |
| Draft business-type grants via Admin API | BACKEND GAP | Draft version endpoint forces `businessTypeGrants: null` |
| ReplaceDraftPlanVersionGrants | APPLICATION-ONLY | registered in DI; no route |
| Dedicated Admin `POST .../renew` | BACKEND GAP | Local Validation can simulate renewal; no Admin renew route |
| `POST .../subscriptions/{id}/activate` | INTENTIONAL BLOCK | always `PaymentRequiredForPaidActivation` |
| Create product outside Testing | INTENTIONAL GATE | `RuntimeProductCreationDisabled` |
| `isTest` on SaaS payment DTO | PARTIAL | Online method is a weak proxy |
| Manual payment actor binding | PARTIAL | free-text attested-by fields (PWEB-28) |
| MaxActiveStaff at invite time | BACKEND GAP | Plan column + plan-change **preview** text exist; `InvitationUseCases` does not check `MaxActiveStaff` |
| Numeric device limit on introspect DTO | NOT REQUIRED for device APIs | POS device capacity reads live `Plan.MaxActivePosDevices`, not token payload |
| Class comment on `SubscriptionEndpoints` claiming plan change unsupported | STALE COMMENT | upgrade/downgrade/convert-trial HTTP exist |

Do not invent a `ChangePlan` endpoint. Use `upgrade`, `downgrade`, `convert-trial`, `apply-pending-plan`.

---

## 6. React-only gaps

These have proven Platform APIs and need React wiring (CSRF foundation already exists):

- Product rename + activate/deactivate/retire (`ManageCatalog`)
- Plan create/rename/commercial patch + activate/deactivate/retire
- Plan version draft, grant upsert, publish
- Org start trial / from-catalog / paid create
- Upgrade, downgrade + preview, convert-trial, apply-pending-plan
- Grace / past-due / suspend / reactivate / cancel / expire
- Entitlement snapshot POST + reconcile
- Feature override create/revoke
- Manual payment record/confirm/reject/void + activate-subscription
- Local Validation `POST /local-validation/payments/simulate` UX (non-Production only)

---

## 7. Platform→POS integration gaps

| Gap | Why it blocks true E2E |
|---|---|
| React Admin cannot drive subscription lifecycle | Operator cannot put an org on Growth, then upgrade/suspend from Admin |
| Development grant-merging | POS bearer merges `DefaultDevelopmentGrants` in Development/Testing or `LocalValidation:Enabled && !Production` — can hide missing entitlements |
| Commercial header fallback | Missing headers → `DevelopmentDefault` (Active + full grants) in Dev/Testing only |
| Session issue DTO omits feature codes / subscription status | POS API gets them via introspect; React POS UI gating is mostly role + `productAccessAllowed` |
| `store-advanced-reports` / `store-export` | present on plan/snapshot; **no POS runtime gate** found |
| Ordering/Delivery | granted on **all** MVP BasicStore plans (including Starter) — cannot currently differentiate Starter vs Pro for these two codes |
| Device APIs treat Suspended as active-like | selling denied; device register/capacity may still resolve the plan |

**These Development/Testing aids are not production security.**

---

## 8. Subscription flow gaps

Domain statuses (no `NoSubscription` enum — absence of an active-like row is “no subscription”):

`Trialing → Active → GracePeriod → PastDue → Suspended → Cancelled → Expired`

| Intended operator action | Domain | HTTP | React |
|---|---|---|---|
| Start Free Trial | StartTrial | `POST .../organizations/{id}/subscriptions/trials` | MISSING |
| Subscribe / PayNow | paid create / from-catalog | `POST .../subscriptions` (requires `paymentId`); `POST .../from-catalog` | MISSING |
| Trial → paid | ConvertTrialToPaid | `POST .../{id}/convert-trial` | MISSING |
| Upgrade | ApplyImmediatePlanUpgrade | `POST .../{id}/upgrade` (+ payment) | MISSING |
| Downgrade | SchedulePlanDowngrade | `POST .../{id}/downgrade` | MISSING |
| Apply scheduled downgrade | ApplyPendingPlanChange | `POST .../{id}/apply-pending-plan` | MISSING |
| Enter grace | EnterGracePeriod | `POST /subscriptions/{id}/grace-period` | MISSING |
| Mark past due | MarkPastDue | `POST .../past-due` | MISSING |
| Suspend | Suspend | `POST .../suspend` | MISSING |
| Reactivate | Reactivate → Active | `POST .../reactivate` | MISSING |
| Cancel | Cancel | `POST .../cancel` | MISSING |
| Expire | Expire | `POST .../expire` | MISSING |
| Dedicated renew | — | **no Admin HTTP** | NOT REQUIRED until backend exists |
| History | paged list + org Activity | GET list / audit | PARTIAL (list + audit, no dedicated history UX) |

Cancelled/Expired **cannot** reactivate; create a new subscription.

One active-like subscription per `(organizationId, productCode)` (`Trialing|Active|GracePeriod|PastDue|Suspended`).

---

## 9. Entitlement / limit enforcement gaps

| Limit / feature | Plan seed | Snapshot grant | Enforcement |
|---|---|---|---|
| Max POS devices | Starter 1 / Growth 3 / Pro 10 | `plan-max-active-pos-devices` | **YES** — Platform `RegisterCurrentDevice` vs live `Plan.MaxActivePosDevices`; React POS Device Management shows used/allowed |
| Max branches | 1 / 3 / 10 | `plan-max-branches` | **YES** — branch create |
| Max active staff | 3 / 10 / 30 | `plan-max-active-staff` | **PARTIAL** — preview conflict text; **no invite-time check found** |
| Max business types | 1 / 3 / 6 | `plan-max-active-business-types` | **YES** — activation capacity |
| Customer credit | F / T / T | `customer-credit-view\|repay\|create` | **YES** — POS `UtangCapability` (subject to Dev grant merge) |
| Advanced reports | F / T / T | `store-advanced-reports` | **CATALOG ONLY** — no POS API gate |
| Export | F / T / T | `store-export` | **CATALOG ONLY** — no POS API gate |
| Customer ordering | granted on all MVP plans | `store-customer-ordering` | POS capability exists; **not a Starter differentiator today** |
| Delivery | granted on all MVP plans | `store-delivery-orders` | POS capability exists; **not a Starter differentiator today** |

Prices in `MvpPosPlanCatalog` are **DEVELOPMENT/default PHP placeholders**, not launch pricing.

---

## 10. Master readiness matrix

Legend: **R** = React UI, **B** = Backend HTTP/domain, **E** = runtime enforcement, **T** = E2E covering the commercial spine.

| Capability | React UI | Backend | Enforcement | E2E | Action |
|---|---|---|---|---|---|
| Product list | IMPLEMENTED | AVAILABLE | N/A | Vitest/Playwright read | Keep |
| Product detail | IMPLEMENTED | AVAILABLE | N/A | Playwright read | Keep |
| Product edit (rename) | MISSING | AVAILABLE | N/A | none | PA-COM-02 |
| Product lifecycle | MISSING | AVAILABLE | N/A | none | PA-COM-02 |
| Create product | NOT REQUIRED (UI prohibited) | Testing-only | N/A | — | Do not implement in Admin |
| Plan list | IMPLEMENTED | AVAILABLE | N/A | Playwright read | Keep |
| Plan detail | IMPLEMENTED | AVAILABLE | display only | Playwright read | Keep |
| Plan edit (commercial) | MISSING | AVAILABLE | N/A | none | PA-COM-03 |
| Plan versioning | MISSING | PARTIAL (no version retire HTTP) | N/A | none | PA-COM-03; stop on retire-version UI |
| Trial setup (catalog trials) | MISSING | AVAILABLE | N/A | none | PA-COM-03 |
| Subscription create (paid) | MISSING | AVAILABLE (payment required) | one active-like slot | none from Admin | PA-COM-04 + 06 |
| Start trial | MISSING | AVAILABLE | plan.TrialAllowed | none from Admin | PA-COM-04 |
| Subscribe / PayNow | MISSING | AVAILABLE | payment invariant | none from Admin | PA-COM-04 + 06 |
| Upgrade | MISSING | AVAILABLE | snapshot regen + live plan | none from Admin | PA-COM-04 + 06 |
| Downgrade | MISSING | AVAILABLE (scheduled) | preview conflicts | none from Admin | PA-COM-04 |
| Suspend | MISSING | AVAILABLE | POS entry deny; device APIs still active-like | none from Admin | PA-COM-04 |
| Reactivate | MISSING | AVAILABLE | POS entry restore | none from Admin | PA-COM-04 |
| Cancel | MISSING | AVAILABLE | continuity grants only | none from Admin | PA-COM-04 |
| Billing history | IMPLEMENTED (read) | AVAILABLE | N/A | Playwright read | Keep; extend in PA-COM-06 |
| Payment simulation | MISSING | AVAILABLE (Local Validation; Production 404) | N/A | none | PA-COM-06 |
| Entitlement snapshot | IMPLEMENTED (read) | AVAILABLE generate | composer | none mutate | PA-COM-05 |
| Entitlement reconciliation | MISSING | AVAILABLE | N/A | none | PA-COM-05 |
| Feature override | MISSING | AVAILABLE | override wins in composer | none | PA-COM-05 |
| Device limit | PARTIAL (plan field only) | AVAILABLE | POS + Platform device APIs | unit/integration; Playwright mocked | PA-COM-07 |
| Staff limit | PARTIAL (plan field) | PARTIAL | invite gap | preview tests | PA-COM-07; possible backend package |
| Branch limit | PARTIAL (plan field) | AVAILABLE | branch create | platform tests | PA-COM-07 |
| Business-type limit | PARTIAL (plan field) | AVAILABLE | activation | platform tests | PA-COM-07 |
| Customer credit entitlement | PARTIAL (plan boolean) | AVAILABLE | POS capability | POS integration (Dev-sensitive) | PA-COM-07 |
| Advanced reports entitlement | PARTIAL (plan boolean) | AVAILABLE (catalog) | **no POS gate** | none | document; do not fake UI enforcement |
| Export entitlement | PARTIAL (plan boolean) | AVAILABLE (catalog) | **no POS gate** | none | document; do not fake UI enforcement |
| Ordering entitlement | MISSING on plan DTO | ALWAYS-ON in MVP seed | POS capability | POS tests; not plan-differentiated | do not invent Starter-off without seed change |
| Delivery entitlement | MISSING on plan DTO | ALWAYS-ON in MVP seed | POS capability | POS tests; not plan-differentiated | same |
| Platform→POS introspection | MISSING Admin view | AVAILABLE | POS bearer | POS tests | PA-COM-07 |
| Subscription status enforcement | MISSING Admin actions | AVAILABLE | CanEnterProduct + UtangCapability | not Admin-driven | PA-COM-04 + 07 |
| Cross-org protection | implicit (API 404/403) | AVAILABLE | org-scoped routes | existing API tests | keep fail-closed |
| Audit trail | IMPLEMENTED (org Activity read) | AVAILABLE | server audit actions | Playwright read | PA-COM-08 verify events |

---

## 11. Recommended PA-COM package order

Evidence says **seed Starter/Growth/Pro already exist**. Product lifecycle is not on the critical E2E path. Safer execution order:

```text
PA-COM-01  Commercial contract + React mutation foundation
    → PA-COM-04  Organization subscription lifecycle actions   (uses seed plans)
    → PA-COM-06  Local Validation billing / payment actions    (paid subscribe/upgrade)
    → PA-COM-05  Entitlement generate / reconcile / override UX
    → PA-COM-03  Plan commercial editing + versioning          (optional for seed-based E2E)
    → PA-COM-07  Platform → POS commercial integration verification
    → PA-COM-02  Product lifecycle (existing product only)
    → PA-COM-08  Commercial E2E matrix + hardening
```

**Implement first when authorized:** **PA-COM-01**.  
**Do not start PA-COM-01 in this documentation task.**

Mapping to existing (unimplemented) PWEB specs — reuse contracts, do not duplicate blindly:

| PA-COM | Overlaps |
|---|---|
| 01 | PWEB-20 (done) + new typed clients / permissions |
| 02 | PWEB-IMPL-25 |
| 03 | PWEB-IMPL-26 |
| 04 | PWEB-IMPL-29 (subscription) |
| 05 | PWEB-IMPL-29 (entitlement) |
| 06 | PWEB-IMPL-27 + 28 + Local Validation simulate |
| 07–08 | new |

PWEB-IMPL-21…24 (users/roles/org lifecycle) are **not** prerequisites for commercial E2E if the operator is already a Platform Administrator.

---

## 12. Stale documentation (explicit)

| Document | Why stale relative to this audit |
|---|---|
| `documentation-status.md` header (“Scaffold only”, “STOPPED AFTER PWEB-IMPL-01”) | Historical DOC-01–10 planning; React is well past scaffold |
| `Screens/commercial-and-product-screens.md` “implementation not authorized” | Read-only commercial screens **are** implemented; mutations are not |
| `navigation-registry.md` “Documentation Only — implementation not authorized” | Products/Plans/Org workspace are implemented read-only |
| `SubscriptionEndpoints.cs` class comment (plan changes unsupported) | upgrade/downgrade HTTP exist |
| `docs/engineering/entitlement-state-matrix.md` (reconciled 2026-08-03) | Uses “PastDue within grace” language; canonical enum separates `GracePeriod` vs `PastDue` |
| PWEB-IMPL-08…14 reports saying later tabs “NOT STARTED” | Those tabs were implemented later as read-only |
| FILE-MANIFEST Admin.Web blurb “read-only Organizations list as of PWEB-IMPL-07” | Catalog + full org workspace exist |

Authoritative commercial docs for the next agent:

- [commercial-subscription-implementation-plan.md](../commercial-subscription-implementation-plan.md)
- [commercial-platform-pos-contract.md](../commercial-platform-pos-contract.md)
- [commercial-e2e-validation-matrix.md](../commercial-e2e-validation-matrix.md)

---

## 13. Completeness math

| Slice | Weight | Score | Notes |
|---|---|---|---|
| React inspection surfaces | 0.40 | 0.70 | catalog + org commercial tabs |
| React mutations | 0.60 | 0.00 | no commercial useMutation |
| **Weighted React commercial %** | | **~28–32%** | reported as **~30%** |

Backend availability is high; it does not raise the React completeness figure.

---

## 14. Flags

```
PLATFORM_ADMIN_REACT_COMMERCIAL_AUDIT=COMPLETE
PLATFORM_ADMIN_REACT_COMMERCIAL_IMPLEMENTATION=NOT_STARTED
PLATFORM_POS_COMMERCIAL_E2E=NOT_READY_UNTIL_AUDIT_GAPS_CLOSED
PA_COM_01_AUTHORIZED=NO
PRODUCTION_CUTOVER=NO
```
