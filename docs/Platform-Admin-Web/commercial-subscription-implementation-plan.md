# React Platform Admin — Commercial / Subscription Implementation Plan

**Status:** PA-COM-01 COMPLETE; PA-COM-04 COMPLETE (awaiting Product Owner / ChatGPT review)
**Audit:** [PLATFORM-WEB-COMMERCIAL-READINESS-AUDIT-01](./Reports/PLATFORM-WEB-COMMERCIAL-READINESS-AUDIT-01.md)
**PA-COM-01 report:** [Reports/PLATFORM-WEB-PA-COM-01-commercial-mutation-foundation.md](./Reports/PLATFORM-WEB-PA-COM-01-commercial-mutation-foundation.md)
**PA-COM-04 report:** [Reports/PLATFORM-WEB-PA-COM-04-subscription-lifecycle-ui.md](./Reports/PLATFORM-WEB-PA-COM-04-subscription-lifecycle-ui.md)
**Contract:** [commercial-platform-pos-contract.md](./commercial-platform-pos-contract.md)
**E2E matrix:** [commercial-e2e-validation-matrix.md](./commercial-e2e-validation-matrix.md)
**Audit baseline HEAD:** `525bae3633fb7fde1bbc9b855435a05f5f616c09`
**Implementation started:** YES (PA-COM-01 + PA-COM-04)
**PA-COM-01:** COMPLETE (typed clients + hooks + tests)
**PA-COM-04:** COMPLETE (Organization → Subscription lifecycle UI; acceptance tests PASS)
**PA-COM-06:** **NOT STARTED** / not authorized
PA-COM-02, 03, 05, 07, 08 remain unauthorized. This plan does **not** authorize PA-COM-06.

Target application: `src/Platform/ExItS.Platform.Admin.Web`  
Stack (do not replace): React + TypeScript + Vite, Tailwind, shadcn/ui, Lucide, TanStack Query, TanStack Table, React Hook Form, Zod.  
Do **not** introduce Ant Design Blazor into React.  
Do **not** copy Blazor Admin visuals.  
Do **not** modify POS React in these packages.

---

## 1. Commercial spine (future validation sequence)

```text
Platform Admin
  → configure Product/Plan (or use seed Starter/Growth/Pro)
  → organization chooses/gets plan
  → subscription created (trial or paid)
  → entitlement snapshot generated
  → organization opens POS React
  → product capability/limits enforced
  → device registration validated
  → staff/branch/features validated
  → plan upgrade/downgrade validated
  → subscription suspension validated
  → reactivation validated
  → billing/history/audit verified
```

This is the commercial spine for full ExItS testing. React Admin can now **start a trial and run lifecycle actions** (PA-COM-04). Paid subscribe / paid upgrade / convert-trial still require PA-COM-06. Live Platform→POS proof remains Agent 1 + Local Validation.

---

## 2. Product vs Plan (do not collapse)

| Layer | Meaning | Operational POS data? |
|---|---|---|
| **Product** | `pinoy-business-pos` — Pinoy Business POS | **No.** Catalog identity + status only |
| **Plan** | Starter / Growth / Pro commercial package | **No.** Limits, feature grants, placeholder prices |
| **Subscription** | This organization’s commercial state for that product | No POS tickets/stock |
| **Entitlement snapshot** | Effective grants + limits at a point in time | Consumed by Platform access / POS commercial |
| **POS product-local role** | Owner / Manager / Cashier / Viewer | Separate from subscription |

Do not implement Loan / Pawnshop / BNPL products. Future products may reuse the same catalog APIs by id/code.

MVP seed (`MvpPosPlanCatalog`) — **DEVELOPMENT defaults, not launch pricing**:

| Plan | Branches | Staff | POS devices | Business types | Credit | Adv. reports | Export | Trial |
|---|---|---|---|---|---|---|---|---|
| `starter` | 1 | 3 | 1 | 1 | off | off | off | 14 days |
| `growth` | 3 | 10 | 3 | 3 | on | on | on | 14 days |
| `pro` | 10 | 30 | 10 | 6 | on | on | on | not allowed |

