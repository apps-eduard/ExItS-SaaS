# POS-REACT RMAP-21B — Browser LocalStore + encrypted outbox

**Status:** PASS (foundation)  
**Dependencies:** 21A matrix locked; 21A.0 diagnostic safety  

## Delivered

| Capability | Evidence |
| --- | --- |
| IndexedDB via `idb` | `src/offline/db.ts` |
| Schema version | `OFFLINE_SCHEMA_VERSION = 1` |
| Personal vs Organization DB isolation | Separate DB names per scope key |
| AES-GCM encrypted envelopes | `src/offline/crypto.ts` + outbox tests |
| Outbox states (Pending…BlockedByAccess) | `src/offline/types.ts` |
| Atomic encrypt+enqueue transaction | `enqueueEncryptedOperation` |
| Idempotency key preserved on retry | unit test |
| Dependency ordering | unit test |
| Abandoned Syncing recovery | unit test |
| Safe metadata (no plaintext in diagnostics list) | `listSafeOutboxMetadata` |
| Auth tokens in IndexedDB | **NO** |

## Explicit non-claims

- Not native SecureStorage / Keystore / Keychain parity
- Cold-start unlock still `DEFERRED_SECURITY_GAP`
- Catalog/sale/customer/Personal domain stores beyond outbox envelope: continue in 21D–21G
- Sync network processor wiring: 21C/21H

## Tests

`npx vitest run src/offline` — 6 passed

## Next

RMAP-21C real Connection & Sync UI driven by outbox counts.
