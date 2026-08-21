# RMAP-10b — Browser POS device authorization

## Status

**COMPLETE**

## Baseline

starting SHA: `aff4803d` (Master Run 02 hard stop at device gap)

## Contract review

| Area | Finding |
|------|---------|
| Durable installation id | `localStorage` key `exits.pos-client.installation-device-id.v1`; `crypto.randomUUID()`; survives logout/user/org |
| Fail-closed storage | No ephemeral register id when storage/crypto unavailable |
| Malformed id | UUID validation; replace before register |
| Platform APIs | list / capacity / register / registration-tokens / redeem / authorize / revoke |
| Header | `X-Pos-Installation-Device-Id` on POS HTTP when durable id available |
| Authorize hydrate | Workspace binds org+branch → authorize → `PosDeviceContext` |
| moneyPostReady | `shiftGateReady` **and** authorized matching device only |
| Branch conflict | Register/redeem reject different-branch install (409); redeem does not consume token |
| Staff redeem ACL | Wrong-branch staff → `BranchAccessDenied` without consuming token |
| HTTP mapping | `PosDeviceNotAuthorized` / `PosDeviceRevoked` → 403; `PosDeviceBranchConflict` → 409 |
| Owner decision | NO |

## Device contract conclusion

Browser/PWA now has a contracted PosDevice path: durable installation identity + Platform register/redeem/authorize + POS header. No Development bypass, no fake authorized terminal, no Capacitor, no sale POST (RMAP-11).

## Implementation

- Backend: `PlatformApiResults.MapStatusCode`; Platform unit tests for redeem ACL / branch conflict / register conflict
- `browser-installation-identity.ts` + unit matrix
- `pos-devices-client.ts` (platform-http)
- `pos-http.ts` central installation header
- Expanded `PosDeviceContext` + `hydratePosDeviceContext` in `WorkspaceProvider`
- UI: `/org/devices` (Owner/Admin), `/devices/register` (staff redeem, no camera)
- i18n en + fil-PH
- Playwright `rmap-10b-browser-pos-device.spec.ts`

## Exclusions

- Sale POST / checkout payment (RMAP-11)
- Development money bypass
- Capacitor / native device plugins
- Full governance step-up UX for revoke (API wired; step-up may still be required server-side)
- Offline device grant / offline PIN

## Implementation SHA

`d48da9a8` (feat); docs commit on `feat/pos-react-client` (see Master Run 02 Final HEAD)

## Validation

### React gates

| Gate | Result |
|------|--------|
| Vitest | 41 files / **176** tests passed |
| typecheck | PASS |
| lint | PASS (0 errors; existing react-refresh warnings only) |
| format:check | PASS |
| build | PASS |
| Playwright `rmap-10` | **15** passed |
| Playwright `rmap-10b` | **8** passed |

### Platform unit tests

| Gate | Result |
|------|--------|
| PosDeviceRegistrationTokenTests + RegisterCurrentDeviceBranchConflictTests (+ Authorize) | **13** passed |

Responsive matrix (org devices):

| Viewport | Result |
|----------|--------|
| 375×812 | PASS (e2e) |
| 768×1024 | PASS (e2e) |
| 1024×768 | PASS (e2e) |
| 1440×900 | PASS (e2e) |

### Proven behaviors

- Durable install id persists across logout simulation
- Owner registers this browser; status becomes authorized for selected branch
- Staff redeems registration code without camera
- Unregistered/unauthorized device keeps sell Pay disabled (no sale POST)
- moneyPostReady true only with authorized matching device + open shift gate
- Wrong-branch register/redeem rejected; token not consumed on redeem conflict

### Flags

- `RMAP_10B_PASS=YES`
- `RMAP_10B_RESPONSIVE_MATRIX_PROVEN=YES`
- `RMAP_10B_DURABLE_INSTALLATION_IDENTITY=YES`
- `RMAP_10B_HEADER_ATTACHED=YES`
- `RMAP_10B_NO_FAKE_DEVICE=YES`
- `RMAP_10B_NO_DEV_BYPASS=YES`
- `RMAP_10B_SALE_POST_EXCLUDED=YES`
- `RMAP_10B_MONEY_POST_GATED=YES`
- `HARD_STOP=NO`
- `RMAP11_BROWSER_DEVICE_CONTRACT_GAP=CLEARED`

## Next

RMAP-11 — Checkout / sale (online cash first). Use authorized browser PosDevice + `X-Pos-Installation-Device-Id`; do not invent devices or add Development bypass.
