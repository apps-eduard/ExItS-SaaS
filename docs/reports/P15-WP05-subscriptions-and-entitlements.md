# P15-WP05 — Subscriptions and Product Entitlements (completion)

[Phase 15](../phases/phase-15-ant-design-platform-admin.md) | [Portfolio](../portfolio-progress.md) | [ADR-015](../decisions/ADR-015-antdesign-blazor-platform-admin.md)

## Status

**Complete.** Starting tip `06f73d07f4696fe10e4567c3b00f4056e12e06e4`. Final tip recorded after push. P15-WP06 not started.

## Domain decisions (audit-first)

| Topic | Decision |
|---|---|
| Subscription statuses | Unchanged: Trialing / Active / GracePeriod / PastDue / Suspended / Cancelled / Expired |
| Create paid | Domain `Subscription.ActivatePaid` exposed via `ActivatePaidSubscription` + `POST …/organizations/{id}/subscriptions` |
| Change plan/version | **Not invented.** Aggregate `PlanId`/`PlanVersionId` remain immutable; terminate then create a new subscription. Historical entitlement snapshots retain prior plan references. |
| Entitlement mutations | Snapshot generate/reconcile gated by `ManageSubscriptions`; feature overrides remain `ManageEntitlementOverrides` |
| SaaS billing | Existing `SaaSPayment` manual Cash/BankTransfer/GCash only — no gateway, no POS money |

## Routes

| Route | Capability |
|---|---|
| `/admin/subscriptions` | List/search/filter + start trial / create paid |
| `/admin/subscriptions/{id}` | Detail + lifecycle + related payments/entitlement |
| `/admin/entitlements` | Latest entitlement summaries |
| `/admin/entitlements/{id}` | Snapshot detail, overrides, generate/reconcile |
| `/admin/entitlements/history/{org}/{product}` | Snapshot history |
| Org shell Current Plan / My Products | Deep-link to existing org detail / product-access (read-only for Org Admin) |

## Endpoints

| Method | Path | Authz |
|---|---|---|
| GET | `/api/v1/platform/subscriptions` | ViewPortfolio (search/filter/sort/page/org/product/status/trial/plan) |
| GET | `/api/v1/platform/subscriptions/{id}` | Org view (platform or trusted membership) |
| POST | lifecycle activate/grace/past-due/suspend/reactivate/cancel/expire | ManageSubscriptions (+ optional `expectedVersion`) |
| GET | `/organizations/{id}/subscriptions` (+ current) | EnsureCanViewOrganization |
| POST | `/organizations/{id}/subscriptions` | ManageSubscriptions — paid create |
| POST | `/organizations/{id}/subscriptions/trials` | ManageSubscriptions |
| POST | `…/entitlements/snapshots` + reconcile | ManageSubscriptions + audit |
| GET | entitlement/override reads (org-scoped) | EnsureCanViewOrganization |
| POST | feature-overrides create/revoke | ManageEntitlementOverrides (existing) |

## Subscription capabilities

- Platform Admin: list/search/filter/sort/page; create trial or paid; activate/suspend/resume/cancel/expire/grace/past-due; concurrency via `expectedVersion`; audit; related payment/entitlement summaries in UI
- Inactive/retired plan or inactive product / non-active org → 409 on new subscription
- Duplicate active-like per org+product → 409
- No hard delete

## Entitlement capabilities

- Generate/reconcile snapshots (ManageSubscriptions)
- Create/revoke feature overrides with duplicate-active conflict (ManageEntitlementOverrides)
- Org Admin: read own org entitlement/subscription surfaces only; no mutate
- Entitlement does not grant POS Owner/Manager/Cashier/Viewer

## Lifecycle rules

- Reuse existing domain transitions only
- Suspension preserves history; cancel/expire terminal
- Closed/suspended org cannot start new subscription
- Plan change deferred (no domain ChangePlan)

## RBAC / isolation

- Platform Admin / Billing Admin manage subscriptions per existing matrix
- Org Admin: own-org read OK; platform-wide list and mutate → 403
- Menu visibility is not authorization

## Billing boundary

- Platform SaaS subscription fees/trials/manual payments only
- No POS sales, drawers, customer balances, or gateway

## Tests

- Integration: paid create, list/search, concurrency, retired plan / closed org blocks, Org Admin isolation, override duplicate/revoke, existing subscription/entitlement suites
- Admin Ant Design guards for Subscriptions/Entitlements
- Full Release suite: **1292 passed / 0 failed / 0 skipped** (`ASPNETCORE_ENVIRONMENT=Testing`, `dotnet test ExItS.slnx -c Release`)

## Residual gaps

- In-place change plan/version not in domain (terminate + recreate)
- Payments Admin page still Report\* shell (not this WP’s primary surface)
- Dedicated Org Admin “current plan” page deferred — uses org commercial tabs
- External payment gateway out of scope
