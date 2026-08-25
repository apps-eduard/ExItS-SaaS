# POS-REACT RMAP-21C — Real Connection & Sync UI

**Status:** PASS  
**Depends on:** 21B outbox counts

## Delivered

- Shell title upgraded to **Connection & Sync**
- Connection section: browser Online/Offline (not “access verified” from navigator alone)
- Synchronization section from real outbox counts:
  - All changes synced
  - N changes waiting
  - Offline · N waiting
  - Syncing…
  - N change needs attention
  - Access required to finish syncing
- Last synced only when meta `lastSuccessfulSyncAt` exists **and** outbox fully synced
- Refresh from server = query invalidation (distinct from Retry sync)
- Retry sync = recover abandoned Syncing + refresh counts (processor continues in 21H)

## Fake claims

| Claim | Allowed? |
| --- | --- |
| All changes synced with empty/no outbox | YES (honest zero) |
| Last synced without successful sync meta | NO |
| Pending count without outbox rows | NO |

## Next

21D POS offline Sell + Cash (requires catalog LocalStore + sale enqueue + idempotency headers).
