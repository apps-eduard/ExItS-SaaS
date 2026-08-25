# POS-REACT RMAP-21F — Personal Utang offline

**Status:** PASS
**Dependencies:** 21A matrix; 21B LocalStore + encrypted outbox; 21C Connection & Sync UI; 21D offline Sell + Cash; 21E Business customers offline

## Capability flags

| Flag                                      | Value                    |
| ----------------------------------------- | ------------------------ |
| `OFFLINE_PERSONAL_UTANG_CACHE`            | YES (encrypted)          |
| `OFFLINE_PERSONAL_CONTACT_CREATE`         | YES                      |
| `OFFLINE_PERSONAL_RELATIONSHIP_CREATE`    | YES (contact side only)  |
| `OFFLINE_PERSONAL_ENTRY_LOAN`             | YES                      |
| `OFFLINE_PERSONAL_ENTRY_PAYMENT`          | YES                      |
| `OFFLINE_PERSONAL_ENTRY_ADJUSTMENT`       | NO (`OnlineRequired`)    |
| `OFFLINE_PERSONAL_CONTACT_IDENTITY_LINK`  | NO (`OnlineRequired`)    |
| `OFFLINE_PERSONAL_UTANG_INVITE`           | NO (`OnlineRequired`)    |
| `OFFLINE_PERSONAL_UTANG_QR_SHARE`         | NO (`OnlineRequired`)    |
| `OFFLINE_PERSONAL_UTANG_ACCEPT_DECLINE`   | NO (`OnlineRequired`)    |
| `OFFLINE_PERSONAL_UTANG_REMINDER`         | NO (`OnlineRequired`)    |
| `PERSONAL_STORE_FOR_ORG_STAFF`            | NO (refused, fail-closed) |
| `PERSONAL_ORG_DB_ISOLATION`               | YES (separate DB + scope stamp) |
| `SERVER_DEDUPE_ON_PERSONAL_ROUTES`        | NO (recorded honestly)   |
| `OFFLINE_BUSINESS_UTANG_CHECKOUT`         | NO (unchanged, 21D)      |
| `OFFLINE_GCASH`                           | NO (unchanged, 21D)      |
| `DEVICE_SHIFT_SELL_PRESERVED`             | YES                      |

## Why these three mutations are offline-capable and the rest are not

The test for 21E was "does the server accept a client-chosen id and an `Idempotency-Key`". No
Personal route passes that test, so 21F needed a different and stricter test: **is this something
the person holding the device is recording about their own money, which the server needs no live
state and no second human to accept?**

| Route | Server evidence | Verdict |
| ----- | --------------- | ------- |
| `POST /api/v1/personal/utang/contacts` | `PersonalEndpoints.cs` → `CreatePersonalContact`; a name in the owner's own address book, scoped to `PlatformUserId` | Offline allowed |
| `POST /api/v1/personal/utang/relationships` | `CreatePersonalDebtRelationship`; accepted with a contact on the counterparty side and the owner on the other | Offline allowed (contact side only) |
| `POST .../relationships/{id}/entries` (`Loan`, `Payment`) | `RecordPersonalUtangEntry`; append-only, `ExpectedVersion` is optional, and the server recomputes `BalanceAfter` | Offline allowed |
| `POST .../relationships/{id}/entries` (`Adjustment`) | Same route, but an Adjustment rewrites a balance to a number the device believed at the time | `OnlineRequired` |
| relationship create naming a second `UserIdentityId` | Creates an obligation for somebody who is not holding this device | `OnlineRequired` |
| contact create with `LinkedUserIdentityId` | Attaches a real ExItS account to a private contact — an identity decision | `OnlineRequired` |
| `POST .../relationships/{id}/invitations`, `/invitations/accept`, `/decline`, `/{id}/resend`, `/{id}/revoke` | Every one needs the other party and live invitation state | `OnlineRequired` |
| `POST .../relationships/{id}/reminders`, `/reminders/{id}/deliver`, `/cancel` | Delivery is a server act; a device must not pretend it notified anyone | `OnlineRequired` |

