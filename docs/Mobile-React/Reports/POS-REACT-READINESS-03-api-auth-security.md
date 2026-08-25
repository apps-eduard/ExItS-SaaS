# POS-REACT-READINESS-03 — API and Auth Security Report

**Package:** POS-REACT-READINESS-03  
**Branch:** `docs/pos-react-implementation-readiness`  
**Worktree:** `C:/Users/speed/Desktop/ExItS-SaaS-pos-react-docs`  
**Base `origin/main`:** `5979a9ce008bb24a3257abd28ae79bc1a5a9b569`  
**Depends on:** POS-REACT-READINESS-02 `6943418bf710647d2f3b5f7089ae91d39f96a64f`  
**Status:** Documentation complete for this package. Implementation is **NOT AUTHORIZED**.

Canonical deliverable: [03-api-auth-security-readiness.md](../Implementation-Readiness/03-api-auth-security-readiness.md)

---

## 1. Verdict

Existing Platform and POS HTTP contracts can support a future React Browser/PWA and later Capacitor client **without inventing new authority**, if:

- typed TS clients mirror the current `ApiClient` contracts and have contract tests
- browser session stays cookie/same-origin (or equivalent) and **not** Bearer-in-localStorage
- browser Platform mutations wait for PWEB-20 CSRF review
- CORS remains deny-by-default; access is via reverse-proxy/BFF, not a wide allowlist

---

## 2. Contract source of truth

| Item | Evidence |
|---|---|
| Typed client | `ExItS.PinoyBusinessPOS.ApiClient` (MAUI + Organization Web) |
| Dual bases | `PosApi` (Platform) + `PosBusinessApi` (POS) |
| OpenAPI | **Absent** → `TYPED_CLIENT_GENERATION_CONTRACT_MISSING` |
| Idempotency | `Idempotency-Key` + payload hash + operation headers |
| Errors | problem+json |
| Client-generated SaleId | Yes, for checkout / offline cash replay |

This package did **not** add OpenAPI.

---

## 3. Auth targets

| Delivery | Target |
|---|---|
| Browser/PWA | Browser-safe session; no reusable token in ordinary storage |
| Capacitor | Native secure storage + Bearer/session + server introspect |
| Plugin choice | **Not made** |

Checkpoint: **PWEB20_CSRF_COMPATIBILITY_REVIEW_REQUIRED** (do not merge `feat/platform-admin-web-v2`; do not modify Platform).

---

## 4. CORS

POS and Platform default to **deny all browser origins** when `Cors:AllowedOrigins` is empty. Broad CORS is not the browser strategy. Preferred: same-origin reverse proxy or BFF, matching P14/ADR-022.

---

## 5. Authorization lock

| Item | Status |
|---|---|
| React implementation | **NO** |
| Platform / POS API change | **NO** |
| CSRF design finalized | **NO** |
| OpenAPI added | **NO** |

---

## 6. Next package

POS-REACT-READINESS-04: PWA + offline + device migration sequence.
