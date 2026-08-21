# POS-REACT RMAP-21 REVIEW REPAIR 01 — Offline Cash finality

**Status:** COMPLETE — awaiting Product Owner / ChatGPT review
**Branch:** `feat/pos-react-client`
**Worktree:** `C:\Users\speed\Desktop\ExItS-SaaS-pos-react-client`
**Baseline SHA:** `0e0ad50adc5c5743dbf481548cb0f1605978b69d`

| Commit | Message |
| --- | --- |
| `b3f36236` | feat(pos): add offline price authority for cash finality |
| `03665e5e` | feat(pos-react): price offline cash sales from server price leases |
| docs tip | docs(pos-react): record RMAP-21 review repair 01 |

## The defect this repairs

RMAP-21D queued an offline Cash sale as `ProductId` + `Quantity` only. The server priced the sale
from the live catalog when the outbox drained. A shelf price edited while the device was offline
therefore rewrote a sale the customer had already paid for in cash:

- **Price raised** — the recorded total exceeded the money in the drawer, and the shop absorbed the
  difference with no record of why.
- **Price cut** — the server computed change against a total the cashier never charged, inventing
  change nobody handed back.

Neither is acceptable for money. The cashier's receipt and the server's ledger must agree, and the
device cannot be trusted to simply assert a price of its own.

The MAUI WP08 path solves this by trusting a client-sent `UnitPriceSnapshot`. That is not adopted
here: a browser is a far more editable client than a signed mobile build, and a snapshot the client
merely asserts is a price the client can invent.

## What was built

### Server-signed offline price lease

A device that may need to sell offline is issued a short-lived, signed commitment to a price
*before* the network drops. The lease names exactly one sellable shape and one price:

| Field | Purpose |
| --- | --- |
| `AuthorityId` | Identity of this lease |
| `OrganizationId`, `BranchId` | Scope it was issued for; a lease is not portable across either |
| `ProductId`, `SellingUnitId` | The sellable shape — a pack and a single piece are separate leases |
| `UnitPrice`, `UnitOfMeasure`, `SellingMode` | The committed price and how it is billed |
| `IssuedAtUtc`, `ExpiresAtUtc` | Validity window (default 8 hours, configurable) |
| `Signature` | HMAC-SHA256 over a canonical pipe-separated, culture-invariant rendering of every field above |

The canonical string fixes decimal precision and uses Unix seconds for timestamps, so a .NET signer
and a JavaScript replayer never disagree over formatting.

The signing key comes from `PosOffline:PriceAuthoritySigningKey`. The development default is a known
string so tests can run; `PosDevelopmentEnvironment` now refuses to start Production with it, in the
same way it already refuses other development-only credentials.

The 8-hour window is deliberately shorter than the 24-hour offline readiness window: a device may be
allowed to keep selling longer than any single price is allowed to stand.

### Issuing endpoint

`POST /api/v1/pos/offline-price-authorities`, organization- and branch-scoped exactly like catalog
browse. It takes the products (and optional sell units) the device just browsed and returns leases
for them. Issuing records nothing and moves no money — it only commits the server to a price for a
bounded window.

### Checkout verification

`CheckoutSaleLineRequest` gained an optional nested `OfflinePriceAuthority` token. In
`SaleUseCases.ResolveDraftsAsync` a line that carries one is verified before anything else: the
signature must be one this server produced, and the lease must match the checkout's organization,
branch, and product binding, and must still be inside its window. Only then is the line priced —
**from the lease**, never from any amount the client also sent.

The client still sends `UnitPriceSnapshot` and `LineTotal` alongside the lease so the queued sale and
the paper receipt carry the same numbers. The server treats them as claims to check, not as prices:
if they disagree with the lease, the sale is refused rather than quietly corrected. Discounts and
price overrides remain refused on this path, as they already were.

Three paths now exist, and they do not overlap:

| Line carries | Priced from |
| --- | --- |
| Offline price authority (React offline Cash) | The lease |
| `UnitPriceSnapshot` only (MAUI WP08) | Unchanged WP08 behavior |
| Neither (online checkout) | Live catalog |

`POST /sales/quote` refuses leases outright: an online cart has a live catalog in front of it and no
reason to price from a lease.

### React client

- Leases are issued alongside the existing catalog write-through on the Sell Floor. A product a
  cashier can see on a warm sell floor is a product they may need to sell after the network drops,
  so its price is leased at the moment it is cached — one lease per sellable shape.
