# P7-WP02 — Offline Queue and Idempotency

Phase marker: `P7-WP02-offline-queue-and-idempotency`

## Status

**Complete with documented risks.** Generic encrypted SQLite outbox, FIFO processor, AES-GCM payload protection, server idempotency, access revalidation, operational sync indicator, and Dev/Testing probe. **No real offline customer/credit/repayment workflows.** P7-WP03 was not started.

Feature commit: `aa1f92eba97bc77775f59de8209b42c9d7a475cc`

## Delivered capability

| Area | Delivered |
|---|---|
| Generic queue | `offline_operations` envelope for all future POS operation types |
| Encryption | AES-GCM; key in SecureStorage only (`pos.local.payload.key`); unique nonce; AAD binding |
| States | Pending, Syncing, Succeeded, RetryableFailure, PermanentFailure, Conflict, BlockedByAccess |
| Ordering | FIFO by CreatedUtc, OperationId tie-breaker |
| Idempotency | `pos.idempotency_records`; exact replay; hash mismatch conflict; concurrent convergence |
| Retry | Transient only; max 8 attempts; exponential backoff + jitter |
| Access | Revalidate before process; BlockedByAccess retains work |
| OD-10 | Resolved — retain encrypted pending work; never silent delete; no time-based purge |
| Sync UX | Pending Sync (count), Syncing, Sync Failed, Last Synced wired truthfully |
| Dev probe | `/api/v1/pos/dev/offline-probe` (Development/Testing only) |

## Explicit exclusions (P7-WP03+)

Real offline customer/credit/repayment/reversal/due-date workflows, business-data cache, automatic financial conflict resolution, production sync scheduling, sales/inventory/gateways/QR/cards, SQLCipher, offline entitlement grace (R-022), time-based retention purge.

## Generic queue architecture, schema, and encryption

Local schema **v2** adds `offline_operations` + `local_sync_meta`. Payloads stored as ciphertext/nonce/tag. Key never in SQLite. Key loss fails closed with localized recovery message; encrypted DB preserved.

## Idempotency and duplicate handling

Identity: organization + product + operation type + idempotency key. First success persists outcome; exact replay returns it; different payload hash → conflict; concurrent inserts converge to one row.

## Retry, ordering, and crash recovery

Abandoned `Syncing` rows recover to `Pending` on process/restart. Transient failures back off; after max attempts → PermanentFailure (retained). Permanent/authz/validation/conflict never auto-retry indefinitely.

## Access revalidation, retention, and isolation

Logout closes active context but retains encrypted queue rows. Only the same user/org/product context may resume. Revoked/Suspended/offline → BlockedByAccess. Per-context SQLite isolation from P7-WP01 preserved.

## Sync indicator and diagnostics

Shell shows truthful Online/Offline/Reconnect/Pending Sync/Syncing/Sync Failed/Last Synced. Last Synced updates only after confirmed server success. Diagnostics at `/dev/offline-foundation` show safe queue counts/metadata only (Production gated).

## Tests, Android evidence, risks, and limitations

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Full `ExItS.slnx` Release | **573** | **0** | **0** |

Baseline 563 preserved and exceeded.

Android Release APK:

`src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/bin/Release/net10.0-android/com.exits.pinoybusinesspos-Signed.apk`

Interactive device validation **not** claimed — **R-109** open.

| ID | Notes |
|---|---|
| R-109 | No interactive Android validation |
| R-022 | Offline entitlement grace still undefined |
| R-129 | SQLitePCLRaw NU1903 transitive advisory |
| Key loss | Recovery requires SecureStorage restore; no automatic re-key of existing ciphertext |

## Documentation and Git

Updated Phase 7, offline-sync design, portfolio, FILE-MANIFEST, README, security/testing/data-ownership, risks, release-plan, reports index, this report.

Exact next work package: **P7-WP03 — Customer and Credit Sync** (do not begin until authorized).

## Portfolio independence

Root a nested foreign product tree must remain absent/untracked and outside `ExItS.slnx`.
