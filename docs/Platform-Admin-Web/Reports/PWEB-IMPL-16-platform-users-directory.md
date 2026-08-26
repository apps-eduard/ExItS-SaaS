# PWEB-IMPL-16 — Platform Users Directory

**Status:** COMPLETE

**Branch:** `feat/platform-admin-web-v2`

**Message:** `feat(platform-web): add platform users directory`

## Screen

One implementation for `/admin/users` with URL views:

- All Accounts — no directory
- Platform Staff — `directory=PlatformStaff`
- Organization Accounts — `directory=Organization`
- Personal Accounts — `directory=Personal`
- Needs Review — `directory=Unassigned`

Server contract: `GET /api/v1/platform/users` with `status`, `search`, `directory`, `sortBy`, `sortDesc`, `page`, `pageSize`.

Displayed fields are list DTO values only (display name, username, email, account classes, status). No password or token fields. No create/update/delete/suspend controls.

Rows link to `/admin/users/:userId` for PWEB-17. Authorization: `managePlatformUsers`; unauthorized fail-closed.

## Evidence

`docs/Platform-Admin-Web/Reports/impl-16-platform-users/`

## Visual approval

**AWAITING PRODUCT OWNER + CHATGPT**