- Leases are cached in the Organization LocalStore in a new `priceAuthorities` store
  (`OFFLINE_SCHEMA_VERSION` 6). The cache can only keep, hand back, or discard a lease; it can never
  mint, extend, or edit one. Reads fail closed to "no lease", so a broken cache blocks a sale rather
  than letting one through at a price nobody signed.
- Before an offline Cash confirm, every cart line must map to a live lease. If any line does not, the
  checkout shows **"Connect to refresh prices before selling."** and Confirm stays disabled. The
  offline totals shown to the cashier are computed from the leases, so the amount tendered and the
  change advised are the amounts the server will record.
- `enqueueOfflineCashSale` re-checks the same conditions at the point of queueing, because a queued
  sale outlives the screen that queued it: once the payload is encrypted, nothing downstream can tell
  an authorized price from an invented one.
- On sync, the outbox processor compares the server's recorded total against the total the device
  committed to. A disagreement is marked **Conflict** rather than Succeeded, so a silent divergence
  can never be reported as a clean sync.

## Second defect found while proving this

Writing the Personal offline end-to-end spec exposed an unrelated but serious bug: React Query's
default `networkMode` pauses a mutation while the browser reports offline and fires it on reconnect.
Every Personal offline write goes through `useMutation`, so **nothing was ever queued while
offline** — the person watched a spinner, the outbox stayed empty, and the write only left the device
later, outside the outbox that guards replay. Only a real browser could show this; the unit tests
call the enqueue functions directly and passed throughout.

Mutations are now `networkMode: "always"`. An offline write reaches its own offline branch and
queues; a write with no offline branch fails visibly instead of silently deferring.

The Organization Cash checkout was never affected — it confirms through a plain async handler, not a
mutation.

## Tests

Backend:

- Issue and verify round-trip; tampered price rejected; wrong organization rejected; wrong branch
  rejected; wrong product binding rejected; expired lease rejected.
- Checkout with a lease after the catalog price changed still bills the leased price, for both a
  price rise and a price cut.
- Weighted sale: 0.5 kg at a leased 120.00 bills 60.00.
- Idempotent replay: same key and same payload replays; same key and different payload conflicts.
- Quote refuses a lease.

Client unit:

- Lease cache: one lease per sellable shape, newest lease replaces older, closed windows treated as
  no lease and pruned.
- Offline cart mapping: every line priced from its lease and the cart totalled from those amounts;
  the cart's own remembered price is ignored entirely; the whole cart is refused when any line has no
  lease, when the lease is for the wrong sell unit, or when its window has closed.
- Enqueue: refuses an unleased line, an expired lease, and amounts edited away from the lease.
- Outbox: Conflict when the server records a different total than the cashier collected.

Client end-to-end (`e2e/rmap-21-offline-cash-sync.spec.ts`, Playwright `setOffline`):

- Queues a Cash sale offline, counts it in Connection & Sync, replays it exactly once on reconnect
  with the sale idempotency headers, and the replayed line carries the lease and its price.
- A price rise to 120 while offline does not raise a 25.00 sale.
- A price cut to 10 while offline does not invent change.
- With no lease on the device, the offline confirm is refused with the refresh-prices message.
- The queued sale is stored as ciphertext with no cart, tender, lease, or credential plaintext.

Personal end-to-end (`e2e/rmap-21-personal-offline-queue.spec.ts`, new):

- A private To-do written offline is readable on the device immediately, marked as waiting, stored as
  ciphertext, and replayed once on reconnect with the queue row settling to Succeeded.
- A private Utang contact written offline queues and replays the same way.

## Build / test evidence

| Gate | Result |
| --- | --- |
| Backend `dotnet test` UnitTests (Release) | **1118 passed / 0 failed** |
| Backend IntegrationTests, sales + offline authority filter (Release, Testcontainers PostgreSQL) | **86 passed / 4 failed** — the 4 are migration apply/rollback tests that fail identically without this work when run alongside API tests on the shared container, and pass in isolation |
| Client `npm run typecheck` | PASS |
| Client `npx eslint` | 0 errors (17 pre-existing `react-refresh` warnings) |
| Client `npm run test` | **462 passed / 0 failed** (93 files) |
| Client `npm run build` | PASS (PWA precache 16 entries) |
| Playwright `rmap-21-offline-cash-sync` + `rmap-21-personal-offline-queue` + `rmap-22h` regression | **14 passed / 0 failed** |
| Client `npm run format:check` | One pre-existing unformatted file (`src/features/role/RoleHomePages.tsx`), untouched by this work |

