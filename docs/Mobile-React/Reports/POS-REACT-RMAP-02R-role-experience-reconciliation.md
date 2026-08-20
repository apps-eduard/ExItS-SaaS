# RMAP-02R — Role / experience authority reconciliation

## Status

**COMPLETE**

## Product Owner model (locked)

**DEFAULT POS ROLES:** Owner / Manager / Cashier

| Role | Admin | Operations | Selling |
|------|-------|------------|---------|
| Owner | YES | YES | YES (by POS capability) |
| Manager (StoreManager) | NO by default | YES (strong ops) | YES if CreateSale/EnterPos |
| Cashier | NO | limited ops | YES per capability |

Experience selection does **not** mutate security role.

**OrganizationAdministrator** = explicit admin-side authority; **not** synonymous with POS Manager.

**StoreManager** = POS operations; **not** automatic Organization Web administration.

Future custom admin/operation roles: deferred, capability-driven.

Legacy `Admin` / `InventoryStaff` / `ReportingUser`: preserved for compatibility; not default UX roles; no migration.

## Implementation summary

### Organization Web

- Split conflated `IsOrgManager` into `HasOrganizationManagementAuthority`, `IsOrganizationAdministrator`, `IsPosOperationsManager`.
- `CanAccessOrganizationWeb` denies StoreManager/Manager alone; allows Owner/Administrator; keeps limited legacy POS roles; removes capability fallback that re-admitted StoreManager.

### React

- Experience helpers in `pos-capabilities.ts` (admin / operations / selling / invite).
- Route guards: `RequireAdminExperience`, `RequireInviteStaff`, role-home eligibility.
- Owner experience chooser: Manage business / Operations / Start selling (security role label unchanged).
- Staff invite gated on OrganizationOwner membership (matches Platform `CanManageOrganizationStaff`).

### Tests

- Org Web unit tests: StoreManager alone denied admin host.
- RMAP-01b: Owner invite success; Manager/Cashier invite denied (Cashier false-positive removed).
- RMAP-02R Playwright suite + responsive Owner chooser viewports.

## Reconciliation note

RMAP-01b and RMAP-02 remain PASS **subject to this RMAP-02R evidence**. Prior Cashier-mocked invite success was incorrect under the locked Product Owner decision and is repaired here.

## Next

**RMAP-03** — Branch / device operational context

Do **not** mark RMAP-03 started in this package.