Currency seed: `PHP`. Monthly/annual figures are placeholders (`299/2990`, `699/6990`, `1499/14990`).

`store-customer-ordering` and `store-delivery-orders` are granted on **all** MVP BasicStore plans. Do not document them as Starter-disabled unless seed/grants change.

---

## 3. Architecture rules

| Rule | Requirement |
|---|---|
| A | Platform Admin does **not** create customer organizations |
| B | Platform Admin does **not** create products (Testing-gated API; UI prohibited) |
| C | Platform SaaS billing ≠ POS Cash / Manual GCash / Utang |
| D | Server remains authoritative; UI hiding is convenience |
| E | Reuse PWEB-20 CSRF (`platform-http` mutations + in-memory antiforgery). No ad-hoc `fetch` |
| F | Do not invent FeatureCodes, subscription transitions, payment methods, or providers |
| G | Development/Testing grant-merge and commercial headers are **not** production security |
| H | Do not mark Production Ready / Cutover Authorized |

---

## 4. UX requirements (all mutation packages)

- RHF + Zod for forms; sectioned cards/sheets — no giant forms
- Confirmation for destructive/commercial transitions (suspend, cancel, retire, void)
- Loading + disable duplicate submit
- Normalized Platform problem+json errors; conflict (`409` / `ExpectedUpdatedAtUtc`) refetch
- Success toast/inline feedback
- Query invalidation of list + detail + org commercial-summary + entitlements + billing as relevant
- Accessible dialogs/sheets; desktop-first, tablet usable
- EN + fil-PH keys
- Fail closed on 401/403 without leaking amounts or grants the caller may not see

**Organization workspace relationship (keep):**

```text
Organization
  Overview
  People
  Branches
  Products          ← what THIS org has (not global catalog admin)
  Subscription
  Entitlements
  Billing
  Activity
```

Global `/admin/products` and `/admin/plans` define the catalog. Do not duplicate catalog editors inside the org workspace.

Prefer org-workspace actions for subscription/entitlement/billing. Global `/admin/subscriptions` and `/admin/payments` may remain later convenience lists; they are not required to unblock POS E2E.

---

## 5. Permission model (UI + server)

| Permission | Code | Typical commercial use | Roles (catalog) |
|---|---|---|---|
| View portfolio | `platform.permission.view_portfolio` | Catalog/subscription reads | Admin, Billing, Support, Auditor |
| Manage catalog | `platform.permission.manage_catalog` | Product/plan/feature/trial mutations | PlatformAdministrator **only** among system roles |
| Manage subscriptions | `platform.permission.manage_subscriptions` | Lifecycle + snapshot generate/reconcile | Admin, Billing |
| Manage entitlement overrides | `platform.permission.manage_entitlement_overrides` | Override create/revoke | Admin only (Billing **lacks** this) |
| Manage manual payments | `platform.permission.manage_manual_payments` | SaaS payments | Admin, Billing |
| Manage organizations | `platform.permission.manage_organizations` | Org support (not a substitute for catalog) | Admin, Billing |
| View audit | `platform.permission.view_audit_records` | Activity | Admin, Billing, Support, Auditor |

Org commercial mutations also allow a **trusted organization Owner** on org-scoped routes (`EnsureCanManageOrganizationCommercialAsync`). Platform Admin UI must still send Platform session credentials and fail closed on 403.

React `PLATFORM_PERMISSIONS` now includes `manage_catalog` and `manage_product_access` (PA-COM-01). Org commercial tabs still rely on API 401/403 rather than per-tab permission checks — keep server authoritative; add UI gating as convenience in later packages.

`view_global_catalog` is the **SKU/global product catalog**, not SaaS Products/Plans. Do not gate `/admin/products` on it.

---

## 6. Feature codes (authoritative; do not invent)

Named constants from `FeatureCode.cs`:

