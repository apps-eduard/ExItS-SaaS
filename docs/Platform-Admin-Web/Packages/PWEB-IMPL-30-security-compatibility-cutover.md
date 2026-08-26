# PWEB-IMPL-30 — Security / Compatibility / Cutover Blocker Closure

**Package ID:** PWEB-IMPL-30  
**Title:** Security / Compatibility / Cutover Blocker Closure  
**Starting dependency:** PWEB-IMPL-21…29 as implemented (final queue package)  
**Contract classification:** **UNRESOLVED** until proven  
**Implementation:** NOT STARTED (planning only)  
**Nature:** Security / compatibility / cutover validation — **not** ordinary feature expansion

## 1. Objective

Validate and close (only when proven) the remaining security and cross-client compatibility blockers for Platform Admin Web cookie mutations and cutover readiness. Do **not** automatically retire Blazor. Do **not** declare Production Ready or Cutover Authorized unless every explicit blocker is closed with evidence.

## 2. Current repository evidence

| Item | State |
|---|---|
| Browser CSRF foundation | PROVEN_EXISTING (PWEB-20) |
| React antiforgery client | PROVEN_EXISTING (`platform-http`, in-memory token) |
| Blazor session-header exemption | PROVEN_EXISTING |
| POS Platform HttpClient cookie isolation | PROVEN_EXISTING (compat gate `06e5cc1c`) |
| POS React app in this tree | ABSENT / parallel work elsewhere |
| PLM PWA in this tree | ABSENT (docs-only); parallel work elsewhere |
| Social `sessionToken=` on return URL | **OPEN** `BLOCKS_CUTOVER` (`ExternalAuthEndpoints`) |
| React social login buttons | DEFERRED / omitted |

## 3. Must validate and close only when proven

1. **External/social-auth return-URL session token blocker** — replace URL credential transport with a safe pattern; prove no session token in URLs/logs  
2. **CSRF compatibility** with then-current:  
   - Platform React Admin  
   - Blazor Admin  
   - POS React (flag below)  
   - PLM PWA (flag below)  
3. Re-login / session expiry behavior under mutations  
4. CORS remains non-broadened (no wildcard + credentials)  
5. Antiforgery token **not** persisted in URL, localStorage, sessionStorage, IndexedDB, service-worker cache, or logs  
6. Full security regression for auth/session/CORS/antiforgery/logout/protected mutations  

## 4. Compatibility flags (authoritative until closed here)

| Flag | Required value entering PWEB-30 |
|---|---|
| `PLM_PWA_CSRF_COMPAT_REVIEW_REQUIRED` | **YES** |
| `POS_REACT_CSRF_COMPAT_REVIEW_REQUIRED` | **YES** |

PWEB-30 may set each to `NO_CHANGE_REQUIRED` / `FIX_REQUIRED_AND_COMPLETED` / `BLOCKED_WITH_REASON` **only with evidence** against then-current clients. Do not modify POS/PLM in packages 21–29; changes here only if a proven compat fix is required and authorized.

## 5. Authorization / UI scope

No new business mutation UI. Security plumbing and validation only. Social-auth UX only if fixing the URL blocker requires a minimal safe callback change.

## 6. Explicit exclusions

- Blazor retirement  
- Declaring Production Ready / Cutover Authorized while blockers open  
- Broad CORS relaxation  
- Weakening HttpOnly / Secure / SameSite  
- Feature expansion disguised as “security”

## 7. Change allowances

| Area | Allowance |
|---|---|
| Platform API | Narrow security fixes for proven blockers |
| React Admin | Narrow CSRF/social callback fixes |
| Blazor | Minimal compatibility only |
| POS / PLM | Only if proven CSRF compat fix authorized |
| DB | NONE expected |

## 8. Tests required

- Full antiforgery matrix (missing/invalid/valid token; no session; GET unaffected; CORS; logout)  
- Social return URL no longer leaks session token (when fixed)  
- Blazor header path still works  
- POS React / PLM PWA evidence recorded  
- No token persistence scanners/tests  

## 9. Evidence / report path

`docs/Platform-Admin-Web/Reports/PWEB-IMPL-30-security-compatibility-cutover.md`

## 10. Proposed commit message

`fix(platform-web): close security cutover blockers`  
(or split commits only if Product Owner authorizes multiple security fixes)

## 11. Stop conditions

`PWEB30_CUTOVER_BLOCKERS_OPEN` — any claim of cutover/production readiness while:

- social URL blocker open, or  
- POS/PLM CSRF flags still YES without evidence, or  
- CSRF regression, or  
- Blazor broken without justified compat fix  

## 12. Definition of PASS

All targeted blockers proven closed **or** explicitly remain open with cutover still **NO**. Production Ready remains **NO** unless Product Owner separately authorizes after this package’s evidence. Blazor remains active unless a later authorized retirement package exists (not this one).
