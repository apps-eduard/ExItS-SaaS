# POS-REACT RMAP-21H — Reconnect / recovery / PWA / sync processor

**Status:** PASS  
**Depends on:** 21B–21G queued operations

## Delivered

| Capability | Evidence |
| --- | --- |
| Outbox processor (claim → decrypt → resolve refs → POST → state) | `outbox-processor.ts` |
| Cash sale v1 body replay + Idempotency-Key | unit test |
| QueuedRequest v2 platform/pos replay | processor |
| Ambiguous Personal create → PermanentFailure (no auto-dupe) | unit test + `mayAutoRetry` |
| HTTP 403 → BlockedByAccess | unit test |
| Abandoned Syncing recovery on drain / bind | `recoverAbandonedSyncing` |
| Debounced drain on browser online | `OutboxSyncHost` |
| Retry sync from Connection & Sync | `retrySync` → `drainOutbox` |
| PWA API caching | NetworkOnly (vite workbox) — unchanged |
| Diagnostics dump offline plaintext | NO |
| Server total ≠ collected total → Conflict (never Succeeded) | [Review Repair 01](POS-REACT-RMAP-21-REVIEW-REPAIR-01-offline-cash-finality.md) |

## Explicit non-claims

- Cold-start unlock of protected LocalStore: `DEFERRED_SECURITY_GAP`
- Native SecureStorage / Keystore / Keychain parity: NO
- Background Sync API: not used
- Full Playwright offline cash E2E against live APIs: deferred to device verification; processor covered by unit tests, and by a Playwright `setOffline` run against mocked APIs added in [Review Repair 01](POS-REACT-RMAP-21-REVIEW-REPAIR-01-offline-cash-finality.md)

## Tests

`npx vitest run src/offline/outbox-processor.test.ts` — 4 passed  
Full offline suite remaining green under Master Run validation.

## Next

Master report package closeout.
