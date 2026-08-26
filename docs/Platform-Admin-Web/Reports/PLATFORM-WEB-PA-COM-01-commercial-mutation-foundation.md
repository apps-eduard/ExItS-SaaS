# PLATFORM-WEB-PA-COM-01 — Commercial Mutation Foundation

**Package:** PA-COM-01
**Status:** COMPLETE (foundation only; awaiting Product Owner / ChatGPT review)
**Application:** `src/Platform/ExItS.Platform.Admin.Web`
**Implementation branch:** `feat/platform-admin-pa-com-01`
**Worktree:** `C:\Users\speed\Desktop\ExItS-SaaS-PlatformWeb-PA-COM-01`
**Starting branch:** `docs/platform-admin-commercial-readiness`
**Starting HEAD:** `ffa97209b269341f762fa776a681cc995fbead4d`
**Implementation commit:** `e871fb80`

This package builds the React Platform Admin **commercial mutation foundation**. It does **not** add subscription lifecycle UI, plan editors, payment simulation forms, or entitlement override screens.

`PA_COM_04_AUTHORIZED=NO`. Do not start PA-COM-04 from this report.

---

## 1. Scope delivered

| Area | Result |
|---|---|
| Permission constants | Added exact `manageCatalog` and `manageProductAccess`; existing commercial permissions unchanged |
| Mutation transport | Reused PWEB-20 `platformRequest` (cookie credentials + antiforgery). No second HTTP stack |
| Typed catalog clients | Plan commercial PATCH, activate/deactivate/retire, rename, create, draft version, publish, upsert draft grant |
| Typed subscription clients | Trial, paid create, from-catalog, upgrade/downgrade/convert/apply-pending, preview GET, suspend/reactivate/cancel/grace/past-due/expire; activate typed as blocked |
| Typed payment clients | Manual create/confirm/reject/void; activate-from-payment; Local Validation simulate gated and hidden from production UI |
| Typed entitlement clients | Generate snapshot, reconcile, create/revoke override |
| Mutation hooks | Plan + subscription + generate-snapshot hooks; `retry: false`; scoped invalidation |
| Commercial UI actions | **None.** Existing read-only Product / Plan / Subscription / Entitlement / Billing pages unchanged |
| Agent 3 auth/runtime files | **Not modified** |
| POS React | **Not modified** |
| Backend domain | **Not modified** |

---

## 2. Permissions

React `PLATFORM_PERMISSIONS` now includes canonical codes from `PlatformPermission.cs`:

| React key | Backend constant | Value |
|---|---|---|
| `manageCatalog` | `ManageCatalog` | `platform.permission.manage_catalog` |
| `manageProductAccess` | `ManageProductAccess` | `platform.permission.manage_product_access` |
| `manageSubscriptions` | `ManageSubscriptions` | `platform.permission.manage_subscriptions` |
| `manageManualPayments` | `ManageManualPayments` | `platform.permission.manage_manual_payments` |
| `manageEntitlementOverrides` | `ManageEntitlementOverrides` | `platform.permission.manage_entitlement_overrides` |
| `viewPortfolio` | `ViewPortfolio` | `platform.permission.view_portfolio` |

No invented permission codes. Tests parse `PlatformPermission.cs` and assert exact string match.

---

## 3. Mutation transport (PWEB-20 reuse)

All commercial mutations call `commercialMutationRequest` → `platformRequest`:

- `credentials: "include"`
- Mutation methods bootstrap `GET /api/v1/platform/antiforgery/token` once, then send `X-XSRF-TOKEN`
- GET remains antiforgery-free
- Errors stay `PlatformApiError` (problem+json, correlation / trace ids)
- In-memory CSRF token only; no session tokens in React commercial state
- Identical in-flight method/path/body share one promise (double-submit protection). After settle, a later retry is a new request (`retry: false` on hooks)

---

## 4. Domain types

Reused existing read DTOs; no duplicate commercial models:

| Concept | Reused type |
|---|---|
| Product | Existing catalog product DTO |
| Plan | `CatalogPlan` |
| PlanVersion | `CatalogPlanVersion` (added to catalog types; mapper shared) |
| FeatureGrant | `CatalogFeatureGrant` / `EntitlementGrant` |
| Subscription | `OrganizationSubscription` |
| SubscriptionStatus | `ORGANIZATION_SUBSCRIPTION_STATUSES` = `Trialing \| Active \| GracePeriod \| PastDue \| Suspended \| Cancelled \| Expired` |
| Payment | `OrganizationPayment` |
| PaymentStatus | `ORGANIZATION_PAYMENT_STATUSES` |
| EntitlementSnapshot | `EntitlementSnapshot` |
| FeatureOverride | `FeatureOverride` (added to entitlement read types) |

