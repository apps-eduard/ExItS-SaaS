# PLATFORM-WEB-PA-COM-05 — Entitlement Operator UI + Feature Override Lifecycle

**Package:** PA-COM-05  
**Status:** COMPLETE (awaiting Product Owner / ChatGPT review)  
**Branch:** `feat/platform-admin-pa-com-05`  
**Worktree:** `C:\Users\speed\Desktop\ExItS-SaaS-PlatformWeb-PA-COM-01`  
**Starting HEAD:** `73530607ac674094130e427be2baf637e031c89b`

`PA_COM_01=APPROVED` · `PA_COM_04=APPROVED` · `PA_COM_06=APPROVED`

---

## 1. Scope delivered

Extended Organization → Entitlements (read-only foundation from PA-COM-04 era) with **operator controls** for Platform commercial access.

| Capability | Result |
|---|---|
| Latest snapshot summary | `GET .../entitlements/snapshots/latest` panel: plan, status, version, timestamps, grant summary + expansion |
| Snapshot history | Existing paginated table/cards preserved under **Snapshot history** |
| Generate snapshot | `POST .../entitlements/snapshots` with `expectedNextVersion` from current server snapshot |
| Reconcile | `POST .../entitlements/reconcile` with required reason |
| Feature override list | `GET .../feature-overrides` with status filter + pagination; expired overrides shown as **Expired** in UI |
| Create override | Catalog feature picker (`GET /catalog/products/{product}/features`); enabled/disabled; reason; optional expiry; numeric limit only when feature metadata supports it |
| Revoke override | `POST .../feature-overrides/{id}/revoke` with destructive confirm + reason |
| Actor identity | **Server-authoritative** — `CreatedByUserId` / `RevokedByUserId` removed from API request bodies; derived from authenticated Platform actor |
| Override → snapshot | Overrides do **not** auto-regenerate snapshots; UI shows reconcile hint after create/revoke |
| Commercial boundary | UI copy states overrides are not POS roles, branch permissions, or product-local auth |
| POS / Agent 3 | **Not modified** |

---

## 2. Permissions (React hide + server enforce)

| Action | Permission |
|---|---|
| Generate / Reconcile | `platform.permission.manage_subscriptions` |
| Create / Revoke override | `platform.permission.manage_entitlement_overrides` |
| Reads | Organization-view authorization |

---

## 3. Effective state model

- **Plan grant** — snapshot grant with `source=Plan` or `Trial`
- **Feature override** — persisted override row (may differ from current snapshot until reconcile)
- **Effective snapshot** — latest reconciled/generated snapshot grants; override effect appears with `source=Override` only after snapshot generation

`OVERRIDE_REQUIRES_RECONCILE=YES`

---

## 4. Backend change (narrow)

`EntitlementEndpoints.cs`: create/revoke override derive actor from `PlatformAuthz.CurrentActor.PlatformUserId`; client-supplied actor GUIDs removed from request contracts.

Feature/product validation remains in `CreateFeatureOverride` use case (`GetByProductAndCodeAsync`).

---

## 5. Quality evidence

| Gate | Result |
|---|---|
| Vitest | 343 passed |
| `npm run typecheck` | PASS |
| `npm run lint` | PASS (0 errors) |
| `npm run build` | PASS |
| Playwright `organization-entitlements.spec.ts` | 6/6 PASS (desktop/tablet/mobile, light/dark, axe) |

Integration tests updated: `ApiEntitlementTests`, `ApiSubscriptionEntitlementAdminTests`, `Phase3CommercialCloseoutTests`.

---

## 6. Explicit exclusions

- PA-COM-06 billing paid activation UI (separate branch)
- PA-ERR-01 error diagnostics merge
- POS React / COM-INT-04
- Auto snapshot on override mutation (backend does not do this)