**Customer credit:** `customer-credit-view`, `customer-credit-repay`, `customer-credit-create`  
**Basic store:** `store-catalog-view|manage`, `store-sales-view|create|void`, `store-inventory-view|manage`, `store-expenses-view|manage`, `store-dashboard-view`, `store-reports-view`  
**Full POS:** `store-suppliers-*`, `store-shifts-*`, `store-returns-*`, `store-permissions-*`, `store-registers-*`  
**Plan quantity limits:** `plan-max-branches`, `plan-max-active-staff`, `plan-max-active-pos-devices`, `plan-max-active-business-types`  
**Plan booleans:** `store-advanced-reports`, `store-export`, `store-customer-ordering`, `store-delivery-orders`

Arbitrary hyphenated codes can be created via catalog API; React editors must list **existing product feature definitions**, not hardcode a second catalog.

---

## 7. Package index and safer order

IDs are stable. **Execution order differs** from numeric ID order because seed plans already exist.

| Order | ID | Title | Unblocks E2E? |
|---|---|---|---|
| 1 | PA-COM-01 | Commercial contract + React gap-closure foundation | Foundation only |
| 2 | PA-COM-04 | Organization subscription lifecycle actions | Trial / suspend / reactivate / cancel |
| 3 | PA-COM-06 | Local Validation billing / payment actions | Paid subscribe / upgrade |
| 4 | PA-COM-05 | Entitlement lifecycle / reconcile / override UX | Visible grant truth |
| 5 | PA-COM-03 | Plan commercial editing + versioning | Change limits without seed-only |
| 6 | PA-COM-07 | Platform → POS commercial integration verification | True vs Dev-merged grants |
| 7 | PA-COM-02 | Product lifecycle (existing products) | Not on critical POS path |
| 8 | PA-COM-08 | Commercial E2E matrix + hardening | Spine proof |

---

## PA-COM-01 — Commercial contract + React gap-closure foundation

**Status.** COMPLETE — [PLATFORM-WEB-PA-COM-01-commercial-mutation-foundation.md](./Reports/PLATFORM-WEB-PA-COM-01-commercial-mutation-foundation.md)
**Branch.** `feat/platform-admin-pa-com-01`
**Review.** `PA_COM_01=AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW`

**Objective.** Make React Admin able to call proven commercial mutations safely, without shipping lifecycle UI yet.

**Dependencies.** PWEB-IMPL-20 CSRF (COMPLETE). No PWEB-21–24 requirement.

**Delivered**

- `PLATFORM_PERMISSIONS.manageCatalog` / `manageProductAccess` match `PlatformPermission.cs`
- Commercial mutations reuse PWEB-20 `platformRequest` + antiforgery via `commercialMutationRequest`
- Typed catalog, subscription, payment, and entitlement mutation clients for **existing** routes only
- TanStack Query hooks with scoped invalidation and `retry: false`
- Gap register in `src/api/commercial/commercial-backend-gaps.ts`
- No commercial UI actions (no Start Trial / Suspend / plan edit / payment simulation forms)

**Backend changes.** None.

**Acceptance criteria**

- All commercial mutation paths go through `platform-http` + antiforgery — **PASS**
- `ManageCatalog` exists in React permission constants — **PASS**
- No new routes required — **PASS**
- No production payment provider — **PASS**

**STOP gate (historical).** PA-COM-04 was separately authorized (`PA_COM_04_AUTHORIZED=YES`) and implemented on `feat/platform-admin-pa-com-04`. Do **not** start PA-COM-06 until separately authorized. `PA_COM_06_AUTHORIZED=NO`.

---

## PA-COM-02 — Product lifecycle management

**Objective.** Operate **existing** catalog products: rename, activate, deactivate, retire.

**Dependencies.** PA-COM-01. Overlaps PWEB-IMPL-25.

**Files/areas.** `features/products/ProductDetailPage.tsx`, Products list row actions, confirmation dialogs, i18n.

**Backend.** Existing:

- `PATCH /api/v1/platform/catalog/products/{id}/rename`
- `POST .../activate|deactivate|retire`

**React.** Confirmation + `ManageCatalog` gate. **No Create Product.**

**Tests.** Valid transitions `Active↔Inactive`, `→Retired`; Retired has no outbound; 409 concurrency; unauthorized hidden/fail-closed.

**Acceptance.** Operator can retire/deactivate Pinoy Business POS only via server rules; POS operational data untouched.

