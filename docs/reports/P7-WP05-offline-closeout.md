# P7-WP05 — Offline Closeout

Phase marker: `P7-WP05-offline-closeout`

## Status

**Complete with documented risks. Phase 7 closed.** Reconciled P7-WP01 through P7-WP04 as one coherent offline subsystem. Hardened confirmed closeout defects (projection double-count after credit confirm, plaintext pending due-date/reversal fields, unsafe conflict JSON, missing per-operation capability revalidation, RecoveryRequired sync state, key-unavailable recovery surfacing, download N+1 customer summary). **No new business capability.** **Not production-ready** while R-109, R-022, R-129 / full-database encryption, production authentication/roles, and production background scheduling remain open. **Phase 8 was not started.**

Feature commit: `3b5a1e72294eb102f51f46c995e784138685faa4`

## Phase 7 closeout decision

Mark Phase 7 **complete with documented risks** when:

- P7-WP01–P7-WP04 form one subsystem (identity → encrypted queue → customer/credit → payment/recovery → closeout hardening)
- No critical offline-sync defect remains in the reconciled scope
- Sensitive local business values remain row-level AES-GCM encrypted; context isolation proven
- Queue processing is durable, deterministic, idempotent, and crash-safe
- Financial projections reconcile with server authority after confirm/reject/rebuild
- Conflicts and access loss never resolve silently
- Sync UX is truthful (including Recovery Required)
- Full `ExItS.slnx` Release tests pass (619 / 0 / 0)
- Android Release APK builds
- Documentation matches implementation
- No nested HealthCare product tree in this repository
- Git is clean; `main` matches `origin/main` after push

**Do not claim production readiness** while documented blockers remain open.

## Final Phase 7 architecture

```text
SecureStorage DeviceId + pos.local.payload.key
        ↓
Per-context SQLite (user × org × product) — schema v1→v4
  - offline_operations (AES-GCM ciphertext)
  - local_customer_projection / local_credit_projection / local_repayment_projection
  - local_sync_meta / local_schema_info / local_context_info
        ↓
OfflineQueueProcessor
  - access revalidation + per-operation Utang capability
  - decrypt → dispatch → mark Succeeded / retry / permanent / conflict / BlockedByAccess
        ↓
POS API + pos.idempotency_records (PostgreSQL)
        ↓
MAUI PosShell persistent sync indicator (truthful states)
```

## DeviceId lifecycle

- Stable random id in SecureStorage only
- Never an authorization credential
- Bound into operation envelopes for diagnostics/correlation only
- Restart preserves DeviceId; logout/context switch does not grant cross-context access

## Local database and encryption model

| Item | Decision |
|---|---|
| Isolation | One DB file per user + organization + product (`ContextHash`) |
| Schema | v1 foundation → v2 encrypted outbox → v3 customer/credit cache → v4 repayment projections |
| Encryption | Row-level AES-GCM via `ILocalPayloadProtector`; key only in SecureStorage |
| Full-DB encryption | **Deferred** (SQLCipher). Explicit production gate before release |
| Key loss | Fail closed; preserve ciphertext; surface Recovery Required; never overwrite unreadable rows |
| Plaintext ban | Queue payloads, customer PII, credit/repayment amounts/remarks, pending due-date/reversal reasons stay out of SQLite text columns |
| Conflict JSON | Safe metadata only (`statusCode`/`errorCode` or `status`/`updatedAtUtc`) — no PII dumps |

## Queue envelope and states

One generic envelope (`offline_operations`). States: Pending, Syncing, Succeeded, RetryableFailure, PermanentFailure, Conflict, BlockedByAccess.

Ordering: FIFO by `CreatedUtc`, then `OperationId`, within active context. Dependencies block overtaking. Concurrent claims cannot process one item twice. Succeeded never resubmitted. Failed/blocked retained (OD-10). No silent delete of unsynced work.

## Operation inventory (generic queue only)

| Constant | Value |
|---|---|
| `DevOfflineProbe` | `dev.offline-probe` (Development/Testing only) |
| `CustomerCreate` | `customer.create` |
| `CustomerUpdate` | `customer.update` |
| `CreditCreate` | `credit.create` |
| `RepaymentCreate` | `repayment.create` |
| `RepaymentReverse` | `repayment.reverse` |
| `CreditReverse` | `credit.reverse` |
| `CreditDueDateSet` | `credit.due-date.set` |
| `CreditDueDateClear` | `credit.due-date.clear` |

**Not present:** `statement.*`, `receipt.*`, offline customer deactivate/reactivate.

## Idempotency and retry

- Exact replay executes server mutation once (`pos.idempotency_records`)
- Same key + different payload → conflict
- Concurrent duplicates converge
- Transient failures → bounded backoff; permanent failures do not loop
- Timeout after possible server success → idempotency replay

## Offline authorization limitation

No time-based offline entitlement grace (**R-022 open**). Offline work allowed only while:

1. User authenticated online
2. Organization + PinoyBusinessPOS access validated
3. Same in-process continuous session remains active
4. Same user/organization/product context selected

Before each dispatch: session/org/POS access + subscription/feature capability for the operation type. Denied → `BlockedByAccess`; work retained. Local acceptance never overrides later server authorization.

## Recovery matrix

