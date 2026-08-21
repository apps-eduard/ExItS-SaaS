# POS-REACT MASTER RUN 02 — Status

## Status

**RMAP-10b COMPLETE** — browser PosDevice contract cleared. Ready for authorized RMAP-11.

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

## Former blocker — CLEARED

**Code:** `RMAP11_BROWSER_DEVICE_CONTRACT_GAP` → **CLEARED** by RMAP-10b.

**Evidence after RMAP-10b:**

- Durable browser installation id (`exits.pos-client.installation-device-id.v1`) survives logout.
- Platform register / redeem / authorize wired in React; POS HTTP attaches `X-Pos-Installation-Device-Id` when available.
- `moneyPostReady` requires authorized matching device + open shift gate (no invented terminal; no Dev bypass).
- Sale POST still excluded until RMAP-11.

## Not started

RMAP-11, RMAP-11b, RMAP-12, RMAP-13, RMAP-14, RMAP-15+, RMAP-B01, RMAP-12b, RMAP-B04, RMAP-TAX, provider payments, Owner Personal switcher.

## Exact next

**RMAP-11 — Checkout / sale (online cash first)** using the contracted browser PosDevice path.

Do **not** invent devices, add Development money bypass, or start RMAP-11b without authorization.

## Final HEAD

`e356ee16d823b5ef17b282df928051deaa90d713` (= `origin/feat/pos-react-client`)
