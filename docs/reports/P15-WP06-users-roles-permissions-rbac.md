# P15-WP06 — Users, Roles, Permissions, and RBAC (completion)

[Phase 15](../phases/phase-15-ant-design-platform-admin.md) | [Portfolio](../portfolio-progress.md) | [ADR-015](../decisions/ADR-015-antdesign-blazor-platform-admin.md)

## Status

**Complete.** Starting tip `78d0e90cc5ec75261e340288e8ccef3f5c6e8951`. Final tip `81a2340d282b70d232213bdee3f6cc3f0b083f15` (feature `2b9657bbb4c0e597c2098ef1a2fa5bb1e630ba52`). P15-WP07 not started.

## User terminology

| Platform Admin view | Meaning |
|---|---|
| All Users | Every Platform identity |
| Unassigned Users | No non-removed organization membership |
| Organization Users | ≥1 Active/Suspended membership |
| Platform Staff | ≥1 active system or custom platform role assignment |
| Roles & Permissions | Platform role definitions + permission catalog |

Organization Admin **People**: Members, Invitations, Roles & Permissions (current org only). Never sees platform-wide directory or platform roles.

## Routes

| Route | Scope |
|---|---|
| `/admin/users` | All Users |
| `/admin/users/unassigned` | Unassigned |
| `/admin/users/organization` | Organization Users |
| `/admin/users/platform-staff` | Platform Staff |
| `/admin/users/{id}` | User detail |
| `/admin/platform-roles` | Platform role list |
| `/admin/platform-roles/{id}` | Platform role detail/lifecycle |
| `/admin/organization-users` | Org picker → members (retained) |
| `/admin/organizations/{id}/members` | Members / invitations |
| `/admin/organizations/{id}/roles` | Custom org roles |
| `/admin/organizations/{id}/roles/{roleId}` | Org role detail |

## Endpoints (additive)

| Method | Path | Authz |
|---|---|---|
| GET | `/users?directory=` | ManagePlatformUsers |
| GET | `/authorization/permissions` | ManagePlatformUsers |
| GET | `/authorization/organization-permissions` | ManageMemberships |
| GET/POST/PUT | `/authorization/role-definitions` (+ activate/deactivate/retire) | ManagePlatformUsers |
| GET/POST | `/authorization/custom-assignments` (+ revoke) | ManagePlatformUsers |
| GET | `/authorization/users/{id}/effective-permissions` | ManagePlatformUsers |
| GET/POST/PUT | `/organizations/{id}/role-definitions` (+ lifecycle) | ManageMemberships or org governing admin |
| GET/POST | `/organizations/{id}/role-assignments` (+ revoke) | same |
| GET | `/organizations/{id}/members/{userId}/effective-permissions` | same |

Existing system role assignment APIs retained. `GET /authorization/roles` now requires ManagePlatformUsers.

## Platform RBAC

- Built-in roles: PlatformAdministrator / BillingAdministrator / PlatformSupport (seeded definitions; assignments still via `PlatformSystemRole`)
- Custom roles: Active → Inactive → Retired; permissions from `PlatformPermission.All` only
- Effective permissions = union of active system assignments + active custom assignments whose definitions are Active
- Last PlatformAdministrator revoke blocked (409)

## Organization RBAC

- Built-in Owner/Admin/Member unchanged on memberships; static `OrganizationPermission` catalog
- Custom org roles are org-scoped; assignments only to active members
- No platform role assignment from org workflows; no product-local roles

## Lifecycle / safety

- No hard delete
- Inactive/retired roles cannot receive new assignments; grant no effective permissions
- Built-in platform roles cannot deactivate/retire or change permissions
- Optimistic concurrency via `expectedVersion` → 409
- Audit on role/assignment mutations

## Tests

- Unit: Authorization filter green; Admin guards updated
- Integration: `ApiRbacAdminTests` (directory, custom role lifecycle, last-admin, org isolation, unknown permission, concurrency)
- Full Release suite: **1295 passed / 0 failed / 0 skipped** (`ASPNETCORE_ENVIRONMENT=Testing`, `dotnet test ExItS.slnx -c Release`)

## Residual gaps

- User detail role-assignment UI is list-oriented via Roles pages + APIs (not a full in-page assignment wizard)
- Org “Invitations” nav deep-links to members page tab host
- Product-local POS role administration remains product-owned (intentionally out of scope)
