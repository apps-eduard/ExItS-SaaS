# POS-REACT SHELL NOTIFICATIONS SEPARATION 01

**Status:** COMPLETE  
**Start SHA:** `b3a07a88b55b8c6f03b3346b59ff8132bc356378`  
**Implementation commit:** `b1304116037156e933a71c70c2bcd0d528cff415`  
**Branch:** `feat/pos-react-client`

## Delivered

Separated Organization and Personal notification bells (MAUI parity) and continued shell / admin UX polish from the prior wave.

### Notifications (primary fix)

| Shell | Bell | Route | API |
|-------|------|--------|-----|
| Organization (`AppTopBar`) | Org bell | `/org/notifications` | `GET/POST …/organizations/{id}/notifications` |
| Personal (`PersonalShell`) | Personal bell | `/personal/notifications` | `GET/POST …/personal/notifications` |

- Org bell **does not** switch AccountProfile to Personal.
- Close on org notifications stays in Organization and returns to the prior org page (or `/org`).
- Personal close returns only within `/personal/*`.
- New: `organization-notifications-client.ts`, `OrgNotificationsPage`, `org-notifications.ts` (+ unit tests).
- Personal return helper kept for in-Personal navigation only.

### Related polish in this commit

- Branch fulfillment list/edit readiness checklist UI + e2e assertion updates
- Management dashboard / `ReportFilters` form-section polish
- Top bar: sticky shell, workspace pill, connection panel icons, account subtitle = role
- Personal notifications list/close alignment; `notifications-return` helper
- i18n keys for org notifications across `en`, `fil-PH`, `ceb-PH`, `ilo-PH`, `hil-PH`

## Tests / validation

| Check | Result |
|-------|--------|
| `npm run typecheck` | PASS |
| `org-notifications.test.ts` | PASS |
| `notifications-return.test.ts` | PASS |
| `message-parity.test.ts` | PASS |

## Exclusions

- Inline accept/decline for supplier-request notifications on the org list (Open → connected requests instead)
- Supplier-facing connected-PO deep links (`/connected-suppliers/incoming/…` not in React router yet)
- Backend / Platform API contract changes
- MAUI Blazor surfaces
- Full `ExItS.slnx` Release build

## Identity / security note

Org and Personal notification inboxes remain separate by AccountClass and API path. Opening the org bell must never select a Personal AccountProfile.

## Next

- Optional: wire supplier-facing PO notification deep links when that React route exists
- Optional: inline respond actions on org notification rows (MAUI parity)
