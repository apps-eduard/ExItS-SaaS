# RMAP-B00 — Staff Identity / Formal Same-Human Link

**Status:** PASS

**Branch:** `feat/pos-react-client`

**Implementation SHA:** `1cd582bac95f66101fe8e666d4dbac0b8c721e86`

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

Migration: `20260820182830_AddPlatformUserLinkedPersonalUserId`. Production auto-`Migrate()` remains unused.

## Invitation flows

| Path | Endpoint | Result |
|------|----------|--------|
| No Personal | `POST /api/v1/platform/invitations/accept` (and auth twin) anonymous | New staff principal; `LinkedPersonalUserId` null |
| Existing Personal | `POST /api/v1/platform/invitations/accept-as-personal` (and auth twin) | Authenticated Personal + token + new staff password → new staff principal + formal link |
| Personal exists, anonymous accept | Same anonymous routes | `InvitationRequiresAuthenticatedPersonal` — sign in with Personal |

Proof required for Personal accept: valid pending token + authenticated Personal principal + verified email match + explicit accept. Wrong Personal → generic invitation not found (no account leak). Contact-email equality never creates the link.

## MAUI

`PersonalInvitationAccept` uses authenticated Personal accept when the session `AccountClass` is Personal; otherwise the anonymous path. Staff invite/sign-in screens are unchanged.

## Tests

### Platform unit

| Suite / filter | Passed | Failed | Skipped |
|----------------|--------|--------|---------|
| `OrganizationScopedStaffIdentityTests` | 19 | 0 | 0 |
| `Identity\|Invitation\|CustomerLink\|AccountProfile\|Membership\|Session` | 267 | 0 | 0 |

### Platform integration (Testcontainers PostgreSQL)

| Suite / filter | Passed | Failed | Skipped |
|----------------|--------|--------|---------|
| `ApiOrganizationStaffCustomerSeparationTests` (includes authenticated Personal staff invite) | 9 | 0 | 0 |
| `Wrong_invitation_type_accept_is_anti_enumeration_safe` + `ApiAccountScopeIsolationTests` + `ApiCredentialLifecycleTests` | 11 | 0 | 0 |
| `ApiPhase16CloseoutSecurityTests` full class | 9 | 2 | 0 |

The two full-class failures (`Key_phase16_actions_emit_audit_records`, `Migration_replay_with_same_idempotency_key_is_safe`) failed on `POST /api/v1/personal/start-business` (NotFound). They are Start a Business / Utang migration, not invitation or person-link. The invitation case in that class (`Wrong_invitation_type_accept_is_anti_enumeration_safe`) passed in the focused rerun.

### MAUI

| Suite / filter | Passed | Failed | Skipped |
|----------------|--------|--------|---------|
| `PersonalPageGuardTests` + SignIn / Invitation / OrganizationContext | 23 | 0 | 0 |

### React

No RMAP-01 / RMAP-01b UI started. RMAP-00 Playwright already closed out.

## Markers

| Marker | Status |
|--------|--------|
| `RMAP_B00_CREDENTIAL_SEMANTICS_UNRESOLVED` | **RESOLVED** |
| `ORGANIZATION_STAFF_EXISTING_PERSON_LINK_CONTRACT_MISSING` | **RESOLVED** |
| `ORGANIZATION_STAFF_LATE_PERSONAL_LINK_FLOW_DEFERRED` | **OPEN** (staff who later create Personal; no auto-merge) |

## Next

STOP for Product Owner + ChatGPT Git/diff/schema/security review. Do **not** start RMAP-01.
