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
| C. Linkage to existing Personal? | **Soft only** — `NormalizedContactEmail` may equal Personal email; **no** FK / `LinkedPersonalUserId` |
| D. Personal accept invite → same-identity Staff? | **No** — forbidden; Personal pending list empty; Staff/Member membership requires matching `HomeOrganizationId` |
| E. Reuse vs create | Personal untouched; create staff `PlatformUser` + credential + Organization profile + membership (+ optional POS role) |
| F. Multi-org same human | Owner: yes on one Personal user. Staff: same contact email may exist in multiple orgs as **separate** staff `PlatformUser`s |
| G. Alias per org | **Yes** — each staff row gets its own `local@ORG######` |
| H. Remove one membership | Membership Removed; product-access for that membership revoked; sessions cleared for that userId+org; **does not** delete `PlatformUser` |
| I. Preserve Personal / other orgs | **Yes** — Personal and other staff rows remain Active when Org A staff suspended/deactivated |
| J. Alias semantics | **Separate credential principal** = separate `PlatformUser` whose unique login **is** the alias (`NormalizedEmail`) + own credential — **not** alias-only on Personal |

### CURRENT diagram

```text
Physical human
 ├─ PlatformUser [Personal]  NormalizedEmail=maria@gmail.com  HomeOrg=null
 │    ├─ PlatformUserCredential
 │    ├─ AccountProfile(Personal)
 │    └─ OrganizationMembership(Owner)*  (Start a Business; multi-org owner OK)
 │
 ├─ PlatformUser [Staff Org A]  NormalizedEmail=maria@org001842  Contact=maria@gmail.com  HomeOrg=A
 │    ├─ PlatformUserCredential (new)
 │    ├─ AccountProfile(Organization)
 │    └─ OrganizationMembership(Staff)
 │
 └─ PlatformUser [Staff Org B]  NormalizedEmail=maria@org004911  Contact=maria@gmail.com  HomeOrg=B
      └─ (same pattern; distinct Id)
```

### CURRENT evidence

| Artifact | Path / detail |
|----------|---------------|
| Staff create | `PlatformUser.CreateOrganizationStaff` |
| Alias rules | `StaffLoginNameRules.Build` / `FormatForDisplay` |
| Accept | `AcceptOrganizationInvitation.ExecuteAsync` → always `_users.AddAsync(staffUser)` |
| Routes | `POST /api/v1/platform/auth/organization-invitations/accept`; `POST /api/v1/platform/invitations/accept` (anonymous) |
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
| Soft contact-email correlator | **PROVEN_PARTIAL** (string only) |
| Formal person-link / Personal-accept-as-staff | **PROVEN_MISSING** |

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
| One human not duplicated for employment | Separate `PlatformUser` per staff job | One person; memberships/profiles under same human |
| Personal can accept staff invite | Forbidden | Required |
| Org-scoped alias | Exists as staff principal login | Must remain available (may become alias/credential under same person) |
| Multi-org | Separate staff users per org | Same human, isolated memberships |
| Removal isolation | Membership/user scoped; Personal preserved | Same outcome required |

**Marker:** `ORGANIZATION_STAFF_EXISTING_PERSON_LINK_CONTRACT_MISSING` — warranted (no FK/person-link; Personal cannot accept onto same identity).

**Roadmap:** Backend package **RMAP-B00** before React staff-identity parity that implements the desired model. See [Migration/react-migration-roadmap.md](Migration/react-migration-roadmap.md).

**Readiness:** `READY_FOR_REACT_STAFF_IDENTITY_PARITY` = **NO** until RMAP-B00 completes and owner approves the resulting contract.

React may still authenticate against **CURRENT** staff login strings for session/smoke testing only if a WP explicitly scopes “CURRENT staff principal login” — it must **not** claim desired person-link parity.

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
