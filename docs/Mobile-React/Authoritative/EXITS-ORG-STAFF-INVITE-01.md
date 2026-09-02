# EXITS-ORG-STAFF-INVITE-01 — ExItS-native Staff Invitation

## Purpose

Organization Owner invites an **existing ExItS Personal user** by Personal EX-ID or Personal QR. The invitee receives an in-app Personal notification and must Accept or Decline. Acceptance creates a **separate organization-scoped staff identity** (not Personal membership).

## UX

1. Manage Staff → Invite Staff  
2. Enter EX-ID or scan Personal QR  
3. Confirm recipient  
4. Choose POS product role (Cashier / Store Manager / POS Owner); Organization role is Staff  
5. Send → Personal notification  
6. Personal → Staff invitations → Accept (set staff password) / Decline  

## Identity model

- Personal identity = invitation destination / consent only  
- Accept → `CreateOrganizationStaff` + `HomeOrganizationId` + server `local@ORG######` login  
- `LinkedPersonalUserId` correlation only  
- Personal password is never copied  

## Notification

- `relatedType`: `OrganizationStaffInvitation`  
- Deep link: `/personal/staff-invitations`  

## Authorization

- Owner-only create / resolve (server)  
- Accept/Decline only by `TargetPersonalUserId`  
- Personal session may call recipient routes under `/api/v1/platform/invitations/my-pending`, `.../{id}/decline`, and `.../{id}/accept-as-personal` (AccountScopeGuard allowlist)  
- No email primary invite in React; legacy email create remains API-compatible  

## Branch assignment

Branch is **not** assigned during ExItS-native invitation in this package.
Owner assigns branch after acceptance via existing Manage Staff / membership branch flows.

## Non-goals

- No Personal↔Staff identity merge  
- No email transport / SMS / push  
- No offline queue  
- Ownership Transfer remains separate  

## Tests

- Domain: Decline / AcceptForPersonalTarget  
- API: resolve-target, create by EX-ID, my-pending, accept-by-id, decline  
- React: invite steps + Personal review  