**No `NoSubscription` enum.** Absence of a subscription record remains a UI empty state.

Concurrency/idempotency fields carried where the API already has them:

- Plan commercial: `expectedUpdatedAtUtc`
- Subscription transitions: `expectedVersion`, `idempotencyKey` on catalog/upgrade/downgrade/convert bodies
- Entitlement generate: `expectedNextVersion`
- Local Validation simulate: `idempotencyKey` (client-gated; production UI must not call)

Server remains authoritative for plan state, subscription state machine, and payment outcomes.

---

## 5. Query invalidation

Stable roots in `commercial-query-keys.ts` match existing read keys:

| Scope | Key root |
|---|---|
| Product | `catalog-products`, `platform-catalog-products` |
| Plans | `catalog-plans`, `platform-catalog-plans` |
| Organization commercial summary | `organizations/commercial-summary/{orgId}` |
| Organization subscriptions | `organizations/subscriptions/{orgId}` |
| Organization entitlements | `organizations/entitlement-snapshots/{orgId}` |
| Organization billing | `organizations/payments/{orgId}` |
| Organization activity | `organizations/audit/{orgId}` |
| Dashboard subscriptions | `dashboard/subscriptions` |

Plan mutations invalidate catalog keys only. Organization mutations invalidate that org’s commercial keys + dashboard subscriptions. They do **not** invalidate the whole app (users, settings, unrelated dashboards).

Failure paths do not invalidate; cached reads stay intact.

---

## 6. Mutation error model

`classifyCommercialMutationFailure` maps `PlatformApiError` for future UI. Operator-facing text is `problem.detail` / `title`. Stack traces are not exposed.

| HTTP / code | Kind |
|---|---|
| 400 (generic) | `validation` |
| 401 | `session_expired` |
| 403 | `permission_denied` |
| 404 | `not_found` |
| 409 | `conflict` |
| 422 / invalid_transition / ineligible | `domain_rule` |
| `application.payment.required_for_paid_activation` | `payment_required` |

Do not collapse these to a generic “Something went wrong.”

---

## 7. Backend endpoints confirmed

Operator routes typed from existing Platform API (not invented):

**Catalog / plan**

- `PATCH .../catalog/products/{productCode}/plans/{planId}/commercial`
- `POST .../activate` / `deactivate` / `retire`
- `PATCH .../rename`
- `POST .../plans` (create)
- `POST .../versions/draft`
- `POST .../versions/{versionNumber}/publish`
- `PUT .../versions/{versionNumber}/feature-grants/{featureCode}`
- `GET .../versions` (read helper for later packages)

**Subscription**

- `POST .../organizations/{orgId}/subscriptions/trials`
- `POST .../organizations/{orgId}/subscriptions`
- `POST .../from-catalog`
- `POST .../{id}/upgrade` / `downgrade` / `convert-trial` / `apply-pending-plan`
- `GET .../{id}/plan-change-preview`
- `POST /api/v1/platform/subscriptions/{id}/suspend|reactivate|cancel|grace-period|past-due|expire`
- `POST /api/v1/platform/subscriptions/{id}/activate` — **exists, always payment-required**

**Payment**

- `POST /api/v1/platform/payments/manual`
- `POST .../payments/{id}/confirm|reject|void`
- `POST .../payments/{id}/activate-subscription`
- `POST /api/v1/platform/local-validation/payments/simulate` — development/Local Validation only; client refuses unless `localValidationToolsEnabled`

**Entitlement**

- `POST .../entitlements/snapshots`
- `POST .../entitlements/reconcile`
- `POST .../feature-overrides`
- `POST /api/v1/platform/feature-overrides/{id}/revoke`

Register: `src/api/commercial/commercial-backend-gaps.ts`.

---

## 8. Backend gaps confirmed (not fixed)

| Gap | Available? |
|---|---|
| Plan-version retire HTTP | **No** (`PlanVersion.Retire` exists in domain; no version retire MapPost) |
| Draft business-type grants | **No** (draft endpoint forces `businessTypeGrants: null`) |
| Subscription renew operator HTTP | **No** (Local Validation can simulate renewal; no Admin `POST .../renew`) |
| Subscription activate without payment | **No** (`ActivateSubscription` always `application.payment.required_for_paid_activation`) |
| MaxActiveStaff invite enforcement | **No** (`InvitationUseCases` does not check `Plan.MaxActiveStaff`) |
| Entitlement generate / reconcile / override HTTP | **Yes** |

No backend contract bug required a Platform API change. No invented routes.

---

## 9. Agent 1 integration contract (read-only)

