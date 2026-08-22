# POS RMAP-21-FIX01 — Cold-Start IndexedDB Unlock

**Package:** RMAP-21-FIX01  
**Branch:** `feat/pos-react-client`  
**Starting HEAD:** `ecd2113ae967d38687ec22cd588a8e28822d88a4`  
**Status:** COMPLETE (automated evidence; physical PWA not owner-verified)

---

## Executive summary

Closed the accepted RMAP-21 gap **Cold-start IndexedDB unlock** by introducing a **device-bound offline operating grant** stored in `localStorage`, evaluated when the browser/PWA cold-starts while offline. The grant restores session, workspace, POS device context, and the existing scope-derived AES-GCM key material so encrypted IndexedDB (outbox, customer caches) opens without storing passwords, bearer tokens, or Platform session cookies as encryption keys.

**Validation flag:** `COLD_START_INDEXEDDB_UNLOCK=PASS`

---

## Root cause (prior behavior)

| Step | Prior result |
| ---- | ------------ |
| Online sign-in + branch bind + offline prep | Warm session could open scoped IndexedDB |
| Browser/PWA fully closed | In-memory session lost |
| Cold restart while offline | `GET /auth/me` failed → `unauthenticated` → workspace cleared → scope key unknown → encrypted outbox unreadable |

Warm-session hooks (`organization-offline-context`, `OfflineSyncProvider`) could not run without authenticated session.

---

## Target behavior (delivered)

1. **Online bootstrap:** successful org branch bind with authorized POS device → `establishOfflineOperatingGrant()` persists grant (7-day window, HMAC integrity).
2. **Cold restart offline:** `SessionProvider` evaluates grant → `cold_start_offline` status → `WorkspaceProvider` restores bound workspace + device + capability facts (no bearer token).
3. **IndexedDB:** same `organizationScopeKey(userId:orgId:branchId:installationDeviceId)` → `deriveScopeKeyFromBinding` unchanged → outbox decryptable.
4. **Cash sell:** existing lease + outbox paths unchanged; unsupported flows remain blocked.
5. **Reconnect:** online sign-in refreshes grant; outbox sync uses live POS bearer as before.

---

## Key / material hierarchy

| Material | Generated | Stored | Lifetime | Plaintext secret? | Logout | Org switch |
| -------- | --------- | ------ | -------- | ----------------- | ------ | ---------- |
| Installation device UUID | Browser `localStorage` on first use | `localStorage` | Durable until cleared | No (public identifier) | Retained | Same browser profile |
| Offline operating grant | Online branch bind | `localStorage` `exits.pos-client.offline-operating-grants.v1` | 7 days from issue | No auth tokens in document | Retained (MAUI parity) | Per-userId entry; scope includes org/branch/device |
| Grant HMAC integrity key | SHA-256(`exits-offline-grant-integrity:v1:{installationDeviceId}`) | Not stored (derived per verify) | N/A | No | N/A | Device-bound |
| IndexedDB AES-GCM key | SHA-256(`exits-offline-v1:{scopeBinding}`) | Non-exportable CryptoKey in memory only | Session | No | Lost on tab close; re-derived on unlock | Scope binding includes userId+org+branch+installation |
| Price leases | Server-signed | IndexedDB plaintext | Until expiry | Signature only | Survives restart | Branch/org scoped in validation |
| POS bearer / Platform cookie | Server | Memory / sessionStorage patterns | Online session | Yes (not used as DB key) | Cleared on sign-out | Replaced on bind |

**PLAINTEXT_SECRET_PERSISTED=NO** for IndexedDB encryption keys and grant document.

---

## Boundaries

- **Org isolation:** IndexedDB name + scope binding include `organizationId`; grant includes org/branch/device; wrong-org grant cannot decrypt another org's DB without matching scope.
- **Branch isolation:** `branchId` in scope binding and grant.
- **Device binding:** Grant requires matching `installationDeviceId` and authorized `posDeviceId`.
- **Logout:** Clears online session artifacts; grant retained so offline cold-start after logout still works if grant valid (documented MAUI parity). Online-only admin mutations still blocked offline.
- **Account switch:** New online bind writes grant for new userId; cold-start selects most recently validated grant for installation device; prior user's encrypted DB remains but is not selected unless that grant wins.
- **Browser profile copy:** Copying IndexedDB without matching installation id + grant integrity fails closed.

**Not implemented in FIX01:** offline PIN (MAUI has PIN). React uses grant-only unlock for browser PWA.

---

## Storage loss

| Cleared | Effect |
| ------- | ------ |
| IndexedDB only | Queued sales / leases lost; grant may remain but store empty → reconnect + refresh |
| Grant `localStorage` | Cold-start locked; encrypted DB orphaned until online re-bind |
| Installation device id | New installation id → device mismatch → cannot unlock prior DB |
| All site data | Full re-registration / online bootstrap required; truthful sign-in locked UX |

No silent unlimited device re-registration.

---

## Error UX

Sign-in page shows **Offline data is locked** with `offline.coldStartLocked` / `offline.coldStartReconnect` when offline bootstrap cannot unlock. Copy Error Details continues to redact secrets (`client-error-report.test.ts`, `pos-error-report.test.ts`).

---

## Test evidence

| Requirement | Test location |
| ----------- | ------------- |
| Cold-start DB unlock | `cold-start-indexeddb-unlock.test.ts`, `offline-operating-grant.test.ts` |
| Tampered / expired grant | `offline-operating-grant.test.ts` |
| Device isolation | `offline-operating-grant.test.ts` |
| No auth token in grant | `offline-operating-grant.test.ts` |
| No bearer in cold-start facts | `offline-operating-grant.test.ts` |
| Outbox / immutable totals / replay | `outbox.test.ts`, `cash-sale-offline.test.ts`, `outbox-processor.test.ts` |
| Lease enforcement | `price-authority-cache.test.ts`, `cash-sale-offline.test.ts` |
| PWA / SW | `pwa.test.tsx`, `dev-service-worker-guard.test.ts` |

---

## Files changed

- `src/offline/offline-operating-grant.ts` (+ tests, cold-start IndexedDB test)
- `src/session/SessionProvider.tsx`, `SessionGuards.tsx`
- `src/workspace/WorkspaceProvider.tsx`, `WorkspaceBootNavigator.tsx`
- `src/app/RootLayout.tsx`, `features/shell/OrgBottomNav.tsx`
- `src/features/auth/SignInPage.tsx`
- `src/offline/OutboxSyncHost.tsx`
- i18n locales (en, fil, ceb, hil, ilo)
- `docs/Mobile-React/Authoritative/Offline/react-pwa-offline-capability-matrix.md`
- `docs/Mobile-React/Reports/POS-REACT-RMAP-24-final-validation.md`

---

## Exclusions (unchanged)

- GCash, Utang checkout, discounts, overrides offline
- Device register/revoke offline
- Offline PIN (future parity)
- Physical device / live camera verification (`DEVICE_VERIFIED=NO`)
- COM-INT-04, RMAP-TAX, RMAP-B05, MAUI retirement, main merge, production cutover

---

## Physical device

`DEVICE_VERIFIED=NO` — owner did not perform physical PWA offline cash validation during this package.
