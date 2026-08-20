# Identity, Personal, and Organization Lifecycle

## User identity (current)

There is **no** separate `UserIdentity` table. `PlatformUser` is the credential-bearing identity.

| Concept | Implementation | Evidence |
|---------|----------------|----------|
| Identity | `PlatformUser` → table `platform_users` | Platform Domain / `PlatformDbContext` |
| Credentials | `PlatformUserCredential` 1:1 → `platform_user_credentials` | password hash, stamp, lockout |
| Login key (Personal / Platform staff) | `NormalizedEmail` (unique) | real email |
| Staff login key | `NormalizedEmail` stores synthetic `local@ORG######` | not a mailbox |
| Staff contact | `NormalizedContactEmail` (non-unique) | recovery/contact only |
| Home org | `HomeOrganizationId` null for Personal/Owner; set for org staff | immutable after staff create |
| Account profiles | `AccountProfile` unique per (user, `AccountClass`) | `account_profiles` |
| Sessions | `PlatformAuthSession` bound to one profile/class | `platform_auth_sessions` |

Status: **PROVEN_CURRENT**

## Account classes

Enum `AccountClass`: `Platform`, `Personal`, `Organization`.

| Class | Typical identity | Session behavior |
|-------|------------------|------------------|
| Personal | Real-email `PlatformUser`, `HomeOrganizationId = null` | Personal API family |
| Organization | Owner (same Personal user + Org profile) **or** staff (`CreateOrganizationStaff`) | Org context required; staff locked to home org |
| Platform | Platform staff (`StaffNumber` `STF-######`) | Platform Admin APIs |

Evidence: `AccountClass.cs`, ADR-017, P16 WP02 report, P19 report.

## Organization-scoped staff login (owner-confirmed preserved)

Owner requires preservation of current org-scoped staff aliases. Audit of **actual** implementation:

| Topic | Current rule |
|-------|--------------|
| Format | `{local}@{ORG######}` via `StaffLoginNameRules.Build` |
| Display | `FormatForDisplay` uppercases host (`maria@ORG001842`) |
| Local part | Derived from contact email local; alnum only; collisions `maria`, `maria1`, … |
| Uniqueness | Case-insensitive unique on `platform_users.normalized_email` |
| Mutability | Login is the staff identity email; not a Personal-email attach model |
| Credential ownership | New `PlatformUser` + credential on invite accept |
| Personal reuse | **Forbidden** for Staff/Member membership; Owner may stay on Personal identity |
| Auth path | Login identifier → `GetByNormalizedEmailAsync` → Organization profile → `HomeOrganizationId` auto-selected; switch denied (`StaffOrganizationSwitchDenied`) |
| Invite create | Pending invitation (org + contact email + token); **no** stub user |
| Invite accept | Anonymous `POST /api/v1/platform/auth/organization-invitations/accept` with token + password |
| Create invite | `POST /api/v1/organizations/{organizationId}/staff-invitations` |
| Multi-org employment | Separate staff `PlatformUser` per employer org |
| Revocation | Membership/invitation lifecycle (see invitation use cases); staff cannot switch org |
| Org membership roles | `OrganizationOwner`, `OrganizationAdministrator`, `OrganizationMember` (UI often Owner/Staff) |
| POS roles | Separate `ProductLocalRoleGrant` codes `Owner`/`Manager`/`Cashier`/`Viewer` → POS `Owner`/`StoreManager`/`Cashier`/`ReportingUser` |

Evidence:

- `src/Platform/ExItS.Platform.Domain/Identity/StaffLoginNameRules.cs`
- `PlatformUser.CreateOrganizationStaff`
- `AcceptOrganizationInvitation` in invitation use cases
- `tests/.../Identity/OrganizationScopedStaffIdentityTests.cs`
- `docs/reports/P19-organization-scoped-staff-identities.md`
- `docs/architecture/user-creation-flow-and-account-scope-rules.md` §9.1