Inspected `feat/pos-react-client` reports `POS-COM-INT-01` / `POS-COM-INT-02`. That branch was **not** modified.

| Target | Contract |
|---|---|
| Growth effective device limit | POS reads **3** from Platform plan / entitlement / device capacity APIs |
| Pro upgrade | POS receives increased authoritative capacity from Platform (not a POS-local rewrite) |
| Suspended | POS protected operations blocked (`CanEnter` false) |
| Entitlement refresh | Admin mutations must trigger Platform **Subscription → Entitlement → Introspection**. Platform Admin must **not** call POS APIs to push entitlements |
| Strict POS testing | `CommercialValidation:Strict=true` so Dev grant-merge does not hide real Platform grants |

Platform remains authoritative.

---

## 10. Agent 3 isolation

Not modified:

- `SignInPage.tsx`
- `DevelopmentTestUserTools.tsx`
- `development-tools.ts`
- `env.ts`
- `vite.config.ts`
- `public/config.js`
- Local Validation launch / Tailscale configuration

Local Validation payment simulation **reads** the existing `localValidationToolsEnabled` runtime flag. It does not change how that flag is set.

---

## 11. Tests and quality gates

| Gate | Result |
|---|---|
| Vitest | **PASS** — 51 files, 273 tests |
| Typecheck (`tsc -b`) | **PASS** |
| ESLint | **PASS** |
| Production `vite build` | **PASS** |

Coverage mapped to the package list:

1–3. Permission constants match `PlatformPermission.cs`  
4. 401 unauthorized / 403 permission denied preserved  
5–6. Antiforgery bootstrap + in-memory token reuse  
7. GET remains unaffected  
8–11. Plan / subscription / payment / entitlement mutation serialization  
12–14. 409, 403, domain/payment-required preserved  
15–16. Success invalidates scoped keys; failure does not corrupt cache  
17. DTO reuse (catalog plan + organization subscription mappers)  
18–22. Existing Product / Plan / Organization Subscription / Entitlement / Billing page tests still in the passing suite  
23–26. typecheck / ESLint / Vitest / production build PASS  

No fake PASS. No commercial action buttons added.

---

## 12. Files changed (implementation)

**New**

- `src/api/commercial/commercial-http.ts`
- `src/api/commercial/commercial-errors.ts`
- `src/api/commercial/commercial-query-keys.ts`
- `src/api/commercial/commercial-backend-gaps.ts`
- `src/api/commercial/commercial-mutations.test.ts`
- `src/api/catalog/plan-mutations-client.ts`
- `src/api/subscriptions/subscription-mutations-client.ts`
- `src/api/payments/payment-mutations-client.ts`
- `src/api/entitlements/entitlement-mutations-client.ts`
- `src/features/commercial/use-commercial-mutations.ts`
- `src/features/commercial/use-commercial-mutations.test.tsx`
- `src/api/authorization/platform-permissions.test.ts`

**Extended (read DTO reuse, not duplicate models)**

- `src/api/authorization/authorization-types.ts`
- `src/api/catalog/plan-catalog-types.ts`
- `src/api/catalog/plan-catalog-client.ts`
- `src/api/organizations/subscription-list-query.ts`
- `src/api/organizations/billing-list-query.ts`
- `src/api/organizations/entitlement-list-query.ts`
- `src/api/organizations/organization-client.ts`
- `src/api/platform-http.antiforgery.test.ts` (GET antiforgery-unaffected assertion)

---

## 13. Explicit exclusions

- No Start Trial / Subscribe / Upgrade / Downgrade / Suspend / Reactivate / Cancel buttons
- No plan edit form
- No payment simulation form
- No entitlement override UI
- No merge to `feat/platform-admin-web-v2`, `fix/platform-admin-react-local-access`, `feat/pos-react-client`, or `main`
- No production cutover
- No production payment provider

---

## 14. Next package

**PA-COM-04** (organization subscription lifecycle UI) is the next commercial package **when separately authorized**. It is **not** authorized by PA-COM-01 completion.

---

## 15. Flags

```
PA_COM_01_PERMISSION_FOUNDATION=PASS
PA_COM_01_CSRF_MUTATION_TRANSPORT=PASS
PA_COM_01_TYPED_COMMERCIAL_CLIENTS=PASS
PA_COM_01_MUTATION_HOOKS=PASS

PA_COM_01=AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW
PA_COM_04_AUTHORIZED=NO

AGENT_3_AUTH_RUNTIME_FILES_MODIFIED=NO
POS_REACT_MODIFIED=NO
MERGE_TO_PLATFORM_ADMIN_V2=NO
MERGE_TO_MAIN=NO
PRODUCTION_CUTOVER=NO
```
