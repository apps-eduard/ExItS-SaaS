# POS-REACT RMAP-21E — Business customers / customer-credit offline

**Status:** PASS
**Dependencies:** 21A matrix; 21B LocalStore + encrypted outbox; 21C Connection & Sync UI; 21D offline Sell + Cash

## Capability flags

| Flag                                | Value                |
| ----------------------------------- | -------------------- |
| `OFFLINE_CUSTOMER_CACHE`            | YES (encrypted)      |
| `OFFLINE_CUSTOMER_CREATE`           | YES                  |
| `OFFLINE_CUSTOMER_UPDATE`           | YES                  |
| `OFFLINE_REPAYMENT_CREATE`          | YES                  |
| `OFFLINE_CREDIT_CREATE`             | NO (`OnlineRequired`) |
| `OFFLINE_CREDIT_REVERSE`            | NO (`OnlineRequired`) |
| `OFFLINE_REPAYMENT_REVERSE`         | NO (`OnlineRequired`) |
| `OFFLINE_CREDIT_DUE_DATE`           | NO (`OnlineRequired`) |
| `OFFLINE_CUSTOMER_STATUS_CHANGE`    | NO (`OnlineRequired`) |
| `OFFLINE_CUSTOMER_STATEMENT`        | NO (`OnlineRequired`) |
| `OFFLINE_CUSTOMER_IDENTITY_LINK`    | NO (`OnlineRequired`) |
| `OFFLINE_BUSINESS_UTANG_CHECKOUT`   | NO (unchanged, 21D)  |
| `OFFLINE_GCASH`                     | NO (unchanged, 21D)  |
| `DEVICE_SHIFT_SELL_PRESERVED`       | YES                  |

## Why these three mutations are offline-capable and the rest are not

Each queued mutation had to be provable against the shipped server contract before it was allowed
offline. Proof means the route accepts a **client-chosen entity id** and honours
`Idempotency-Key` + `X-Pos-Payload-Hash`, so replaying the queued operation lands on the same row
instead of creating a second one.

| Route                                     | Server evidence                                                                                                                                                              | Verdict         |
| ----------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------- |
| `POST /api/v1/pos/customers`              | `CustomerEndpoints.cs` routes through `PosIdempotencyEndpointHelper.ExecuteMutationAsync(..., OfflineOperationTypes.CustomerCreate, ...)`; `CreateCustomerRequest.CustomerId` is accepted and `CreatePOSCustomer` returns the existing row when that id already exists | Offline allowed |
| `PUT /api/v1/pos/customers/{id}`          | Same helper with `OfflineOperationTypes.CustomerUpdate`; `ExpectedUpdatedAtUtc` gives the server a concurrency check                                                          | Offline allowed |
| `POST /api/v1/pos/customers/{id}/repayments` | `RepaymentEndpoints.cs` uses the same helper with `OfflineOperationTypes.RepaymentCreate`; `CreateRepaymentRequest.RepaymentId` is client-suppliable                       | Offline allowed |
| `POST .../credit-entries`                 | Server-side idempotency exists, but **no React client function exists** and extending credit is a decision against a live balance and credit policy                            | `OnlineRequired` |
| `POST .../credit-entries/{id}/reverse`, `POST /repayments/{id}/reverse` | Reversal of recorded money; requires the live entry state                                                                                        | `OnlineRequired` |
| `PUT .../credit/due-date`                 | Changes a due date the customer is judged against                                                                                                                             | `OnlineRequired` |
| `POST .../deactivate` / `/reactivate`     | No idempotency wiring on these routes; changing a customer's active status is an authorization act                                                                             | `OnlineRequired` |
| `GET .../statement`                       | Server-computed document (opening/closing balance, overdue math)                                                                                                               | `OnlineRequired` |
| `PUT .../exits-identity/*`, `/platform-correlation` | Personal↔Business identity linking must never happen silently on a device                                                                                            | `OnlineRequired` |

## Delivered