**STOP.** `PWEB25_PRODUCT_LIFECYCLE_CONTRACT_MISSING` / inventing metadata fields.

**Not on critical E2E path** — POS product is already Active via seed.

---

## PA-COM-03 — Plan commercial editing + plan lifecycle/versioning

**Objective.** Edit Starter/Growth/Pro commercial package fields and manage plan versions/grants using proven APIs.

**Dependencies.** PA-COM-01. PWEB-25 recommended but not required if product remains Active. Overlaps PWEB-IMPL-26.

**Files/areas.** `features/plans/*`, product detail plans section, new version/grants panels.

**Backend (proven)**

- `POST .../products/{productCode}/plans`
- `PATCH .../plans/{planId}/rename`
- `PATCH .../plans/{planId}/commercial` (limits, trial, prices, credit/reports/export toggles)
- `POST .../plans/{planId}/activate|deactivate|retire`
- `GET/POST .../plans/{planId}/versions/draft`
- `PUT .../versions/{n}/feature-grants/{featureCode}`
- `POST .../versions/{n}/publish`
- `GET/POST .../products/{productCode}/features`, `.../trials`

**Backend gaps — do not fake UI**

- Plan version retire HTTP missing
- Draft business-type grants forced null on Admin draft POST

**React.** Sectioned editor: identity, pricing (labeled DEVELOPMENT default), limits, boolean features. Version list: Draft vs Published. Grant editor bound to **product feature definitions**.

**Tests.** Commercial patch round-trip; Draft filter includes `Draft`; cannot invent feature codes; CSRF.

**Acceptance.** Changing Growth `MaxActivePosDevices` persists and is visible on plan detail.

**STOP.** `PWEB26_PLAN_MUTATION_CONTRACT_MISSING`; BT-grant UI without API; retire-version UI without route; finalizing launch prices.

Seed-based E2E can proceed **without** this package if operators accept catalog defaults.

---

## PA-COM-04 — Organization subscription lifecycle actions

**Status.** COMPLETE — [PLATFORM-WEB-PA-COM-04-subscription-lifecycle-ui.md](./Reports/PLATFORM-WEB-PA-COM-04-subscription-lifecycle-ui.md)
**Branch.** `feat/platform-admin-pa-com-04`
**Implementation commit.** `dfcbbe1d`
**Review.** `PA_COM_04=AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW`

**Objective.** From `/admin/organizations/:id/subscription`, perform proven lifecycle transitions.

**Dependencies.** PA-COM-01. Seed plans sufficient. Overlaps PWEB-IMPL-29 subscription half.

**Files/areas.** `OrganizationSubscriptionsPage.tsx`, confirmation sheets, status presentation helpers.

**Backend (proven)** — permission `ManageSubscriptions` (or org Owner on org-scoped routes):

| UI action | Method + path | Resulting status | Audit |
|---|---|---|---|
| Start Free Trial | `POST .../organizations/{id}/subscriptions/trials` | Trialing | `platform.subscription.trial_started` |
| From catalog (trial or pay-now) | `POST .../from-catalog` | Trialing or Active (pay-now may cancel on failed charge) | trial/paid started |
| Paid create | `POST .../subscriptions` + `paymentId` | Active | `platform.subscription.paid_started` |
| Convert trial | `POST .../{id}/convert-trial` | Active | `platform.subscription.trial_converted` |
| Upgrade | `POST .../{id}/upgrade` | same lifecycle; new plan; snapshot regen | `platform.subscription.upgraded` |
| Preview plan change | `GET .../{id}/plan-change-preview` | none | `platform.subscription.plan_change_previewed` |
| Downgrade (schedule) | `POST .../{id}/downgrade` | pending plan | `platform.subscription.downgrade_scheduled` |
| Apply pending | `POST .../{id}/apply-pending-plan` | plan bind | `platform.subscription.pending_plan_applied` |
| Enter grace | `POST /api/v1/platform/subscriptions/{id}/grace-period` | GracePeriod | `...grace_period_entered` |
| Mark past due | `POST .../past-due` | PastDue | `...past_due_marked` |
| Suspend | `POST .../suspend` | Suspended | `...suspended` |
| Reactivate | `POST .../reactivate` | Active | `...reactivated` |
| Cancel | `POST .../cancel` | Cancelled | `...cancelled` |
| Expire | `POST .../expire` | Expired | `...expired` |

