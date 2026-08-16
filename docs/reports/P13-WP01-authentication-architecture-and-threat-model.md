# P13-WP01 — Authentication Architecture and Threat Model

Phase marker: `P13-WP01-authentication-architecture-and-threat-model`

Package: **P13-WP01 — Authentication Architecture and Threat Model**
Prior tip: `77f63f2409b64c3919eafa5485cd12b31a5486a5`
Docs tip: `40a48349ae2a42e9dc267bde0df64afb004af3ae`

## Status

**Complete.** Architecture and threat model documentation only. No authentication code, migrations, cookies, tokens, Razor auth pages, email delivery, MFA implementation, production infrastructure, or new NuGet packages.

Exact next: **P13-WP02 — Identity Credentials and Auth Persistence** when authorized (do **not** begin).

**R-091 remains open.**

## 1. Delivered capability

| Deliverable | Path |
|---|---|
| Authoritative architecture | `docs/engineering/authentication-architecture.md` |
| Threat model | `docs/engineering/authentication-threat-model.md` |
| Phase 13 roadmap | `docs/phases/phase-13-production-authentication-and-identity.md` |
| This report | `docs/reports/P13-WP01-authentication-architecture-and-threat-model.md` |

## 2. Locked identity model

```text
Platform User
  → Organization Membership
  → Product Access / Entitlement
  → Product-Local Role and Grants
```

| Layer | Meaning |
|---|---|
| Platform User | One global human identity (ID, username/email, status, future credential/session/audit state) |
| Organization Membership | User↔org relationship; org-level Platform roles; not a second identity |
| Product Access | Commercial launch eligibility; not operational permission |
| Product-Local Role | Product-owned ops (e.g. POS Store Manager) |

Evidence spine today: `PlatformUser`, `OrganizationMembership`, `ProductAccessAssignment`, POS `PosRole` / `PosRoleMatrix` — profiles/access exist; **credentials/auth middleware do not**.

## 3. Current vs target

| Concern | Current | Target (Phase 13 direction) |
|---|---|---|
| Actor proof | Dev headers / GUID MAUI session | Cookie session (Admin) + Platform bearer tokens (APIs/MAUI) |
| Credentials | None on `PlatformUser` | Platform-owned hashed passwords (MVP) |
| Production | Fail closed without auth | Authenticate then authorize |
| SSO/AD/MFA enforce | Absent | SSO/AD deferred; MFA readiness only unless authorized |

## 4. Decisions

| ID | State | Summary |
|---|---|---|
| **D-P13-01** | Closed | Access chain locked |
| **D-P13-02** | Closed | Platform owns auth SoR |
| **D-P13-03** | Closed (direction) | Admin cookie/session; API/MAUI bearer tokens |
| **D-P13-04** | Closed (MVP scope) | Local password MVP; SSO/AD deferred |
| **D-P13-05** | Closed (scope) | MFA readiness; enforcement deferred |
| **D-P13-06** | Closed | Dev headers ≠ Production auth |
| **R-091** | Open | No production auth code yet |
| **D-P12-03** | Open | Commercial-state transport preserved |
| **D-P12-05** | Open | Honest Dev/Production language until R-091 evidenced |

## 5. Threat model summary

Covered: header spoofing, org/commercial forgery, session/token theft, elevation via Product Access, repudiation, enumeration, auth DoS/lockout abuse, DevelopmentOperator misuse. Residual: R-091, D-P12-03, R-098, R-109, R-129, no formal pen-test.

## 6. Explicit exclusions

No identity entities/migrations; login/logout; cookies/tokens; Razor auth pages; email delivery; MFA implementation; production infrastructure; new NuGet packages; no real product scaffold; no Phase 12 reopen.

## 7. Validation

| Check | Result |
|---|---|
| Full Release tests | **1186 passed / 0 failed / 0 skipped** (baseline held; docs-only) |
| No `src/` auth implementation in this WP | Pass |
| No foreign product tree | Pass |
| Portfolio independence | Pass |
| `main = origin/main` | After push |

## 8. Files / docs changed

- Created architecture, threat model, Phase 13 roadmap, this report
- Updated portfolio, phases index, FILE-MANIFEST, risks, release-plan, security/authorization pointers, Product Foundation open-decision future point as needed

## Exact next work package

**P13-WP02 — Identity Credentials and Auth Persistence** when explicitly authorized. Do not begin P13-WP02.
