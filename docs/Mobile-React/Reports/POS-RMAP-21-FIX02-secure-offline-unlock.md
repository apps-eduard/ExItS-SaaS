# POS RMAP-21-FIX02 — Secure Offline Unlock Hardening

**Package:** RMAP-21-FIX02  
**Branch:** `feat/pos-react-client`  
**Starting HEAD:** `f0358af6d11af73215d1240e517230cc78cf3668`  
**Final HEAD:** _(recorded at commit)_  
**Status:** COMPLETE (automated evidence; physical PWA not owner-verified)

---

## Executive summary

FIX01 restored **functional** cold-start IndexedDB unlock but Product Owner security review rejected closure: the browser could recompute grant integrity from public `installationDeviceId`, derive AES keys from public scope identifiers, and silently restore offline access after logout without user verification.

FIX02 replaces client-minted trust with:

1. **Server-signed offline operating grant** (ECDSA P-256 / SHA-256, schema v4) issued only after authenticated online session + authorized POS device.
2. **Random 256-bit LocalStore DEK** wrapped with a PIN-derived PBKDF2-SHA256 key (MAUI-aligned policy: 6+ digit PIN, 100k iterations, lockout).
3. **Offline PIN unlock** required on cold start and after explicit logout; no silent offline resurrection.
4. **Safe FIX01 migration** on next online authenticated session.

**Validation flags**

| Flag | Result |
| ---- | ------ |
| `FUNCTIONAL_COLD_START` | PASS |
| `SECURE_OFFLINE_UNLOCK` | PASS |

---

## Audit (pre-implementation)

| Question | Answer |
| -------- | ------ |
| `MAUI_OFFLINE_GRANT_MODEL` | Client-established after online session; not server-signed; HMAC integrity from device-local material |
| `MAUI_OFFLINE_PIN_MODEL` | PBKDF2-SHA256, 6+ digits, 100k iterations, lockout; PIN unlock does not extend grant |
| `SERVER_SIGNED_OFFLINE_GRANT_ALREADY_EXISTS` | NO — added in FIX02 |
| `EXISTING_SIGNING_PRIMITIVE_REUSABLE` | YES — pattern from offline price authority signing; grant uses ECDSA P-256 for client-side verification |

---

## Cryptographic grant architecture

**Issuance (online only)**

`POST /api/v1/pos/offline-operating-grants` after:

- organization + branch scope headers
- authenticated actor
- commercial `ViewCatalog` entitlement
- Platform POS device authorization (or Testing bypass)

**Signing**

- Algorithm: ECDSA P-256, SHA-256 over canonical pipe-delimited payload (`OfflineOperatingGrantSigning.CanonicalVersion = v1`)
- Private key: server-only (`PosOffline:OperatingGrantSigningPrivateKeyPem`)
- Development key embedded in `OfflinePriceAuthorityOptions.DevelopmentOperatingGrantPrivateKeyPem`
- React verifies with embedded **public key only** — no signing secret in JS bundle

**Bound fields (immutable without valid signature)**

`grantId`, `schemaVersion`, `userId`, `organizationId`, `branchId`, `installationDeviceId`, `posDeviceId`, `roleCode`, `issuedAtUtc`, `lastOnlineValidatedAtUtc`, `expiresAtUtc`

**Client cannot**

- mint grants
- extend expiry
- alter role/org/branch/device and re-sign (adversarial tests fail verification)

**Dev verification public key (React)**

```
-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEkld6WGOTRLooj2ArP2UV2S+nTVtA
yfFYSN1+JNozH4BKAVf5/c1MwCGTLCel38wB0fnM9/1cYKEGKrh9xldC7Q==
-----END PUBLIC KEY-----
```

---

## Offline PIN / LocalStore key architecture

| Material | Generated | Stored | Plaintext? |
| -------- | --------- | ------ | ---------- |
| LocalStore DEK | `crypto.getRandomValues(32)` | Wrapped blob in `localStorage` only | NO |
| PIN | User entry at enroll | Never stored | NO |
| PIN verifier | PBKDF2-SHA256 hash + salt | `localStorage` per userId | Hash only |
| Server grant | POS API | `localStorage` per userId | Signature only |
| Unwrapped DEK | After successful PIN | Memory only (`offline-unlock-session`) | Cleared on logout |

Legacy FIX01 scope-derived AES (`SHA-256(exits-offline-v1:…public ids…)`) used only during online migration to re-encrypt under the random DEK.

---

## Logout behavior

After explicit logout:

- online bearer/session cleared
- in-memory DEK cleared
- offline workspace locked

Durable encrypted grant + wrapped DEK envelope may remain, but next cold start shows **Offline Unlock** and requires correct PIN. No PIN configured → reconnect/sign-in required.

---

## Migration (FIX01 → FIX02)

On next online authenticated org branch bind:

1. Request server-signed grant from POS API
2. Enroll offline PIN (if not configured)
3. Generate random DEK; wrap with PIN-derived key
4. Decrypt legacy FIX01 IndexedDB records using trusted online session scope binding
5. Re-encrypt under new DEK; mark migration complete

Queued Cash sales, price leases, and immutable totals preserved (`offline-pin-security.test.ts`, `cold-start-indexeddb-unlock.test.ts`, `outbox.test.ts`).