### The idempotency gap, stated plainly

None of the Personal Utang routes accept a client-supplied id and none consult an idempotency
store — `CreatePersonalContact` only rejects a duplicate **email** with
`PersonalContactEmailConflict`, and a contact with no email is created again on a replay. So a
replayed Personal mutation can create a second contact, a second debt, or a second payment against
a friend.

A browser cannot distinguish "the request never left the device" from "the server committed and the
response was lost". Rather than paper over that, the gap is encoded:

- `src/offline/server-dedupe-policy.ts` records `serverDedupeMode(operationType)`. The three
  Personal types return `"none"`; POS money types return `"idempotency-key"`.
- `mayAutoRetry(operationType, failure)` lets the 21H processor retry freely when the request was
  never dispatched, but **refuses to auto-retry a Personal mutation after an ambiguous transport
  failure**. Those will be parked for the person to confirm instead of silently doubling a debt.
- The local operation id is still sent as the queue's idempotency key, so the day these routes learn
  to deduplicate, the key is already the right one.

`SERVER_DEDUPE_ON_PERSONAL_ROUTES = NO` is therefore a real, carried gap and not a passing grade.

## Delivered

| Capability | Evidence |
| ---------- | -------- |
| Personal LocalStore, never the Org DB | `src/offline/personal-offline-context.ts` — opens `exits-offline-Personal-<userId>:Personal`; the Organization store is a different database entirely |
| Staff can never open a Personal store | `personalOfflineEligibility` refuses `organizationContextLocked` (staff), non-`Personal` account class, `Platform`, unknown class, and no session — the database is not even created |
| Scope stamped in the database | `src/offline/db.ts` writes `meta.scopeKind` at open time; `assertOfflineScope` lets any writer fail closed instead of trusting its caller |
| Every Personal write asserts its scope | `personal-utang-cache.ts` and `personal-utang-offline.ts` call `assertOfflineScope(db, "Personal")` before touching a store |
| Schema version 4 | `OFFLINE_SCHEMA_VERSION = 4`; adds `personalContacts`, `personalRelationships` (index `byPerspective`), `personalEntries` (index `byRelationship`) |
| Encrypted people / lent / borrowed / history cache | `src/offline/personal-utang-cache.ts` — AES-GCM per row under the Personal scope key; only local routing ids, `origin`, `perspective`, and timestamps stay readable |
| Fail-closed cache reads | Every read returns `[]` / `null` on a missing store, a corrupt envelope, or a foreign scope key |
| Cached Personal identity id | `cachePersonalUserIdentityId` stores `userIdentityId` from `/api/v1/personal/me` so an offline debt can name its owner. An identifier, **not** a credential |
| Queued contact upsert | `enqueuePersonalContactCreate` → `POST /api/v1/personal/utang/contacts` on the **platform** API |
| Queued relationship, dependent on its contact | `enqueuePersonalRelationshipCreate` sets `dependsOnOperationId` to the contact operation, and puts a `{{local:<id>}}` placeholder where the server contact id will go |
| Local-reference resolution for the processor | `src/offline/queued-request.ts` — `localRefToken`, `collectLocalRefs`, `resolveLocalRefs`; unresolved placeholders make the request unsendable rather than sending a literal placeholder |
| Queued entry record | `enqueuePersonalUtangEntry` for `Loan` / `Payment`, with `expectedVersion: null` because the entry is append-only and the server recomputes the balance |
| Adjustment refused structurally | `rejectOfflineAdjustment()` plus a UI block, so a future caller cannot widen the offline surface by passing `entryType: "Adjustment"` |
| New online-required codes | `personal_contact_link`, `personal_utang_invite`, `personal_utang_reminder`, `personal_utang_adjustment` — all four localized in all five locales |
| People page offline | `PersonalUtangPages.tsx` — `enabled: online`, cached list fallback, cached-data notice, queue on save |
| Lent / Owe pages offline | Cached contacts + cached perspective list, queue on save, "waiting to sync" chip on local rows |
| Detail page offline | Cached relationship + cached history, last-agreed balance, queued Loan/Payment, invite & reminder panel replaced by its online-required reason |
| Auth tokens in IndexedDB | **NO** |
| Workbox API caching | **NO** (API routes remain `NetworkOnly`) |