| Capability                                | Evidence                                                                                                                                                        |
| ----------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Idempotency headers on customer create    | `src/api/pos/pos-customers-client.ts` — client-chosen `customerId` in body + `Idempotency-Key`/`X-Pos-Payload-Hash`/`X-Pos-Operation-Id`/`X-Pos-Operation-Type`  |
| Idempotency headers on customer update    | Keyed on the **edit attempt** id, not the customer id, so two offline edits of one customer do not collide on one key                                            |
| Idempotency headers on repayment create   | Client-chosen `repaymentId` + the same four headers                                                                                                             |
| Shared online/offline bodies              | `buildCreateCustomerPayload`, `buildUpdateCustomerPayload`, `buildCreateRepaymentPayload` used by both the live client and the offline enqueue                   |
| Schema version 3                          | `OFFLINE_SCHEMA_VERSION = 3`; adds `customers` (index `byStatus`) and `customerCredit`                                                                           |
| Schema bumps now migrate                  | `src/offline/db.ts` — the version was removed from the database name, so a bump upgrades in place instead of stranding queued money under an unread name          |
| Encrypted customer cache                  | `src/offline/customer-cache.ts` — AES-GCM per row under the organization scope key; only `customerId`, `organizationId`, `status`, `updatedAtUtc` stay readable  |
| Encrypted outstanding balance cache       | `cacheCustomerCreditSummary` / `getCachedCustomerCreditSummary`                                                                                                 |
| Fail-closed cache reads                   | Every read returns `[]` / `null` on a missing store, a corrupt envelope, or a foreign scope key                                                                  |
| Queued request envelope                   | `src/offline/queued-request.ts` — payload version 2 stores the exact replayable request (`api`, `method`, relative `path`, `body`); rejects absolute URLs         |
| Offline customer/credit enqueue           | `src/offline/customer-offline.ts` — `customer.create`, `customer.update`, `repayment.create`, matching the MAUI `OfflineOperationTypes` strings                  |
| Shared organization offline context       | `src/offline/organization-offline-context.ts`; `sell-offline-context.ts` is now a thin Sell alias so Sell and Customers share one outbox and one sync count      |
| New online-required codes                 | `src/offline/online-required.ts` — credit extend, credit reverse, customer status, statement, identity link (all five localized in all five locales)              |
| Customers list offline                    | `CustomersListPage.tsx` — write-through on a successful online read, cached fallback + local name/mobile/status search, cached-data notice                       |
| Customer detail offline                   | `CustomerDetailPage.tsx` — cached customer + cached balance, statement hidden, status toggle answers with the online-required reason                             |
| Customer form offline                     | `CustomerFormPage.tsx` — edit seeds from the cached row (including the concurrency token), save queues, offline notice before saving                             |
| Repayment offline                         | `CustomerRepayPage.tsx` — cached balance with a "saved balance" notice, queues with a client-chosen `repaymentId`                                                |
| Auth tokens in IndexedDB                  | **NO**                                                                                                                                                          |
| Workbox API caching                       | **NO** (API routes remain `NetworkOnly`)                                                                                                                        |

## Offline customer flow

1. Online: every successful customer list, detail, and credit-summary read is written through to the
   encrypted cache. Nothing is ever cached from a failed or partial response.
2. Network drops: list, detail, form, and repayment screens stop issuing requests (`enabled: online`)
   and read the cache instead of burning react-query retries on a dead network.
3. Create: the client picks the customer id, queues `customer.create`, and navigates to that id — the
   id the device shows is the id the server will adopt.
4. Edit: the queued `customer.update` carries the `expectedUpdatedAtUtc` the cashier actually saw, so
   the server can still reject a stale edit as a conflict.
5. Payment: the client picks the `repaymentId` and queues `repayment.create`. The screen says the
   server confirms it against the live balance — it does not claim the debt is reduced.
6. Nothing is sent in this package. Queued rows stay `Pending` until the 21H processor runs.

## Fail-closed and honesty rules

