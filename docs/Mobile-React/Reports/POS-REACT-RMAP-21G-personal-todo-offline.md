# POS-REACT RMAP-21G — Personal To-do offline

**Status:** PASS
**Dependencies:** 21A matrix; 21B LocalStore + encrypted outbox; 21C Connection & Sync UI; 21D offline Sell + Cash; 21E Business customers offline; 21F Personal Utang offline (Personal store, scope stamp, dedupe policy)

## Capability flags

| Flag                                    | Value                           |
| --------------------------------------- | ------------------------------- |
| `OFFLINE_PERSONAL_TODO_CACHE`           | YES (encrypted, private-by-default) |
| `OFFLINE_PERSONAL_TODO_CREATE`          | YES                             |
| `OFFLINE_PERSONAL_TODO_UPDATE`          | YES (with `expectedVersion`)    |
| `OFFLINE_PERSONAL_TODO_COMPLETE`        | YES                             |
| `OFFLINE_PERSONAL_TODO_REOPEN`          | YES                             |
| `OFFLINE_PERSONAL_TODO_CANCEL`          | YES (the API's delete-equivalent) |
| `OFFLINE_PERSONAL_TODO_HARD_DELETE`     | NO (no such route exists)       |
| `OFFLINE_PERSONAL_TODO_SHARE`           | NO (`OnlineRequired`)           |
| `OFFLINE_PERSONAL_TODO_REMINDER_DELIVERY` | NO (no local notification, real or simulated) |
| `FAKE_PUSH_NOTIFICATIONS`               | NO                              |
| `PERSONAL_STORE_FOR_ORG_STAFF`          | NO (refused, fail-closed)       |
| `PERSONAL_ORG_DB_ISOLATION`             | YES (separate DB + scope stamp) |
| `SERVER_DEDUPE_ON_PERSONAL_ROUTES`      | NO (create can duplicate; transitions converge) |
| `OFFLINE_BUSINESS_UTANG_CHECKOUT`       | NO (unchanged, 21D)             |
| `OFFLINE_GCASH`                         | NO (unchanged, 21D)             |
| `DEVICE_SHIFT_SELL_PRESERVED`           | YES                             |

## Why every To-do mutation the API offers is offline-capable

21E's test was "does the server accept a client-chosen id and an `Idempotency-Key`". 21F's was "is
this the person recording something about their own money that needs no live state and no second
human". A private To-do passes the 21F test more cleanly than anything before it: it is a note the
person writes to themselves, nobody else is bound by it, and no money moves.

| Route                                  | Server evidence                                                                | Verdict         |
| -------------------------------------- | ------------------------------------------------------------------------------ | --------------- |
| `POST /api/v1/personal/todos`           | `PersonalEndpoints.cs` → `CreatePersonalTodo`; owner-scoped by `PlatformUserId`, no counterparty | Offline allowed |
| `PUT /api/v1/personal/todos/{id}`       | `UpdatePersonalTodo`; accepts optional `ExpectedVersion`                        | Offline allowed |
| `POST .../todos/{id}/complete`          | `CompletePersonalTodo`; assigns a target status to a row the owner already has  | Offline allowed |
| `POST .../todos/{id}/reopen`            | `ReopenPersonalTodo`; same shape                                               | Offline allowed |
| `POST .../todos/{id}/cancel`            | `CancelPersonalTodo`; the only removal the API offers                          | Offline allowed |
| hard delete                             | **No route exists.** Cancel is the delete-equivalent, so that is what "delete offline" queues | N/A |
| share / assign to another person         | **No route exists** — nothing to prove safe and nothing to approximate         | `OnlineRequired` |
| reminder delivery                       | `reminderAtUtc` is a server-side field; delivery is a server act                | `OnlineRequired` |

### Idempotency: honest, and better than 21F for four of the five

The Personal routes still accept no client-supplied id and consult no idempotency store, so
`SERVER_DEDUPE_ON_PERSONAL_ROUTES` stays **NO**. But the To-do routes split into two genuinely
different risk classes, and `src/offline/server-dedupe-policy.ts` now records that split:

- `personal.todo.create` → `"none"`. The server mints the To-do id, so a replay creates a second
  To-do. Treated exactly like the 21F Personal mutations: `mayAutoRetry` refuses to auto-replay it
  after an **ambiguous** transport failure, and it is parked for the person to confirm.
- `personal.todo.update` / `complete` / `reopen` / `cancel` → new mode `"target-state"`. Each
  addresses an existing row by its own id and assigns a state rather than appending. Replaying
  "this is done" converges on the same row, so these **may** be auto-retried after an ambiguous
  failure. This is a property of the request shape, not an assumption about the server.

A duplicate private To-do is also the mildest failure in the offline programme — no money, no
counterparty, and the person can cancel it — which is why create is permitted offline at all despite
the missing server dedupe.

## Delivered

| Capability                                    | Evidence                                                                                                          |
| --------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| Private-by-default To-do LocalStore           | `src/offline/personal-todo-cache.ts` — AES-GCM per row under the Personal scope key; the store is only ever opened through the Personal context |
| Personal store only, never the Org DB         | Reuses `usePersonalOfflineContext` (21F): `exits-offline-Personal-<userId>:Personal`                              |
| Staff can never open it                       | `personalOfflineEligibility` refuses `organizationContextLocked` (staff), non-`Personal` class, `Platform`, unknown class, and no session |
| Every To-do write asserts its scope           | `assertOfflineScope(db, "Personal")` in both the cache and the enqueue module                                     |
| Schema version 5                              | `OFFLINE_SCHEMA_VERSION = 5`; adds `personalTodos` (index `byStatus`), upgrading in place                          |
| Title, notes, times, priority, related pointer encrypted | Only `id`, `status`, `origin`, `serverId`, `pendingLocalChange` and sync bookkeeping stay readable, so the agenda tabs can filter without decrypting the content |
| Fail-closed cache reads                       | `[]` / `null` on a missing store, corrupt envelope, or foreign scope key — never an authoritative empty agenda      |
| Queued create                                 | `enqueuePersonalTodoCreate` → `POST /api/v1/personal/todos` on the **platform** API; local id becomes a `{{local:…}}` placeholder for later operations |
| Queued update with concurrency                | `enqueuePersonalTodoUpdate` sends the version the person was actually looking at, so the server can reject a stale edit instead of silently overwriting a newer one |
| Queued complete / reopen / cancel             | `enqueuePersonalTodoTransition`; `expectedVersion: null` by design — a target state must not be rejected because the version was read hours ago |
| Offline edits to a still-queued To-do          | `dependsOnOperationId` + a `{{local:…}}` path placeholder resolved by the 21H processor, so "create then complete" offline stays one row |
| A local change survives a stale server read   | `cachePersonalTodos` will not overwrite a row whose `pendingLocalChange` is still true                             |
| Sharing refused structurally                  | `rejectOfflineTodoShare()` and a guard on the create path, so a future caller cannot widen the surface by passing a share target |
| New online-required code                      | `personal_todo_share` — localized in all five locales                                                             |
| To-do agenda offline                          | `PersonalTodoPages.tsx` — `enabled: online`, cached-list fallback, cached-data notice, "waiting to sync" chip on pending rows |
| Reminder honesty in the UI                    | `offline.todoNoReminders` states plainly that a reminder set offline will not fire until the To-do reaches the server |
| Auth tokens in IndexedDB                      | **NO**                                                                                                            |
| Workbox API caching                           | **NO** (API routes remain `NetworkOnly`)                                                                           |

## Offline To-do flow

1. Online: every successful To-do read is written through to the encrypted Personal cache.
2. Network drops: the To-do queries stop issuing requests (`enabled: online`) and the agenda renders
   from the cache instead of burning react-query retries on a dead network.
3. Add a To-do: the client picks the id, queues `personal.todo.create`, and the row appears
   immediately marked "waiting to sync". If a reminder time was set, the UI says outright that the
   reminder cannot fire yet.
4. Complete / reopen / cancel: the cached row flips state at once and the transition is queued. If
   the To-do itself is still queued, the transition gets `dependsOnOperationId` and a
   `{{local:…}}` placeholder in its path.
5. Edit: the queued body carries the cached `version`, so the server — not the device — decides
   whether the edit is stale.
6. Nothing is sent in this package. Queued rows stay `Pending` until the 21H processor runs.

## Fail-closed and honesty rules

| Situation                                        | Behavior                                                                                    |
| ------------------------------------------------ | ------------------------------------------------------------------------------------------- |
| Organization staff principal                     | `personalOfflineEligibility` → `staff-locked`; no Personal database is opened or created     |
| Owner currently in the Organization context      | → `not-personal`; the To-do store belongs to the Personal identity                          |
| To-do write attempted on an Organization DB      | `assertOfflineScope` throws `scope mismatch`; nothing is queued or cached (asserted in tests) |
| Cache unreadable, corrupt, or from another Personal user | Reads return `[]` / `null`, never an authoritative "you have nothing to do"           |
| Transition or edit on a To-do this device never read | `offline.personal.todo.not_cached`; nothing is queued and nothing is invented            |
| Empty title                                      | `offline.personal.todo.title_required`; nothing is queued                                   |
| Offline share attempt                            | `offline.requiredPersonalTodoShare`; nothing is queued                                      |
| Reminder set offline                              | `offline.todoNoReminders` — the device raises **no** notification, real or simulated         |
| Secure randomness unavailable                     | Enqueue is refused (`offline.todoEnqueueFailed`); no guessable id is minted                 |
| Ambiguous transport failure on `todo.create`      | `mayAutoRetry` → `false`; parked for confirmation                                           |
| Ambiguous transport failure on a transition/edit  | `mayAutoRetry` → `true`, justified by the `target-state` request shape                       |
| Locally queued rows in the agenda                 | Marked with a "waiting to sync" chip                                                        |

## Explicit non-claims

- No sync processor in this package. Queued To-do operations stay `Pending` until 21H.
- **No push notification, local notification, scheduled alarm, or simulated reminder is raised
  offline.** A reminder time saved offline is inert until the row reaches the server.
- No offline sharing or assigning of a To-do. The platform API has no such route; this is not a
  deferred implementation of an existing capability.
- No offline hard delete, because the API has none. Cancel is what "delete" queues, and the report
  does not call that a delete.
- Two offline devices can complete or edit the same To-do; last writer wins on the server for the
  transitions, and `expectedVersion` makes the server reject the losing **edit** rather than merge.
- A queued create can still duplicate if the response is lost and the person retries manually. This
  is accepted only because the blast radius is one private note.
- The Personal scope key remains `<userId>:Personal`, not `<userId>:<accountProfileId>` (21F).
- One IndexedDB schema serves both scopes, so an Organization database physically contains an empty
  `personalTodos` store. The `meta.scopeKind` stamp plus `assertOfflineScope` is what keeps rows from
  crossing.
- Not native SecureStorage / Keystore / Keychain parity. `COLD_START_OFFLINE_UNLOCK` remains
  `DEFERRED_SECURITY_GAP` from 21B: a warm browser profile can read the cached To-do content without
  re-authenticating.

## Tests

- `src/offline/personal-todo-cache.test.ts` (8) — encrypted round-trip; **no title, notes, due time,
  reminder time, priority, or related-entity pointer in the stored row** while `status` stays
  readable for the tabs; a foreign Personal scope key cannot decrypt; one Personal user's to-dos are
  absent from another's database; a To-do write into an Organization DB is refused with nothing
  written; a stale server read does not overwrite a change still waiting in the outbox; an unknown
  To-do is not invented; the agenda tabs filter correctly off cached rows
- `src/offline/personal-todo-offline.test.ts` (13) — create queued against the **platform** API with
  `scopeKind: "Personal"` and null org/branch; empty title and offline share rejected; update carries
  the cached `expectedVersion`; complete / reopen / cancel queue with `expectedVersion: null` and
  flip the cached row; a transition on a still-queued To-do carries `dependsOnOperationId` and a path
  placeholder that resolves to the server id; transition and edit on an uncached To-do rejected; a
  To-do cannot be queued into an Organization store; queued plaintext absent from safe sync metadata;
  `serverDedupeMode` returns `target-state` for the four transitions and `none` for create, and
  `mayAutoRetry` follows that split
- `src/offline/schema-upgrade.test.ts` (2) — a v4 database upgrades in place to v5, gains
  `personalTodos`, and its queued operation survives; the scope stamp still rejects the wrong scope
- `src/offline/cash-sale-offline.test.ts` — updated (not weakened) to assert schema v5 and the full
  store list including `personalTodos`

Offline module: **11 files, 82 tests passed**.
Full suite: `npx vitest run` — **90 files, 444 tests passed**. `npm run typecheck` clean.
`npm run lint` — 0 errors, 17 pre-existing `react-refresh` warnings, none from this package's files.

## Gaps carried to 21H

- **No server-side idempotency on any Personal route.** `todo.create` can duplicate on a lost
  response; the policy module keeps the processor from making it worse.
- No conflict UX for a queued To-do the server rejects on sync — the `expectedVersion` rejection path
  needs 21H to surface it as a `Conflict` the person can resolve.
- Placeholder resolution depends on the 21H processor writing the server id into `entityMap` when the
  parent create succeeds.
- Reminders remain server-delivered only. Offline-scheduled local notifications would need a service
  worker notification design and explicit permission handling; deliberately out of scope.
- The Personal dashboard and social surfaces are still online-only reads.

## Next

RMAP-21H — outbox sync processor, reconnect recovery, PWA verification, and E2E.
