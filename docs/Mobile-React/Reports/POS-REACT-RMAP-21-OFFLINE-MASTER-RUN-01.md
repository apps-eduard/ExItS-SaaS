# POS-REACT RMAP-21 OFFLINE MASTER RUN 01

**Status:** COMPLETE — awaiting Product Owner / ChatGPT review  
**HARD STOP:** after 21H (this package)

## Authorization

| Flag | Value |
| --- | --- |
| RMAP_21_AUTHORIZED | YES |
| RMAP_22_PERSONAL_MASTER_RUN_01 | APPROVED |
| RMAP_22_REVIEW_REPAIR_01 | APPROVED |
| POS_OPERATIONS_UX_REPAIR_01 | APPROVED |
| OWNER_QUICK_FIX_POLISH | ACCEPTED_BASELINE |
| RMAP_23_AUTHORIZED | NO |
| RMAP_B04_AUTHORIZED | NO |
| RMAP_B05_AUTHORIZED | NO |
| RMAP_TAX_AUTHORIZED | NO |
| RMAP_24_AUTHORIZED | NO |
| PRODUCTION_CUTOVER | NO |

## Git

| Item | SHA |
| --- | --- |
| Starting SHA | `86ded4380c6c1d45ef89ef08855c20fb00f17d38` |
| Implementation ending SHA (21H) | `30312ec4fe0d7318ef8e693f86c358af01f71662` |
| Docs tip (master report + roadmap) | `12f46dbf5e248c16beb01186cd6ad1871c3fc410` |
| Branch | `feat/pos-react-client` |
| Worktree | `C:\Users\speed\Desktop\ExItS-SaaS-pos-react-client` |

### Package commits (in order)

| Package | Commit | Message |
| --- | --- | --- |
| 21A.0 | `bcb55d49` | fix(pos-react): sanitize client diagnostic reports |
| 21A | `37ca0030` / `a042ffc1` | docs reconcile + roadmap in progress |
| 21B | `952245cd` | feat(pos-react): add browser local store and outbox |
| 21C | `4028a802` | feat(pos-react): wire real Connection and Sync UI |
| 21D | `e07a8270` | feat(pos-react): enable offline cash sell with catalog cache |
| 21E | `f69fe819` | feat(pos-react): add offline business customers cache |
| 21F | `518efeb6` | feat(pos-react): add personal utang offline local store |
| 21G | `1400bf08` (+ docs `bf8d0de7`) | feat(pos-react): add personal todo offline support |
| 21H | `30312ec4` | feat(pos-react): reconnect recovery and offline sync processor |
| Master report | `5ef9109a` | docs(pos-react): record RMAP-21 offline master run 01 |

## Delivered capability

1. **21A.0** — Diagnostic URL = origin+pathname; secret redaction; no arbitrary object dump  
2. **21A** — Capability matrix locked; warm-session required; cold-start unlock deferred  
3. **21B** — IndexedDB LocalStore + AES-GCM encrypted outbox; Personal vs Org isolation  
4. **21C** — Connection & Sync from real outbox counts  
5. **21D** — Cached Sell catalog; Cash enqueue; DEVICE→SHIFT→SELL preserved; sale Idempotency-Key  
6. **21E** — Business customer cache + queueable create/update/repay where proven  
7. **21F** — Personal Utang LocalStore + contact/relationship/entry queue (no silent linking)  
8. **21G** — Personal To-do private cache + create/update/complete/reopen/cancel queue  
9. **21H** — Outbox processor, reconnect drain, abandoned Syncing recovery  

## Explicit exclusions

- Offline GCash / ManualGCash  
- Offline Business Utang checkout  
- Offline commercial discount / price override  
- Offline lot/expiry allocation (fail closed)  
- Offline inventory, purchasing, suppliers, reports, branch fulfillment admin, staff admin, billing  
- Cold-start protected-data unlock without proven safe architecture (`DEFERRED_SECURITY_GAP`)  
- Native SecureStorage / Keystore / Keychain parity  
- Workbox caching of API/auth/session (remains NetworkOnly)  
- RMAP-23 / B04 / B05 / TAX / production cutover  

## SECURITY

| Check | Result |
| --- | --- |
| Client diagnostic raw query strings | NO |
| Client diagnostic URL fragments | NO |
| Client diagnostic auth-secret exposure | NO |
| Client diagnostic arbitrary object dump | NO |
| Offline plaintext included in AI report | NO |
| RMAP21_CLIENT_DIAGNOSTIC_SAFETY | PASS |
| Auth tokens / password / refresh / antiforgery in IndexedDB | NO |
| Personal vs Organization LocalStore isolation | YES |
| Staff may open Personal LocalStore | NO |
| DEVICE → SHIFT → SELL preserved | YES |
| OFFLINE_SELL_CASH | YES |
| OFFLINE_GCASH | NO |
| OFFLINE_BUSINESS_UTANG_CHECKOUT | NO |
| OFFLINE_DISCOUNT | NO |
| OFFLINE_PRICE_OVERRIDE | NO |
| LOT_EXPIRY_OFFLINE | FAIL_CLOSED |
| COLD_START_OFFLINE_UNLOCK | DEFERRED_SECURITY_GAP |
| API Workbox caching | NetworkOnly |
| Ambiguous Personal create auto-retry | NO (parked for human) |

## Build / test evidence

- Client `npm run typecheck` — PASS (at 21H close)  
- `npx vitest run src/offline` — 86 passed (12 files) at 21H  
- Diagnostics suite — PASS (21A.0)  
- i18n message parity — PASS when locales updated  

## Portfolio independence

- Work performed only in `ExItS-SaaS-pos-react-client` worktree on `feat/pos-react-client`  
- No nested foreign product tree imported  
- Platform / Product DB ownership boundaries unchanged  
- No production cutover  

## Package reports

- [21A](./POS-REACT-RMAP-21A-offline-current-state-reconciliation.md)  
- [21B](./POS-REACT-RMAP-21B-browser-localstore-outbox.md)  
- [21C](./POS-REACT-RMAP-21C-connection-sync-ui.md)  
- [21D](./POS-REACT-RMAP-21D-pos-offline-sell-cash.md)  
- [21E](./POS-REACT-RMAP-21E-business-customers-offline.md)  
- [21F](./POS-REACT-RMAP-21F-personal-utang-offline.md)  
- [21G](./POS-REACT-RMAP-21G-personal-todo-offline.md)  
- [21H](./POS-REACT-RMAP-21H-reconnect-recovery-sync.md)  
- Matrix: [react-pwa-offline-capability-matrix.md](../Authoritative/Offline/react-pwa-offline-capability-matrix.md)

## Risks / open decisions

1. Cold-start unlock remains deferred — warm browser session required for protected offline data.  
2. Personal create ops without server idempotency park on ambiguous transport (intentional).  
3. Full device Playwright offline cash + reconnect E2E against live APIs not claimed as Device Verified.  
4. Cached catalog stock can drift until reconnect.  

## Exact next work package

**HARD STOP.** Do **not** start RMAP-23 / B04 / B05 / TAX / production cutover until separately authorized.
