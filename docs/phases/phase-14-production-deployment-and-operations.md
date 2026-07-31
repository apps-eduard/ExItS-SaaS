# Phase 14 — Production Deployment and Operations

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-13-production-authentication-and-identity.md) | [Architecture](../engineering/production-deployment-architecture.md) | [Readiness audit](../engineering/production-readiness-audit.md)

## Status

**In progress.** **P14-WP01** is the active authorized work package (docs/discovery only). Phase 13 remains closed. Portfolio remains **not Production-ready**.

Authoritative docs:

- [`production-deployment-architecture.md`](../engineering/production-deployment-architecture.md)
- [`production-readiness-audit.md`](../engineering/production-readiness-audit.md)
- Report: [`P14-WP01`](../reports/P14-WP01-deployment-architecture-and-production-readiness-audit.md)

## Progress

| WP | Status | Report / tip |
|---|---|---|
| P14-WP01 — Deployment Architecture and Production Readiness Audit | **Complete** | [report](../reports/P14-WP01-deployment-architecture-and-production-readiness-audit.md) · *(feature tip recorded after commit)* |
| P14-WP02 — Production Packaging and Compose Baseline | Not started | — |
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

Production-oriented images/Compose/versioning **when authorized**. Must not silently promote `docker-compose.pilot.yml` as Production.

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

## Exact next after P14-WP01

**P14-WP02 — Production Packaging and Compose Baseline** when explicitly authorized. Do **not** begin P14-WP02 from this page alone.
