# PinoyBusinessPOS Offline Synchronization

[Home](../index.md) | [Security](security.md) | [Phase 7](../phases/phase-07-offline-sync.md)

## Purpose

Safe offline-first operation and synchronization for PinoyBusinessPOS. Work packages are sequenced; do not implement later WP scope early.

## P7-WP01 decisions (authoritative)

### Scope — foundation only

Delivered in P7-WP01:

- SQLite local-store infrastructure (Microsoft.Data.Sqlite)
- Local schema versioning and migrations
- Per-user / per-organization / per-product database isolation
- Durable local DeviceId (SecureStorage)
- Local-context lifecycle (open/close on validated online access)
- Persistent sync-status shell indicator (Online / Offline / Reconnect only)
- Development/Testing diagnostics at `/dev/offline-foundation`

**P7-WP01 does not enable offline business operations.**

### Explicit deferrals (P7-WP02+)

- Offline mutation queue/outbox and idempotency processing
- Customer / credit / repayment / ledger caching
- Offline create/update/reversal operations
- Sync workers, retry scheduling, conflict resolution
- Server device registration
- Cached entitlement snapshots and offline authorization grace periods (**R-022** remains open)
- Pending-operation retention (**OD-10** remains open; not blocking WP01 because no business data is stored)
- SQLCipher / DB encryption for cached business payloads (required decision before first offline business-data WP)
- Production synchronization; sales/inventory/gateways/QR/cards

### Device identity

- One cryptographically random UUID generated on first use
- Persisted via existing `ISecureTokenStore` / MAUI SecureStorage
- Survives restart, logout, user change, organization change
- New id after reinstall, app-data clear, or secure-storage loss
- Not derived from hardware, IMEI, MAC, username, organization, or advertising IDs
- Not authentication or authorization proof
- No Platform/POS registration in WP01
- No reset/rotation UI in WP01

### Local database isolation

- Separate SQLite file per `User + Organization + Product`
- Deterministic hashed filename (no raw user/org IDs in path)
- Files under MAUI application sandbox; OS file protection only (no SQLCipher in WP01)
- No tokens, passwords, authorization headers, or entitlement grants in SQLite
- DesignSystem and Razor must not open SQLite directly

### Authorization (no offline grace window in WP01)

- Local DB open requires authenticated **online** session and validated org/product access
- Offline startup cannot enter protected POS routes using SQLite or prior session alone
- DeviceId and DB existence never grant access
- Fail closed for missing/stale/revoked/Suspended/unknown access

### Sync-status shell (permanent UX)

Shared contract states: Online, Offline, Pending Sync, Syncing, Sync Failed, Last Synced, Reconnect to verify access.

**WP01 wires only:** Online, Offline, Reconnect to verify access.  
Queue-driven states are deferred until P7-WP02 (must not be fabricated).

## Later phase (preview — not WP01)

MAUI stores approved offline business data in SQLite and queues commands with OperationId, DeviceId, and idempotency key (P7-WP02+).

Supported first when those WPs authorize:

- Customer creation
- Remarks-based credit
- Payment on existing credit
- Later: sales and inventory movements

Financial records remain append-only. Retry must not duplicate balances. Offline entitlement grace policy remains **undefined** until an explicit decision (R-022).
