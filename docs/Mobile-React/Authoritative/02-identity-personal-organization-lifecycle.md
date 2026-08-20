# Identity, Personal, and Organization Lifecycle

## User identity (CURRENT)

There is **no** separate `UserIdentity` table/entity. `PlatformUser` is the credential-bearing principal.

| Concept | Implementation | Evidence |
|---------|----------------|----------|
| Identity principal | `PlatformUser` → `platform.platform_users` | Platform Domain / `PlatformDbContext` |
| Credentials | `PlatformUserCredential` 1:1 → `platform_user_credentials` | password hash, stamp, lockout |
| Login key (Personal / Platform staff) | `NormalizedEmail` (unique) | real email |
| Staff login key | `NormalizedEmail` stores synthetic `local@ORG######` | not a mailbox |
| Staff contact | `NormalizedContactEmail` (non-unique) | recovery/contact only |
| Home org | `HomeOrganizationId` null for Personal/Owner; set for org staff | immutable after staff create |
| Account profiles | `AccountProfile` unique per (user, `AccountClass`); field `UserIdentityId` is typed as `PlatformUserId` | `account_profiles` |
| Sessions | `PlatformAuthSession` bound to one profile/class | `platform_auth_sessions` |

Status: **PROVEN_CURRENT**

## Account classes

Enum `AccountClass`: `Platform`, `Personal`, `Organization`.

| Class | Typical identity | Session behavior |
|-------|------------------|------------------|
| Personal | Real-email `PlatformUser`, `HomeOrganizationId = null` | Personal API family |
| Organization | Owner (same Personal user + Org profile) **or** staff (`CreateOrganizationStaff` = **separate** `PlatformUser`) | Org context required; staff locked to home org |
| Platform | Platform staff (`StaffNumber` `STF-######`) | Platform Admin APIs |

Evidence: `AccountClass.cs`, ADR-017, P16 WP02 report, P19 report.

---

## Organization staff — CURRENT model (entity-level)

### Answers (CURRENT)

| Q | CURRENT answer |
|---|----------------|
| A. Separate human identity? | **Yes** — staff accept creates a **new** `PlatformUser` Guid, not a profile on Personal |
| B. Same UserIdentity + second credential? | **No** — there is no `UserIdentity` entity; credential is 1:1 on the **new** staff `PlatformUser` |
| C. Linkage to existing Personal? | **Formal when Personal accepts** — `LinkedPersonalUserId` FK on the staff principal. **Not** inferred from `NormalizedContactEmail`. Standalone/legacy staff remain unlinked (null) |
| D. Personal accept invite → same-identity Staff? | **Creates a new staff principal** linked to Personal. Does **not** convert Personal or attach Staff membership to Personal |
| E. Reuse vs create | Personal untouched; create staff `PlatformUser` + separate credential + Organization profile + membership (+ optional POS role) + optional person-link |
| F. Multi-org same human | Owner: yes on one Personal user. Staff: same Personal may formally link to **separate** staff `PlatformUser`s per employer |
| G. Alias per org | **Yes** — each staff row login **is** `local@ORG######` (not display-only) |
| H. Remove one membership | Membership Removed; product-access for that membership revoked; sessions cleared for that userId+org; **does not** delete `PlatformUser` |
| I. Preserve Personal / other orgs | **Yes** — Personal and other staff rows remain Active when Org A staff suspended/deactivated |
| J. Alias semantics | **Separate credential principal** = separate `PlatformUser` whose unique login **is** the alias (`NormalizedEmail`) + own credential — **not** alias-only on Personal |

### CURRENT diagram

