# POS-REACT MASTER RUN 02 — Status

## Status

**RMAP-11 COMPLETE** — online cash checkout via authorized browser PosDevice. Ready for authorized RMAP-11b / RMAP-12.

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
| RMAP-11 | PASS | `a43d26b8` | _(docs commit)_ |

## Former blocker — CLEARED

**Code:** `RMAP11_BROWSER_DEVICE_CONTRACT_GAP` → **CLEARED** by RMAP-10b.

**Evidence after RMAP-11:**

- Durable browser installation id + authorize + `X-Pos-Installation-Device-Id` on sale POST.
- `moneyPostReady` required for Pay → cash checkout (no invented terminal; no Dev bypass).
- Online Cash `POST /api/v1/pos/sales` with client `saleId` idempotency; Transaction Summary wording (not Invoice).

## Not started

RMAP-11b, RMAP-12, RMAP-13, RMAP-14, RMAP-15+, RMAP-B01, RMAP-12b, RMAP-B04, RMAP-TAX, provider payments, Owner Personal switcher.

## Exact next

**RMAP-11b — Commercial Discount UX** when authorized (or RMAP-12 payments expansion).

Do **not** invent devices, add Development money bypass, or start RMAP-11b/12 without authorization.

## Final HEAD

_(filled after docs push)_
