# P16-WP11 Defect Log — Account and user menu terminology and scope

**Status:** Recorded under Validation (P16-WP11 remains In Progress)  
**Date:** 2026-08-02  
**Title:** Account and user menu terminology and scope were inconsistent

## Previous menu names

Platform shell (prior):

- Platform Users
  - All Users
  - Unassigned Users
  - Organization Users
  - Platform Staff
  - Roles & Permissions
  - Organization Memberships

Organization shell (prior):

- People
  - Organization Profile
  - Members
  - Invitations
  - Roles & Permissions

Personal shell (prior account/user related):

- Utang Tracker → People (mixed with Utang product navigation)

## Final Platform Accounts menu

```text
Accounts
  All Accounts          → /admin/users (clickable when ManagePlatformUsers)
  Platform Accounts     → /admin/users/platform-staff
  Organization Accounts → /admin/users/organization
  Personal Accounts     → Coming soon (no route; directory filter not implemented)
  Needs Review          → /admin/users/unassigned
```

Roles & Permissions and Organization Memberships remain under Operations (not Accounts).

## Final Organization People menu

```text
People
  Organization Staff  → /admin/organizations/{id}/members (Owner/Admin)
  Invitations         → members?tab=invitations (Owner/Admin)
  Customers           → Coming soon
  Customer Linking    → Coming soon (Owner/Admin only)
```

Organization Staff see only Customers (Coming soon). Staff directory / Invitations / Customer Linking are hidden for non-Owners.

## Final Personal Contacts menu

```text
Contacts → /admin/personal/utang/people
```

No Accounts or People sections on Personal sessions.

## Implemented versus Coming soon

| Item | State |
|---|---|
| All Accounts | Implemented |
| Platform Accounts | Implemented |
| Organization Accounts | Implemented |
| Personal Accounts | Coming soon |
| Needs Review | Implemented |
| Organization Staff | Implemented |
| Invitations | Implemented |
| Customers | Coming soon |
| Customer Linking | Coming soon |
| Contacts | Implemented |

## Role visibility

| Role / identity (Local Validation catalog) | Account/user menu |
|---|---|
| Olivia Mendoza — Platform Administrator | Full Accounts (5 items; Personal Accounts Coming soon) |
| Daniel Garcia — Platform (no admin role) | No Accounts items (unauthorized hidden) |
| Rafael Torres / Carlo Reyes — Organization Owner | People: Staff, Invitations, Customers, Customer Linking |
| Maria Santos / Ana Cruz — Organization Staff | People: Customers only (Coming soon) |
| Luis Navarro / Sofia Ramos — Personal | Contacts only |

Note: Prompt examples naming Rafael as Platform Support / Maria as ABC Owner differ from the current Local Validation seed. Seed was not changed for this menu fix. Validation uses the catalog above.

## Tests

`tests/ExItS.Platform.Admin.UnitTests/AdminAccountUserNavTests.cs` covers:

1. Platform Administrator five Accounts items  
2. Platform Support / no ManagePlatformUsers → empty Accounts  
3. Organization Owner People items  
4. Organization Staff Owner-only items hidden  
5. Personal Contacts only  
6–8. Cross-scope menu key isolation  
9. Planned items have no route  
10. Unauthorized Accounts hidden  
11. Distinct scope labels (session menu model)  
12. All eight Local Validation identities mapped to expected menus  
13. AdminNav markup terminology guards  

## Manual validation

Run Local Validation Admin login for each of the eight identities and record only the Accounts / People / Contacts section. Confirm terminology, clickable routes, Coming soon (disabled, no route), unauthorized hidden, and no prior-session menu after sign-out / sign-in.

## Phase status

- Phase 16 — Implementation Complete, Under Validation  
- P16-WP11 — In Progress  
- P16-WP12 — Not Started  
