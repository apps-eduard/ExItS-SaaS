# POS-REACT MASTER RUN 02 — Status

## Status

**RMAP-11b COMPLETE** — commercial discount UX on React cash checkout via server quote. Ready for authorized RMAP-12.

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
| RMAP-11b | PASS | `f9fd88a4` | _(docs; see Cursor response)_ |

## Former blocker — CLEARED

**Code:** `RMAP11_BROWSER_DEVICE_CONTRACT_GAP` → **CLEARED** by RMAP-10b.

**Evidence after RMAP-11b:**

- Durable browser installation id + authorize + `X-Pos-Installation-Device-Id` on sale POST.
- Online Cash checkout with optional commercial discount intents + authoritative `POST /sales/quote`.
- Cashier denied discount UI; server rejects discount intents without ApplyCommercialDiscount.
- Zero-total Cash after full discount: tendered/change 0; “No payment required”.

## Not started

RMAP-12, RMAP-13, RMAP-14, RMAP-15+, RMAP-B01, RMAP-12b, RMAP-B04, RMAP-TAX, provider payments, Owner Personal switcher.

## Exact next

**RMAP-12 — Payments expansion + void** when authorized.

Do **not** invent devices, add Development money bypass, or start RMAP-12 without authorization.

## Final HEAD

_(Omit tip SHA in package report per RMAP-11b commit rules; Cursor response records HEAD externally.)_
