# P7-WP04 — Payment Sync and Recovery

Phase marker: `P7-WP04-payment-sync-and-recovery`

## Status

**Complete with documented risks.** Offline `RepaymentCreate`, `RepaymentReverse`, `CreditReverse`, `CreditDueDateSet`, and `CreditDueDateClear` via the generic queue; encrypted local repayment and due-date projections; dependency-safe FIFO; crash/uncertain-outcome recovery via idempotency replay; confirmed / pending credit / pending repayment / projected outstanding. **No** offline statements, receipts, customer deactivate/reactivate, automatic conflict merge, or production background scheduling. P7-WP05 was not started.

Feature commit: `9c862b4bcd1604a351334120823bdf1e4a2014cb`

## Delivered capability

| Area | Delivered |
|---|---|
| Local read models | Encrypted SQLite `local_repayment_projection` (schema v4); due-date pending fields on credit projections |
| Offline mutations | `RepaymentCreate`, `RepaymentReverse`, `CreditReverse`, `CreditDueDateSet`, `CreditDueDateClear` enqueued on generic `offline_operations` outbox |
| Balance projection | `Projected outstanding = confirmed + pending credit − pending repayment` (never below zero locally) |
| Overpayment guard | Local `local_overpayment` when repayment exceeds available balance |
| Dependencies | Repayment for locally created customer waits on `CustomerCreate`; reversal/due-date require `ServerConfirmed` source entry |
| Recovery | `RebuildOptimisticBalancesAsync`; rejected repayments remove pending repayment totals; discard clears optimistic pending due dates |
| Download/reconcile | `/api/v1/pos/sync/repayments` incremental endpoint; server-confirmed repayments upserted locally |
| Server idempotency | Repayment offline dispatchers integrated with `pos.idempotency_records` |
| Sync UX | Payment queue operations contribute to Pending/Syncing/Failed indicator |

## Explicit exclusions (P7-WP05+)

Offline statements/receipts, customer deactivate/reactivate offline, automatic financial conflict resolution, production sync scheduling, sales/inventory/gateways/QR/cards, SQLCipher, offline entitlement grace (R-022), time-based retention purge.

## Encryption

Full-database encryption (SQLCipher) **deferred** — R-129 must not be worsened.

**Chosen mechanism:** authenticated **row-level AES-GCM** using the existing SecureStorage-backed `ILocalPayloadProtector` / `pos.local.payload.key` architecture. Repayment amount and remarks encrypted; no plaintext financial data in SQLite text columns. Key never in SQLite. Key loss: fail closed; preserve encrypted DB; never overwrite unreadable ciphertext; never log decrypted business data.

## Operation types and dependencies

| Constant | Value |
|---|---|
| `RepaymentCreate` | `repayment.create` |
| `RepaymentReverse` | `repayment.reverse` |
| `CreditReverse` | `credit.reverse` |
| `CreditDueDateSet` | `credit.due-date.set` |
| `CreditDueDateClear` | `credit.due-date.clear` |

**Not present:** no `statement.*` or `receipt.*` offline operation types.

- Repayment for locally created customer → successful `CustomerCreate`
- Repayment against locally created credit → successful `CreditCreate` (when applicable)
- Reversal / due-date → original entry is `ServerConfirmed`
- Duplicate pending reversal blocked (`credit_not_reversible` / `reversal_already_pending`)
- Permanent dependency failure → dependents Conflict/blocked; retained for review

## Balance projection

`Projected outstanding = confirmed outstanding + pending credit − pending repayment` (never below zero). No editable balance column. Rejected repayments remove optimistic pending repayment effects and retain history.

## Recovery

Recover abandoned `Syncing` after restart; rebuild optimistic balances from `PendingCreate` projections; never assume timeout failed — use idempotency replay; refresh ledger/balances after confirmed/rejected financial ops; never silent discard or last-write-wins. Safe actions: Retry, Review, Refresh from server, Discard local only when never server-confirmed with explicit user confirm.

## Offline authorization and session limitation

No time-based offline entitlement grace (R-022 remains open). Offline payment work is allowed only while the user previously authenticated online, org + PinoyBusinessPOS access were validated, the **same in-process application session** remains active, and the same user/organization/product context remains selected.

## Tests, Android evidence, and limitations

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Full `ExItS.slnx` Release | **601** | **0** | **0** |

Baseline 586 preserved and exceeded (+15 tests: 12 unit, 3 integration).

Unit coverage (`PaymentOfflineStoreTests`): pending repayment/projected balance; local overpayment; exact balance; customer-create dependency ordering; credit reverse requires ServerConfirmed; duplicate PendingReversal blocked; due-date pending/discard; rejected repayment balance correction; `RebuildOptimisticBalancesAsync`; schema v4 `local_repayment_projection` (no `repayments` table); encryption; operation-type guards.

Integration coverage (`PosPaymentOfflineIdempotencyApiTests`): repayment idempotency replay; payload hash mismatch 409; sync repayments endpoint.

Android Release APK (expected path):

`src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/bin/Release/net10.0-android/com.exits.pinoybusinesspos-Signed.apk`

Interactive device validation **not** claimed — **R-109** open.

## Risks, blockers, and deferred items

| ID | Notes |
|---|---|
| R-109 | No interactive Android validation |
| R-022 | Offline entitlement grace still undefined |
| R-129 | SQLitePCLRaw NU1903 transitive advisory — **mitigated for WP04** by avoiding SQLCipher; advisory remains open on Microsoft.Data.Sqlite |
| Key loss | Recovery requires SecureStorage restore; no automatic re-key of existing ciphertext |
| OD-10 | Retain encrypted pending work; never silent delete; no time-based purge |

## Documentation and Git

Updated Phase 7, offline-sync design, portfolio, FILE-MANIFEST, README, security/data-ownership/testing-strategy, risks, release-plan, reports index, this report.

| Field | Value |
|---|---|
| Commit hash | `9c862b4bcd1604a351334120823bdf1e4a2014cb` |
| Final working tree | uncommitted (per task instruction) |

## Portfolio independence

Root a nested foreign product tree must remain absent/untracked and outside `ExItS.slnx`.

## Exact next work package

**P7-WP05 — Offline Closeout** (do not begin until authorized).
