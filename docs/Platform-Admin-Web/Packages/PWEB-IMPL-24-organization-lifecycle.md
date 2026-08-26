# PWEB-IMPL-24 — Existing Organization Lifecycle Management

**Package ID:** PWEB-IMPL-24  
**Title:** Existing Organization Lifecycle Management  
**Starting dependency:** PWEB-IMPL-20 PASS  
**Contract classification:** **PROVEN_EXISTING**  
**Implementation:** NOT STARTED (planning only)

## 1. Objective

Add authorized mutation controls for **existing** organizations (suspend / reactivate / close and approved profile fields). **Create Organization is PROHIBITED** in Platform Admin.

## 2. Current repository evidence

- Org workspace read-only through PWEB-08…15  
- Lifecycle: `POST .../organizations/{id}/suspend|reactivate|close`  
- Profile/branding PUT endpoints exist  
- `POST .../organizations` returns 403 outside Testing (`RuntimeOrganizationCreationDisabled`)  
- Canonical create: Personal `POST /api/v1/personal/start-business`

## 3. Existing APIs / contracts found

| Operation | Route | Classification |
|---|---|---|
| Suspend | `POST .../organizations/{organizationId}/suspend` | PROVEN_EXISTING |
| Reactivate | `POST .../organizations/{organizationId}/reactivate` | PROVEN_EXISTING |
| Close | `POST .../organizations/{organizationId}/close` | PROVEN_EXISTING |
| Update profile fields | `PUT .../organizations/{organizationId}` | PROVEN_EXISTING |
| Update branding | `PUT .../organizations/{organizationId}/branding` | PROVEN_EXISTING |
| Create organization (Admin) | `POST .../organizations` | PROVEN_PARTIAL / **PROHIBITED in UI** (Testing-only) |

**Statuses:** `OrganizationStatus` = `Active | Suspended | Closed`

## 4. DTO / semantics

- `PlatformOrganizationDto` + profile/branding DTOs as returned today  
- Lifecycle POSTs: no inventing request bodies beyond what endpoints accept (audit current handler for reason fields at implementation time)  
- Subscription/product **impact**: do not invent; document only server-enforced consequences discovered at implementation (STOP if unclear)

## 5. Authorization

`ManageOrganizations` via `EnsureCanManageOrganizationLifecycleAsync` for lifecycle; profile update dual-path (ManageOrganizations or trusted Owner) — Admin UI must use Platform Admin permission path only.

## 6. UI / route scope

- Organization workspace overview (and/or list actions) for existing orgs  
- Strong confirmation for suspend/close  
- **No Create Organization button**

## 7. Mutation behavior

CSRF; confirmations; refresh org detail/commercial summary after success; surface access consequences returned by server only

## 8. Audit

Server audit on lifecycle/profile mutations

## 9. Security / CSRF

PWEB-20

## 10. Error states

401/403/404/400/409 as returned

## 11. Concurrency / idempotency

Follow server; re-fetch on conflict

## 12. A11y / i18n / responsive

Standard

## 13. Explicit exclusions

- **Create Organization**  
- Restoring obsolete create UX  
- POS/PLM operational org data  
- Invented subscription auto-cancel claims without server evidence

## 14–17. Change allowances

Backend none expected; DB none; POS/PLM/Blazor unchanged

## 18. Tests required

Suspend/reactivate/close; no create control; CSRF; 403; refresh; axe

## 19. Evidence path

`docs/Platform-Admin-Web/Reports/PWEB-IMPL-24-organization-lifecycle.md`

## 20. Proposed commit message

`feat(platform-web): add organization lifecycle controls`

## 21. Stop conditions

`PWEB24_ORG_LIFECYCLE_CONTRACT_MISSING`; any Create Organization UI; unclear subscription impact requiring Product Owner decision

## 22. Definition of PASS

Existing-org lifecycle mutations only; Create Organization absent; CSRF correct; architecture rule A preserved.
