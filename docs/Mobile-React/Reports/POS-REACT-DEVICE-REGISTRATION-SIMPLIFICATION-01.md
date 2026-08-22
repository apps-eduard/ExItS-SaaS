# POS React — Device Registration Simplification + Sales Execution Gate

**Status:** `AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW`  
**Branch:** `feat/pos-react-client`  
**Starting HEAD (this gap-fill):** `3127f8415a9799c22cfe16cca3deacd9032fbc09`  
**Prior implementation:** `0d8fb021` (core simplification) + `9ad0723e` (active-only UI)  
**Implementation HEAD:** `e937dd256f241a958f2b8eb286a0a0803301711d`  
**Final HEAD:** `PLACEHOLDER`

## Canonical product rule

> Users may authenticate from any permitted endpoint.  
> POS device capacity applies only to endpoints explicitly registered for POS execution.  
> Only registered POS devices may execute sales.  
> Registration codes are not part of the normal React POS onboarding or device-management experience.  
> Active Device Management lists Active devices only; revoked rows remain soft-retained for audit.

## Gap-fill delivered on top of prior simplification

| Item | Change |
|---|---|
| Residual "Register with a code" footer | Removed; link uses **Register this device** |
| Dead redeem/createCode i18n | Removed from all five locales |
| Customer copy | Device terminology; no registration-code sell help |
| Concurrent capacity | `RegisterCurrentDevice` + MAUI redeem under `ExecuteWithOrganizationLockAsync` |
| Tests | Capacity slot + registration_required authorize + no-code UX Vitest |
| Docs | `device-and-payment-integration.md`, Authoritative devices section |

## Compatibility retained

| Surface | Why |
|---|---|
| Platform `POST .../registration-tokens` + redeem | MAUI `OrgPosDevices.razor` / `PosDeviceRegister.razor` |
| React client helper methods (unused by pages) | Optional; not shown in UX |

## Sales execution gate

- Client: `view_only` + `moneyPostReady: false`; Pay disabled; banner
- Server: `application.pos_device.registration_required` via Platform authorize + POS authorizer on sale/payment/return/expense mutations
- Offline: warm snapshot only when previously money-ready; unregistered cannot invent offline money authority

## Flags

`DEVICE_REGISTRATION_SIMPLIFICATION=AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW`  
`POS_EXECUTION_DEVICE_GATE=AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW`  
`REGISTRATION_CODE_REACT_UX_REQUIRED=NO`  
`ACTIVE_DEVICE_UI_ONLY=APPROVED`  
`REVOKED_DEVICE_DB_RETENTION=APPROVED`  
`NEXT_RMAP_AUTHORIZED=NO`  
`PRODUCTION_CUTOVER=NO`
