# PWEB-IMPL-23 — Platform User Role Assignments

**Package ID:** PWEB-IMPL-23  
**Title:** Platform User Role Assignments  
**Starting dependency:** PWEB-IMPL-21 + PWEB-IMPL-22  
**Contract classification:** **PROVEN_EXISTING**  
**Implementation:** NOT STARTED (planning only)

## 1. Objective

Allow authorized operators to **assign and revoke** Platform system roles and custom role definitions on the user detail / authorization surface, without creating organization membership, product access, product-local roles, or subscription entitlements.

## 2. Current repository evidence

- PWEB-17 lists assignments read-only via `GET .../authorization/assignments?platformUserId=`  
- Assign/revoke APIs exist for system and custom roles  
- Last platform-wide `PlatformAdministrator` revoke blocked (409)  
- Self-revoke protection: **MISSING** (SEMANTICS UNRESOLVED for UI policy)

## 3. Existing APIs / contracts found

| Operation | Route | Classification |
|---|---|---|
| List system assignments | `GET .../authorization/assignments` | PROVEN_EXISTING |
| Assign system role | `POST .../authorization/assignments` | PROVEN_EXISTING |
| Revoke system role | `POST .../authorization/assignments/{id}/revoke` | PROVEN_EXISTING |
| List custom assignments | `GET .../authorization/custom-assignments` | PROVEN_EXISTING |
| Assign custom role | `POST .../authorization/custom-assignments` | PROVEN_EXISTING |
| Revoke custom role | `POST .../authorization/custom-assignments/{id}/revoke` | PROVEN_EXISTING |
| Effective permissions | `GET .../authorization/users/{userId}/effective-permissions` | PROVEN_EXISTING |

**Bodies:** assign `{ PlatformUserId, Role, OrganizationId?, Reason }` or `{ PlatformUserId, RoleDefinitionId, Reason }`; revoke `{ Reason }`.

## 4. DTO / semantics

- `PlatformRoleAssignmentDto` / custom assignment DTO with Active/Revoked status and grant/revoke metadata  
- Conflicts: `application.role_assignment.conflict`, `application.custom_role_assignment.conflict`  
- Last admin: `application.role_assignment.last_platform_administrator`  
- Invalid role: `platform.role_assignment.role.invalid`  
- Custom not assignable / built-in protected codes as returned by server

**Multiple active roles:** allowed as returned by server; UI must not invent exclusivity rules.

## 5. Authorization

`ManagePlatformUsers`. Fail closed.

## 6. UI / route scope

- Extend `/admin/users/:userId` authorization section  
- Assign dialog from server role lists / assignable custom definitions only  
- Revoke with reason confirmation  
- Optional effective-permissions refresh panel using proven GET

## 7. Mutation behavior

CSRF; busy state; refresh assignments + effective permissions after success; never imply org membership created

## 8. Audit

Server audit on assign/revoke; reason required where API requires it

## 9. Security / CSRF

PWEB-20 mutation path

## 10. Error states

401/403/404/409/400 as returned

## 11. Concurrency / idempotency

Treat conflict responses as authoritative; re-fetch

## 12. A11y / i18n / responsive

Standard

## 13. Explicit exclusions

- Creating Organization membership  
- Product access assignment  
- POS/PLM product-local roles  
- Entitlement grants  
- Claiming self-final-admin protection as server-enforced until proven

## 14–17. Change allowances

Backend: none expected (optional self-guard only if Product Owner authorizes); DB none; POS/PLM/Blazor unchanged

## 18. Tests required

Assign/revoke system + custom; last-admin revoke 409; conflict; CSRF; effective permission refresh; exclusions asserted

## 19. Evidence path

`docs/Platform-Admin-Web/Reports/PWEB-IMPL-23-platform-user-role-assignments.md`

## 20. Proposed commit message

`feat(platform-web): add platform user role assignments`

## 21. Stop conditions

`PWEB23_ASSIGNMENT_CONTRACT_MISSING`; UI that mutates org/product access under this package

## 22. Definition of PASS

Assign/revoke works on proven contracts; last-admin revoke protected; no cross-domain side effects claimed; CSRF correct.
