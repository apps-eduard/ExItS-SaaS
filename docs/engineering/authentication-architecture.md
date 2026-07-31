# Authentication Architecture

[Home](../index.md) | [Threat model](authentication-threat-model.md) | [Security](security.md) | [Authorization](authorization-matrix.md) | [Product Foundation](../Product-Foundation/exits-product-foundation-reference.md) | [Phase 13](../phases/phase-13-production-authentication-and-identity.md) | [P13-WP01 report](../reports/P13-WP01-authentication-architecture-and-threat-model.md)

**Status:** Authoritative architecture direction (**P13-WP01**). Credentials (**P13-WP02**), browser login/session (**P13-WP03**), password/verification lifecycle (**P13-WP04**), trusted organization context (**P13-WP05**), product-client bearer integration (**P13-WP06**), MFA readiness / auth hardening (**P13-WP07**), and Google/Facebook external login (**P13-WP08**) delivered. Phase closeout remains **P13-WP09**. **R-091** remains open until closeout evidences production readiness.

**Scope of this document:** identity model, trust boundaries, session/token model, Dev vs Production behavior, MFA readiness (non-enforcing), and decisions.

---

## 1. Purpose

Define production authentication and identity for ExItS **before** writing authentication code, so later work packages do not:

- confuse Platform User with organization membership or product roles
- treat Product Access as operational permission
- promote Dev/Testing headers into production authentication
- invent SSO/AD/MFA as if already decided
- store credentials in product databases
- claim Production readiness without evidence

---

## 2. Locked access chain

```text
Platform User
  → Organization Membership
  → Product Access / Entitlement
  → Product-Local Role and Grants
```

| Layer | Owner DB | Grants | Does **not** grant |
|---|---|---|---|
| **Platform User** | Platform | Global human identity (who is authenticated) | Org membership, product launch, or operational permission |
| **Organization Membership** | Platform | Belonging to an org; org-level Platform roles | Product operational roles (Cashier, Loan Officer, …) |
| **Product Access / Entitlement** | Platform (+ product local projection) | Commercial eligibility to **launch/use** a product for an org | Product-local operational permission |
| **Product-Local Role / Grants** | Product | Operational authority inside that product | Platform Admin authority or another product’s ops |

**Invariant:** One human has **one** Platform User. Memberships link that user to many organizations. Do not duplicate identity per organization.

**Invariant:** Platform product access ≠ product operational permission (ADR-011; Product Foundation).

---

## 3. Current state (evidence — not production auth)

| Surface | Today | Production behavior |
|---|---|---|
| Platform User profile | Exists (`PlatformUser`) — username/email/status | Profile remains SoR for identity attributes |
| Credentials | **Implemented (P13-WP02):** `platform_user_credentials` + ASP.NET Core `PasswordHasher<TUser>`, lockout, email-verified flag | Ready |
| Browser session | **Implemented (P13-WP03):** `platform_auth_sessions` + login/logout/me + Admin cookie | Ready |
| Access tokens | **Implemented (P13-WP06):** `platform_access_tokens` + issue/bind/introspect/revoke | Ready |
| MFA | **Readiness only (P13-WP07):** contracts/options/DTO signals; no enrollment/challenge; Production forbids enabling enforcement | Enforcement deferred |
| External login | **Implemented (P13-WP08):** Google/Facebook create/link Platform User + browser session; no auto privilege grants | Secrets via config when Enabled |
| Platform actor | Session principal when authenticated; Dev/Testing may still use header / DevelopmentOperator | Production Admin requires login; Dev headers still ignored in Production |
| Platform Authz | `PlatformAuthz` + role assignments | AuthN binds actor when session present |
| Platform Admin | Login/logout UI; Production requires authenticated user + HTTPS Platform API BaseUrl | Not full production-ready portfolio claim |
| POS / MAUI | Password+Bearer with introspect; Dev GUID/header fallback Dev/Testing-only; Production HTTPS PlatformAuth BaseUrl when set | Org/actor/commercial headers rejected or fail closed |
| ASP.NET auth middleware | **PlatformSession** scheme + Admin cookie auth + POS bearer middleware | — |