| Condition | Required result |
|---|---|
| Crash before enqueue commit | No partial optimistic state |
| Crash after transactional enqueue | Operation + projection recover |
| Crash while `Syncing` | Abandoned claim recovered to Pending |
| Timeout after server acceptance | Idempotency replay returns original result |
| Retryable network/server error | Bounded retry |
| Permanent validation failure | Retained as PermanentFailure / Rejected |
| Version conflict | Explicit Conflict review; no silent merge |
| Dependency failure | Dependents blocked/retained |
| Access revoked / capability denied | `BlockedByAccess`; no processing |
| Encryption key unavailable | Fail closed; Recovery Required; preserve encrypted data |
| Projection drift | Refresh + deterministic `RebuildOptimisticBalancesAsync` |
| Logout with pending work | Retained and isolated (OD-10) |
| User/org switch | Prior queue inaccessible |
| Local discard request | Allowed only if never server-confirmed |

**No automatic financial conflict resolution.**

## Sync indicator behavior

Persistent on protected POS screens. Priority:

1. Reconnect to verify access
2. Recovery Required (Conflict / PermanentFailure / BlockedByAccess / key unavailable)
3. Syncing
4. Sync Failed (retryable failures)
5. Pending Sync
6. Last Synced (only after server-confirmed success)
7. Online / Offline

Counts scoped to active context. Local save never displays server-synced success. Diagnostics Development/Testing-only; no raw payloads, keys, paths, tokens, headers, or exception dumps.

## Financial reconciliation

`Projected = max(0, confirmed + pendingCredit − pendingRepayment)`.

Confirmed credit success rebuilds optimistic balances so pending credit is not double-counted. Rejected ops remove only their optimistic effect. Reversals require `ServerConfirmed` originals. Due-date conflicts never silently overwrite server history.

## Confirmed defects fixed in P7-WP05

1. Credit confirm left pending credit → projected double-count → rebuild after confirm
2. Pending due-date / reversal reason plaintext columns → encrypted JSON; columns NULL
3. Conflict JSON could hold PII → safe metadata only
4. Queue process lacked per-operation capability revalidation → `RevalidateOperationAsync`
5. Recovery Required not a first-class sync kind → `PosSyncStatusKind.RecoveryRequired`
6. Key unavailable not surfaced as Recovery Required
7. Download credit/repayment path N+1 customer summary → batched unique customer IDs
8. Missing focused closeout tests (migration chain, capability deny, recovery status, plaintext)

## Explicit deferred capabilities

- Offline statements and receipts
- Offline customer deactivate/reactivate
- Production background sync scheduling
- Time-based offline authorization grace (R-022)
- Persisted repayment allocation
- Sales, inventory, gateways, QR/cards, tax invoices
- Interest, penalties, limits, installments, write-offs
- Full-database encryption (SQLCipher)
- Phase 8 Basic Store functionality

## Migration chains

| Store | Versions |
|---|---|
| Local SQLite | Clean init → v4; incremental v1→v2→v3→v4 proven in unit tests |
| Server POS | Existing idempotency + repayment sync migrations (apply / rollback where supported / re-apply already covered by Phase 6/7 integration tests) |

No new server migration required for closeout (no confirmed schema defect needing one).

## Tests, volumes, Android

| Suite | Passed | Failed | Skipped |
|---:|---:|---:|---:|
| Full `ExItS.slnx` Release | **619** | **0** | **0** |

Baseline 601 preserved and exceeded (+18 focused closeout tests).

Tested data volumes (MVP-scale unit/integration): single-context queues (1–8 concurrent claims), small customer/credit/repayment sets, schema chain seeds. Not load-tested for large offline backlogs; production sync throughput remains an open limitation.

Android Release APK:

`src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/bin/Release/net10.0-android/com.exits.pinoybusinesspos-Signed.apk`

Interactive device/emulator validation **not** claimed (`adb` unavailable) — **R-109 remains open**.

## Risks and production limitations

| ID / topic | Status |
|---|---|
| R-109 device validation | **Open** — no interactive offline E2E on device |
| R-022 offline authorization grace | **Open** — process-lifetime session only |
| R-129 SQLitePCLRaw NU1903 | **Open** — mitigated by row-level AES-GCM; SQLCipher deferred |
| Full-database encryption | **Required gate before production** while business data is not whole-DB encrypted |
| OD-10 retained pending work | **Resolved as retain** — never silent delete |
| Encryption-key loss recovery | Fail closed; manual SecureStorage restore; no auto re-key |
| Local-data retention | Retained across logout; no time-based purge |
| Projection rebuild correctness | Covered by rebuild-after-confirm + unit tests |
| Sync throughput | MVP-scale only; not production-proven |
| Idempotency-record retention | Server policy unchanged; no silent POS data duplication |
| Production background scheduling | **Deferred** |
| Production auth / POS roles | **Open** (prior phases) |

## Documentation and Git

Updated Phase 7, offline-sync design, portfolio, FILE-MANIFEST, README, security/data-ownership/testing-strategy, risks, release-plan, reports index, this report.

| Field | Value |
|---|---|
| Feature commit | `3b5a1e72294eb102f51f46c995e784138685faa4` |
| Docs hash-record commit | `1d4c6eb9fabd0b8a3014f131529a83659df8fe6f` |
| Final working tree | clean after push |

## Portfolio independence

Root `HealthCare/` must remain absent/untracked and outside `ExItS.slnx`.

## Exact next authorized phase / work package

**Phase 8 — Basic Store** (do not begin until explicitly authorized).
