# POS-REACT-RMAP-22E1 — Personal To-do Backend

## Status

**PASS** (additive Platform domain + API; no React UI)

## Delivered

- Domain: `PersonalTodo` with Open | Completed | Cancelled and None | Low | Normal | High
- Owner-only authorization helpers; related-entity metadata does not grant access
- Optimistic concurrency via `Version` / `expectedVersion`
- Application use cases: create, list own, get own, update, complete, reopen, cancel
- Persistence: `personal_todos` table via additive migration **AddPersonalTodos**
- API: `/api/v1/personal/todos` (GET list/by id, POST create, PUT/PATCH update, POST complete/reopen/cancel)
- Staff/org principals fail closed via `TryRequirePersonalAccountClass`

## Migration

| Item | Value |
| --- | --- |
| Name | `AddPersonalTodos` |
| Table | `platform.personal_todos` |

## Tests

| Suite | Result |
| --- | --- |
| `ExItS.Platform.UnitTests` PersonalTodo domain | PASS (create / complete / concurrency / reopen / cancel / owner) |

## Explicit exclusions

- React To-do UI / Today agenda (RMAP-22E2)
- RMAP-21 offline / outbox
- RMAP-B04 / B05 / Tax
- Fake historical to-dos / seed data
- Destructive migrations
- Real PersonalTodo reminder delivery pipeline (separate from Utang reminders)

## Next

**RMAP-22E2 — Personal To-do React + Today agenda composition**
