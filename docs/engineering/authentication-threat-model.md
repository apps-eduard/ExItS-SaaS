# Authentication Threat Model

[Architecture](authentication-architecture.md) | [Security](security.md) | [Authorization](authorization-matrix.md) | [P13-WP01 report](../reports/P13-WP01-authentication-architecture-and-threat-model.md)

**Status:** Authoritative threat model for Phase 13 production authentication (**P13-WP01**). Controls listed as **Target** are not yet implemented. Current Production posture is **fail closed without authentication** (P9-WP01), which blocks anonymous abuse but does **not** close R-091.

Method: asset-centric STRIDE with ExItS trust boundaries. Residual risks remain explicit.

---

## 1. Assets

| Asset | Sensitivity | Owner |
|---|---|---|
| Platform User identity | High | Platform |
| Credentials / password hashes / reset tokens | Critical | Platform (future) |
| Interactive Admin session | Critical | Platform |
| API access / refresh tokens | Critical | Platform |
| Organization membership & Platform roles | High | Platform |
| Product access / entitlements | High | Platform (+ product projection) |
| Product-local roles & operational data | High / Critical (financial) | Product |
| Authentication audit trail | High | Platform |
| MAUI SecureStorage session/tokens | High | Device + product client |

---

## 2. Actors

| Actor | Capability today | Target after Phase 13 |
|---|---|---|
| Anonymous internet client | Hits Production APIs that fail closed / 403 | Same + cannot obtain session without credentials |
| Dev/Testing operator | Header-based actor / full DevOperator | Unchanged in Dev/Testing only |
| Authenticated Platform User | N/A in Production | Scoped by membership + Platform Authz |
| Malicious insider with membership | Org-scoped if headers forged in Dev | Still constrained by Authz; audit |
| Compromised device (MAUI) | SecureStorage session theft | Short-lived access token + refresh revoke |
| Cross-product attacker | Separate DBs already | Must not gain ops via Platform Access alone |

---

## 3. Trust boundaries

1. **Internet → Platform Admin / Platform API / Product API** (TLS required in Production)
2. **Authentication boundary → Authorization boundary** (identity proven ≠ permission granted)
3. **Platform DB ↔ Product DB** (no shared tables / cross-DB FKs)
4. **Client device storage** (SecureStorage / browser cookie jar) — untrusted for org/role claims
5. **Email / notification channel** (out of auth security core; phishing-capable)

---

## 4. STRIDE catalog

### 4.1 Spoofing

| Threat | Current exposure | Target control |
|---|---|---|
| Forge `X-Dev-Platform-User-Id` | Ignored in Production; powerful in Dev | Keep Dev-only; Production uses session/token only (**D-P13-06**) |
| Forge org / commercial headers | Rejected / Unknown fail-closed in Production | Replace with server-derived membership + approved commercial contract (**D-P12-03** open) |
| Impersonate Platform User without password | Production has no login — APIs deny sensitive ops | Password (or later IdP) + rate-limited auth |
| Steal Admin session cookie | N/A (no cookie auth yet) | HttpOnly Secure cookie; session revoke; CSRF strategy for cookie APIs |
| Steal MAUI bearer token | Dev session in SecureStorage | Short access TTL; rotating refresh; logout revoke |

### 4.2 Tampering

| Threat | Current exposure | Target control |
|---|---|---|
| Client supplies org ID as authority | Dev headers; Production rejects | Membership-checked server org context |
| Token/claim tampering | No JWT yet | Signed tokens; validate iss/aud/exp/sig |
| Privilege escalation via Product Access | Product-local roles separate (POS) | Preserve chain; never map Access → PosRole automatically |

### 4.3 Repudiation

| Threat | Current exposure | Target control |
|---|---|---|
| Unauthenticated mutations with string actor fields | Historical payment `confirmedBy` strings | Authenticated actor ID on auth-sensitive ops |
| Missing login/failure audit | Limited | Append-only auth audit (no secrets) |

### 4.4 Information disclosure

| Threat | Current exposure | Target control |
|---|---|---|
| User enumeration via login errors | N/A | Generic failure messages; careful timing |
| Secrets in logs/ProblemDetails | Hardened in P9 | Preserve; extend to auth endpoints |
| PHI in Platform auth audit | POS non-PHI; HC contracts exclude PHI | Keep invariant |

### 4.5 Denial of service

| Threat | Current exposure | Target control |
|---|---|---|
| Auth endpoint flooding | Partitioned rate limits exist | Auth-specific limits + lockout backoff |
| Lockout abuse (DoS accounts) | N/A | Progressive delay; admin unlock; monitor |

### 4.6 Elevation of privilege

| Threat | Current exposure | Target control |
|---|---|---|
| DevelopmentOperator full access in Production | Disabled (`GrantDevelopmentOperatorFullAccess=false`) | Keep disabled forever in Production |
| Platform Admin → POS Cashier | Not automatic | Preserve product-local Authz |
| Suspended user retains session | N/A | Session/token validation checks account + membership status |
| PastDue commercial continuity misuse | Feature-grant matrix | Unchanged commercial rules; authenticated principal only |

---

## 5. Abuse scenarios (must fail closed)

1. **Production request with Dev actor header** → ignore header; deny if unauthenticated.
2. **Valid login, no org membership** → authenticated but cannot select org / launch products.
3. **Membership without Product Access** → cannot launch product; no product-local ops.
4. **Product Access without product-local role** → launch may succeed; operational mutations deny.
5. **Cross-organization token reuse** → 404/403; no data bleed.
6. **Refresh token replay after logout/revoke** → reject.
7. **Password reset token reuse** → single-use; expire.
8. **UI-only permission hide** → API still enforces Authz.

---

## 6. Residual risks (explicit)

| ID | Residual | Notes |
|---|---|---|
| **R-091** | No production authentication implemented | Closed only when WPs ship + tests evidence |
| **D-P12-03** | Commercial transport unresolved | AuthN does not invent this |
| **R-098** | DevOperator misuse on misconfigured hosts | Config discipline |
| **R-109** | Interactive Android validation | Device auth UX later |
| **R-129** | Full local DB encryption | Offline token/data at rest |
| Email phishing / SIM swap | Out of MVP | MFA readiness helps later |
| No formal pen-test | Process | Do not claim certified |

---

## 7. Validation expectations for later WPs

Architecture acceptance for implementations must include tests proving:

- Production rejects Dev identity headers
- Unauthenticated sensitive routes deny
- Suspended user / membership cannot use session
- Cross-org concealment
- Product Access ≠ product-local role
- Audit events omit secrets
- Logout / revoke invalidates continuation

P13-WP01 itself adds **no** runtime tests beyond documenting the baseline suite still green.