Status: **PROVEN_CURRENT**

### SUPERSEDED (do not reintroduce)

- Personal email as org-staff login
- Auto-accept org invites onto Personal activation
- Staff membership without matching `HomeOrganizationId`
- Treating contact email as unique login/authorization key

## Session boundaries

| Boundary | Rule | Status |
|----------|------|--------|
| One AccountClass per session | Fixed on session; change via profile select (new session semantics) | PROVEN_CURRENT |
| Org context | Only Organization sessions; membership-validated | PROVEN_CURRENT |
| Staff lock | Cannot clear/switch away from home org | PROVEN_CURRENT |
| Owner multi-org | Same Personal identity; Organization session may switch owned orgs | PROVEN_CURRENT |
| MAUI context switch | Local store wipe / cache invalidation on org switch | PROVEN_CURRENT (MAUI + architecture docs) |

Primary APIs:

- `POST /api/v1/platform/auth/login`
- Account profile list/select endpoints under platform auth
- `PUT` organization-context endpoints
- `GET /api/v1/platform/auth/me`

## Personal → Organization (Start a Business)

| Step | Behavior | Status |
|------|----------|--------|
| Entry | Personal session `POST /api/v1/personal/start-business` | PROVEN_CURRENT |
| Creates | Organization + main branch (+ profile seed) | PROVEN_CURRENT |
| Membership | `OrganizationOwner` on **same** Personal `PlatformUser` | PROVEN_CURRENT |
| Profiles | Ensures Organization `AccountProfile` alongside Personal | PROVEN_CURRENT |
| Session | Selects Organization profile + org context | PROVEN_CURRENT |
| POS grant | Optional entitlement + `ProductLocalRoleGrant` POS Owner — **not** implied by org Owner alone | PROVEN_CURRENT |
| Explore POS | Commercial plans `GET /api/v1/commercial/plans`; MAUI `/personal/explore-pos` | PROVEN_CURRENT |

Evidence: `StartBusinessUseCases.cs`, `PersonalEndpoints.cs`, MAUI `StartBusiness.razor`.

## Hard Personal / Staff / Customer boundaries

| Boundary | Current rule | Status |
|----------|--------------|--------|
| Staff ≠ customer | Distinct flows; customer link ≠ membership | PROVEN_CURRENT |
| Customer link acceptance ≠ staff membership | Separate invitation/link models | PROVEN_CURRENT |
| Personal linked customer ≠ POS role | Link grants Business Utang / storefront views, not POS roles | PROVEN_CURRENT |
| Personal Business Utang view ≠ Personal Utang copy | Read projection; separate ledgers | PROVEN_CURRENT |
| Org Owner ≠ automatic selling role | Requires product role grant | PROVEN_CURRENT |
| Org management ≠ POS checkout authority | Membership vs `CreateSale` / Cashier | PROVEN_CURRENT |

Evidence: `docs/architecture/personal-organization-identity-boundaries.md`, P16/P19/P24 reports, customer-link vs staff-invite use cases.

## Personal surface inventory (current)

| Capability | Status | Evidence |
|------------|--------|----------|
| Personal Utang (I Lent / I Borrowed) | PROVEN_CURRENT | `/api/v1/personal/utang/*`, MAUI `/personal/utang/*` |
| Linked merchants | PROVEN_CURRENT | Platform linked-merchants + MAUI shop |
| Personal QR | PROVEN_CURRENT | Platform public identity APIs / MAUI |
| Start a Business | PROVEN_CURRENT | as above |
| React Personal home | SHELL_ONLY | `PersonalHomePage.tsx` placeholder |

## React implications

React account/session/workspace parity must implement:

1. Cookie/browser session login for Personal **and** org-scoped staff login strings
2. Account profile / workspace resolution
3. Organization context binding and staff lock behavior
4. Product access + local role before sell floor

Do not invent a unified “one human / one PlatformUser for employment” model — that path is **SUPERSEDED** by P19.
