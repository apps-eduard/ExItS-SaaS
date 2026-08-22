# PLATFORM-WEB-PA-COM-04 — Organization Subscription Lifecycle UI

**Package:** PA-COM-04
**Status:** COMPLETE (awaiting Product Owner / ChatGPT review)
**Application:** `src/Platform/ExItS.Platform.Admin.Web`
**Implementation branch:** `feat/platform-admin-pa-com-04`
**Worktree:** `C:\Users\speed\Desktop\ExItS-SaaS-PlatformWeb-PA-COM-01`
**Starting branch:** `feat/platform-admin-pa-com-01`
**Starting HEAD:** `e2b34770bb293033518d09e08a0c5b1087b32af6`
**Implementation commit:** `dfcbbe1d8d89a3d62a6887e7e70de2e2e3a0e093`

`PA_COM_01=APPROVED` (authorized by Product Owner for this follow-on).
`PA_COM_04_AUTHORIZED=YES`.
Do **not** start PA-COM-06 from this report.

---

## 1. Scope delivered

Make Organization → Subscription operational for **currently supported** Platform subscription lifecycle actions using PA-COM-01 typed clients.

| Area | Result |
|---|---|
| Commercial summary | Compact cards per subscription: product, plan, status (label + tone), trial end, current/paid period end, pending plan, catalog POS device limit. Pinoy Business POS badge. No entitlement IDs |
| No-subscription UX | Empty state when the organization has no Pinoy Business POS subscription **record**. Start trial when `manage_subscriptions` and catalog `trialAllowed` |
| Start trial | Eligible active catalog plans + published version + active trial definition → confirm → `useStartTrialMutation` |
| Plan change | Change plan dialog: catalog diffs (devices first), preview GET, upgrade vs scheduled-downgrade copy, apply pending when `pendingPlanId` exists |
| Suspend / Reactivate / Cancel | Confirmation dialogs; Reactivate only for **Suspended**; Cancelled/Expired do not show Reactivate |
| Support actions | Grace / past due / expire under **Support actions** (not primary buttons) |
| Activate | **Not exposed.** `POST .../activate` remains payment-required (PA-COM-06) |
| Convert trial | **Not exposed.** Requires payment (PA-COM-06) |
| Paid create / pay-now | **Not exposed.** PA-COM-06 |
| Agent 3 auth/runtime files | **Not modified** |
| POS React | **Not modified** |
| Backend domain | **Not modified** |
| Platform → POS direct calls | **None** |

---

## 2. Backend transition matrix (UI derives from this)

Canonical statuses: `Trialing | Active | GracePeriod | PastDue | Suspended | Cancelled | Expired`. There is **no** `NoSubscription` enum. No subscription = no record.

| From | Primary UI | Support actions | Plan change |
|---|---|---|---|
| *(no POS record)* | Start trial (if eligible) | — | — |
| Trialing | Change plan, Suspend, Cancel | Grace, Past due, Expire | Yes |
| Active | Change plan, Suspend, Cancel | Grace, Past due, Expire | Yes |
| GracePeriod | Suspend, Cancel | Past due, Expire | No |
| PastDue | Suspend, Cancel | Grace, Expire | No |
| Suspended | Reactivate, Cancel | Expire | No |
| Cancelled / Expired | none | none | No |

Reactivate from GracePeriod/PastDue requires a new paid period range and is **not** a primary PA-COM-04 control.

Server authorization remains authoritative. React hiding is convenience.

---

## 3. Start trial

1. Operator opens Organization → Subscription.
2. If no Pinoy Business POS subscription and `platform.permission.manage_subscriptions` and at least one Active catalog plan with `trialAllowed`.
3. Dialog: choose eligible plan (catalog data, not hardcoded IDs). Device limit shown from catalog.
4. Confirm disabled until a **Published** plan version and **Active** trial definition exist.
5. `POST .../organizations/{orgId}/subscriptions/trials` with `{ planId, planVersionId, trialDefinitionId }`.
6. Success invalidates organization subscriptions, commercial summary, entitlements, billing, activity, dashboard subscription summary.

Backend remains authoritative for eligibility.

---

## 4. Upgrade / downgrade

- Eligible plans come from `GET .../catalog/products/{code}/plans` (`status === Active`, excluding current).
- Direction from catalog rank: `maxActivePosDevices`, then monthly price, then `sortOrder`.
- Device-limit difference is the primary line (`Growth 3 → Pro 10` when those are the catalog values).
- Preview: `GET .../plan-change-preview`. Blocking usage conflicts disable confirm.
- Upgrade: `POST .../upgrade` (Trialing skips payment; Active paid upgrade returns payment-required → operator-facing PA-COM-06 copy; **no Activate button**).
- Downgrade: `POST .../downgrade` and copy states the change is **scheduled**, not immediate.
- Apply pending: shown when `pendingPlanId` exists on Trialing/Active.

---

## 5. Suspend / Reactivate / Cancel

| Action | Copy constraint |
|---|---|
| Suspend | Protected POS operations may be blocked. Data is **not** deleted |
| Reactivate | Restores the **existing** subscription. Does not create a new org/product identity. Hidden for Cancelled/Expired |
| Cancel | Access may end per Platform policy. Historical organization/POS data is **not** deleted |

Platform Admin does not call POS APIs. Downstream POS deny/restore is Agent 1.

