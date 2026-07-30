# PinoyBusinessPOS Offline Synchronization

[Home](../index.md) | [Security](security.md) | [Phase 7](../phases/phase-07-offline-sync.md)

## Purpose

Safe offline-first operation and synchronization for PinoyBusinessPOS. Work packages are sequenced; do not implement later WP scope early.

## P7-WP01 decisions (complete)

Foundation: DeviceId, per-context SQLite isolation, schema versioning, local-context lifecycle, sync-status shell shell (Online/Offline/Reconnect), Dev diagnostics. No offline business operations.

## P7-WP02 decisions (authoritative)

### Scope — infrastructure only

Deliver:

- Generic SQLite outbox (`offline_operations`) shared by all future POS operation types
- AES-GCM payload encryption; key in SecureStorage only (no SQLCipher)
- Explicit queue state machine and FIFO processing
- Bounded retry/backoff with transient vs permanent classification
- Server idempotency persistence for future mutations
- Crash/restart recovery of abandoned `Syncing` claims
- Access revalidation before processing (`BlockedByAccess`)
- Operational sync indicator (Pending Sync / Syncing / Sync Failed / Last Synced)
- Development/Testing probe handler + diagnostics (not a production business endpoint)

**Does not enable real offline customer, credit, repayment, or other business workflows.**

### OD-10 resolution (retention)

Pending operations remain encrypted across logout and access loss, isolated to their original user/organization/product context, and are never processed until that context is reauthorized. They are not silently deleted. No time-based retention period is invented in this work package.

### Generic envelope

One reusable envelope for all current and future POS offline operations. Future WPs add versioned `OperationType` values and handlers — they must not create separate queues.

Required fields: OperationId, DeviceId, UserId, OrganizationId, ProductCode, OperationType, PayloadVersion, encrypted payload (ciphertext/nonce/tag), payload hash, idempotency key, CreatedUtc, NextAttemptUtc, AttemptCount, QueueState, LastAttemptUtc, safe failure code/summary, server reference, concurrency/version metadata.

### Queue states

`Pending` · `Syncing` · `Succeeded` · `RetryableFailure` · `PermanentFailure` · `Conflict` · `BlockedByAccess`

### Processing order

FIFO by `CreatedUtc` within a context; `OperationId` is the deterministic tie-breaker. Later operations must not overtake earlier pending ones in the same context.

### Payload protection

- Random 256-bit key in SecureStorage (`pos.local.payload.key`)
- AES-GCM authenticated encryption; unique nonce per payload
- AAD binds ciphertext to context hash + operation identity where practical
- Key never in SQLite, Preferences, logs, UI, or docs
- Key loss: fail closed; preserve encrypted DB; localized recovery error; no overwrite/process

### Server idempotency

Identity: organization + product + operation type + idempotency key.

- First valid submission executes once
- Exact replay returns original outcome
- Same key + different payload hash → conflict
- Concurrent duplicates converge to one execution
- Authorization checked before protected results
- Idempotency never bypasses validation, scope, entitlements, or business invariants

### Retry

Retry only transient: connectivity, timeout, temporary unavailability, approved 5xx.  
Do not auto-retry: validation, authz denial, org mismatch, revoked access, business conflicts, incompatible version, key/payload mismatch, financial invariant failures.

Bounded exponential backoff with jitter; max attempts documented. After limit: retain, classify, require explicit later retry/review; never auto-delete.

### Explicit deferrals (P7-WP03+)

Customer/credit/repayment offline workflows, business-data cache, automatic financial conflict resolution, production sync scheduling, sales/inventory/gateways/QR/cards, SQLCipher, offline entitlement grace (R-022), time-based retention purge.

## P7-WP03 decisions (authoritative)

### Scope

Encrypted local customer + credit read models; offline `CustomerCreate`, `CustomerUpdate`, and `CreditCreate` via the **same** generic queue; download/reconcile; optimistic concurrency conflicts without silent merge; confirmed vs projected outstanding. **No** offline repayments, reversals, due-date changes, statements/receipts, or production sync scheduling.

### Offline authorization (session continuity)

No time-based offline entitlement grace (R-022 remains open). Offline customer/credit work is allowed only while the user previously authenticated online, org + PinoyBusinessPOS access were validated, the **same in-process application session** remains active, and the same user/organization/product context remains selected.

After app restart while offline, logout, user/org switch, or secure-session loss: protected POS requires reconnect; cache must not unlock the prior context; offline mutations unavailable; queued work remains encrypted and retained (OD-10). Reconnect revalidates access before download or queue processing.

### Local business-data encryption

Full-database encryption (SQLCipher) remains deferred — R-129 (transitive SQLitePCLRaw NU1903 on Microsoft.Data.Sqlite) must not be worsened by adding SQLCipher packages in this WP.

**Chosen mechanism:** authenticated **row-level AES-GCM** using the existing SecureStorage-backed `ILocalPayloadProtector` / `pos.local.payload.key` architecture. Unique nonce per row; AAD binds context + entity identity. No plaintext customer mobile, address, notes, credit remarks, or financial amounts in SQLite. Fail closed on key loss; never overwrite unreadable ciphertext; never log decrypted business data.

### Operation types (generic queue only)

| Constant | Value |
|---|---|
| `CustomerCreate` | `customer.create` |
| `CustomerUpdate` | `customer.update` |
| `CreditCreate` | `credit.create` |

`CreditCreate` for a locally created customer **depends on** that customer’s `CustomerCreate` operation succeeding. Unresolved dependencies block later ops. Permanent/conflict failure of the dependency marks dependents Conflict/blocked — never submit independently.

### Local read-model states

`ServerConfirmed` · `PendingCreate` · `PendingUpdate` · `Syncing` · `Conflict` · `Rejected`

Local-only data never appears ServerConfirmed. Client-generated IDs remain stable. Server concurrency/version (`UpdatedAtUtc`) retained. No editable outstanding balance column — confirmed vs pending financial effects are distinguished.

### Balance projection

`Projected outstanding = confirmed outstanding + pending locally accepted credit`. On credit rejection: remove from pending/projected; retain rejected history; safe localized reason; never silent-delete the operation.

### Customer conflict policy

Optimistic concurrency uses last confirmed server `UpdatedAtUtc`. On conflict: retain local + server versions; mark Conflict; user may discard local or review server and retry deliberately. No silent overwrite/merge. Deactivate/reactivate remain online-only.

### Explicit deferrals (P7-WP04+)

Offline repayments, credit/repayment reversals, due-date changes, statements/receipts, automatic conflict merging, production background sync scheduling, sales/inventory/gateways/QR/cards, SQLCipher, offline entitlement grace (R-022).

## Later phase (preview)

**P7-WP04 — Payment Sync and Recovery** follows Customer and Credit Sync.
