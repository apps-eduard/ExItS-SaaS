# Phase 13 — Production Authentication and Identity

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-12-product-foundation-and-bootstrap.md) | [Architecture](../engineering/authentication-architecture.md) | [Threat model](../engineering/authentication-threat-model.md)

## Status

**In progress.** **P13-WP01**, **P13-WP02**, and **P13-WP03** are **complete**. Exact next: **P13-WP04 — Password Lifecycle, Lockout, and Verification** when authorized (do **not** begin).

Phase 12 remains closed. **R-091** remains open — login/session shipped; password lifecycle, trusted API actor hardening, product client wiring, and closeout remain.

Authoritative docs:

- [`authentication-architecture.md`](../engineering/authentication-architecture.md)
- [`authentication-threat-model.md`](../engineering/authentication-threat-model.md)
- Reports: [`P13-WP01`](../reports/P13-WP01-authentication-architecture-and-threat-model.md), [`P13-WP02`](../reports/P13-WP02-identity-credentials-and-auth-persistence.md), [`P13-WP03`](../reports/P13-WP03-platform-login-logout-and-browser-session.md)

## Progress

| WP | Status | Report / tip |
|---|---|---|
| P13-WP01 — Authentication Architecture and Threat Model | **Complete** | [report](../reports/P13-WP01-authentication-architecture-and-threat-model.md) · `40a48349ae2a42e9dc267bde0df64afb004af3ae` |
| P13-WP02 — Identity Credentials and Auth Persistence | **Complete** | [report](../reports/P13-WP02-identity-credentials-and-auth-persistence.md) · `51ace5b90fc6c0bcb33fe483826481529bdfeb77` |
| P13-WP03 — Platform Login, Logout, and Browser Session | **Complete** | [report](../reports/P13-WP03-platform-login-logout-and-browser-session.md) · `6298b668c5d0555a84eb206b2a2313b138c9b892` |
| P13-WP04 — Password Lifecycle, Lockout, and Verification | Not started | — |
| P13-WP05 — Trusted API Actor and Organization Context | Not started | — |
| P13-WP06 — Product Client Auth Integration (Admin + MAUI/POS) | Not started | — |
| P13-WP07 — MFA Readiness and Auth Hardening | Not started | — |
| P13-WP08 — Phase 13 Closeout | Not started | — |

## Purpose

Replace Development/Testing actor mechanisms with production-grade authentication and identity for the ExItS Platform and independently deployed products, while preserving:

- Platform identity and commercial access vs product-local operational roles
- separate Platform and product databases
- fail-closed Production guards already delivered in Phase 9

This phase is intended to close **R-091 — Missing production authentication** when implementation WPs are complete and evidenced.

## Phase Objective

Deliver a secure, testable authentication and identity system supporting:

- real login and logout
- trusted authenticated Platform User identity
- secure browser sessions (Admin)
- Platform-issued tokens for APIs / MAUI
- password lifecycle, lockout, and verification
- MFA readiness (enforcement gated)
- trusted organization context
- Platform Admin route protection
- product launch/access validation without granting product-local roles
- authentication audit events
- strict Dev/Testing isolation from Production
- honest readiness documentation

## Architectural Principles

1. One global Platform User per human; memberships are relationships, not duplicate identities.
2. Locked chain: User → Membership → Product Access → Product-Local Role (**D-P13-01**).
3. Platform owns authentication SoR (**D-P13-02**).
4. Admin interactive auth uses cookie/server session; APIs/MAUI use bearer tokens (**D-P13-03**).
5. Phase 13 MVP = local password; SSO/AD deferred (**D-P13-04**).
6. MFA readiness ≠ MFA enforced unless authorized (**D-P13-05**).
7. Dev headers never become Production authentication (**D-P13-06**).
8. Do not invent D-P12-03 commercial transport under the guise of auth.
9. No fake login; no weakening Production fail-closed guards.
10. UI hide ≠ authorization.

## Explicit Exclusions (phase-level)

Unless a later WP explicitly authorizes:

- SSO / Active Directory / external OIDC IdP
- Full MFA enforcement
- Email vendor production infrastructure beyond what a verification WP requires
- Customer/Patient login as Platform User
- Cross-product operational role sharing
- Claiming portfolio Production-ready status

## Phase Work Packages

### P13-WP01 — Authentication Architecture and Threat Model

**Complete** — see [P13-WP01 report](../reports/P13-WP01-authentication-architecture-and-threat-model.md).

#### Objective

Define authoritative production authentication architecture and threat model before code.

#### Deliverables

- `docs/engineering/authentication-architecture.md`
- `docs/engineering/authentication-threat-model.md`
- Phase 13 roadmap
- Completion report
- Decisions D-P13-01…06 recorded; R-091 / D-P12-03 / D-P12-05 preserved open where unresolved

#### Acceptance

- Identity layers unambiguous
- Access chain locked
- Threat model covers spoofing, elevation, session/token theft, Dev header misuse
- No authentication code, migrations, packages, or UI added
- Tests baseline unchanged
- Documentation matches repository reality

### P13-WP02 — Identity Credentials and Auth Persistence

**Complete** — see [P13-WP02 report](../reports/P13-WP02-identity-credentials-and-auth-persistence.md).

#### Objective

Add Platform persistence for credentials, verification, and lockout state without public login UI (unless separately authorized).

#### Status

**Complete.** `platform_user_credentials`, ASP.NET Core `PasswordHasher<TUser>`, hardened bootstrap controls, credential APIs. No login/session.

### P13-WP03 — Platform Login, Logout, and Browser Session

**Complete** — see [P13-WP03 report](../reports/P13-WP03-platform-login-logout-and-browser-session.md).

#### Objective

Implement Admin interactive login/logout and cookie/server session.

#### Status

**Complete.** Server-side sessions, login/logout/me APIs, Admin login UI, trusted session actor. No password-reset delivery, MFA, or bearer tokens.

### P13-WP04 — Password Lifecycle, Lockout, and Verification

#### Objective

Password change/reset, lockout, and email verification flows as authorized.

#### Status

Not started — begin only when authorized.

### P13-WP05 — Trusted API Actor and Organization Context

#### Objective

Replace Production reliance on absent auth with trusted authenticated actor + membership-checked org context for Platform APIs (and contracts for products).

#### Status

Not started — begin only when authorized.

### P13-WP06 — Product Client Auth Integration (Admin + MAUI/POS)

#### Objective

Wire Admin and MAUI/POS clients to real Platform auth; remove Production dependence on Dev GUID/header identity.

#### Status

Not started — begin only when authorized.

### P13-WP07 — MFA Readiness and Auth Hardening

#### Objective

MFA extension points and auth hardening; enforcement only if authorized.

#### Status

Not started — begin only when authorized.

### P13-WP08 — Phase 13 Closeout

#### Objective

Reconcile Phase 13, evidence R-091 disposition, document residual risks, set exact next phase.

#### Status

Not started — begin only when authorized.

## Phase Exit Criteria

- Production authentication implemented and evidenced for authorized surfaces
- Dev headers remain Dev/Testing-only
- Access chain preserved
- R-091 closed or explicitly residual with evidence
- Tests pass; `main = origin/main`; working tree clean
- Portfolio not falsely claimed Production-ready

## Exact next after P13-WP03

**P13-WP04 — Password Lifecycle, Lockout, and Verification** when explicitly authorized. Do not begin P13-WP04.
