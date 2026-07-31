# Phase 13 — Production Authentication and Identity

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-12-product-foundation-and-bootstrap.md) | [Architecture](../engineering/authentication-architecture.md) | [Threat model](../engineering/authentication-threat-model.md)

## Status

**Complete with documented residuals.** **P13-WP01**–**P13-WP09** are **complete**. **R-091 closed for Phase 13 scope** (credentials, browser session, password lifecycle, org context, product Bearer, MFA readiness/hardening, Google/Facebook, optional verified recovery email). Portfolio remains **not Production-ready**.

Authoritative docs:

- [`authentication-architecture.md`](../engineering/authentication-architecture.md)
- [`authentication-threat-model.md`](../engineering/authentication-threat-model.md)
- Reports: [`P13-WP01`](../reports/P13-WP01-authentication-architecture-and-threat-model.md) … [`P13-WP09`](../reports/P13-WP09-phase-13-closeout.md)

## Progress

| WP | Status | Report / tip |
|---|---|---|
| P13-WP01 — Authentication Architecture and Threat Model | **Complete** | [report](../reports/P13-WP01-authentication-architecture-and-threat-model.md) · `40a48349ae2a42e9dc267bde0df64afb004af3ae` |
| P13-WP02 — Identity Credentials and Auth Persistence | **Complete** | [report](../reports/P13-WP02-identity-credentials-and-auth-persistence.md) · `51ace5b90fc6c0bcb33fe483826481529bdfeb77` |
| P13-WP03 — Platform Login, Logout, and Browser Session | **Complete** | [report](../reports/P13-WP03-platform-login-logout-and-browser-session.md) · `6298b668c5d0555a84eb206b2a2313b138c9b892` |
| P13-WP04 — Password Lifecycle, Lockout, and Verification | **Complete** | [report](../reports/P13-WP04-password-verification-and-lockout-lifecycle.md) · `65b261eca7353a7efea2f8f1899c252f0dcee6dc` |
| P13-WP05 — Trusted API Actor and Organization Context | **Complete** | [report](../reports/P13-WP05-trusted-organization-context-and-membership-selection.md) · `e64f352161bb20447a99ae762d1a69ec1a3846fe` |
| P13-WP06 — Product Client Auth Integration (Admin + MAUI/POS) | **Complete** | [report](../reports/P13-WP06-product-client-auth-integration.md) · `68f13c0a4281071087e526ecf8e51414f2a78b12` |
| P13-WP07 — MFA Readiness and Auth Hardening | **Complete** | [report](../reports/P13-WP07-mfa-readiness-and-authentication-hardening.md) · `7b767f664e63c5c296e0444062129acd7ee36727` |
| P13-WP08 — Google and Facebook External Authentication | **Complete** | [report](../reports/P13-WP08-google-facebook-external-authentication.md) · `7c9338090f55b0fc2e289fe3b95fb3b4ce5d7938` |
| P13-WP09 — Phase 13 Closeout | **Complete** | [report](../reports/P13-WP09-phase-13-closeout.md) · `ef949b78c2a8e2271dfd7f1e5b54e72092db74d1` |

## Purpose

Replace Development/Testing actor mechanisms with production-grade authentication and identity for the ExItS Platform and independently deployed products, while preserving:

- Platform identity and commercial access vs product-local operational roles
- separate Platform and product databases
- fail-closed Production guards already delivered in Phase 9

**R-091 — Missing production authentication** is **closed for Phase 13 scope** with residuals documented in the [P13-WP09 report](../reports/P13-WP09-phase-13-closeout.md).

## Locked access chain

```text
Platform User → Active Organization Membership → Product Access / Entitlement → Product-Local Role
```

## Phase Exit Criteria

- Production authentication implemented and evidenced for authorized surfaces — **met**
- Dev headers remain Dev/Testing-only — **met**
- Access chain preserved — **met**
- R-091 closed or explicitly residual with evidence — **closed for Phase 13 scope** with residuals
- Tests pass; `main = origin/main`; working tree clean — evidenced at closeout
- Portfolio not falsely claimed Production-ready — **not claimed**

## Residuals (not Phase 13 reopen)

- MFA enrollment/enforcement deferred (**D-P13-05**)
- Enterprise SSO/AD beyond Google/Facebook deferred
- Outbound email vendor not selected
- Portfolio Production still blocked by R-109, R-129/NU1903, TLS-PROD, MAUI HTTPS, D-P12-03, etc.

## Exact next after P13-WP09

**Await explicit authorization for the next phase** (Phase 14 scope not defined in-repo). Do **not** begin Phase 14, MFA enforcement, email-vendor selection, or D-P12-03 from this closeout alone.