| Situation                                       | Behavior                                                                                                                                                     |
| ----------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Cache unreadable, corrupt, or from another scope | Reads return empty/null; the UI says the customer is not saved on this device rather than showing an authoritative "no customers"                             |
| Filtered/searched page cached                    | Cache **merges** rather than replaces, because a filtered page is not proof the absent customers were deleted                                                 |
| Offline edit of an uncached customer             | Form refuses with `offline.customerNotCached` instead of queueing an edit built from blank fields                                                             |
| Offline identity link attempt                    | `enqueueOfflineCustomerCreate` throws `offline.customer.identity_link_not_supported`; nothing is queued                                                       |
| Offline status change                            | Toggle answers with `offline.requiredCustomerStatus`; no local status flip                                                                                    |
| Offline statement                                | Button hidden; the statement is a server-computed document                                                                                                   |
| Repayment above the cached balance               | The existing client-side guard still uses the cached balance for immediate feedback, but the **server** re-checks on sync; a rejected payment surfaces in Connection & Sync as needing attention |
| Secure randomness unavailable                    | Enqueue is refused (`offline.customerEnqueueFailed`); no guessable id or key is ever minted                                                                  |
| Two offline edits of one customer                | Two operations with two distinct idempotency keys; both replay in creation order                                                                              |

## Explicit non-claims

- No sync processor in this package. Queued customer and repayment operations stay `Pending` until 21H.
- A queued repayment is **not** a settled payment. The outstanding balance shown offline is the last
  balance the server gave this device, not a recalculated one — the client never does credit math.
- No offline credit extension, reversal, due-date change, status change, statement, or identity link.
- No offline Business Utang checkout (unchanged from 21D).
- No Personal-domain reads or writes in this package.
- Not native SecureStorage / Keystore / Keychain parity. `COLD_START_OFFLINE_UNLOCK` remains
  `DEFERRED_SECURITY_GAP` from 21B: the cache key is derived from the organization scope binding, so a
  warm browser profile can read the cache without re-authenticating.
- The cached customer body is encrypted, but `status`, `organizationId`, `updatedAtUtc`, and the
  customer id remain readable in IndexedDB by design (index and merge columns), mirroring the MAUI
  `LocalEncryptedCustomerCreditStore` column split.

## Tests

- `src/offline/customer-cache.test.ts` — fails closed before write-through; encrypted round-trip of
  customer and outstanding balance; **no customer name or mobile number in the stored row**; a foreign
  scope key cannot decrypt; pages merge instead of deleting; cached search by name/mobile/status
- `src/offline/customer-offline.test.ts` — `customer.create` keyed on the client customer id; identity
  link and nameless customer rejected with nothing queued; two edits get two keys; repayment keyed on
  the client `repaymentId` with rounded amount; non-positive and customer-less repayments rejected;
  queued plaintext absent from safe sync metadata
- `src/offline/schema-upgrade.test.ts` — a v2 database upgrades in place to v3 and its queued
  operation survives
- `src/offline/cash-sale-offline.test.ts` — updated (not weakened) to assert schema v3 and the full
  store list including `customers` and `customerCredit`

Full suite: `npm test -- --run` — **86 files, 395 tests passed**. `npm run typecheck` clean.
`npm run lint` — 0 errors, 15 pre-existing `react-refresh` / `exhaustive-deps` warnings, none from
this package's new files.

## Gaps carried to 21F–21H

- No conflict UX for a queued customer edit or repayment the server rejects on sync (stale token,
  balance moved, capability revoked) — needs 21H.
- The credit-entry and repayment history lists are still online-only reads; only the customer
  projection and the outstanding balance are cached.
- Cached balances can be stale for as long as the device stays offline; there is no offline reservation
  of credit, so two devices can both queue payments against the same balance.
- Customer create offline does not check the server's duplicate-name or duplicate-mobile behavior; the
  server decides on sync.

## Next

RMAP-21F — Personal Utang offline in the Personal LocalStore only.