**Do not wire** a generic Activate that hits `POST /subscriptions/{id}/activate` — it always fails payment-required.

**Do not wire** Renew until an Admin HTTP exists.

**Preconditions (domain)** — show server errors; do not invent client state machines beyond:

- Trialing → Active, GracePeriod, PastDue, Suspended, Cancelled, Expired
- Active → GracePeriod, PastDue, Suspended, Cancelled, Expired
- GracePeriod → Active, PastDue, Suspended, Cancelled, Expired
- PastDue → Active, GracePeriod, Suspended, Cancelled, Expired
- Suspended → Active, Cancelled, Expired
- Cancelled / Expired → none (new subscription)

Reactivate from GracePeriod/PastDue **requires** a new paid period range.

**Entitlement effect.** Server regenerates snapshots on plan change; composer adjusts credit-create on PastDue/Suspended/Cancelled. Invalidate entitlement queries after success.

**POS effect.** See contract doc. UI copy must not claim device APIs deny on Suspended unless that gap is closed.

**Tests.** Each wired transition; invalid transition 409/domain error; CSRF; no Activate/Cancel for unauthorized; cross-product isolation.

**Acceptance (package tests).** Operator UI can start a trial, change plan (catalog diffs + preview), suspend, reactivate, and cancel using Platform APIs. Vitest / typecheck / ESLint / production build / Playwright subscription spec **PASS**. Live Local Validation login is Agent 3; Platform→POS deny/restore is Agent 1.

**Not delivered.** Paid activation, convert-trial, payment simulation — PA-COM-06.

**STOP.** `PWEB29_SUBSCRIPTION_MUTATION_CONTRACT_MISSING`; payment bypass; inventing NoSubscription status enum; starting PA-COM-06 without authorization.

---

## PA-COM-05 — Entitlement lifecycle / reconciliation / effective-grant UX

**Objective.** Make entitlements an operator tool: generate, reconcile, inspect effective grants, optional override.

**Dependencies.** PA-COM-01; better after PA-COM-04 so there is a live subscription. Overlaps PWEB-IMPL-29 entitlement half.

**Files/areas.** `OrganizationEntitlementsPage.tsx`.

**Backend**

- `POST .../products/{productCode}/entitlements/snapshots` (`ManageSubscriptions`)
- `POST .../reconcile` (`ManageSubscriptions`)
- `GET` latest / paged / by version (already used)
- `POST .../feature-overrides/` + `POST .../feature-overrides/{id}/revoke` (`ManageEntitlementOverrides`)

**React.** Effective grants: featureCode, enabled, numericLimit, source (`Plan|Trial|Override`). Confirm override/revoke. BillingAdministrator must not see override actions as if permitted.

**Tests.** Generate increments snapshot version; reconcile; override precedence; 403 for Billing on overrides.

**Acceptance.** After Growth trial, snapshot shows `plan-max-active-pos-devices` = 3.

**STOP.** Granting POS roles via entitlement; inventing grant sources.

---

## PA-COM-06 — Local Validation billing / payment actions

**Status.** **NOT STARTED.** `PA_COM_06_AUTHORIZED=NO`. Do not start from PA-COM-04.

**Objective.** Separate Platform SaaS money UX from POS tenders. Enable paid subscribe/upgrade in Local Validation.

**Dependencies.** PA-COM-01. Paid paths in PA-COM-04 need this (or equivalent confirmed `paymentId`). Overlaps PWEB-27/28.

**Money boundary (non-negotiable)**

| Money | Owner |
|---|---|
| SaaS subscription / GCash-as-SaaS-channel / bank / cash attestation / Local Validation Online | Platform |
| POS sale Cash / Manual GCash / Utang | POS — never on this screen |

**Manual SaaS methods:** `Cash`, `BankTransfer`, `GCash`. **Not** Cash Deposit / Other. `Online` is provider/Local Validation only — not `CreateManual`.