---

## 6. Permissions and errors

Required mutation permission: `platform.permission.manage_subscriptions`.

Read-only `view_portfolio` operators see summary + list, no mutation controls.

Failures use `classifyCommercialMutationFailure`:

- permission denied (403)
- conflict / stale version (409)
- invalid transition / trial ineligible (domain)
- payment required
- validation / network / unknown

No stack traces. Duplicate submit: confirm `disabled` while pending + PA-COM-01 in-flight dedupe. Retry is explicit user action (`retry: false`).

---

## 7. Query invalidation

PA-COM-01 `invalidateCommercialQueries` with `organizationId`:

- organization subscriptions
- commercial summary
- entitlements
- billing/payments
- organization activity
- dashboard subscription summary

Does not invalidate users, settings, or the full query cache.

---

## 8. Tests and quality gates

| Gate | Result |
|---|---|
| Vitest | **PASS** — 53 files, 301 tests |
| Typecheck | **PASS** (`tsc -b`) |
| ESLint | **PASS** |
| Production build | **PASS** (`vite build`; existing >500 kB chunk warning) |
| Playwright `e2e/organization-subscriptions.spec.ts` | **PASS** — 3 tests (empty/axe, filters, retry) |

Covered scenarios include: empty POS + Start trial with/without permission; trial success/failure; Trialing actions; plan preview device limits from catalog; upgrade submit; scheduled downgrade copy; suspend confirmation + reactivate refresh; Cancelled has no Reactivate; support actions under menu; no Activate; 403/409/invalid transition; duplicate submit disabled; read-only operator; filters remain; Entitlements/Billing/Activity pages still pass; narrow viewport; dialog title/description/Escape.

---

## 9. Local Validation preparation

Agent 3 runtime/auth files were not modified. Once Agent 3 Local Validation login works, the intended operator path is:

```text
Platform Admin login
  → organization
  → Subscription
  → Start trial
  → Change plan (upgrade while Trialing)
  → Suspend
  → Reactivate
```

Uses normal Platform APIs. No fake admin path.

---

## 10. Known gaps

- Paid activation, convert-trial, and payment management belong to **PA-COM-06** (not started).
- Active paid upgrade is offered; if the Platform returns payment-required, UI explains billing is required — it does not fake activation.
- Catalog diffs for a **second** non-POS product load only the first extra `productCode` (POS critical path is complete).
- Lifecycle summary uses an unfiltered first page of subscriptions (page size 20).
- Live Local Validation + Agent 1 Platform→POS proof is out of this package.
- Plan-version retire HTTP and Admin renew remain backend gaps from PA-COM-01.

---

## 11. Files changed

**New**

- `src/api/catalog/trial-catalog-client.ts`
- `src/components/exits/ConfirmActionDialog.tsx`
- `src/features/organizations/OrganizationSubscriptionLifecycle.tsx`
- `src/features/organizations/OrganizationSubscriptionLifecycle.test.tsx`
- `src/features/organizations/subscription-lifecycle.ts`
- `src/features/organizations/subscription-lifecycle.test.ts`
- `src/features/organizations/commercial-mutation-feedback.ts`
- `docs/Platform-Admin-Web/Reports/PLATFORM-WEB-PA-COM-04-subscription-lifecycle-ui.md`

**Extended**

- `src/features/organizations/OrganizationSubscriptionsPage.tsx`
- `src/features/organizations/OrganizationSubscriptionsPage.test.tsx`
- `src/features/commercial/use-commercial-mutations.ts`
- `src/features/catalog/use-catalog-detail-queries.ts`
- `src/api/subscriptions/subscription-mutations-client.ts`
- `src/lib/i18n/messages.ts`
- `src/test/auth-fixtures.ts`
- `e2e/organization-subscriptions.spec.ts`
- `docs/Platform-Admin-Web/commercial-subscription-implementation-plan.md`
- `docs/Platform-Admin-Web/commercial-e2e-validation-matrix.md`
- `docs/Platform-Admin-Web/README.md`
- `docs/Platform-Admin-Web/documentation-status.md`
- `docs/Platform-Admin-Web/implementation-status.md`

**Not modified (Agent 3 / POS)**

- `SignInPage.tsx`, `DevelopmentTestUserTools.tsx`, `development-tools.ts`, `env.ts`, `vite.config.ts`, `public/config.js`
- POS React

---

## 12. Flags

```text
PA_COM_01=APPROVED
PA_COM_04=AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW
START_TRIAL_UI=PASS
PLAN_CHANGE_UI=PASS
SUSPEND_UI=PASS
REACTIVATE_UI=PASS
CANCEL_UI=PASS
PAID_ACTIVATION_UI=NOT_IN_SCOPE_PA_COM_06
PAYMENT_MANAGEMENT_UI=NOT_IN_SCOPE_PA_COM_06
PLATFORM_TO_POS_DIRECT_CALLS=NO
AGENT_3_RUNTIME_FILES_MODIFIED=NO
POS_REACT_MODIFIED=NO
PA_COM_06_AUTHORIZED=NO
MERGE_TO_PLATFORM_ADMIN_V2=NO
MERGE_TO_MAIN=NO
PRODUCTION_CUTOVER=NO
```

**HARD STOP.** Do not start PA-COM-06.
