# P16-WP11 Defect / Change Log — Staff fields and Organization UI

**Status:** Open (P16-WP11 In Progress)  
**Phase:** Phase 16 — Implementation Complete, Under Validation  
**Work package:** P16-WP11 — Validation, Stabilization, and User Acceptance  
**Date:** 2026-08-03  
**Commit message:** `fix(validation): finalize staff fields and organization UI`

## Title

Finalize MVP Platform/Organization Staff fields, Invitations route, product/role deduplication, and Organization Staff actions UI

## Root causes

1. **Invitations navigation** — Organization Staff and Invitations shared `/members` with a query `?tab=` that did not reliably remount content; path stayed on members and the staff table remained visible.
2. **My Products duplication** — Discovery listed product codes from subscription joins without stable product-id/code dedupe; UI also rendered `DisplayName (product-code)`.
3. **Organization role “Staff Staff” / “Owner Owner”** — Role column rendered both a Tag label and an inline Select that repeated the same display label.
4. **Platform Staff fields** — Create/edit collected username-centric identity without First/Last, Staff Number, or Require Email Verification → Pending Verification semantics.

## Final terminology (user-facing)

| Concept | Values |
|---|---|
| Account class | Platform, Organization, Personal |
| Organization role | Owner, Staff |
| POS / product role | POS Owner, Store Manager, Cashier, Reporting User |
| Account status | Pending Verification, Active, Suspended, Deactivated |
| Invitation status | Pending, Sent, Accepted, Expired, Revoked, Delivery Failed |

Do not display Member, Disabled, or Removed as labels. Keep account status, invitation status, organization role, and product role separate.

## MVP field model

### Platform Staff

Required: First Name, Last Name, Display Name, Email, Platform Role, Require Email Verification  
Generated: Staff Number (`STF-000001`, unique, immutable, server-generated), Account Status, Created At, Created By  
Optional: Phone, Employee Code

### Organization Staff

Required: First Name, Last Name, Display Name, Email, Organization Role (Owner/Staff), Require Email Verification  
Optional: Phone, Employee Code, Branch, POS/Product Role (separate column/badge group)

## Fixes

- Dedicated route `/admin/organizations/{id}/invitations`; nav keys `org-staff` / `org-invitations`; path-based section load; deep-link and refresh supported
- `DiscoverEnabledProducts` GroupBy product code + `seenProductIds`; display name **Pinoy Business POS**; internal key unchanged `pinoy-business-pos`
- Single Organization Role Tag; Product Roles column from membership query grants
- Icon-only Ant Design actions with tooltips / aria-labels; status- and permission-aware visibility; confirmations for destructive actions
- Owner Tag uses gold + bold text (not color-only); Staff uses neutral Tag
- Staff Number via `IStaffNumberGenerator` / `EfStaffNumberGenerator`; create/edit APIs and Admin Users UI

## Tests

Focused unit/admin tests cover Staff Number uniqueness/immutability, platform role required, Organization Owner/Staff-only roles, Member never as label, Invitations dedicated route, My Products uniqueness + display name, product vs organization role separation, Pending Verification sign-in block, activation → Active, last-owner safeguards (existing), and related WP11 cases.

## Manual Local Validation

- Platform: create staff → Staff Number generated; role required; edit allowed fields; Staff Number unchanged
- Organization: Organization Staff → Invitations changes URL and content; refresh stays on Invitations; Owner/Staff once; POS role separate; icon actions with tooltips
- My Products: Pinoy Business POS once; no raw `pinoy-business-pos` label; launch still works

## Phase status

Phase 16 remains **Implementation Complete, Under Validation**.  
**P16-WP11 — In Progress.**  
**P16-WP12 — Not Started.**  
Phase 17 was not started.
