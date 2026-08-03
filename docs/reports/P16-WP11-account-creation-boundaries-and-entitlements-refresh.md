# P16-WP11 — Account creation boundaries and Entitlements refresh stability

> **Status:** In Progress (validation)  
> **Phase:** Phase 16 — Implementation Complete, Under Validation  
> **Work package:** P16-WP11  
> **Related:** `docs/architecture/user-creation-flow-and-account-scope-rules.md`, `docs/architecture/saas-scopes-users-boundaries-navigation.md`

---

## Defects

1. Platform Admin user directory exposed Create on Organization / Personal / All Accounts routes (misleading Platform Staff form).
2. `POST /api/v1/platform/users` allowed identity-only create without `PlatformRole`.
3. Platform Staff create UI could set an initial password.
4. Hard refresh on Commercial → Entitlements caused “Unexpected error”, collapsed nav to Dashboard-only, and required a second refresh.
5. Table sort helper used `ConfigureAwait(false)`, leaving Blazor Server circuit sync context.

---

## Root cause (Entitlements refresh)

Ant Design `RemoteDataSource` fires `OnChange` on first mount after hard refresh. `AdminTableSort.ApplyChangeAsync` awaited reload with `ConfigureAwait(false)`, so the continuation resumed off the Blazor dispatcher and faulted the circuit. Concurrently, `AdminNav` marked ready before `PlatformPermissionState` finished loading, so `CanView` returned false and only Dashboard rendered until a later reload.

---

## Fix

| Area | Change |
|---|---|
| Account create UI | Create Platform Staff only on `/admin/users/platform-staff` |
| Account create API | `PlatformRole` required; 400 if omitted |
| Passwords | No Platform-set initial password; verification issues activation without permanent password |
| AdminTableSort | Stay on Blazor sync context (no `ConfigureAwait(false)`) |
| AdminNav | Await shell + permissions before Platform menu; Spin while loading; `Permissions.Changed` refresh |
| Entitlements | Wait for shell/permissions; scope/unauthorized panels; suppress initial table race; controlled retry on load failure |

### Remaining approved creation flows

- Platform Admin → Platform Staff only  
- Public signup → Personal only  
- Start a Business → Organization Account + Owner membership  
- Organization Owner invite → Organization Account + current-org membership  

### Tables / sorting

Prior content-fit and server-side sort work retained; Entitlements friendly names (ProductDisplayName, OrganizationDisplayName, Revision, local Generated) preserved.

---

## Tests

Focused unit + integration coverage for PlatformRole requirement, Platform-only staff create, create visibility PlatformStaff-only, AdminTableSort sync-context guard, Entitlement list display/sort, account-scope isolation.

---

## Manual validation

- Platform Accounts: Create Platform Staff only  
- Organization / Personal / All Accounts: no Create  
- Entitlements: hard refresh once — no Unexpected error; full nav remains  
- Direct URL and Platform Administrator / Support paths  

---

## Implementation SHA

`c256c51a0ba8189960579a9eb2b971a43e3e2b3c`

---

## Status

- Phase 16 — Implementation Complete, Under Validation  
- **P16-WP11 — In Progress**  
- P16-WP12 — Not Started  
