# P19 — Organization-Scoped Staff Identities

| Field | Value |
|---|---|
| Status | **Code Complete** · Physical device validation **Not performed** |
| Phase | [Phase 19](../phases/phase-19-mobile-pos-operations-and-cashier-experience.md) — Open |
| Related | [P16-WP11 platform and organization staff creation](P16-WP11-platform-and-organization-staff-creation.md), [P16-WP07 staff/customer separation](P16-WP07-organization-staff-customer-separation.md), [P19-offline-operability-foundation](P19-offline-operability-foundation.md) |
| Architecture | [User creation flow and account scope rules](../architecture/user-creation-flow-and-account-scope-rules.md) §9.1 |
| Device Verified | **No** |
| Production Ready | **No** |
| Date | 2026-08-08 |

## 1. Problem

Organization staff previously shared a global PlatformUser keyed by personal email. That made multi-employer employment, removal isolation, and login UX ambiguous (staff had to pick an organization after login). Personal/Owner identity must remain separate from employment identity.

## 2. Identity model

| Concept | Login key (`NormalizedEmail`) | Contact | Home org |
|---|---|---|---|
| Personal / Owner | `maria@gmail.com` | (same / null) | null |
| Org A staff | `maria@org001842` (display `maria@ORG001842`) | `maria@gmail.com` | Org A |
| Org B staff | `maria@org004911` | `maria@gmail.com` | Org B |

Each staff login is a **separate PlatformUser** permanently scoped to one organization (`HomeOrganizationId`). Contact email is for invitation/recovery only and is **not** unique and **not** an authorization key.

Public organization id `ORG######` is immutable and used as the staff-login host. Database GUIDs are never exposed as login names.

## 3. Invitation lifecycle

1. Owner/admin enters **contact email** + role/permissions.
2. Pending invitation stores org + contact email + hashed token + expiry (no PlatformUser stub).
3. Accept (anonymous token + password) allocates staff login, creates staff PlatformUser + credential + Organization account profile + membership (+ optional product role).
4. Confirmation shows organization name, contact email, and staff login.
5. Auto-accept-into-personal-identity is disabled for org staff invites.

## 4. Authentication / authorization

- Login label: **Username or email** (personal email or staff login).
- Staff session auto-selects `HomeOrganizationId`; client-supplied org switch is rejected (`StaffOrganizationSwitchDenied`).
- Staff UI hides organization switcher and Personal home when `OrganizationContextLocked`.
- SERVER UNREACHABLE ≠ SERVER ACCESS DENIED remains unchanged for offline grant revalidation.

## 5. Removal / revocation

Disabling/removing Org A staff affects only that staff PlatformUser (status/membership/sessions/tokens per existing patterns). Personal identity and other orgs’ staff identities remain intact. Audit/sale history retain immutable user/org GUIDs.

## 6. Offline PIN / grant

Offline operate grant remains bound to `UserId` + `OrganizationId` + device after successful online staff auth. PIN unlock restores that staff/org context only. No offline org switching.

## 7. Migration / backward compatibility

- Migration `AddOrgScopedStaffIdentities` adds `public_organization_id`, `normalized_contact_email`, `home_organization_id` and backfills public org ids.
- Existing personal-email users with memberships continue to authenticate; they are not silently duplicated.
- **New** org staff invites always create org-scoped staff logins.
- Automatic conversion of legacy multi-membership personal identities is deferred (unsafe without operator confirmation).

## 8. Security invariants

- Never trust client OrganizationId after staff login.
- Staff cannot alter `HomeOrganizationId`.
- Login names unique case-insensitively.
- Invitation accept is token-bound and expiry-checked.
- Passwords hashed with existing identity implementation.
- Org ID is not a password/secret.

## 9. Tests

Platform unit coverage includes allocation collisions, multi-employer contact email, org-switch denial, invite token failures, personal login smoke, and invitation create-without-user. Maui guards cover `OrganizationContextLocked` switcher hiding. Physical device validation: **not performed**.

## 10. Limitations / future OAuth

External OAuth linking for synthetic staff logins is out of scope. Future providers must bind to the authenticated PlatformUser id, never to contact email alone.

## 11. Git

Feature commits: `bdf3232` (platform model/auth), `a2cd391` (UI + tests), `01af206` (docs). **Not pushed.**