```text
Physical human (Paul)
 ├─ PlatformUser [Personal]  NormalizedEmail=paul@gmail.com  HomeOrg=null
 │    ├─ PlatformUserCredential (Personal password / lockout)
 │    └─ AccountProfile(Personal)
 │
 ├─ PlatformUser [Staff Org A]  login=paul@ORG######  Contact=paul@gmail.com
 │    HomeOrg=A  LinkedPersonalUserId → Personal (when accepted from authenticated Personal)
 │    ├─ PlatformUserCredential (separate staff password / lockout)
 │    ├─ AccountProfile(Organization)
 │    └─ OrganizationMembership(Staff)
 │
 └─ PlatformUser [Staff Org B]  login=paul@ORG######  Contact=paul@gmail.com
      HomeOrg=B  LinkedPersonalUserId → same Personal
      └─ (independent credential, membership, POS grants)
```

Person-link is identity correlation only. Authorization remains staff principal + membership + session class + entitlements + product role.

### CURRENT evidence

| Artifact | Path / detail |
|----------|---------------|
| Staff create | `PlatformUser.CreateOrganizationStaff` |
| Alias rules | `StaffLoginNameRules.Build` / `FormatForDisplay` |
| Accept (no Personal) | `AcceptOrganizationInvitation.ExecuteAsync` → new staff; `LinkedPersonalUserId` null |
| Accept (existing Personal) | `ExecuteForAuthenticatedPersonalAsync` requires **active Personal `AccountProfile`** + verified email match + token → new staff + `LinkedPersonalUserId` |
| Anonymous gate | Requires authenticated Personal **only when** matching login principal has active Personal profile (Platform-only same email may still anonymous-accept unlinked staff) |
| Atomicity | `ExecuteWithOrganizationLockAsync(organizationId)` — re-read invitation inside lock; outbound email after commit |
| Audit | `platform.invitation.accepted`; when linked also `platform.user.person_link.established` (no tokens/passwords) |
| Routes | Anonymous: `POST /api/v1/platform/invitations/accept` and `/api/v1/platform/auth/organization-invitations/accept`. Personal: `.../accept-as-personal` twins |
| Person-link storage | `platform_users.linked_personal_user_id` (nullable, Restrict, staff-only check) |
| Tests | `OrganizationScopedStaffIdentityTests`; staff/customer separation API tests |
| Membership guard | `AddOrganizationMembership` → `HomeOrganizationRequired` for Staff/Member |
| Personal pending | `ListPendingOrganizationInvitationsForUser` returns empty for Personal |
| Tables | `platform_users`, `platform_user_credentials`, `account_profiles`, `organization_invitations`, `organization_memberships` |
| Tests | `OrganizationScopedStaffIdentityTests`; suspend/deactivate Org A does not affect Personal/Org B |
| Docs | P19 report; `user-creation-flow-and-account-scope-rules.md` §9.1 |

### CURRENT classifications

| Capability | Status |
|------------|--------|
| Org-scoped staff login alias format | **PROVEN_CURRENT** — preserve format |
| Separate staff `PlatformUser` per employment | **PROVEN_CURRENT** |
| Formal person-link / Personal-accept-as-staff | **PROVEN_CURRENT** (Option C: staff principal + `LinkedPersonalUserId`; not authorization) |
| Soft contact-email correlator | **PROVEN_CURRENT** as contact/recovery only — **not** same-human proof |

### SUPERSEDED (do not reintroduce as CURRENT)

- Personal email as org-staff login without org-scoped principal
- Auto-accept org invites onto Personal activation attaching Staff membership to Personal
- Staff membership without matching `HomeOrganizationId`
- Treating contact email as unique login/authorization key

---

## Organization staff — OWNER-CONFIRMED desired model

**Classification:** `OWNER_CONFIRMED_CHANGE` relative to CURRENT separate-staff-`PlatformUser` employment model.

Desired conceptual model:

```text
Verified Person / Human Identity
├── Personal Account
├── Organization membership: Org A
│   └── organization-scoped login alias
├── Organization membership: Org B
│   └── organization-scoped login alias
└── other authorized account profiles
```

Owner confirms:

- Existing Personal user may be invited by an Organization
- Acceptance may make that **same human** Organization Staff
- Do **not** create an unrelated duplicate human merely because employment begins
- Organization-specific login alias must remain available
- Person may belong to multiple Organizations; memberships isolated
- Removing Org A must not delete Personal Account, Org B membership, or unrelated profile data
- Organization role ≠ POS product role
- Customer linkage ≠ Staff membership

