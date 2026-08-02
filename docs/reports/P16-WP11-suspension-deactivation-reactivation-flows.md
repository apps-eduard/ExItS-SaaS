# P16-WP11 Defect Log — Suspension, deactivation, and reactivation flows

**Status:** Open (P16-WP11 In Progress)  
**Phase:** Phase 16 — Implementation Complete, Under Validation  
**Work package:** P16-WP11 — Validation, Stabilization, and User Acceptance  
**Date:** 2026-07-29

## Title

Platform Account and Organization membership lifecycle transitions were incomplete and labels were ambiguous

## Gaps closed

### Global Platform Account

| Transition | Confirmation |
|---|---|
| Active → Suspended | Confirmation + reason |
| Active → Deactivated | Password/MFA step-up + reason |
| Suspended → Active | Confirmation only |
| Suspended → Deactivated | Password/MFA step-up + reason |
| Deactivated → Active | Password/MFA step-up + reason |
| Deactivated → Suspended | Password/MFA step-up + reason (Move to Suspended; login stays blocked) |

- Sessions/tokens revoked on Suspend and Deactivate
- Platform profile and Platform role retained
- Final active Platform Administrator protection on Suspend/Deactivate from Active
- Deactivated is reversible retained state (not deletion)

### Organization membership

| Transition | Notes |
|---|---|
| Active ↔ Suspended | Suspend Membership / Reactivate Membership |
| Active/Suspended → Removed (UI: Deactivated) | Deactivate Membership + reason |
| Removed → Active / Suspended | Reactivate Membership / Move to Suspended |

- Global identity unchanged
- Last Owner protection retained

### Global identity (Org/Personal)

- Admin labels: **Global Account Suspension** / **Global Account Reactivation**
- Same identity status APIs with `global: true` (reason required)
- Distinct from routine Suspend Membership

## APIs

- `POST /users/{id}/suspend` (`reason`, optional `global`)
- `POST /users/{id}/reactivate` (`reason`, `actorPassword`, `mfaCode`, optional `global`)
- `POST /users/{id}/deactivate` (reason + actorPassword/mfaCode; `/disable` alias)
- `POST /users/{id}/move-to-suspended` (reason + actorPassword/mfaCode)
- Membership `/revoke` requires reason (Deactivate Membership)

## Tests

- Domain transition matrix for PlatformUser and OrganizationMembership
- Integration: full Platform Account lifecycle including Move to Suspended and Deactivated → Active

## Follow-up (UI validation)

- Account-type directory sort rewritten to EF-safe EXISTS ranks (fixes Admin sort error)
- Platform users list/detail show Owner/Staff tags beside organization names
- Deactivate and Move to Suspended now require administrator password (and MFA when enabled); Suspended → Active remains confirmation-only

## Phase status

Phase 16 remains **Under Validation**. P16-WP11 remains **In Progress**. P16-WP12 and Phase 17 were not started.
