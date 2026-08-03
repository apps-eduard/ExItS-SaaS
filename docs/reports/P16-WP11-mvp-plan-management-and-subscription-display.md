# P16-WP11 — MVP Plan management and Subscription display

> **Status:** In Progress (validation)  
> **Phase:** Phase 16 — Implementation Complete, Under Validation  
> **Work package:** P16-WP11  
> **Related:** `docs/architecture/product-catalog-entitlement-and-role-model.md`

---

## Defect / change

Platform Subscriptions table showed technical IDs (Organization GUID, Plan GUID, Product key) as primary values. Plan commercial packaging for Pinoy Business POS lacked Starter / Business / Pro MVP plans and full Plan CRUD (including deactivate / commercial package edit).

---

## Correction

### Plan versus Subscription

- **Plan** — Platform-owned commercial package for a Product (`PlanKey` immutable; statuses Draft / Active / Inactive / Retired).
- **Subscription** — one Organization’s enrollment in a Plan; controls commercial eligibility and entitlement enablement only (does not assign Product roles).

### MVP plans (Pinoy Business POS)

Idempotent seed via `EnsureMvpPosPlans`:

| PlanKey | Display | Limits / features | Trial |
|---|---|---|---|
| `starter` | Starter | 1 branch / 3 staff; credit/reports/export off | configurable |
| `business` | Business | 3 / 15; credit/reports/export on | default 14 days |
| `pro` | Pro | 10 / 50; credit/reports/export on | configurable |

Legacy Local Validation / Start-Business provisional subscriptions remap safely onto **Business**; unused `local-validation-pos` may be retired when idle.

### Plan CRUD

Commercial → Plans: list, create, view, edit commercial package, activate, deactivate, retire. Product picker uses display names. Plan Key is not editable after create. Permission: `ManageCatalog` (Platform-only).

### Subscription display

Columns: Organization, Product, Plan, Subscription Status, Trial Start/End, Paid Through, Renewal Date, Updated UTC, Actions.  
Filters: Organization search, Product dropdown, Plan dropdown, status, trialing only.  
Sort/filter server-side before pagination. Technical IDs in advanced details only.

### Entitlement lifecycle

Trialing/Active → Enabled; Past Due may stay Enabled in grace; Suspended/Cancelled/Expired → Suspended/Disabled per policy. No Product Instance hard-delete on expire/suspend.

---

## Tests

Focused unit + integration coverage for seed idempotency, PlanKey uniqueness/immutability, no hard-delete, retired plan rejection, ManageCatalog authz, display-name enrichment, filters/sort-before-page, duplicate active subscription block, trial entitlement enablement, expired launch deny without data wipe, subscription ≠ roles, cross-org isolation.

---

## Manual Local Validation

Plans: Commercial → Plans shows Starter/Business/Pro once; create Draft → edit → activate → deactivate → retire; Plan Key immutable.  
Subscriptions: ABC/XYZ names; Product `Pinoy Business POS`; Plan display names; filter Business / Trialing; sort Trial End; advanced IDs only in detail; entitlement matches subscription state.

---

## Implementation SHA

`1a06026d71dd65032fc6cfbfab4d524899514066`

## Documentation SHA

`bc030cea32fc587275c92482f611754cf01d0b12`

---

## Status

- Phase 16 — Implementation Complete, Under Validation  
- **P16-WP11 — In Progress**  
- P16-WP12 — Not Started  
