# P16-WP11 Defect Log — Platform and Organization staff creation flows

**Status:** Open (P16-WP11 In Progress)  
**Phase:** Phase 16 — Implementation Complete, Under Validation  
**Work package:** P16-WP11 — Validation, Stabilization, and User Acceptance  
**Date:** 2026-07-29

## Title

Platform Staff and Organization Staff creation did not enforce required roles or correct exclusive account profiles

## Root cause

Platform Admin user create called identity-only `CreatePlatformUser`, which persisted a username/email without a Platform system role and without a Platform Account profile. Login-time profile inference could leave identities unassigned or incorrectly classified as Personal.

Organization invitation create recorded email + Organization role only; it did not provision an Organization-scoped identity. Accept/membership later added Organization profile, but invite-time staff creation was not aligned to “Organization Account only / active organization membership / required Organization role.”

## Incorrect behavior

- Platform create: no required Platform role selection in Admin UI; no Platform Account profile; optional email verification not available
- Organization invite/add: Organization role existed but labels/validation were weak; invite did not ensure Organization-only account profile (no Platform/Personal companion)

## Fix

### Official account-creation rules enforced

| Path | Result |
|---|---|
| Personal self-signup / login without roles | Personal Account only (unchanged inference) |
| Start a Business | Organization Account + Owner in new org; Personal may remain (`exclusiveOrganizationProfile: false`) |
| Organization Owner invites/adds staff | Organization Account only + membership in active org + required Organization role; product-local roles remain separate |
| Platform Administrator creates staff | Platform Account only + required Platform role; optional email verification with initial password |

### Implementation

- `CreatePlatformStaffUser` — create identity, assign Platform role, exclusive Platform profile; optional password + email verification
- `EnsureOrganizationStaffIdentity` — invite-time Organization-only identity provision
- `CreateOrganizationInvitation` — provisions staff identity before invitation row
- `POST /api/v1/platform/users` — when `PlatformRole` is provided, runs staff provisioning (Admin create always sends it)
- Admin Users create form — required Platform role; optional send email verification + initial password
- Admin Organization Staff — readable Organization role labels (Owner / Staff); role required on add/invite

## Tests

- Unit: Platform staff create → role + exclusive Platform profile
- Unit: Organization staff identity → exclusive Organization profile (no Personal/Platform)
- Integration: staff create with role → `accountClasses == [Platform]` + role assignment
- Integration: org invite by email → provisioned user `accountClasses == [Organization]`

## Manual validation

1. As Platform Admin, create user with Platform Support (no email verification) → appears under Platform Accounts only
2. Create with email verification checked → password required; verification token issued (debug when enabled)
3. As Organization Owner, invite staff with Organization Staff role → identity is Organization-only for that invite org on accept
4. Confirm Start a Business still keeps Personal when upgrading
5. Confirm product-local roles are not offered on invite/add staff

## Phase status

Phase 16 remains **Under Validation**. P16-WP11 remains **In Progress**. P16-WP12 and Phase 17 were not started.
