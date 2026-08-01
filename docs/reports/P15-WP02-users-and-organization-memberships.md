# P15-WP02 — Users and Organization Memberships (completion)

[Phase 15](../phases/phase-15-ant-design-platform-admin.md) | [Portfolio](../portfolio-progress.md) | [ADR-015](../decisions/ADR-015-antdesign-blazor-platform-admin.md)

## Status

**Complete.** Starting tip `8df3920eb6b37a115e4fc6340ca2be4102927ba5` (= `origin/main` at start). Feature tip `e607a10a8712a5e326e42b3a6bf56a38ac1abe4c`. P15-WP03 not started. Existing Ant navigation and Settings sidebar preserved.

## Delivered capability

### Platform Admin
- Platform Users list/detail (existing Ant Table) with create, profile edit, suspend/reactivate/disable, credentials (no secret/hash/token exposure)
- Organization Users hub `/admin/organization-users` → org picker → members
- Organization members Ant Design surface: members table, role change, suspend/reactivate/revoke, invitations tab, read-only product-access tab
- Membership DTO enriched with username/displayName/email

### Organization Admin
- Members nav enabled for Owner/Administrator membership (trusted selected org)
- Same members/invitations UI scoped by server-side org isolation
- Cannot assign platform-wide roles; cannot assign product-local roles
- OrganizationAdministrator cannot invite/assign OrganizationOwner

### API / domain
- Authz on sensitive user/member/product-access GETs
- `PlatformMembershipAuthz`: ManageMemberships **or** active Owner/Administrator in trusted org context
- Final governing admin protection (last Owner/Admin cannot be suspended/revoked/demoted)
- Organization invitations (R-080 closed): create / list / resend / revoke / accept
- Migration `20260801042150_AddOrganizationInvitations`
- Accept tokens returned once on create/resend only; never listed; never shown in Admin UI
- Accept requires authenticated Platform User whose email matches (single-use; expiry 7 days)

## Routes

| Route | Role |
|---|---|
| `/admin/users`, `/admin/users/{id}` | Platform Users |
| `/admin/organization-users` | Platform org picker |
| `/admin/organizations/{id}/members` | Members + invitations + product-access (read-only) |

## Endpoints added

| Method | Path |
|---|---|
| GET | `/api/v1/platform/organizations/{organizationId}/invitations` |
| POST | `/api/v1/platform/organizations/{organizationId}/invitations` |
| POST | `/api/v1/platform/invitations/{invitationId}/resend` |
| POST | `/api/v1/platform/invitations/{invitationId}/revoke` |
| POST | `/api/v1/platform/invitations/accept` |

Existing membership/user endpoints retained with stronger authz/safety.

## Authorization / isolation

- Platform ManageMemberships (platform-wide or org-scoped role assignment) continues to work
- Org Owner/Admin: only when session trusted `OrganizationId` matches target org
- Cross-org membership mutations remain 403
- Platform user list/detail requires ManagePlatformUsers
- Product-access grant/revoke still ManageProductAccess; org admin may **read** product-access for trusted org

## Invitation behavior

- Pending, Accepted, Revoked, Expired
- Single-use accept; email must match invitee
- Resend rotates token + extends expiry
- No email vendor integration — delivery out of band (residual)

## Safety rules

- Last governing admin protected (`platform.membership.last_governing_admin` → 409)
- Org Admin cannot assign Owner
- Membership/entitlement never grants POS/product-local roles
- Social login still does not auto-grant privileges

## Tests

- Unit: invitation domain transitions; membership guard
- Integration: invitation lifecycle; final-admin revoke denial; existing membership/access flows
- Admin architecture/localization guards updated
- Full Release suite: **1275 passed / 0 failed / 0 skipped** (`dotnet test ExItS.slnx -c Release`, `ASPNETCORE_ENVIRONMENT=Testing`)

## Explicit exclusions / residual gaps

- Email/SMS invitation delivery channel not implemented
- Admin does not display accept tokens (by design)
- Server-side sort parameters for users/members not added (client Sortable remains local)
- Platform role-assignment Admin UI still deferred (API exists; P15-WP06 likely)
- Organization profile CRUD beyond members deferred to P15-WP03
- Formal WCAG certification not claimed

## Files / docs

- Domain/Application/Infrastructure invitations + membership guards
- API: MembershipEndpoints, InvitationEndpoints, Identity/Access GET authz, PlatformMembershipAuthz
- Admin: OrganizationMembers (Ant), OrganizationUsers, nav, ApiClient, resx EN + fil-PH
- Docs: this report, phase 15, portfolio, R-080 closed

## Git / push evidence

| Item | Value |
|---|---|
| Starting commit | `8df3920eb6b37a115e4fc6340ca2be4102927ba5` |
| Feature commit | `e607a10a8712a5e326e42b3a6bf56a38ac1abe4c` |
| Force-push / history rewrite | Not used |
| Exact next WP | P15-WP03 (Organization Lifecycle) or P14-WP03 — only when authorized |
