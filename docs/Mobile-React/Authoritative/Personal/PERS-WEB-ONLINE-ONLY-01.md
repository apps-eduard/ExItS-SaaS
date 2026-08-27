# PERS-WEB-ONLINE-ONLY-01

**Package:** PERS-WEB-ONLINE-ONLY-01  
**Status:** COMPLETE  
**Branch:** `feat/personal`  
**Product Owner decision:** Personal React Web/PWA runtime is **ONLINE-ONLY** (same high-level channel policy as Organization Web/PWA).

## Baseline

| Label | SHA |
| --- | --- |
| Required baseline | `774e09016c449999462280af4a8b22a4719887a1` |
| Implementation | `c500193ef150a64cc13d191896c2735b76a3ea93` |
| Tip / remote | `e95519189be5576ad4b0c9fc2a8ed1075399f546` |

## Policy

Runtime source of truth:

- `src/runtime/personal-web-runtime-policy.ts` → `personalWebRuntimePolicy`
- Legacy pending rows: `PERSONAL_WEB_LEGACY_PENDING_OUTBOX_POLICY = "preserve-and-drain-when-online"`

| Flag | Web value |
| --- | --- |
| `requiresOnlineSession` | `true` |
| `offlineSession` | `false` |
| `offlineBusinessReads` | `false` |
| `offlineBusinessMutations` | `false` |
| `offlineQueueing` | `false` |
| `offlineBackgroundSync` | `false` |

### Session

- Cold start offline → branded **Online Required** + Retry (no cached Personal session, no offline PIN unlock).
- Warm authenticated + transient disconnect → keep already-painted safe UI; disable writes; Connectivity notice; reconnect refreshes authoritatively.

### Offline PIN

- Personal Web no longer forces `/offline-pin-setup` or unlocks via `/offline-pin`.
- PIN/DEK implementation **preserved** for future Capacitor/native (`allowPersonalOfflineEngine` / `allowOfflineEngine` opt-in).

### Todo / Utang / People / Commerce

| Surface | Web behavior |
| --- | --- |
| Todo create/update/complete/reopen/cancel | Online server mutations only; no new Web outbox enqueue |
| Utang relationship / Loan / Payment | Online + **PERS-IDEM-01** sticky ids; no new Web outbox enqueue |
| People / contacts | **ONLINE_ONLY** (intentional; offline People UI not implemented) |
| Stores / cart / checkout / orders | Online authoritative (unchanged) |
| Context switch Personal ↔ Organization | Online required (unchanged) |

### Legacy outbox

- **New** Personal Web operations must not enqueue.
- **Existing** pending encrypted Personal outbox rows from earlier builds are **preserved** and **drainable** while online (`usePersonalOfflineContext` still opens LocalStore when online).

### Preserved offline engine

Encrypted LocalStore, AES-GCM outbox, DEK/PIN modules, Todo/Utang offline engines, stable entity ids, and outbox processor remain in the codebase. They are not activated for new Personal Web operations.

### PWA

Installability, manifest, icons, standalone display, and static shell caching remain. Business/API routes stay `NetworkOnly` (not an offline source of truth).

### Auth

**PERS-AUTH-01** preserved: activate / reset remain NetworkOnly; tokens memory-only + URL scrubbed.

## Tests

- `personal-web-runtime-policy.test.ts`
- `personal-web-cold-start.test.ts`
- `personal-web-online-only.test.ts`
- Engine unit tests pass with `{ allowOfflineEngine: true }`
- E2E: RMAP-22H (no Personal PIN enroll), Personal offline enqueue blocked, Org online-only / PIN cold-start → Online Required

## Explicit non-goals

Settlement wizard, ownership-transfer UI, RMAP-22H seller continuation repair, diagnostics, durable cart, Todo share, external-camera QR, Install ExItS onboarding, BNPL/PLM/PSP.
