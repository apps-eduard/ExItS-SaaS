# POS-REACT-RMAP-22D — Invitations, Reminders, Notifications

## Status

**PASS** (React surfaces on existing Platform contracts)

## Delivered

- Invitations list (resend/revoke for Pending)
- Explicit accept/decline page: `/personal/utang/invitations/accept?token=…`
- Invite + one-time reminder panel on relationship detail
- In-app notifications inbox + mark read
- My ExItS ID / public identity (`GET /api/v1/me/public-identity`)
- More-menu links
- Five-locale keys + message parity

## Explicit honesty

- Real push/SMS delivery remains `NullPersonalPushNotificationSink` — not faked
- Accept result fields `CreatedOrganizationMembership` / `GrantedProductRole` are not claimed as UI grants; server remains fail-closed for org membership from Utang invite

## Tests

| Suite | Result |
| --- | --- |
| `typecheck` | PASS |
| `message-parity` | PASS |
| `format:check` | PASS |

Native-speaker certification: **PENDING**

## Next

**RMAP-22E1 — Personal To-do backend**
