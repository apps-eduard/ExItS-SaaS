# P16-WP11 Defect / Change Log — Shared staff fields and Invitations navigation

**Status:** Open (P16-WP11 In Progress)  
**Phase:** Phase 16 — Implementation Complete, Under Validation  
**Work package:** P16-WP11 — Validation, Stabilization, and User Acceptance  
**Date:** 2026-08-03  
**Commit message:** `fix(validation): unify staff fields and repair invitations navigation`

## Title

Unify shared MVP staff person fields and repair Organization Staff → Invitations navigation so Invitations content actually loads

## Root cause (Invitations navigation)

Organization Staff and Invitations were declared as two `@page` directives on the **same** Blazor component (`OrganizationMembers.razor`). Navigating between `/members` and `/invitations` for the same `OrganizationId` reused the component instance. `OnParametersSetAsync` did not remount a new page, so section state (or path sync) could leave the Staff table visible even when the URL/menu showed Invitations.

## Fix

- **Separate page components and routes**
  - Staff: `/admin/organizations/{OrganizationId}/members` → `OrganizationMembers.razor`
  - Invitations: `/admin/organizations/{OrganizationId}/invitations` → `OrganizationInvitations.razor`
- Nav keys `org-staff` / `org-invitations` target those paths (no `?tab=invitations`)
- Active menu matching is path-based (`/invitations` → `org-invitations`, `/members` → `org-staff`)
- Invitations reloads when `OrganizationId` changes (`_loadedOrganizationId`)
- Authorization remains server-side; Admin gates with `ManageMemberships` / Owner membership

## Shared MVP staff fields

Reusable `StaffPersonFieldsModel` + `StaffPersonFieldsForm` (Ant Design Blazor):

- First Name, Last Name, Display Name, Email (required)
- Phone, Employee Code (optional)
- Require Email Verification (required choice)
- Account Status remains system-controlled

**Platform form** = shared fields + Platform Role + Staff Number (generated/immutable).  
**Organization invite form** = shared fields + Organization Role + Branch + Product Role.

## Invitation DTO / persistence

Invitation contracts keep concepts separated: Organization Role, Product Role, Branch, Invitation Status (user-facing; Pending shown as Sent after create), Account Status not used on Invitations. Optional invite snapshot fields persisted: invitee display name, first/last name, branch, product role.

## Tests

Focused Admin unit tests cover shared form usage, separate routes, org-context reload hooks, validation (required names/email; optional phone/employee code; overlong phone), Member never as label, org vs product role separation, invitation status column (not account status), and permission gating for Invitations page.

## Manual Local Validation

- Platform: shared fields + Platform Role; Staff Number generated and immutable on edit
- Organization: Staff table → Invitations changes URL, title, and content; refresh/back/forward follow route; org switch reloads invitations; Organization Role once; Product Role separate

## Phase status

Phase 16 remains **Implementation Complete, Under Validation**.  
**P16-WP11 — In Progress.**  
**P16-WP12 — Not Started.**  
Phase 17 was not started.
