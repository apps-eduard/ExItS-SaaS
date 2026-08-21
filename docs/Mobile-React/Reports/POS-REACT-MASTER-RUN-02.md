# POS-REACT MASTER RUN 02 — Status

## Status

**RMAP-13 COMPLETE** — customers + Business Utang (credit, payments, statements). Ready for authorized RMAP-14.

## Baseline

| Item | Value |
|------|-------|
| Starting SHA (repair command) | `31adf35bf4210f3151701221c5a9dfd92fb05dfe` |
| Branch | `feat/pos-react-client` |

## Completed in this master run

| Package | Status | Impl SHA | Docs SHA |
|---------|--------|----------|----------|
| RMAP-08 (prior + review repair) | PASS | `4c38bb0e` / repair `1771aa0c` | `4ff88ca1` / repair `6aa0d48b` |
| RMAP-09 (prior + review repair) | PASS | `ae433fd2` / repair `1771aa0c` | `31adf35b` / repair `6aa0d48b` |
| RMAP-10 | PASS | `356cdfde` | `d39776ff` |
| RMAP-10b | PASS | `d48da9a8` | `e356ee16` |
| RMAP-11 | PASS | `a43d26b8` | `3d9615eb` |
| RMAP-11b | PASS | `f9fd88a4` | _(docs; see prior Cursor response)_ |
| RMAP-12 | PASS | `7dcd3ab5` | _(docs; see prior package)_ |
| RMAP-13 | PASS | `adf634ee` | _(docs; this package)_ |

## Former blocker — CLEARED

**Code:** `RMAP11_BROWSER_DEVICE_CONTRACT_GAP` → **CLEARED** by RMAP-10b.

**Evidence after RMAP-13:**

- Durable browser installation id + authorize + `X-Pos-Installation-Device-Id` on sale POST.
- Online Cash / ManualGCash / Utang checkout; commercial discount intents + authoritative quote.
- Void on Transaction Summary for Owner/Manager; Cashier denied.
- Customers CRUD + Business Utang Amount owed / Payment / Remaining balance + statement.
- Discounted Utang credit amount displayed from server equals net Amount to Pay.
- `RMAP_B04_STARTED=NO` (buyer purchase projection not started).

## Not started

RMAP-14, RMAP-15+, RMAP-B01, RMAP-12b, RMAP-B04, RMAP-TAX, provider payments, Owner Personal switcher, Personal Utang React.

## Exact next

**RMAP-14 — Returns / refunds** when authorized.

Do **not** invent Card/provider GCash UI, mutate PosRoleMatrix without auth, start RMAP-B04, or start Personal Utang without authorization.

## Final HEAD

_(Omit tip SHA in package report per commit rules; Cursor response records HEAD externally.)_
