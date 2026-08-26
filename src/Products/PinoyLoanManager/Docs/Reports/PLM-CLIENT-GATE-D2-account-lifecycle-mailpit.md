# PLM-CLIENT-GATE-D2 — Account lifecycle + Mailpit

**Package:** PLM-CLIENT-GATE-D2  
**Date:** 2026-08-19  
**Branch:** `feat/plm-react-client`  
**Starting SHA:** `7732b67f15b45950db18b7482314491e75dd3a3a`

Completes the public Personal identity lifecycle for the Pinoy Loan Manager React/PWA: Sign Up, Activate, Forgot Password, Reset Password, and Local Validation Mailpit links that return to the PLM origin. Reuses Platform Identity. Does not grant PLM organization or product access.

---

## Status

| Item | Status |
|---|---|
| PLM-CLIENT-GATE A | **APPROVED** |
| PLM-CLIENT-GATE B | **APPROVED** |
| PLM-CLIENT-GATE C | **APPROVED** |
| PLM-CLIENT-GATE D0 | **APPROVED** |
| PLM-CLIENT-GATE D1 | **TECHNICALLY APPROVED** (visual still **AWAITING PRODUCT OWNER + CHATGPT**) |
| PLM-CLIENT-GATE D2 | **COMPLETE** after validation |
| Identity lifecycle | **IMPLEMENTED** |
| PLM organization/product access | **NOT STARTED** |
| R-091 | **OPEN** |
| Capacitor | **NOT STARTED** |
| PinoyBusinessPOS | **UNCHANGED** |
| PLM loan .NET | **UNCHANGED** |
| DB/migrations | **NONE** |

D1 visual: do not self-approve.

---

## Delivered

- Routes `/sign-up`, `/activate-account`, `/forgot-password`, `/reset-password` on the D1 auth visual layout; `/sign-in` links to Forgot password and Create account
- `POST /platform-api/api/v1/platform/auth/register|activate-account|forgot-password|reset-password`
- Privacy-safe Sign Up acknowledgement: "Check your email to continue."
- Generic Forgot Password acknowledgement (no account enumeration)
- Server-selected public surface `pinoy-loan-manager` only (no arbitrary callback URLs)
- `PlatformEmail:PinoyLoanManagerPublicBaseUrl` (Local Validation `http://localhost:4176`)
- EmailVerification → `{PLM}/activate-account?token=`; PasswordReset → `{PLM}/reset-password?token=`
- Admin default, invitation, and recovery-email links unchanged
- Token captured for the form only; scrubbed from the URL; not stored; `Referrer-Policy: no-referrer`; auth APIs remain NetworkOnly; no Background Sync
- Screenshots: `Docs/Reports/impl-gate-d2-account-lifecycle/`

Visual approval: **AWAITING PRODUCT OWNER + CHATGPT**

---

## Explicit non-goals

- Organization chooser / product grants / PLM roles
- Loan features
- Capacitor
- Auto-login after register / activate / reset

---

## Exact next package

**STOPPED AFTER PLM-CLIENT-GATE-D2.** Do not start remaining Gate D org/product access, PLM-02, Capacitor, or loan features. Do not merge `main`.