## Offline Personal Utang flow

1. Online: each successful contacts / lent / borrowed / relationship / history read is written
   through to the encrypted Personal cache, and `/personal/me` seeds the cached identity id.
2. Network drops: the Personal Utang queries stop issuing requests (`enabled: online`) and the pages
   render the cache instead of burning react-query retries on a dead network.
3. Add a person: the client picks the contact id, queues `personal.contact.create`, and shows the
   contact immediately marked "waiting to sync".
4. Record a debt: if the chosen contact is itself still queued, the relationship operation is given
   `dependsOnOperationId` **and** its body carries `{{local:<contactId>}}` where the server contact id
   belongs. The 21H processor rewrites the placeholder from the entity map after the contact posts.
5. Record a payment: `personal.utang.entry.record` is queued with `expectedVersion: null`. The
   optimistic history row is labelled `Local` and its running balance is explicitly the device's
   estimate.
6. Nothing is sent in this package. Queued rows stay `Pending` until the 21H processor runs.

## Fail-closed and honesty rules

| Situation | Behavior |
| --------- | -------- |
| Organization staff principal | `personalOfflineEligibility` → `staff-locked`; no Personal database is opened or created |
| Owner currently in the Organization context | → `not-personal`; the Personal store belongs to the Personal identity, not the org session |
| Personal write attempted on an Organization DB | `assertOfflineScope` throws `scope mismatch`; nothing is queued or cached (asserted in tests) |
| Cache unreadable, corrupt, or from another Personal user | Reads return `[]` / `null`, never an authoritative "nobody owes you anything" |
| Device has never learned the Personal identity id | Relationship enqueue refuses with `offline.personal.relationship.owner_unknown`; the UI asks the person to open Utang once online |
| Offline identity link attempt | `offline.personal.contact.identity_link_not_supported`; nothing is queued |
| Offline debt against another ExItS account | `offline.personal.relationship.counterparty_identity_not_supported`; nothing is queued |
| Offline Adjustment | Submit disabled with `offline.requiredPersonalUtangAdjustment`; no local balance rewrite |
| Offline invite / QR / accept / reminder | Panel replaced by `offline.requiredPersonalUtangInvite`; no fake notification is ever raised |
| Secure randomness unavailable | Enqueue is refused (`offline.personalEnqueueFailed`); no guessable id is minted |
| Ambiguous transport failure on a Personal mutation | `mayAutoRetry` → `false`; the 21H processor must not silently replay it |
| Locally queued rows in a list | Marked with a "waiting to sync" chip, so a device-only debt is never presented as agreed |

## Explicit non-claims

- No sync processor in this package. Queued Personal operations stay `Pending` until 21H.
- A queued entry is **not** a settled payment, and the balance shown offline is the last figure the
  server gave this device. The client does no authoritative balance math; the `balanceAfter` on a
  local history row is a labelled estimate.
- No offline invitations, QR share, accept, decline, resend, revoke, reminders, or reminder delivery.
  **No push notification is raised offline, real or simulated.**
- No offline Adjustment entry, and no offline linking of a contact to an ExItS identity.
- No Personal↔Business customer linking (unchanged from 21E).
- No offline Business Utang checkout, no offline GCash (unchanged from 21D).
- The Personal scope key is `<userId>:Personal`, not `<userId>:<accountProfileId>`. The browser
  session snapshot does not expose the selected AccountProfile id, and a Personal identity has at
  most one active Personal profile. This still isolates one Personal user from another and every
  Personal store from every Organization store, but it is not profile-level isolation.
