# POS-REACT-RMAP-22C — Personal Utang Core

## Status

**PASS** (React core against existing Platform `/api/v1/personal/utang/*`)

## Delivered

- API client: contacts, lent/borrowed lists, relationship detail/balance/history, create relationship, record Loan/Payment/Adjustment with `expectedVersion`
- People create/list UI
- Money I lent / Money I owe create + list
- Relationship detail: balance, due chip (overdue/soon), activity history, payment/add amount/correct balance
- 409 concurrency → friendly refresh message
- Five-locale keys + message parity

## Explicit non-claims

- Invitations / reminders / notifications (RMAP-22D)
- Personal To-do (RMAP-22E)
- Offline (RMAP-21)

## Tests

| Suite | Result |
| --- | --- |
| `personal-utang-client.test.ts` | PASS |
| `message-parity.test.ts` | PASS |
| `typecheck` | PASS |

Native-speaker certification: **PENDING**

## Next

**RMAP-22D — Invitations, reminders, notifications**
