# PWEB-IMPL-17 — Platform User Detail

**Status:** COMPLETE

**Branch:** `feat/platform-admin-web-v2`

**Message:** `feat(platform-web): add platform user detail`

## Screen

Read-only `/admin/users/:userId` backed by:

- `GET /api/v1/platform/users/{userId}`
- `GET /api/v1/platform/authorization/assignments?platformUserId=`

Displayed fields are server-returned identity/profile values, account classes, organization scope, and role assignments. Role names are presentation only; authorization remains server-side. No assign/revoke/edit/suspend/delete controls.

Invalid GUID and 404 responses use safe not-found behavior. Unauthorized requests fail closed without privileged-content flash.

Roles & Permissions top-level nav remains disabled (`/admin/platform-roles` under development); no fake content added.

## Evidence

`docs/Platform-Admin-Web/Reports/impl-17-platform-user-detail/`

## Visual approval

**AWAITING PRODUCT OWNER + CHATGPT**