**SaaSPaymentStatus:** `PendingConfirmation` → `Confirmed` \| `Rejected`; `Confirmed` → `Voided`.

**Local Validation simulations (actual `MapSimulation` strings)**

| Operator label | Request `simulation` | `PaymentProviderResultStatus` |
|---|---|---|
| Succeeded | `succeed` / `success` | Succeeded |
| Declined | `decline` / `declined` | Declined |
| Pending | `pending` | Pending |
| Failed | `fail` / `failed` | Failed |
| Refunded | `refund` / `refunded` | Refunded |
| Renewal succeeded | `renewal-succeed` / `renewal-succeeded` | RenewalSucceeded |
| Renewal failed | `renewal-fail` / `renewal-failed` | RenewalFailed |

Unknown simulation currently maps to **Succeeded** — UI must not send unknown values. Endpoint: `POST /api/v1/platform/local-validation/payments/simulate` — **AllowAnonymous**, **Production 404**. React must double-gate: Vite/runtime Local Validation flag **and** `GET /local-validation/enabled`. Never show in production-shaped nav.

**Manual HTTP:** `POST /payments/manual`, `.../confirm|reject|void`, `.../activate-subscription`. Permission: `ManageManualPayments`.

**React.** Org Billing tab: record + confirm/reject/void + simulate (Local Validation only). Keep Platform GCash visually distinct from POS Manual GCash (copy: “Platform SaaS payment”).

**Tests.** Production nav hides simulate; paid activation without payment fails; confirm+activate links once.

**Acceptance.** Local Validation: simulate Succeeded → paid Growth subscribe possible.

**STOP.** `PWEB27_PAYMENT_CONTRACT_MISSING`; mixing POS tenders; real production gateway.

**Not in this package:** Stripe/PayMongo/production GCash API.

---

## PA-COM-07 — Platform → POS commercial integration verification

**Objective.** Prove the spine against POS React **without modifying POS React in this package**. Record true-enforcement vs Dev-merged-grant behavior.

**Dependencies.** PA-COM-04 (and 06 for upgrade). POS React Device Management already exists on `feat/pos-react-client`.

**Files/areas.** Admin tests + a verification report. Optional Admin “commercial context” read-only panel (subscription status, latest snapshot limits) — no POS code.

**Backend/POS changes.** None unless Product Owner separately authorizes:

- disable grant-merge for a dedicated test profile
- staff invite `MaxActiveStaff` enforcement
- POS gates for `store-advanced-reports` / `store-export`

**Tests.** Documented matrix in `commercial-e2e-validation-matrix.md`. Prefer a non-merging environment for “true plan” proof.

**Acceptance.** Written PASS/FAIL per device-limit and suspend steps. If Dev merge makes feature tests pass spuriously, mark `TEST HARNESS GAP` not PASS.

**STOP.** Changing POS React; describing Dev headers as production security.

---

## PA-COM-08 — Commercial E2E matrix + hardening

**Objective.** Run and record the full spine; close React defects found; no new invented APIs.

**Dependencies.** 01, 04, 06, 05, 07; 03 if limits were edited; 02 optional.

**Acceptance.** Matrix rows completed; audit events verified on Activity; no Production Ready claim.

**STOP.** Weakening tests; claiming cutover.

---

## 8. Relationship to PWEB-IMPL-21…30

| Track | Next package | Purpose |
|---|---|---|
| Identity/governance | PWEB-IMPL-21 (still not authorized by this audit) | Users/sessions/roles |
| Commercial E2E | PA-COM-06 (not authorized) | Paid subscribe / upgrade / convert-trial; PA-COM-04 lifecycle UI is COMPLETE |

They share CSRF. They do not share screens. Either may start first if Product Owner authorizes that ID explicitly.

Reuse PWEB-25…29 specs as contract checklists inside the matching PA-COM package. If HEAD diverges, **HEAD wins**.

---

## 9. Explicit non-goals

- Production payment gateway
- Launch pricing
- Create Organization / Create Product in Admin
- POS or PLM operational UIs
- Invented FeatureCodes or subscription statuses
- Blazor Admin retirement
- Implementation inside this documentation task
