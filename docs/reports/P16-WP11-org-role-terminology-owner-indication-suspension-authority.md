# P16-WP11 Defect Log — Organization role terminology, owner indication, and suspension authority

**Status:** Open (P16-WP11 In Progress)  
**Phase:** Phase 16 — Implementation Complete, Under Validation  
**Work package:** P16-WP11 — Validation, Stabilization, and User Acceptance  
**Date:** 2026-07-29

## Title

Organization role terminology, Owner indication, membership confirmation dialogs, and suspension authority were misaligned with account-scope rules

## Incorrect behavior

- UI and seed labels used **Member** for the Organization non-Owner role
- Add/invite/role dropdowns offered Organization Administrator
- Role column showed raw enum codes without accessible Owner/Staff tags
- Membership status actions used bare Popconfirm without target identity/status/reason context
- Last-seat protection and org-path membership management treated Owner|Administrator as governing
- Confirmation dialogs for Platform lifecycle actions omitted target status / identity summary

## Fix

### Terminology (safe display mapping)

| Persisted / internal | Displayed business term |
|---|---|
| `OrganizationMember` | Staff |
| `OrganizationOwner` | Owner |
| `OrganizationAdministrator` | Administrator (legacy read-only; not offered for new assign/invite) |

- `OrganizationRoleDisplay` centralizes labels
- DTOs expose `RoleDisplay` while `Role` remains the enum code
- Admin labels, shell role label, Local Validation seed summaries, and validation messages use Staff

### Owner indication

- Organization staff directory and Platform user memberships show emphasized **Owner** and neutral **Staff** tags (text + weight; color is not the only indicator)
- Product-local roles remain separate

### Suspension / reactivation authority

- Organization membership manage path: active **Organization Owner** in trusted org context, or Platform `ManageMemberships` emergency override
- Last active **Owner** protection (Administrator alone does not count as the protected governing seat)
- Only Owners may assign/invite Owner without Platform authority
- Assignable roles for org actors: Owner and Staff only

### Confirmation dialogs

- Organization membership Suspend / Reactivate / Deactivate / Move to Suspended use a modal with user, email, current status, target status, and reason when required
- Platform account lifecycle modal shows display name, email, current status, and target status (Deactivated → Active still requires step-up + reason)

## Tests

- `OrganizationRoleDisplayTests`
- Extended `OrganizationMembershipGuardTests` (Staff cannot assign Owner; sole Owner demotion; legacy Admin does not satisfy last-Owner)

## Phase status

Phase 16 remains **Under Validation**. P16-WP11 remains **In Progress**. P16-WP12 and Phase 17 were not started.
