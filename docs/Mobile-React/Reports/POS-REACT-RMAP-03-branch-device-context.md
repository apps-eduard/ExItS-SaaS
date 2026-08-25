# RMAP-03 — Branch / device operational context

## Status

**COMPLETE**

## Baseline

starting SHA: `77888cbfcccaf384838a50186e4b352dceee79f6`

## Contract review

| Area | Finding |
|------|---------|
| Backend | Platform `GET .../branches`, `PUT .../branch-context`; POS `PUT /api/v1/pos/operational-branch` |
| Branch filter | Server + client Active-only for chooser; Owner/Admin vs staff assignment rules on Platform |
| Device | Money ops require `X-Pos-Installation-Device-Id`; catalog/branch bind do **not** |
| React device identity | **No approved browser PosDevice contract** — deferred honestly; not invented |
| MAUI | Org context + branch-context + operational-branch + device authorize for sell |
| Owner decision | NO |

## Implementation

- After Platform bind + session grant: call `selectOperationalBranch` (MAUI parity); capability 403 keeps Platform bind; inactive/shift failures fail closed
- Stale bound branch revalidated against accessible Active set on workspace reload
- Bind rejects branches outside accessible set
- `posDevice` exposed as `deferred` with null installation id (`DEFERRED_POS_DEVICE_CONTEXT`)
- `RequireWorkspaceBound` + `WorkspaceBootNavigator` for zero-branch → `/no-location`
- POS HTTP continues to stamp org + branch headers; never invents device header

## Exclusions

- Device register/authorize UI
- Fake authorized terminal
- Branch CRUD / fulfillment (RMAP-18)
- Registers / shifts / checkout

## Tests

- Vitest: resolver zero/one/many/inactive; deferred device not money-ready
- Playwright: zero → no-location; single auto-bind; multi chooser; logout clears; viewports 375/768/1024/1440
- Regression: RMAP-01 / 01b / 02 / 02R Playwright green

## Device honesty

`device required` for RMAP-03 operational paths: **NO**  
`device identity source`: deferred (no React installation id)  
Revoked / wrong org / wrong branch device: covered by backend for money ops; not exercised as success path in React

## Next

RMAP-04 — Catalog admin parity
