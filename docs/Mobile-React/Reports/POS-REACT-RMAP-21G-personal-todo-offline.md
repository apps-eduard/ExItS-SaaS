# POS-REACT RMAP-21G — Personal To-do offline

**Status:** PASS  
**Depends on:** 21B outbox, 21F Personal LocalStore isolation, 21F server-dedupe policy

## Delivered

| Capability | Evidence |
| --- | --- |
| Personal-only To-do cache (AES-GCM sealed body) | `personal-todo-cache.ts` |
| Schema store `personalTodos` (v5) | `db.ts` / `types.ts` |
| Queue create / update / complete / reopen / cancel | `personal-todo-offline.ts` |
| Cancel = delete-equivalent (API has no hard delete) | documented in module |
| Share / assign Offline | **OnlineRequired** (no proven API) |
| Fake push / local reminder dispatch | **NO** — reminder field stored only; notice shown |
| Staff reading Personal LocalStore | Forbidden via Personal scope assert |
| UI write-through + offline fallback | `PersonalTodoPages.tsx` |

## Dedupe policy

| Operation | Mode | Auto-retry after ambiguous transport |
| --- | --- | --- |
| `personal.todo.create` | none (server mints id) | NO — park for human |
| update / complete / reopen / cancel | target-state | YES |

## Flags

| Flag | Value |
| --- | --- |
| PERSONAL_TODO_OFFLINE | YES |
| PERSONAL_TODO_SHARE_OFFLINE | NO |
| FAKE_PUSH_NOTIFICATIONS | NO |
| PERSONAL_VS_ORG_ISOLATION | YES |

## Tests

`npx vitest run src/offline/personal-todo` — 21 passed

## Next

RMAP-21H reconnect / outbox processor / PWA / E2E / master report.
