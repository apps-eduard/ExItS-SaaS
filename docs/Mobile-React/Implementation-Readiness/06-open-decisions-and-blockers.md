# POS-REACT-READINESS-06 — Open Decisions and Blockers

**Package:** POS-REACT-READINESS-05 companion  
**Status:** Open items only. This file does **not** decide them.  
**Evidence base:** `origin/main` `5979a9ce008bb24a3257abd28ae79bc1a5a9b569`

Accepted MOBILE-D-001 … D-059 and D-061 … D-070 remain in force. This list carries **unresolved** items plus evidence-backed blockers from the readiness audit. Do not treat a listed option as a decision.

---

## 1. Carried from approved planning baseline

| ID | Item | Status | Notes |
|---|---|---|---|
| **MOBILE-D-060** | PIN length, weak/sequential PIN rejection, and whether identical PIN values may be used by different enrolled users on the same device | **OPEN** | Current MAUI defaults are evidence only. Requires Product Owner approval. Do not close in implementation packages. |
| R-022 (historical) | Time-based offline entitlement grace | Open in Phase 7 docs | Do not invent grace in React |
| Manual GCash duplicate check | Requirement exists; no unique index / finder in current POS schema | Open until a backend package | Must not claim offline GCash until this exists |
| Auto-lock timeout | MOBILE-D-057: no documented numeric timeout | Open | Policy later; not Sign Out |

---

## 2. External integration checkpoints (this readiness queue)

| ID | Item | Status | Why it blocks |
|---|---|---|---|
| **PWEB20_CSRF_COMPATIBILITY_REVIEW_REQUIRED** | Platform browser mutation antiforgery vs Mobile React Platform calls | **OPEN** | Gate D browser auth must not invent a parallel CSRF design. Do not merge `feat/platform-admin-web-v2`. Revalidate every Platform state-changing browser call. |
| **PLM_PWA_PATTERN_REVIEW_REQUIRED** | After PLM H4, compare SW safety, update lifecycle, connectivity UX, PWA tests, responsive tests | **OPEN** | Do not copy PLM business/auth. Patterns only. |
| **TYPED_CLIENT_GENERATION_CONTRACT_MISSING** | No OpenAPI/Swagger for Platform or POS APIs used by MAUI | **OPEN** (proven) | Interim: hand-typed TS DTOs + contract tests. Do not add OpenAPI in a docs package. |

---

## 3. Storage and native plugins (unselected)

| Item | Status | Constraint |
|---|---|---|
| Offline physical storage engine (PWA) | **OPEN** | IndexedDB / OPFS / WASM-capable options — compare only (MOBILE-D-038) |
| Offline physical storage engine (Capacitor) | **OPEN** | Native SQLite-capable options — compare only |
| Capacitor secure-storage plugin | **OPEN** | Need native secure storage; **do not choose here** |
| Capacitor SQLite/storage plugin | **OPEN** | Same |
| Encryption key storage on PWA | **OPEN** | Must not be ordinary localStorage for keys that protect financial outbox |

Must prove before any selection: encryption, secure key storage, crash recovery, quota, per-user/org/product isolation, migration/versioning, idempotent replay.

---

## 4. Hosting / release (unselected)

| Item | Status | Constraint |
|---|---|---|
| Production host / reverse-proxy route | **OPEN** | Prefer same-origin or BFF. Do not broadly enable CORS. Align with P14 nginx HTTPS. |
| Exact `/api` path map (Platform vs POS) | **OPEN** | Dual bases today (`:8091` / `:8092`) |
| Release signing / Android distribution | **OPEN** | Current MAUI csproj has no keystore; Capacitor signing is a later release package |
| PWA production origin / TLS cert | **OPEN** | Existing reverse-proxy model; not authorized to ship |
| OTA / Capacitor live update | **Not assumed** | Would need explicit security review; default is store/sideload packages |

---

## 5. Product / parity dispositions (not decided)

| Item | Status | Constraint |
|---|---|---|
| First cohort scope (POS-only vs full host) | **OPEN** | Gate I may be POS-selling cohort **if written**. Gate J still needs Personal + Owner disposition |
| Personal Mobile stays on this React host vs Personal Web–primary | **OPEN** | Default planning: keep on Mobile Client (MOBILE-D-005). Split requires PO |
| Owner essentials vs Organization Web–only | **OPEN** | Default: practical subset stays on Mobile |
| Live product-barcode camera | **OPEN / not current** | Optional later; not a current-parity blocker |
| Thermal printer / drawer / NFC / real terminal | **OPEN / not current** | Separate authorization; absence is not Gate E/F blocker |
| Windows native Capacitor | **Not in scope** | Desktop = browser/PWA (MOBILE-D-032) |

---

## 6. Debug / Local Validation

| Item | Status | Constraint |
|---|---|---|
| **DEBUG_LOCAL_VALIDATION_CREDENTIAL_EMBEDDED** | Evidence on current main | Replace later with runtime/developer-secret injection. Never print the value. Never ship in Release. |

---

## 7. What this file must not do

- Close MOBILE-D-060
- Pick IndexedDB vs SQLite
- Pick a Capacitor plugin
- Finalize CSRF independently of PWEB-20
- Enable wildcard CORS
- Authorize implementation, PWA production, Capacitor production, MAUI retirement, or main merge
