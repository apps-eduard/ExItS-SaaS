# POS React — Active Device UI + Revoked Device Audit Retention

**Status:** `AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW`  
**Branch:** `feat/pos-react-client`  
**Starting HEAD:** `d52f224fc099c18c8dba88b3f1deca047eac467c`  
**Implementation HEAD:** `9ad0723e`  
**Final HEAD:** `7ff5c91f054c5abe904e5dc0d5f6e46d5ee9e890`

## Canonical rules delivered

1. Normal Device Management = **Active** devices only (server `ListDevices` → `ListActiveByOrganizationAsync`).
2. Revoked devices do not appear in the normal UI.
3. Revoke/Remove soft-sets `PosDeviceStatus.Revoked` — **no physical delete**.
4. Capacity continues `CountActiveAsync` (Active only).
5. Audit: `platform.pos_device.revoked` via `OrganizationGovernanceAuditWriter` retained; history endpoint `GET .../pos-devices/history` for governing admins.
6. Customer action label: **Remove device** (domain remains Revoke).
7. No InstallationDeviceId / technical dump on normal cards.

## Gaps (documented, not redesigned)

- Reactivation clears `RevokedAtUtc` / `RevokedByUserId` on the current PosDevice row — immutable history relies on Platform audit events, not the live row.
- MAUI Device Management inherits active-only list from the shared Platform API (consistent).

## Flags

`ACTIVE_DEVICE_UI_ONLY=AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW`  
`REVOKED_DEVICE_DB_RETENTION=AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW`  
`DEVICE_AUDIT_HISTORY=AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW`  
`NEXT_RMAP_AUTHORIZED=NO`  
`PRODUCTION_CUTOVER=NO`

