# RMAP-B00 — Staff Identity / Formal Same-Human Link

**Status:** PASS

**Branch:** `feat/pos-react-client`

**Implementation SHA (Repair 02):** `1cd582bac95f66101fe8e666d4dbac0b8c721e86`

**Review Repair 03 SHA:** `e4b04e268c3cb05dd59f62caab2d4379a8fe66a2`

**Historical hard stop:** [POS-REACT-RMAP-B00-identity-hard-stop.md](POS-REACT-RMAP-B00-identity-hard-stop.md) (`RMAP_B00_CREDENTIAL_SEMANTICS_UNRESOLVED`)

## Objective

Reconcile organization-scoped staff employment with the Product Owner outcome:

- one physical human is not treated as unrelated duplicate humans after explicit proof
- Personal may accept a staff invitation
- org-scoped `local@ORG######` remains the real staff login
- multi-employer isolation and removal isolation remain P19/MAUI behavior

## Product Owner credential decision (resolved)

| Flag | Value |
|------|-------|
| Separate Personal and staff principals | YES |
| Separate Personal and staff passwords | YES |
| Multi-employer = multiple staff principals | YES |
| Independent lockout / sessions | YES |
| `HomeOrganizationId` lock preserved | YES |
| Org-scoped staff login preserved | YES |
| Contact email is not login | YES |
| Option C formal person-link approved | YES |
| Email-only auto-link | NO |

## Architecture (minimal additive)

Optional `PlatformUser.LinkedPersonalUserId` on organization-scoped staff only.

- Nullable (legacy/standalone staff remain valid)
- FK to `platform_users.id`, `ON DELETE RESTRICT` (Personal delete cannot cascade-remove staff)
- Check: link set ⇒ `home_organization_id IS NOT NULL` and not self
- Index: `ix_platform_users_linked_personal_user_id`
- Correlation only: not membership, not POS role, not session authority
- Application also requires the link target to have an **active Personal `AccountProfile`** when the link is created

Migration: `20260820182830_AddPlatformUserLinkedPersonalUserId` (unchanged in Repair 03). Production auto-`Migrate()` remains unused.

## Invitation flows

| Path | Endpoint | Result |
|------|----------|--------|
| No Personal | `POST /api/v1/platform/invitations/accept` (and auth twin) anonymous | New staff principal; `LinkedPersonalUserId` null |
| Existing Personal | `POST /api/v1/platform/invitations/accept-as-personal` (and auth twin) | Active Personal profile + token + verified email + new staff password → linked staff |
| Active Personal exists, anonymous accept | Same anonymous routes | `InvitationRequiresAuthenticatedPersonal` |
| Platform-only same email | Anonymous accept | Allowed; creates **unlinked** staff (Platform ≠ Personal) |
| Unverified Personal | Authenticated accept | `InvitationPersonalEmailUnverified` |

### Personal classification (Review Repair 03)

Eligible Personal proof is **not** `!IsOrganizationScopedStaff`. Application requires:

- Active `PlatformUser`
- Active `AccountProfile` with `AccountClass.Personal`
- Normalized login email matches invitation contact email
- Personal credential `EmailVerifiedAtUtc` set
- Valid pending invitation token

`AccountScopeGuard` remains additional HTTP defense (Personal-only on accept-as-personal).

### Atomicity (Review Repair 03)

Acceptance runs under `IPlatformUnitOfWork.ExecuteWithOrganizationLockAsync(organizationId)`:

1. Preliminary token lookup for organization id
2. Organization advisory lock + transaction (PostgreSQL)
3. Re-read pending invitation inside the lock
4. Create staff / credential / profile / membership / optional role / person-link / mark accepted / audit
5. Commit
6. Outbound completion email **after** durable success

Membership failure after staff create throws so the transaction rolls back (no orphan staff).

### Audit (Review Repair 03)

Shared application flow emits:

- `platform.invitation.accepted` (always on success)
- `platform.user.person_link.established` when `LinkedPersonalUserId` is set

Actor = Personal principal when linked. Summaries include staff/Personal ids and organization context. Tokens, passwords, and hashes are excluded.

## MAUI

`PersonalInvitationAccept` uses authenticated Personal accept when the session `AccountClass` is Personal; otherwise the anonymous path. Staff invite/sign-in screens are unchanged.

## Tests

### Platform unit

| Suite / filter | Passed | Failed | Skipped |
|----------------|--------|--------|---------|
| `OrganizationScopedStaffIdentityTests` | 22 | 0 | 0 |
| `Identity\|Invitation\|CustomerLink\|AccountProfile\|Membership\|Session` | 270 | 0 | 0 |

### Platform integration (Testcontainers PostgreSQL)

| Suite / filter | Passed | Failed | Skipped |
|----------------|--------|--------|---------|
| `ApiOrganizationStaffCustomerSeparationTests` (Personal accept, Platform-only email, account-scope matrix, parallel same-token) | 12 | 0 | 0 |
| `Wrong_invitation_type_accept` + `ApiAccountScopeIsolationTests` + `ApiCredentialLifecycleTests` | 11 | 0 | 0 |
| Phase16 baseline red (unchanged) | 0 | 2 | 0 |

**PREEXISTING_BASELINE_RED_START_BUSINESS:** `Key_phase16_actions_emit_audit_records` and `Migration_replay_with_same_idempotency_key_is_safe` still fail on `POST /api/v1/personal/start-business` → NotFound. Identical at Repair 03 baseline and after repair. Outside invitation/person-link scope; not fixed here.

### MAUI

| Suite / filter | Passed | Failed | Skipped |
|----------------|--------|--------|---------|
| `PersonalPageGuardTests` + SignIn / Invitation / OrganizationContext | 23 | 0 | 0 |

### React

No RMAP-01 / RMAP-01b UI started. RMAP-00 not re-run (unaffected).

## Markers

| Marker | Status |
|--------|--------|
| `RMAP_B00_CREDENTIAL_SEMANTICS_UNRESOLVED` | **RESOLVED** |
| `ORGANIZATION_STAFF_EXISTING_PERSON_LINK_CONTRACT_MISSING` | **RESOLVED** |
| `ORGANIZATION_STAFF_LATE_PERSONAL_LINK_FLOW_DEFERRED` | **OPEN** (staff who later create Personal; no auto-merge) |

## Next

STOP for Product Owner + ChatGPT final Git/diff/schema/security review of Repair 03. Do **not** start RMAP-01.
