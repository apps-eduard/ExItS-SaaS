# P16-WP11 Defect / Change Log — Organization Staff onboarding invite flow

**Status:** Open (P16-WP11 In Progress)  
**Phase:** Phase 16 — Implementation Complete, Under Validation  
**Work package:** P16-WP11 — Validation, Stabilization, and User Acceptance  
**Date:** 2026-08-03  
**Commit:** `020561af4f6b5280232ad101ac0212f3f4a288e4`  
**Commit message:** `fix(validation): implement proper organization staff onboarding`

## Title

Organization Staff onboarding was implemented as raw GUID membership linking instead of employee-style invitation

## Root cause

The Organization Staff page exposed **Add existing user** with a required **User ID GUID** as the primary action. That is an identity–membership linking tool, not Organization Staff onboarding. Organization Owners must never need an internal GUID to hire or invite staff.

## Corrected staff form

Primary action: **Invite Staff**

Shared person fields (`StaffPersonFieldsForm`):

- First Name, Last Name, Display Name, Email (required)
- Phone, Employee Code (optional)
- Require Email Verification (required choice)

Organization fields:

- Organization Role (Owner / Staff)
- Branch (optional)
- Product Role (optional; separate from Organization Role)

On success, navigation goes to **Invitations** (separate route/page).

## Organization Owner authority

Owners (and authorized org admins) invite staff for the **active organization only** via invitation APIs. They do not use GUID linking.

## New versus existing identity

- **New email:** create Organization-scoped pending identity (when verification required) + Organization profile + invitation + activation email (Mailpit in Local Validation). Activation sets password → Active and accepts pending invitations for that email.
- **Existing email:** do not create a duplicate identity; create an explicit invitation; accept later adds membership (and optional product role) only in the inviting organization. No silent Platform/Personal profile invention.

## Advanced GUID linking

Renamed **Link Existing Identity**:

- Visible only in Platform shell with `ManageMemberships`
- Requires reason (Admin UI)
- Audited as advanced identity link
- Not the default Owner onboarding path
- API rejects non-platform actors with guidance to use Invite Staff

## Invitation lifecycle

Statuses remain separated from account/membership status: Pending / Sent / Accepted / Expired / Revoked / Delivery Failed.

## Tests

Focused Admin tests cover Invite Staff as primary (no GUID primary), shared field validation, Platform-only advanced link + reason, API denial of non-platform GUID linking, and separate Invitations route/content.

## Manual Local Validation

Restart Local Validation; as Maria Santos (ABC Owner): Invite Staff → Invitations → Mailpit activation → staff appears under Organization Staff with Organization Role once and Product Role separate. Confirm Ana Cruz / XYZ isolation. As Platform Admin: GUID link is advanced-only with reason.

## Phase status

Phase 16 remains **Implementation Complete, Under Validation**.  
**P16-WP11 — In Progress.**  
**P16-WP12 — Not Started.**  
Phase 17 was not started.
