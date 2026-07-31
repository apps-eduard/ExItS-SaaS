# Phase 14 — Production Deployment and Operations

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-13-production-authentication-and-identity.md) | [Architecture](../engineering/production-deployment-architecture.md) | [Readiness audit](../engineering/production-readiness-audit.md)

## Status

**Status:** **In progress.** **P14-WP01**–**P14-WP02** complete (including live-preview gap fix). Exact next: **P14-WP03** when authorized. Portfolio remains **not Production-ready**.

Authoritative docs:

- [`production-deployment-architecture.md`](../engineering/production-deployment-architecture.md)
- [`production-readiness-audit.md`](../engineering/production-readiness-audit.md)
- Reports: [`P14-WP01`](../reports/P14-WP01-deployment-architecture-and-production-readiness-audit.md), [`P14-WP02`](../reports/P14-WP02-production-packaging-and-compose-baseline.md), [`P14-WP02 live-preview gap fix`](../reports/P14-WP02-gap-fix-separate-live-preview-stack.md), [`P14-WP02A quick login`](../reports/P14-WP02A-live-preview-test-users-and-quick-login.md)

## Progress

| WP | Status | Report / tip |
|---|---|---|
| P14-WP01 — Deployment Architecture and Production Readiness Audit | **Complete** | [report](../reports/P14-WP01-deployment-architecture-and-production-readiness-audit.md) · `e0e2da2d03babc01dd6efab9d44c6c2a2668457a` |
| P14-WP02 — Production Packaging and Compose Baseline | **Complete** | [report](../reports/P14-WP02-production-packaging-and-compose-baseline.md) · `fa04ee2e9decd200b4dc1407f4f1b88f91f93afe` |
| P14-WP02 Gap Fix — Separate Live Preview Stack | **Complete** | [report](../reports/P14-WP02-gap-fix-separate-live-preview-stack.md) · `16342195ff4999f7c0fc99fa15306fc3fa530074` |
| P14-WP02A — Live Preview Test Users and Quick Login | **Complete** | [report](../reports/P14-WP02A-live-preview-test-users-and-quick-login.md) · *(feature tip recorded after commit)* |
| P14-WP03 — Reverse Proxy, TLS, and Network Hardening | Not started | — |
| P14-WP04 — Production Backup, Restore, and Ops Evidence | Not started | — |
| P14-WP05 — Monitoring, Alerting, and Support Model | Not started | — |
| P14-WP06 — Deployment Readiness Evaluator Alignment | Not started | — |
| P14-WP07 — Phase 14 Closeout | Not started | — |

## Purpose

Deliver honest, evidence-based **Production** deployment and operations for customer on-prem ExItS (Platform + licensed products), without claiming Production readiness early and without violating Platform/product database boundaries.

## Phase objective

- Authoritative Production topology and decisions
- Clear gap analysis from pilot packaging to Production
- TLS, packaging, backup ops, monitoring, and evaluator alignment as authorized WPs
- Preserve access chain and Phase 13 authentication SoR
- Keep pilot artifacts labeled non-production until replaced/superseded with evidence

## Architectural principles

1. Customer on-prem host; reverse-proxy HTTPS (**D-P14-01**).
2. One Platform + independently versioned licensed products; **one PostgreSQL per product** + Platform DB.
3. Secrets environment-owned (**D-P14-02**).
4. Backup-verify-migrate-validate; no Production startup `Migrate()` (**D-P14-03**).
5. Pilot Docker/Compose ≠ Production (**D-P14-04**).
6. Do not invent **D-P12-03** commercial transport under deployment work.
7. Do not nest HealthCare or cross product DBs.
8. UI/Admin remain native CSS — no Ant/Tailwind from ops work.

## Explicit exclusions (phase-level)

Unless a later WP explicitly authorizes:

- Kubernetes / multi-cloud control planes
- HealthCare deployment
- MFA enforcement / email vendor selection (auth residuals — may be separate authorization)
- Product feature work (POS workflows, dashboards)
- Claiming portfolio Production-ready without audit closure

## Work packages

### P14-WP01 — Deployment Architecture and Production Readiness Audit

**Complete** when tip recorded — documentation, discovery, decisions, and planning only.

### P14-WP02 — Production Packaging and Compose Baseline

**Complete.** Default `deploy/docker/compose.yaml` for local packaging tests (Platform/POS APIs + separate PostgreSQL). Pilot Compose preserved as NON-PRODUCTION. Gap fix: separate `compose.live-preview.yaml` (`exits-live-preview`) with Admin on port **8090** — does not reuse packaging ports.

### P14-WP03 — Reverse Proxy, TLS, and Network Hardening

Production TLS evidence, proxy config, MAUI HTTPS-only Production policy alignment **when authorized**.

### P14-WP04 — Production Backup, Restore, and Ops Evidence

Production-oriented backup/restore rehearsal and runbook evidence beyond pilot tooling **when authorized**.

### P14-WP05 — Monitoring, Alerting, and Support Model

Health→ops monitoring model **when authorized** (no fake “full observability” claims).

### P14-WP06 — Deployment Readiness Evaluator Alignment

Update `ExItS.Deployment` readiness/risk register text and gates to match Phase 10/13 evidence **when authorized** (docs-only WP01 records the drift).

### P14-WP07 — Phase 14 Closeout

Reconcile Phase 14; honest Production readiness disposition; exact next phase.

## Phase exit criteria

- Production topology documented and implemented for authorized surfaces
- TLS and packaging evidenced or explicitly residual
- Evaluator honesty matches closed risks
- Tests pass; `main = origin/main`; working tree clean
- Portfolio not falsely claimed Production-ready

## Exact next after P14-WP02

**P14-WP03 — Reverse Proxy, TLS, and Network Hardening** when explicitly authorized. Do **not** begin P14-WP03 from this page alone.
