# P18-WP03 — Organization Selection and Owner Essentials

| Field | Value |
|---|---|
| Status | **Code Complete and Build Verified; Device Validation Pending** |
| Phase | [Phase 18](../phases/phase-18-mobile-personal-organization-and-pos-experience.md) |
| Implementation commit | `4b8b727` |
| Device validation | **Device Validation Blocked** |
| Date | 2026-08-03 |

## 1. Objective

Organization selector and Mobile Organization Owner essentials so owners can operate without being forced to Web for the MVP essentials set.

## 2. Scope

Mobile essentials only. Full Organization Administration remains Web.

## 3. Existing functionality reused

- Platform organizations, members, invitations, product-local roles, subscription current, entitlement snapshot latest
- POS operational setup launch route `/setup` (Phase 17)
- Preference store for selected organization id

## 4. Backend / API work completed

- Client wrappers for org get/update, members list, membership suspend/revoke, invitations, product-local role assign/revoke, subscription/entitlement reads
- Reuse only — no parallel Org Admin API invented

## 5. MAUI screens and flows completed

- `/organization-select` — organization name and membership role; Start a Business CTA when empty
- `/org` summary — status, subscription, entitlement, POS access, enter POS / setup path
- `/org/profile`, `/org/subscription`, `/org/staff`, `/org/staff/invite`, `/org/staff/assign`
- Reminder copy: **For full organization administration, use the Web application.**

## 6. Files / components changed (representative)

- `Maui/Components/Pages/OrganizationSelect.razor`
- `Maui/Components/Pages/Organization/OrgSummary.razor`, `OrgProfile.razor`, `OrgSubscription.razor`, `OrgStaff.razor`, `OrgStaffInvite.razor`, `OrgStaffAssign.razor`

## 7. Authorization and organization-isolation behavior

Org essentials calls use Platform session scoped to the authenticated user and selected organization. POS role grants are organization-scoped product-local roles. Suspend/remove and revoke are server-authorized.

## 8. Tests executed and totals

| Suite | Result |
|---|---|
| MAUI.Tests | **73 passed** |
| Platform unit filter (product-local / StartBusiness related) | included in **60 passed** Platform filter run |

## 9. MAUI build result

**Build Verified**.

## 10. Emulator / device validation result

**Device Validation Blocked**.

## 11. Known limitations

- Staff list UI shows user identity identifiers where display-name enrichment is not returned
- Invitation acceptance UX remains email/token based (Platform), not a full in-app accept wizard
- Web reminder is informational; Web remains required for full admin

## 12. Deferred items

Full Web Org Admin parity on Mobile; advanced audit/history tables; multi-owner (explicitly out of MVP — one Organization Owner).

## 13. Current status

Implemented · Tested · Build Verified · Device Validation Blocked

## 14. Commit reference

Implementation: `4b8b727`. Documentation reconciliation: Phase 18 docs tip on `main`.
