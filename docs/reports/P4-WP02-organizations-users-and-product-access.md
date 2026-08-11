# P4-WP02 — Organizations, Users and Product Access

## 1. Status

**Complete.** Platform users, organization memberships, Platform organization roles, product-access assignments, effective commercial access evaluation, PostgreSQL persistence, Admin APIs/UI, tests, and documentation delivered. No nested HealthCare product tree in this repository. Authentication and product-local roles remain out of scope.

| Field | Value |
|---|---|
| Phase | Phase 4 — Platform Admin Expansion |
| Work package | P4-WP02 — Organizations, Users and Product Access |
| Branch | `main` |
| Date | 2026-07-29 |
| Phase marker | `P4-WP02-organizations-users-product-access` |

## 2. Delivered capability

### Platform user model and lifecycle

Persistent `PlatformUser` aggregate with username (normalized, globally unique), display name, email (normalized, globally unique), status (`Active` / `Suspended` / `Deactivated`), suspension metadata, UTC timestamps, and `xmin` concurrency. No password hashes or authentication secrets.

Operations: create, update profile, suspend, reactivate, disable (deactivate), get, list/search.

### Organization membership and roles

Persistent membership with Platform organization roles only:

- `OrganizationOwner`
- `OrganizationAdministrator`
- `OrganizationMember`

Statuses: `Active` / `Suspended` / `Removed`. One current membership per user+organization. Revoking membership cascades revoke of active product-access assignments for that organization. Membership alone grants no product access.

### Product-access assignment and effective access

Explicit `ProductAccessAssignment` rows (commercial entry eligibility only — never Doctor/Nurse/Cashier/etc.).

**Eligibility decision (fail closed):** new grants and effective access require subscription status **Trialing or Active only**. GracePeriod, PastDue, Suspended, Cancelled, and Expired are denied.

`EvaluateEffectiveProductAccess` checks user → organization → membership → assignment → product → subscription → entitlement snapshot and returns `Allowed`/`Denied` with stable reason codes (`user_inactive`, `membership_inactive`, `subscription_ineligible`, `entitlement_missing`, `entitlement_stale`, `entitlement_denied`, etc.).

## 3. Persistence, constraints, and migration

Migration: `AddPlatformUsersMembershipsAndProductAccess`

| Table | Notes |
|---|---|
| `platform.platform_users` | Unique normalized username/email; `xmin` |
| `platform.organization_memberships` | FKs; filtered unique current membership |
| `platform.product_access_assignments` | FKs; filtered unique active assignment; product_code → products.code |

No password/AspNet Identity/HealthCare/POS tables. Apply → rollback to `AddEntitlementSnapshotsAndOverrides` → re-apply validated in integration tests.

## 4. API routes (development-stage, unauthenticated)

### Users

| Method | Path |
|---|---|
| GET/POST | `/api/v1/platform/users` |
| GET/PUT | `/api/v1/platform/users/{userId}` |
| POST | `/api/v1/platform/users/{userId}/suspend\|reactivate\|disable` |

### Memberships

| Method | Path |
|---|---|
| GET/POST | `/api/v1/platform/organizations/{organizationId}/members` |
| GET | `/api/v1/platform/users/{userId}/memberships` |
| PUT | `/api/v1/platform/memberships/{membershipId}/role` |
| POST | `/api/v1/platform/memberships/{membershipId}/suspend\|reactivate\|revoke` |

### Product access

| Method | Path |
|---|---|
| GET/POST | `/api/v1/platform/organizations/{organizationId}/product-access` |
| GET | `/api/v1/platform/users/{userId}/product-access` |
| POST | `/api/v1/platform/product-access/{assignmentId}/revoke` |
| GET | `/api/v1/platform/access/evaluate` |

## 5. Platform Admin UI

| Route | Purpose |
|---|---|
| `/admin/users`, `/admin/users/{userId}` | List/search/create/edit/lifecycle + memberships/access |
| `/admin/organizations/{id}/members` | Add member, change role, suspend/reactivate/revoke |
| `/admin/organizations/{id}/product-access` | Grant/revoke, evaluate effective access |

Responsive native CSS (mobile cards, confirm dialogs, access-state indicator, toasts). Warnings: product access ≠ product-local roles; APIs unauthenticated; server-side auth still required.

## 6. Explicit exclusions

- Login, passwords, JWT, MFA, SSO, Active Directory, AspNet Identity
- Product-local roles/permissions; HealthCare/POS operational users
- Entitlement delivery / messaging / background jobs
- Payment/invoice/gateway changes
- Bulk import, invitations, SCIM
- P4-WP03

## 7. Test evidence

| Suite | Passed |
|---|---:|
| Unit | 210 |
| Architecture | 39 |
| Admin unit | 11 |
| Integration | 71 |
| **Total** | **331** |

## 8. Runtime validation

Isolated API/Admin smoke: create user; duplicate username/email → 409; membership + product-access grant; evaluate Allowed; suspend membership → Denied; revoke history retained; phase marker `P4-WP02-organizations-users-product-access`.

## 9. Authentication and production limitations

APIs and Admin remain **development-stage and unauthenticated**. Do not expose as production. Platform Admin role ≠ product operational authorization. Product-access assignment ≠ completed provisioning or product-local role.

## 10. HealthCare freeze

`/HealthCare/` remains ignored, untracked, outside `ExItS.slnx`, unchanged.

## 11. Exact next work package

**P4-WP03 — Subscriptions, Payments and Trials**

Do not begin until explicitly authorized.

## 12. Commits

| Kind | Message / hash |
|---|---|
| Feature | `feat(admin): manage users memberships and product access` — `6f1cacbdb192aade258714a3e131bd78b2b4e177` |
| Docs | `docs(admin): record P4-WP02 commit hashes` — `fa9582b736cb8c1bba0a0510fb98d32cee306a5a` |