Dev/Testing identity is **not** production authentication (**R-091**, **R-120**, **D-P12-05**).

---

## 4. Target identity model

### 4.1 Platform User (global identity)

Platform owns:

- immutable Platform User ID
- username / email (normalized uniqueness)
- account status (active / suspended / disabled as already modeled)
- **credential and verification state** (**Implemented P13-WP02** — `PlatformUserCredential`)
- lockout / failed-attempt state (**Implemented P13-WP02**)
- session / security metadata needed for revocation (security stamp stored; session revoke later)
- **authentication audit events** (password set / unlock / email verified / bootstrap; login events later)

Platform User is **not** a POS Customer, HealthCare Patient, or org-local duplicate account.

### 4.2 Organization Membership

Platform owns the relationship: User ↔ Organization, membership status, organization-level Platform roles/grants, default-organization / selection eligibility.

Membership suspension is separate from user suspension (existing invariant).

### 4.3 Product Access / Entitlement

Platform owns commercial product-access assignment and subscription/entitlement facts.

Products enforce commercial gates via **approved contracts / local projections** — **not** direct Platform table reads (**D-P12-03** remains open for final transport).

Product Access allows launch eligibility only.

### 4.4 Product-Local Role

Each product owns operational roles/grants in its own database (e.g. POS `PosRole` / `PosRoleMatrix`). Platform Admin must not assign product-local roles as a side effect of Product Access.

---

## 5. Trust boundaries

```text
┌─────────────────────────────────────────────────────────────┐
│ Browser / MAUI client                                        │
│  - holds session cookie or access/refresh tokens             │
│  - never authoritative for org ID, roles, or entitlements    │
└───────────────────────────┬─────────────────────────────────┘
                            │ TLS (required in Production)
┌───────────────────────────▼─────────────────────────────────┐
│ Platform authentication boundary                             │
│  - verifies credentials / session / tokens                   │
│  - issues trusted Platform User identity                     │
│  - emits authentication audit events                         │
└───────────────────────────┬─────────────────────────────────┘
                            │ trusted actor + org context
        ┌───────────────────┼───────────────────┐
        ▼                   ▼                   ▼
 Platform Admin APIs   Product APIs        Product UIs
 (Platform Authz)      (product Authz)     (UI hide ≠ Authz)
```

**Rules:**

1. Clients never supply a forgeable “I am this Platform User” header in Production.
2. Organization context is server-validated against membership (never trust raw client org IDs alone).
3. Product APIs never query Platform EF/SQL tables directly.
4. Secrets, password hashes, reset tokens, and raw credentials never appear in logs or audit payloads.
5. UI permission chips / nav hiding are not authorization.

---

## 6. Target authentication surfaces

### 6.1 Interactive Platform Admin (browser)

**Direction (D-P13-03):** Cookie-based **server session** for Blazor Server Admin (HttpOnly, Secure, SameSite as hardened in later WPs). Session represents an authenticated Platform User.

Admin then continues to call Platform APIs with a **trusted authenticated principal** (session-derived or short-lived API credential issued after login — exact wiring in later WPs).

### 6.2 Platform / Product APIs and MAUI

**Direction (D-P13-03):** Platform-issued **bearer access tokens** (and refresh tokens where needed) carrying authenticated Platform User identity. Aligns with portfolio boundary “Authentication / refresh tokens — Platform Own (target).”

MAUI replaces GUID/header Dev sign-in with real Platform login and stores tokens in SecureStorage (never as authorization proof by DeviceId alone — existing P7 invariant).

### 6.3 Phase 13 MVP credential method

**Direction (D-P13-04):** Platform-local **username or email + password** with server-side salted hash (algorithm chosen in credential WP; not invented here beyond “industry-standard password hashing”).

