# P7-WP01 — SQLite and Device Identity

Phase marker: `P7-WP01-sqlite-and-device-identity`

## Status

**Complete with documented risks.** Foundation-only offline local store: durable DeviceId, per-user/org/product SQLite isolation, schema migrations, local-context lifecycle, permanent sync-status shell (Online / Offline / Reconnect), and Development/Testing diagnostics. **Does not enable offline business operations.** P7-WP02 was not started.

Feature commit: _(recorded after push)_

## Delivered capability

| Area | Delivered |
|---|---|
| DeviceId | Cryptographic GUID via `IDeviceIdentityProvider` + SecureStorage (`pos.device.id`); survives logout/context switch; regenerates on storage loss; not auth proof; no server registration |
| SQLite | `ExItS.PinoyBusinessPOS.LocalStore` using **Microsoft.Data.Sqlite**; MAUI sandbox paths; no SQLCipher |
| Isolation | One DB file per User + Organization + Product; hashed `pos-local-{32hex}.db` filenames |
| Schema v1 | `local_schema_info`, `local_context_info` only |
| Lifecycle | Open after online org/product validation; close on logout/switch; offline launch does not unlock protected shell |
| Sync status UX | Permanent shell indicator + shared contract; WP01 wires Online / Offline / Reconnect only |
| Diagnostics | `/dev/offline-foundation` gated to Development/Testing |

## Explicit exclusions (P7-WP02+)

Offline mutation queue/outbox, idempotency, customer/credit/repayment/ledger cache, offline mutations, sync workers, conflict resolution, server device registration, entitlement snapshot cache, offline auth grace (R-022), pending-op retention (OD-10), SQLCipher for business data, sales/inventory/gateways/QR/cards, fabricated Pending Sync / Syncing / Sync Failed / Last Synced counts.

## Device identity lifecycle

1. First `GetOrCreateDeviceIdAsync` generates `Guid.NewGuid()` and stores under `SecureTokenKeys.DeviceId`.
2. Subsequent calls and app restarts reuse the same value.
3. Logout / ClearAllSessionKeys does **not** clear DeviceId.
4. User/org/product changes do not regenerate DeviceId.
5. Secure-storage failure returns a non-empty ephemeral id (safe fail); clearing the key creates a new durable id.
6. Never derived from hardware, IMEI, MAC, advertising id, username, or organization.

## SQLite architecture and schema

Dependency direction: MAUI → LocalStore → Application abstractions.

Abstractionsions: `IDeviceIdentityProvider`, `ILocalDatabasePathResolver`, `ILocalDatabaseFactory`, `ILocalDatabaseMigrator`, `ILocalContextManager`, `ILocalStoreRootPathProvider`.

Stack choice: **Microsoft.Data.Sqlite** (Microsoft-supported .NET SQLite). No EF Core SQLite in WP01. DesignSystem and Razor do not open SQLite.

Tables: `local_schema_info` (schema_version, applied_at_utc); `local_context_info` (context_hash, user_id, organization_id, product_code, created_at_utc, last_opened_at_utc). DeviceId is not stored in SQLite (SecureStorage authoritative).

Encryption: app sandbox + OS file protection only. Secrets remain in SecureStorage. Database encryption for cached business data is a **required decision before the first offline business-data WP**.

## User / organization / product isolation

- Separate file per context key `userId|organizationId|productCode` (SHA-256 hashed filename).
- Never reuse another user’s or organization’s database.
- Raw IDs never appear in filenames.
- Org switch closes prior connection and opens the new file.
- Logout closes active context; foundation files preserved (no business/queued data in WP01).

## Authorization and lifecycle behavior

- No offline authorization window in WP01.
- Local DB open requires authenticated online session + validated org/product access.
- `ProtectedShellAccessPolicy`: protected routes require online + HasPosAccess.
- Offline restore keeps durable session shell but denies protected entry → `/reconnect` (“Reconnect to verify access”).
- DeviceId and DB existence never grant access.
- No entitlement snapshot cached.

## Development diagnostics

Route `/dev/offline-foundation` (Settings link in Dev/Testing only). Shows shortened DeviceId, context metadata, hashed filename, schema version, init status, last opened UTC. Blocked in Production. No tokens, headers, raw sandbox paths, or business payloads.

## Tests, Android evidence, risks, and limitations

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Full `ExItS.slnx` Release | **563** | **0** | **0** |

Baseline 544 preserved and exceeded (+19 focused P7-WP01 tests).

Android Release APK:

`src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/bin/Release/net10.0-android/com.exits.pinoybusinesspos-Signed.apk`

Interactive emulator/device lifecycle validation **not** performed — **R-109 remains open**.

### Risks / open decisions

| ID | Notes |
|---|---|
| R-109 | No interactive Android validation |
| R-022 | Offline entitlement grace still undefined |
| OD-10 | Pending-op retention open (not blocking WP01 — no queue yet) |
| R-110 | NU1903: transitive `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 advisory — track upgrade when Microsoft.Data.Sqlite ships a fixed transitive |
| Encryption | Required before first offline business-data WP |

## Documentation and Git

Updated: phase-07, offline-sync-design, portfolio, FILE-MANIFEST, README, engineering (architecture/data-ownership/security/MAUI/testing as affected), risks, release-plan, reports index, this report.

Exact next work package: **P7-WP02 — Offline Queue and Idempotency** (do not begin until authorized).

## HealthCare freeze

Root `HealthCare/` remains ignored, untracked, outside `ExItS.slnx`.
