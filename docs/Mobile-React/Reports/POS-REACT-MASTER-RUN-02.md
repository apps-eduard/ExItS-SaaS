# POS-REACT MASTER RUN 02 — Status

## Status

**HARD STOP after RMAP-13** — Master Run 02 Resume blocked at RMAP-14.

**Code:** `RMAP14_EXPIRY_RETURN_CONTRACT_GAP`

Return restock restores organization on-hand only; expiration-tracked FEFO lots consumed at sale are **not** restored (unlike void’s `RestoreSourceAsync`). React returns were **not** implemented.

## Baseline

| Item | Value |
|------|-------|
| Resume starting SHA | `4db1f09fb2eba3d494144cd693a7ecd1143b08cf` |
| Branch | `feat/pos-react-client` |

## Completed in this master run

| Package | Status | Impl SHA | Docs SHA |
|---------|--------|----------|----------|
| RMAP-08 (prior + review repair) | PASS | `4c38bb0e` / repair `1771aa0c` | `4ff88ca1` / repair `6aa0d48b` |
| RMAP-09 (prior + review repair) | PASS | `ae433fd2` / repair `1771aa0c` | `31adf35b` / repair `6aa0d48b` |
| RMAP-10 | PASS | `356cdfde` | `d39776ff` |
| RMAP-10b | PASS | `d48da9a8` | `e356ee16` |
| RMAP-11 | PASS | `a43d26b8` | `3d9615eb` |
| RMAP-11b | PASS | `f9fd88a4` | `47af61a3` |
| RMAP-12 | PASS | `7dcd3ab5` | `17569653` |
| RMAP-13 | PASS | `adf634ee` | `08ba616c` |
| RMAP-14 | **HARD STOP** | — | [POS-REACT-RMAP-14-returns-refunds.md](./POS-REACT-RMAP-14-returns-refunds.md) |

## Former blocker — CLEARED

**Code:** `RMAP11_BROWSER_DEVICE_CONTRACT_GAP` → **CLEARED** by RMAP-10b.

**Evidence through RMAP-13:**

- Durable browser installation id + authorize + `X-Pos-Installation-Device-Id` on sale POST.
- Online Cash / ManualGCash / Utang checkout; commercial discount intents + authoritative quote.
- Void on Transaction Summary for Owner/Manager; Cashier denied.
- Customers CRUD + Business Utang Amount owed / Payment / Remaining balance + statement.
- Discounted Utang credit amount displayed from server equals net Amount to Pay.
- `RMAP_B04_STARTED=NO` (buyer purchase projection not started).
- Utang return credit reconciliation exists server-side (`ReduceForSaleReturn`) — **not** the RMAP-14 stopper.

## Active blocker

**`RMAP14_EXPIRY_RETURN_CONTRACT_GAP`**

- Void: restores original consumed lots.
- Return: bumps `InventoryAccount` only — lot ledger skipped for `TracksExpiration` products.
- Package forbids inventing lots / fake expiry. Backend contract fix required before React returns.

## Not started

RMAP-14 (blocked), RMAP-15+, RMAP-B01, RMAP-12b, RMAP-B04, RMAP-TAX, provider payments, Owner Personal switcher, Personal Utang React.

## Exact next

Authorize a **backend** expiration-aware return restock contract (mirror void source restore or explicit safe alternative), then restart **RMAP-14 only**.

Do **not** start RMAP-15, invent Card/provider GCash UI, mutate PosRoleMatrix without auth, start RMAP-B04, or start Personal Utang without authorization.

## Final HEAD

_(Omit tip SHA in package report per commit rules; Cursor response records HEAD externally.)_