Full Playwright suite: 295 passed / 17 failed. Those 17 were reproduced **identically** on a clean
worktree at the baseline SHA `0e0ad50a` with none of this work present (`auth-session`, `rmap-01`,
`rmap-01b`, `rmap-03`, `rmap-04`, `rmap-19`, `shell-account-ux`), so they are pre-existing and out of
scope here. They are not repaired or hidden by this package.

## Security notes

- The signing key is configuration-only and never committed. Production refuses to start with the
  development key.
- The client cannot mint or extend a lease. It stores the server's bytes and replays them verbatim.
- A lease is bound to organization, branch, product, and sell unit, so it cannot be moved between
  stores, branches, or products.
- The device clock is the only clock available offline, so lease expiry is evaluated strictly: a
  clock that has drifted forward refuses to sell rather than selling on a lease the server will
  reject at sync.
- An unreadable lease cache blocks the sale instead of falling back to a device-remembered price.

## Limitations

- A lease is a price commitment, not a stock reservation. Two offline devices can still both sell the
  last unit; that remains the RMAP-21D limitation.
- Offline selling is still limited to Cash. GCash, Business Utang, discounts, price overrides, and
  lot/expiry allocation remain online-required.
- A device that never browsed a product online holds no lease for it and cannot sell it offline.
- Weighted offline selling is proven by backend tests over the lease contract, not by a live device
  run; a live weighted offline device pass is not claimed.

## Files changed

Backend:

- `ExItS.PinoyBusinessPOS.Application/Offline/OfflinePriceAuthority.cs` (new)
- `ExItS.PinoyBusinessPOS.Application/Offline/OfflinePriceAuthorityService.cs` (new)
- `ExItS.PinoyBusinessPOS.Application/Sales/CheckoutSaleLineAuthorities.cs` (new)
- `ExItS.PinoyBusinessPOS.Application/Sales/SaleClientDtos.cs`, `Sales/SaleUseCases.cs`
- `ExItS.PinoyBusinessPOS.Application/Common/ApplicationErrorCodes.cs`
- `ExItS.PinoyBusinessPOS.Api/Offline/OfflinePriceAuthorityEndpoints.cs` (new)
- `ExItS.PinoyBusinessPOS.Api/Program.cs`, `Common/PosDevelopmentEnvironment.cs`,
  `appsettings.Development.json`
- `tests/ExItS.PinoyBusinessPOS.UnitTests/Offline/OfflinePriceAuthorityTests.cs` (new)
- `tests/ExItS.PinoyBusinessPOS.IntegrationTests/PosOfflinePriceAuthorityApiTests.cs` (new)

Client:

- `src/api/pos/pos-offline-price-authority-client.ts` (new), `src/api/pos/pos-sales-client.ts`
- `src/offline/price-authority-cache.ts`, `src/offline/price-authority-refresh.ts` (new)
- `src/offline/db.ts`, `src/offline/types.ts`, `src/offline/cash-sale-offline.ts`,
  `src/offline/outbox-processor.ts`
- `src/features/checkout/CheckoutCashPage.tsx`, `src/features/checkout/map-cart-to-checkout.ts`
- `src/features/sell/SellFloorPage.tsx`
- `src/app/providers.tsx` (mutation `networkMode`)
- `src/i18n/locales/*.ts`
- `src/test/mock-price-authority.ts` and unit tests
- `e2e/mock-bound-session.ts`, `e2e/mock-pos-price-authority-route.ts` (new),
  `e2e/rmap-21-offline-cash-sync.spec.ts` (new), `e2e/rmap-21-personal-offline-queue.spec.ts` (new)

Docs:

- This report; `POS-REACT-RMAP-21D-pos-offline-sell-cash.md`,
  `POS-REACT-RMAP-21H-reconnect-recovery-sync.md`, `POS-REACT-RMAP-21-OFFLINE-MASTER-RUN-01.md`,
  `Authoritative/Offline/react-pwa-offline-capability-matrix.md`

## Exact next work package

**HARD STOP.** RMAP-23 / B04 / B05 / TAX / production cutover remain unauthorized.