### Desired vs CURRENT

| Requirement | CURRENT | Desired |
|-------------|---------|---------|
| One human not duplicated for employment | Separate staff principals per job + formal person-link when Personal accepts | Approved Option C (not a single merged principal) |
| Personal can accept staff invite | Authenticated Personal accept creates linked staff principal | Required — implemented |
| Org-scoped alias | Staff principal login `local@ORG######` | Must remain the real login |
| Multi-org | Separate staff users per org, optionally linked to same Personal | Isolated memberships |
| Removal isolation | Membership/user scoped; Personal and other staff preserved | Same outcome required |

**Marker:** `ORGANIZATION_STAFF_EXISTING_PERSON_LINK_CONTRACT_MISSING` — **RESOLVED** (RMAP-B00). Follow-up: `ORGANIZATION_STAFF_LATE_PERSONAL_LINK_FLOW_DEFERRED` (staff who later create Personal; no email auto-merge).

**Roadmap:** RMAP-B00 PASS. React staff-identity UI remains **RMAP-01b**. See [Migration/react-migration-roadmap.md](Migration/react-migration-roadmap.md) and [POS-REACT-RMAP-B00-identity-reconciliation.md](../Reports/POS-REACT-RMAP-B00-identity-reconciliation.md).

**Readiness:** `READY_FOR_REACT_STAFF_IDENTITY_PARITY` = **YES** (backend contract). Do not start RMAP-01 until Product Owner + ChatGPT review this package.

---

## Session boundaries

| Boundary | Rule | Status |
|----------|------|--------|
| One AccountClass per session | Fixed on session; change via profile select | PROVEN_CURRENT |
| Org context | Only Organization sessions; membership-validated | PROVEN_CURRENT |
| Staff lock | Cannot clear/switch away from home org | PROVEN_CURRENT (CURRENT staff model) |
| Owner multi-org | Same Personal identity; Organization session may switch owned orgs | PROVEN_CURRENT |
| MAUI context switch | Local store wipe / cache invalidation on org switch | PROVEN_CURRENT |

Primary APIs: `POST /api/v1/platform/auth/login`, account-profile select, organization-context, `GET .../me`.

## Personal → Organization (Start a Business)

| Step | Behavior | Status |
|------|----------|--------|
| Entry | Personal session `POST /api/v1/personal/start-business` | PROVEN_CURRENT |
| Creates | Organization + main branch | PROVEN_CURRENT |
| Membership | `OrganizationOwner` on **same** Personal `PlatformUser` | PROVEN_CURRENT |
| Profiles | Ensures Organization `AccountProfile` alongside Personal | PROVEN_CURRENT |
| POS grant | Optional entitlement + `ProductLocalRoleGrant` — not implied by org Owner alone | PROVEN_CURRENT |
| Explore POS | `GET /api/v1/commercial/plans`; MAUI `/personal/explore-pos` | PROVEN_CURRENT |

## Hard Personal / Staff / Customer boundaries

| Boundary | Rule | Status |
|----------|------|--------|
| Staff ≠ customer | Distinct flows | PROVEN_CURRENT |
| Customer link ≠ staff membership | Separate models | PROVEN_CURRENT |
| Personal linked customer ≠ POS role | Link ≠ role grant | PROVEN_CURRENT |
| Personal Business Utang view ≠ Personal Utang copy | Separate ledgers | PROVEN_CURRENT |
| Org Owner ≠ automatic selling role | Requires product role grant | PROVEN_CURRENT |

## Personal surface inventory

| Capability | Status | Evidence |
|------------|--------|----------|
| Personal Utang | PROVEN_CURRENT | `/api/v1/personal/utang/*`, MAUI |
| Linked merchants | PROVEN_CURRENT | Platform + MAUI shop |
| Start a Business | PROVEN_CURRENT | as above |
| React Personal home | SHELL_ONLY | `PersonalHomePage.tsx` |
