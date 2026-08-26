# PWEB-IMPL-22 — Platform Roles + Permission Catalog Management

**Package ID:** PWEB-IMPL-22  
**Title:** Platform Roles + Permission Catalog Management  
**Starting dependency:** PWEB-IMPL-20 PASS  
**Contract classification:** **PROVEN_EXISTING**  
**Implementation:** NOT STARTED (planning only)

## 1. Objective

Implement `/admin/platform-roles` (nav `PWEB-NAV-ROLES-PERMISSIONS`, currently under-development) using the **server-defined** Platform permission catalog and role-definition APIs. No arbitrary permission strings; no organization or product-local permission codes.

## 2. Current repository evidence

- Nav: `/admin/platform-roles` AVAILABLE in registry, UNDER_DEVELOPMENT in React  
- System roles: `GET /api/v1/platform/authorization/roles`  
- Permission catalog: `GET /api/v1/platform/authorization/permissions`  
- Custom role definitions: full list/get/create/update + activate/deactivate/retire  
- Built-in roles protected in domain (`platform.role_definition.built_in_protected`)

## 3. Existing APIs / contracts found

| Operation | Route | Classification |
|---|---|---|
| List system roles + permissions | `GET .../authorization/roles` | PROVEN_EXISTING |
| Permission catalog | `GET .../authorization/permissions` | PROVEN_EXISTING |
| List/get role definitions | `GET .../authorization/role-definitions` (+ `/{id}`) | PROVEN_EXISTING |
| Create custom role | `POST .../authorization/role-definitions` | PROVEN_EXISTING |
| Update role metadata/grants | `PUT .../authorization/role-definitions/{id}` | PROVEN_EXISTING (`ExpectedVersion`) |
| Activate / deactivate / retire | `POST .../role-definitions/{id}/activate|deactivate|retire` | PROVEN_EXISTING |
| Separate `ManagePlatformRoles` permission | — | **MISSING** (gated by `ManagePlatformUsers`) |
| Org permission catalog | `GET .../organization-permissions` | Exists but **out of scope** for this package |

## 4. DTO / lifecycle semantics

- `PlatformRoleDefinitionDto`: `Id`, `Code`, `Name`, `Description`, `Kind` (`BuiltIn`/`Custom`), `Status` (`Active`/`Inactive`/`Retired`), `Permissions`, timestamps, `Version`  
- `PermissionCatalogEntryDto`: `Code`, `Description`, `Area` (`platform`)  
- System roles: `PlatformAdministrator`, `BillingAdministrator`, `PlatformSupport`, `PlatformAuditor`  
- Create body: `Code`, `Name`, `Description`, `Permissions`  
- Update body: `Name`, `Description`, `Permissions`, `ExpectedVersion`  
- Lifecycle body: `ExpectedVersion`, `Reason`  
- Conflicts: `application.role_definition.conflict` (409); invalid transition (409)

## 5. Authorization

`ManagePlatformUsers` for all listed routes. UI + route + API + domain.

## 6. UI / route scope

- `/admin/platform-roles` (+ optional detail sub-route only if needed without inventing nav IDs)  
- Show built-in vs custom clearly; disable forbidden built-in mutations  
- Permission picker limited to catalog codes returned by server

## 7. Mutation behavior

CSRF-safe mutations; optimistic UI forbidden for grants; refresh after success; 409 version conflict → re-fetch and inform operator

## 8. Audit

Server-audited mutations; UI does not invent audit events

## 9. Security / CSRF

PWEB-20 path required for POST/PUT

## 10. Error states

401/403/404/409/400 (`platform.role_definition.*`)

## 11. Concurrency / idempotency

`ExpectedVersion` required on update/lifecycle; surface stale conflicts

## 12. A11y / i18n / responsive

Standard Admin Web expectations; long permission lists wrap safely

## 13. Explicit exclusions

- Organization permission management  
- Product-local / POS / PLM permissions  
- Hard delete of roles if lifecycle forbids (use retire)  
- Invented permission strings  
- Changing built-in permission sets

## 14–17. Change allowances

Backend: none expected; DB: none; POS/PLM/Blazor: unchanged

## 18. Tests required

Catalog read; create custom; update with version; activate/deactivate/retire; built-in protected; CSRF; 403; axe

## 19. Evidence path

`docs/Platform-Admin-Web/Reports/PWEB-IMPL-22-platform-roles-permissions.md`

## 20. Proposed commit message

`feat(platform-web): add platform roles permissions management`

## 21. Stop conditions

`PWEB22_PLATFORM_RBAC_CONTRACT_MISSING`; attempt to invent permissions; built-in protection broken

## 22. Definition of PASS

Roles & permission catalog management works against proven APIs only; built-ins protected; CSRF correct; no org/product-local permission leakage.
