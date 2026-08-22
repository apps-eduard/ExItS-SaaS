# PA-OPS-03 — Privacy Compliance React UI Migration

## Status

Complete on `feat/platform-admin-system-health` (Agent 4 / PA-OPS-03). Not merged to `main`.

## Delivered

Migrated the working Blazor Privacy Compliance Admin page family to React Admin (`ExItS.Platform.Admin.Web`) using existing Platform API endpoints and Blazor client filter semantics. UI only — no backend business-logic or regulatory derivation changes.

### Routes

| Route | Page |
| --- | --- |
| `/admin/privacy-compliance` | Overview (readiness, metrics, category readiness, PIA follow-ups, status breakdown, quick links, important gaps) |
| `/admin/privacy-compliance/documents` | Documents (+ category filter) |
| `/admin/privacy-compliance/systems` | Processing systems |
| `/admin/privacy-compliance/evidence` | Aggregated evidence (+ requirement filter) |
| `/admin/privacy-compliance/pias` | PIA category |
| `/admin/privacy-compliance/data-inventory` | Data inventory |
| `/admin/privacy-compliance/retention` | Retention |
| `/admin/privacy-compliance/incidents` | Incidents |
| `/admin/privacy-compliance/vendors` | Vendors |
| `/admin/privacy-compliance/dpo-npc` | DPO / NPC |

### Preserved

- `platform.permission.view_privacy_compliance` fail-closed (missing permission → Shell Not Found / Forbidden)
- Readiness banner + disclaimer / no-certification claim wording
- Category readiness, important gaps, PIA follow-ups, evidence coverage
- Blazor `PrivacyComplianceFilters` client matching (documents / PIA / DPO-NPC / gaps)
- Requirement detail drawer (view + PDF export link + evidence deep-link)
- Loading → skeleton; success/empty → Empty copy; API/5xx → ErrorState + Retry + Copy Error Details
- Never maps failure to fake empty / zero / ready

### Platform API reused (no new endpoints)

- `GET /api/v1/platform/privacy-compliance/overview`
- `GET /api/v1/platform/privacy-compliance/requirements`
- `GET /api/v1/platform/privacy-compliance/requirements/{id}`
- `GET /api/v1/platform/privacy-compliance/requirements/{id}/evidence`
- `GET /api/v1/platform/privacy-compliance/systems`
- `GET /api/v1/platform/privacy-compliance/requirements/{id}/export.pdf` (drawer link)

### Explicit exclusions

- Manage mutations (status/details/ensure-catalog/add evidence) remain Blazor/API-only for this package
- Agent 2 commercial/subscription, Agent 3 Global Catalog, POS, System Health backend, auth architecture, `main` merge

## Validation

- `npm test` — 59 files / 334 passed
- `npm run typecheck` — pass
- `npm run lint` — pass (0 errors)
- `npm run build` — pass
- `npx playwright test e2e/privacy-compliance.spec.ts` — 2 passed

## Git

- Starting HEAD: `5fd2addd6546e8808404fa1b58f2447926207f0b`
- Implementation commit: *(filled after commit)*
- Final HEAD: *(filled after docs hash commit if any)*
- Merge to main: **NO**
