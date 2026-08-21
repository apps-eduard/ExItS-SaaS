# POS-REACT MASTER RUN 02 — Status (HARD STOP)

## Status

**HARD STOP** before RMAP-11 — browser money/sale device contract gap.

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

## Blocker

**Code:** `RMAP11_BROWSER_DEVICE_CONTRACT_GAP`

**Evidence:**

- `PosDeviceTransactionAuthorizer` requires `X-Pos-Installation-Device-Id` for money-affecting POS calls outside the `Testing` environment.
- Missing header → `application.pos_device.not_authorized` (403).
- React retains `DEFERRED_POS_DEVICE_CONTEXT` (RMAP-03): no installation device id; `moneyPostReady` remains false after RMAP-10.
- Platform provides real register/authorize device APIs, but **browser/PWA PosDevice registration + authorization UX was never contracted in Master Run 02**.
- Inventing a fake device id, or weakening production/Development device auth, is forbidden.

**Not a bypass candidate:** Integration tests run under `Testing` (device optional). That does **not** authorize Development/browser checkout without a registered installation.

## Not started

RMAP-11, RMAP-11b, RMAP-12, RMAP-13, RMAP-14, RMAP-15+, RMAP-B01, RMAP-12b, RMAP-B04, RMAP-TAX, provider payments, Owner Personal switcher.

## Exact next (owner / ChatGPT)

Decide one of:

1. Authorize a **browser PosDevice registration** package (Platform `pos-devices/register` or registration-token redeem + persistent installation id + authorize header) before or as a prerequisite to RMAP-11, **or**
2. Explicitly authorize a **Development-only** documented path (still not production), **or**
3. Defer React online checkout until a contracted device path exists.

Do **not** start RMAP-11 sale POST until that decision is recorded.

## Final HEAD at HARD STOP

`d39776ff546b8b5a999fde001e519647b37efc0e` (= `origin/feat/pos-react-client`)
