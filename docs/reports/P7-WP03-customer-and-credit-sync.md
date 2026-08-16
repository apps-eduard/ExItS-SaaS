# P7-WP03 — Customer and Credit Sync

Phase marker: `P7-WP03-customer-and-credit-sync`

## Status

**Complete with documented risks.** Encrypted local customer + credit read models; offline `CustomerCreate`, `CustomerUpdate`, and `CreditCreate` via the generic queue; download/reconcile; optimistic concurrency conflicts; confirmed vs projected outstanding. **No offline repayments.** P7-WP04 was not started.

Feature commit: `3763ca0fe406067eb539b3d8adca21447f813dcf`

## Delivered capability

| Area | Delivered |
|---|---|
| Local read models | Encrypted SQLite customer + credit projections per user/org/product context |
| Offline mutations | `CustomerCreate`, `CustomerUpdate`, `CreditCreate` enqueued on the generic `offline_operations` outbox |
| Download/reconcile | Server-confirmed customers and credits upserted into local store when online |
| Dependencies | `CreditCreate` for a locally created customer depends on that customer's `CustomerCreate` succeeding |
| Balance projection | Confirmed outstanding + pending locally accepted credit = projected outstanding |
| Conflict handling | Optimistic concurrency on `UpdatedAtUtc`; Conflict state; discard local or review server and retry |
| Session gate | Offline mutations only while same in-process session after online auth; restart/logout/switch requires reconnect |
| Sync UX | Existing Pending Sync / Syncing / Sync Failed / Last Synced indicator reflects customer/credit queue truthfully |
| Server idempotency | Customer/credit offline dispatchers integrated with `pos.idempotency_records` |

## Explicit exclusions (P7-WP04+)

Offline repayments, credit/repayment reversals, due-date changes, statements/receipts, automatic conflict merging, production background sync scheduling, sales/inventory/gateways/QR/cards, SQLCipher, offline entitlement grace (R-022), time-based retention purge. Customer deactivate/reactivate remain online-only.

## Encryption

Full-database encryption (SQLCipher) **deferred** — R-129 must not be worsened by adding SQLCipher packages in this WP.

**Chosen mechanism:** authenticated **row-level AES-GCM** using the existing SecureStorage-backed `ILocalPayloadProtector` / `pos.local.payload.key` architecture from P7-WP02. Unique nonce per row; AAD binds context + entity identity. No plaintext customer mobile, address, notes, credit remarks, or financial amounts in SQLite. Key never in SQLite. Key loss: fail closed; preserve encrypted DB; localized recovery error; never overwrite unreadable ciphertext; never log decrypted business data.

## Operation types and dependencies

| Constant | Value |
|---|---|
| `CustomerCreate` | `customer.create` |
| `CustomerUpdate` | `customer.update` |
| `CreditCreate` | `credit.create` |

`CreditCreate` for a locally created customer **depends on** that customer's `CustomerCreate` operation succeeding. Unresolved dependencies block later ops. Permanent/conflict failure of the dependency marks dependents Conflict/blocked — never submit independently.

## Local read-model states

`ServerConfirmed` · `PendingCreate` · `PendingUpdate` · `Syncing` · `Conflict` · `Rejected`

Local-only data never appears ServerConfirmed. Client-generated IDs remain stable. Server concurrency/version (`UpdatedAtUtc`) retained. No editable outstanding balance column — confirmed vs pending financial effects are distinguished.

## Customer conflict policy

Optimistic concurrency uses last confirmed server `UpdatedAtUtc`. On conflict: retain local + server versions; mark Conflict; user may discard local or review server and retry deliberately. No silent overwrite/merge.

## Balance projection

`Projected outstanding = confirmed outstanding + pending locally accepted credit`. On credit rejection: remove from pending/projected; retain rejected history; safe localized reason; never silent-delete the operation.

## Offline authorization and session limitation

No time-based offline entitlement grace (R-022 remains open). Offline customer/credit work is allowed only while the user previously authenticated online, org + PinoyBusinessPOS access were validated, the **same in-process application session** remains active, and the same user/organization/product context remains selected.

After app restart while offline, logout, user/org switch, or secure-session loss: protected POS requires reconnect; cache must not unlock the prior context; offline mutations unavailable; queued work remains encrypted and retained (OD-10). Reconnect revalidates access before download or queue processing.

## Sync indicator

Shell shows truthful Online/Offline/Reconnect/Pending Sync/Syncing/Sync Failed/Last Synced. Customer/credit queue operations contribute to pending/syncing/failed counts. Last Synced updates only after confirmed server success.

## Tests, Android evidence, and limitations

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Full `ExItS.slnx` Release | **586** | **0** | **0** |

Baseline 573 preserved and exceeded.

Android Release APK (expected path):

`src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/bin/Release/net10.0-android/com.exits.pinoybusinesspos-Signed.apk`

Interactive device validation **not** claimed — **R-109** open.

## Risks, blockers, and deferred items

| ID | Notes |
|---|---|
| R-109 | No interactive Android validation |
| R-022 | Offline entitlement grace still undefined |
| R-129 | SQLitePCLRaw NU1903 transitive advisory — **mitigated for WP03** by avoiding SQLCipher; advisory remains open on Microsoft.Data.Sqlite |
| Key loss | Recovery requires SecureStorage restore; no automatic re-key of existing ciphertext |
| OD-10 | Retain encrypted pending work; never silent delete; no time-based purge |

## Documentation and Git

Updated Phase 7, offline-sync design, portfolio, FILE-MANIFEST, README, security/data-ownership/testing-strategy, risks, release-plan, reports index, this report.

| Field | Value |
|---|---|
| Commit hash | `3763ca0fe406067eb539b3d8adca21447f813dcf` |
| Final working tree | clean after push |

## Portfolio independence

No unauthorized nested product tree at repo root; keep `ExItS.slnx` to authorized products only.

## Exact next work package

**P7-WP04 — Payment Sync and Recovery** (do not begin until authorized).
