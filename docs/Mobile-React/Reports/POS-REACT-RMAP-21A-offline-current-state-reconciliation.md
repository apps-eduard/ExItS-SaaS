# POS-REACT RMAP-21A — Offline current-state reconciliation

**Status:** PASS (docs / matrix lock)  
**Master start SHA:** `86ded4380c6c1d45ef89ef08855c20fb00f17d38`  
**21A.0 diagnostic safety tip:** `bcb55d49fad89eb38b1b62369c0098e48797d23c`  
**Authorization:** `RMAP_21_AUTHORIZED=YES`

## Owner quick-fix baseline

Preserved intentional tip:

`86ded438` — `fix(pos-react): owner quick fix and polish shell UX`

Including shell polish, Sell-readiness E2E expectations, and copyable diagnostics (hardened in 21A.0).

## 21A.0 Diagnostic safety

| Flag | Value |
| --- | --- |
| CLIENT_DIAGNOSTIC_RAW_QUERY_STRING | NO |
| CLIENT_DIAGNOSTIC_RAW_FRAGMENT | NO |
| CLIENT_DIAGNOSTIC_AUTH_SECRET_EXPOSURE | NO |
| CLIENT_DIAGNOSTIC_ARBITRARY_OBJECT_DUMP | NO |
| RMAP21_CLIENT_DIAGNOSTIC_SAFETY | PASS |

## Reconciled sources

- React: SessionProvider (memory grant), ShellConnectionButton (Online/Offline only), SellReadinessGate, cash checkout with client `saleId`, PWA NetworkOnly APIs, diagnostics.
- MAUI/Application: OfflineQueueAbstractions states, OfflineQueueProcessor, cash/catalog LocalStore, Personal/CustomerCredit offline sync, ProtectedShellAccessPolicy warm vs cold.
- Backend: SaleId replay + optional Idempotency-Key headers; discount/override offline fail-closed; Personal ExpectedVersion concurrency.

## Locked scope (Master Run 01)

**In:** warm-session offline; POS cached Sell reads; selective Cash queue; business customers/credit where proven; Personal Utang; Personal To-do; real Connection & Sync from outbox.

**Out:** inventory admin, purchasing, suppliers, reports, branch fulfillment admin, staff admin, billing, B04/B05/TAX, GCash offline, Business Utang checkout offline, discount/override offline, lot/expiry offline (fail closed).

## Auth decision

| Item | Value |
| --- | --- |
| OFFLINE_CAPABILITY_MATRIX | LOCKED |
| UNKNOWN_OFFLINE_ACTIONS_FAIL_CLOSED | YES |
| BROWSER_NATIVE_SECURE_STORAGE_ASSUMED | NO |
| WARM_SESSION_OFFLINE_REQUIRED | YES |
| COLD_START_OFFLINE_UNLOCK | DEFERRED_SECURITY_GAP |
| Native SecureStorage parity claimed | NO |

## Gaps to close in later packages (not 21A blockers)

1. React LocalStore/outbox (21B).
2. Wire sale/customer Idempotency-Key headers where MAUI already does (21D/21E).
3. Upgrade Connection → Connection & Sync only from real outbox (21C).
4. Cold-start unlock remains deferred — shell may load offline; protected data stays locked until online auth.

## Next

Continue **RMAP-21B** IndexedDB LocalStore + encrypted outbox.

Matrix: [react-pwa-offline-capability-matrix.md](../Authoritative/Offline/react-pwa-offline-capability-matrix.md)