---

## Threat model and PWA limitations

**Mitigated**

- Grant tamper (role/org/branch/device/expiry) → signature verification fails
- Profile copy without PIN → encrypted IndexedDB unreadable
- Public identifiers alone → insufficient for DEK or grant signing
- Logout → no silent offline restore

**Honest limits**

- Offline mode cannot learn server-side grant revocation until reconnect
- Bounded grant expiry + online refresh required
- Browser/PWA has no hardware-backed keystore parity with native SecureStorage
- `installationDeviceId` is durable browser identity, not a secret — used for binding only, not authorization proof
- `DEVICE_VERIFIED=NO` — no physical PWA offline cash validation in this package

---

## Security UX (React design system)

- Prepare offline access / set-confirm offline PIN (`OfflinePinEnrollPage`, `OfflinePinSetupGate`)
- Offline unlock (`OfflinePinUnlockPage`)
- Wrong PIN, expired authorization, reconnect required messaging
- Localized: en, fil-PH, ceb-PH, hil-PH, ilo-PH

---

## Test evidence

| Area | Location |
| ---- | -------- |
| Server grant verify / tamper | `server-signed-offline-grant.test.ts`, `PosOfflineOperatingGrantApiTests` |
| PIN / DEK / logout / migration | `offline-pin-security.test.ts`, `offline-operating-grant.test.ts` |
| Outbox / leases / replay | `outbox.test.ts`, `cash-sale-offline.test.ts`, `price-authority-cache.test.ts` |
| Copy Error Details redaction | `client-error-report.test.ts`, `pos-error-report.test.ts` |
| Browser E2E | `e2e/offline-pin-security.spec.ts` |

---

## Backend files

- `Application/Offline/ServerSignedOfflineOperatingGrant.cs`
- `Application/Offline/ServerSignedOfflineOperatingGrantService.cs`
- `Api/Offline/OfflineOperatingGrantEndpoints.cs`
- `Application/Offline/OfflinePriceAuthority.cs` (options)
- `Application/Common/ApplicationErrorCodes.cs`
- `Api/Common/PosDevelopmentEnvironment.cs`
- `Api/Program.cs`
- `tests/.../PosOfflineOperatingGrantApiTests.cs`

## React files

- `src/offline/server-signed-offline-grant.ts` (+ tests)
- `src/offline/offline-pin.ts`, `local-store-key.ts`, `offline-unlock-session.ts`, `local-store-migration.ts`
- `src/api/pos/pos-offline-operating-grant-client.ts`
- `src/features/offline/*`
- `src/session/SessionProvider.tsx`, `SessionGuards.tsx`, `WorkspaceProvider.tsx`
- `src/offline/offline-operating-grant.ts`, `crypto.ts`, outbox/cache modules
- i18n locales + `e2e/offline-pin-security.spec.ts`

---

## Exclusions (unchanged)

- GCash, Utang checkout, discounts, overrides offline
- Device register/revoke offline
- COM-INT-04, RMAP-TAX, RMAP-B05, MAUI retirement, main merge, production cutover
- Physical device / live camera verification

---

## Mandatory flags

```
FUNCTIONAL_COLD_START=PASS
SECURE_OFFLINE_UNLOCK=PASS
CLIENT_MINTABLE_GRANT=NO
SERVER_SIGNED_GRANT=YES
SERVER_CONTROLS_ROLE=YES
SERVER_CONTROLS_EXPIRY=YES
GRANT_TAMPER_REJECTED=YES
INSTALLATION_ID_IS_SECRET=NO
INSTALLATION_ID_ALONE_CAN_SIGN=NO
RANDOM_LOCALSTORE_DEK=YES
DEK_PERSISTED_PLAINTEXT=NO
PUBLIC_IDS_ALONE_CAN_DERIVE_DEK=NO
OFFLINE_PIN_IMPLEMENTED=YES
OFFLINE_PIN_PLAINTEXT_STORED=NO
WRONG_PIN_REJECTED=YES
CORRECT_PIN_UNLOCK=YES
LOGOUT_AUTO_OFFLINE_RESTORE=NO
LOGOUT_REQUIRES_OFFLINE_UNLOCK=YES
FIX01_DATA_MIGRATION=YES
PENDING_SALE_PRESERVED=YES
RECONNECT_SYNC_EXACTLY_ONCE=YES
ORG_ISOLATION=YES
BRANCH_ISOLATION=YES
DEVICE_BINDING=YES
ACCOUNT_SWITCH_ISOLATION=YES
EXPIRED_GRANT_REJECTED=YES
PRICE_LEASE_ENFORCEMENT=YES
COPY_ERROR_DETAILS_SECRET_REDACTION=YES
DEVICE_VERIFIED=NO
LIVE_CAMERA_DEVICE_VERIFIED=NO
PHYSICAL_PWA_OFFLINE_CASH=NOT_TESTED
COM_INT_04_AUTHORIZED=NO
RMAP_TAX_AUTHORIZED=NO
RMAP_B05_AUTHORIZED=NO
MAUI_RETIREMENT_AUTHORIZED=NO
MERGE_TO_MAIN=NO
PRODUCTION_CUTOVER=NO
```
