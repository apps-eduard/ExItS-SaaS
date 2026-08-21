# POS React — Device Registration Simplification + Sales Execution Gate

**Status:** `AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW`  
**Branch:** `feat/pos-react-client`  
**Starting HEAD:** `107237a74c37d5336b1cd0754a68c74f44e5dc07` (responsive bottom-nav tip)  
**Final HEAD:** `1062efffde7258339ce4d53c43cd31efa53af1c5`  
**Implementation commit:** `0d8fb021`  

## Canonical product rule

> Users may authenticate from any permitted endpoint.  
> POS device capacity applies only to endpoints explicitly registered for POS execution.  
> Only registered POS devices may execute sales.  
> Registration codes are not part of the normal React POS onboarding or device-management experience.

## Architecture changes

### React UX

| Before | After |
|---|---|
| Create registration code / redeem paste-code | **Register this device** → `POST .../pos-devices/register` |
| “Register this browser” | Device terminology; browser only as metadata |
| Unregistered Sell hard-blocked at readiness | **View-only Sell** + banner; Pay disabled |
| Capacity blocked create-code | Capacity blocks **register this device** |

### Platform

| Change | Detail |
|---|---|
| Register ACL | Any active org member may register *this* installation (`EnsureCanViewOrganization`), not only governing admins |
| Error code | `application.pos_device.registration_required` when installation is not registered |
| Token APIs | **Kept** for MAUI compatibility (`OrgPosDevices.razor`, `PosDeviceRegister.razor`) |
| Create-token capacity check | Unchanged (MAUI still uses codes) |

### Sales execution gate

- Client: `moneyPostReady` false without authorized device; Pay disabled; view-only banner
- Server: `IPosDeviceTransactionAuthorizer` → Platform authorize; missing/unregistered → `registration_required` / revoked → `revoked`
- Offline: last-good warm snapshot only when previously ready; unregistered endpoint cannot invent offline money authority

## Identity

- Durable `InstallationDeviceId` in localStorage (RMAP-10b)
- `This device` = installation id match only (not User-Agent)
- Tabs/reloads do not consume extra slots

## Explicit exclusions / deferred

- MAUI registration-code UX remains until a separate MAUI migration
- Dead React i18n keys for redeem/createCode retained for locale parity / unused client helpers (API methods kept)
- No fake upgrade/payment path
- No hardware fingerprinting

## Tests exercised

- Vitest: sell-readiness view_only, device presentation/capacity, OrgPosDevicesPage revoke governance, locale parity
- Playwright: ops UX + RMAP-10b updated for direct register / view-only (mock-bound)
- Platform/POS compile gates as run in closeout

## Flags

`DEVICE_REGISTRATION_SIMPLIFICATION=AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW`  
`POS_EXECUTION_DEVICE_GATE=AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW`  
`REGISTRATION_CODE_UX_REQUIRED=NO`  
`NEXT_RMAP_AUTHORIZED=NO`  
`PRODUCTION_CUTOVER=NO`
