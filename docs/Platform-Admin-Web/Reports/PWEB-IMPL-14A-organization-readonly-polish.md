# PWEB-IMPL-14A — Organization workspace read-only polish

**Status:** COMPLETE after validation  
**Branch:** `feat/platform-admin-web-v2`  
**Starting HEAD:** `941ec279933491b52a6d1c2b09e1db325eb8c658`

## Delivered

Polish and repair for read-only organization workspace presentation. No new mutations or backend changes.

### 14A1 — Subscription status normalization

- Shared `organization-subscription-status.ts` helper across Products, Subscription, and Entitlements
- Canonical **Cancelled** spelling with legacy `Canceled` alias
- Consistent status tones (Active success; Trialing/PastDue/GracePeriod/Suspended warning; Cancelled/Expired danger)
- EN and fil-PH labels for Cancelled and Expired

### 14A2 — Entitlement grant visibility

- Grants column and mobile cards show each `featureCode` with Enabled/Disabled badges
- Empty grants array shows truthful **No grants** (not “all disabled”)
- Optional `numericLimit` shown when present

## Explicitly not changed

Backend APIs, DB/migrations, Blazor Admin, POS, PLM, CORS, cookies, CSRF, PWA. Activity/Audit not started.

## Workspace status

| Area | Record |
|---|---|
| Overview | IMPLEMENTED |
| Branches | IMPLEMENTED — READ ONLY |
| People/Memberships | IMPLEMENTED — READ ONLY |
| Products/Access | IMPLEMENTED — READ ONLY |
| Subscription | IMPLEMENTED — READ ONLY |
| Entitlements | IMPLEMENTED — READ ONLY |
| Billing | IMPLEMENTED — READ ONLY |
| Activity/Audit | NOT STARTED |
| CSRF | BLOCKS_FUTURE_MUTATION |
| Social-auth token-in-URL | BLOCKS_CUTOVER |
| Platform Admin | WEB ONLY |
| PWA | NOT PLANNED |

## Commits

| Task | Message | SHA |
|---|---|---|
| 14A1 | `fix(platform-web): normalize organization status presentation` | `1b806a36` |
| 14A2 | `fix(platform-web): show entitlement grant states` | `6ee465ea` |
| 14A3 | `test(platform-web): close organization readonly polish` | (this closeout) |

## Screenshots

`docs/Platform-Admin-Web/Reports/impl-14a-organization-readonly-polish/`

- `01-products-statuses-1440x900-light.png`
- `02-products-statuses-1440x900-dark.png`
- `03-entitlements-grants-1440x900-light.png`
- `04-entitlements-grants-1440x900-dark.png`
- `05-entitlements-grants-375x812.png`
- `06-entitlements-grants-320x800.png`
- `07-subscriptions-statuses-fil-PH.png` (optional)

Visual approval: **AWAITING PRODUCT OWNER + CHATGPT**

Prior impl-10 through impl-14 screenshot folders were not modified.

## Read-only audit

Organization workspace routes remain GET-only from the UI. No new POST/PUT/PATCH/DELETE controls were added in Products, Subscription, Entitlements, or related polish.

## Validation

- `npm run typecheck` — pass
- `npm run lint` — pass
- `npm run format:check` — pass
- `npm run test` — 207 passed
- `npm run build` — pass
- `npm run test:e2e` — 84 passed (includes `visual-14a.spec.ts`)
- `npm run test:e2e:container` — 3 passed (8095 local-validation container)
- CSRF remains `BLOCKS_FUTURE_MUTATION`
- Social-auth token-in-URL remains `BLOCKS_CUTOVER`
- Prior package screenshots were not modified