**Deferred (explicit):** Broad enterprise SSO / Active Directory / arbitrary OIDC IdP federation remain deferred. **Authorized exception (P13-WP08):** Google and Facebook social login for public/store-owner/customer identity, creating or linking Platform User without privilege grants.

**Implemented (P13-WP08):** `platform_external_logins`; OAuth challenge/complete; Admin login entry; testing complete endpoint (Dev/Testing only). Social login must never auto-grant Platform Administrator, membership, entitlement, or product roles.

### 6.4 MFA

**Direction (D-P13-05):** Phase 13 designs **MFA readiness** (extension points, threat coverage). Full MFA enforcement is a later authorized WP unless the owner expands scope. Do not claim MFA shipped by architecture alone.

**Implemented (P13-WP07):** `PlatformMfaOptions`, `IPlatformMfaFactorStore` (null store), `IPlatformMfaReadinessService`, and MFA signals on login/me/token/introspect DTOs (`challengeRequired` always false). Production startup forbids `EnrollmentEnabled` / `EnforcementEnabled`. Reserved audit action codes exist but are not emitted. No enrollment/challenge endpoints.

---

## 7. Dev/Testing vs Production

| Mode | Allowed | Forbidden |
|---|---|---|
| **Development / Testing** | Existing header actor / org / commercial bypass for automated tests and local UX | Describing headers as production auth |
| **Production** | Cookie/session + Platform-issued tokens only (after implemented) | `X-Dev-Platform-User-Id`, POS Dev org/commercial headers as identity proof; DevelopmentOperator full access |

Production already **fails closed** without authentication (P9-WP01). Phase 13 must **add** authentication without weakening those guards.

**D-P12-05:** Keep language honest until R-091 is closed with evidence.

---

## 8. Relationship to commercial-state transport (D-P12-03)

Authenticated identity and membership are **necessary but not sufficient** for product commercial gates.

Final Platform→product commercial-state transport remains **open (D-P12-03)**. Architecture constraint for later WPs:

- Products must receive commercial facts through an **approved contract or projection**
- No direct Platform table reads
- Dev commercial headers remain provisional and Production-unavailable

---

## 9. Audit and privacy

Authentication events (login success/failure, logout, lockout, password change, token revoke) are Platform audit concerns.

Must not store: passwords, hashes, tokens, OTP secrets, full request bodies, PHI.

POS remains non-PHI by default (Product Foundation).

---

## 10. Decisions (P13-WP01)

| ID | Decision | State |
|---|---|---|
| **D-P13-01** | Access chain locked: User → Membership → Product Access → Product-Local Role | **Closed** |
| **D-P13-02** | Platform owns authentication SoR; products never own global credentials | **Closed** |
| **D-P13-03** | Admin interactive session = cookie/server session; APIs/MAUI = Platform bearer tokens | **Closed** (direction) |
| **D-P13-04** | Phase 13 MVP = local password auth; enterprise SSO/AD deferred; Google/Facebook authorized in P13-WP08 | **Closed** (scope revised by P13-WP08) |
| **D-P13-05** | MFA readiness in Phase 13; enforcement deferred unless authorized | **Closed** (scope) |
| **D-P13-06** | Dev headers never become Production authentication | **Closed** |
| **R-091** | Production authentication missing in code | **Open** until shipped |
| **D-P12-03** | Commercial-state transport | **Open** (preserved) |
| **D-P12-05** | Honest Dev vs Production language | **Open** until R-091 evidenced |

---

## 11. Explicit non-goals (this WP and Phase 13 unless authorized)

- Implementing login/logout, cookies, tokens, or migrations (later WPs)
- Email provider selection and delivery infrastructure
- Full MFA enforcement
- SSO / AD / external IdP
- Customer (POS) or Patient login as Platform User
- Shared cross-product operational roles
- Claiming Production-ready portfolio status

---

## 12. Recommended next work package

**Recommended next work package:** **P13-WP09 — Phase 13 Closeout** when explicitly authorized.