- One IndexedDB schema serves both scopes, so an Organization database physically contains empty
  `personalContacts` / `personalRelationships` / `personalEntries` stores and vice versa. The
  `meta.scopeKind` stamp plus `assertOfflineScope` is what keeps rows from crossing; the empty
  stores themselves are inert.
- Not native SecureStorage / Keystore / Keychain parity. `COLD_START_OFFLINE_UNLOCK` remains
  `DEFERRED_SECURITY_GAP` from 21B: the cache key is derived from the Personal scope binding, so a
  warm browser profile can read the Personal cache without re-authenticating. This is the most
  sensitive data in the app and the gap is carried, not closed.

## Tests

- `src/offline/personal-utang-cache.test.ts` (11) — encrypted round-trip of contacts, relationships
  and history; **no contact name, phone, email, note, or balance in the stored row**; a foreign
  Personal scope key cannot decrypt; one Personal user's utang is absent from another's database;
  Personal writes into an Organization DB are refused with nothing written; cached identity id
  round-trip; Lent and Borrowed projections stay separate; eligibility refuses staff, org context,
  Platform, unknown class, and no session
- `src/offline/personal-utang-offline.test.ts` (16) — contact queued against the **platform** API
  with `scopeKind: "Personal"` and null org/branch; identity link and nameless contact rejected;
  lent relationship carries the owner as creditor and a `{{local:…}}` placeholder for its unsynced
  contact, with `dependsOnOperationId` set; borrowed relationship puts the owner on the debtor side;
  counterparty-identity and unknown-owner relationships rejected; payment queued with
  `expectedVersion: null` and a labelled local balance; entry against an unsynced relationship
  routes through a path placeholder that resolves to the server id; non-positive amount rejected;
  Adjustment refused; a Personal debt cannot be queued into an Organization store; queued plaintext
  absent from safe sync metadata; server-dedupe policy and auto-retry rules
- `src/offline/schema-upgrade.test.ts` (2) — a v3 database upgrades in place to v4 and its queued
  operation survives; each database is stamped with its own scope and `assertOfflineScope` rejects
  the wrong one
- `src/offline/cash-sale-offline.test.ts` — updated (not weakened) to assert schema v4 and the full
  store list including the three Personal stores

Offline module: **9 files, 61 tests passed**.
Full suite: `npx vitest run` — **88 files, 423 tests passed**. `npm run typecheck` clean.
`npm run lint` — 0 errors, 15 pre-existing `react-refresh` / `exhaustive-deps` warnings, none from
this package's files.

### Two real defects this package's tests caught

- `collectLocalRefs` / `resolveLocalRefs` originally used an anchored pattern, so a placeholder
  embedded in a **path** (`/relationships/{{local:…}}/entries`) was never detected and would have
  been POSTed literally. Fixed to a non-anchored global substitution.
- A rounding assertion exposed that `(250.005).toFixed(2)` is `"250.00"`, confirming the queued
  amount is the rounded value the person is shown rather than a re-derived one.

## Gaps carried to 21G–21H

- **No server-side idempotency on any Personal Utang route.** The policy module keeps the processor
  from making it worse, but the honest fix is a server-side idempotency store or client-supplied ids
  on the Personal endpoints. This is the largest open risk in the offline programme.
- No conflict UX for a queued Personal operation the server rejects on sync — needs 21H.
- Placeholder resolution depends on the 21H processor writing the server id into `entityMap` when the
  parent operation succeeds; the resolution helpers exist and are tested, the processor does not.
- Cached balances can be stale for as long as the device stays offline, and two devices can queue
  payments against the same relationship.
- The Personal dashboard, social, and customer-link surfaces are still online-only reads.
- Profile-level scope isolation is not available until the session snapshot exposes the selected
  AccountProfile id.

## Next

RMAP-21G — Personal To-do offline, private-by-default.
