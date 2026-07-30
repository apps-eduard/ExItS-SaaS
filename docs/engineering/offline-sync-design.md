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

## Later phase (preview)

P7-WP03 — Customer and Credit Sync will integrate real operation types into this same queue.
