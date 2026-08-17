# ExItS SaaS Portfolio Progress

Authoritative **current status** dashboard. Work-package evidence lives in [reports](reports/README.md). Phase definitions live in [phases](phases/README.md).

[Documentation Home](index.md) | [All phases](phases/README.md) | [Reports](reports/README.md) | [Risks](risks-and-issues.md) | [Production readiness](engineering/production-readiness-audit.md)

## Current Status

| Field | Value |
|---|---|
| Portfolio | ExItS SaaS |
| Active products | ExItS Platform, PinoyBusinessPOS, Personal Web |
| Current phase | **Phase 29** — Open / Partial Closeout ([phase](phases/phase-29-data-integrity-query-performance-and-database-hardening.md)) |
| Latest evidence | **P29-WP14** PostgreSQL backup/restore recovery validation — Code Complete / Validation Evidence Recorded ([report](reports/P29-WP14-postgresql-backup-restore-and-recovery-validation.md)) |
| Development backup/restore | **Proven** (clean restore drill) |
| Production Backup/Restore Proven | **No** |
| Production Payment Ready | **No** |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |
| Open blockers (summary) | TLS-PROD; MAUI-HTTPS; auth email vendor; MFA deferred; owner/physical validation pending on several open phases |
| Last updated | 2026-08-17 |

Also open (prior statuses unchanged): Phases **14**, **19–28**.

## Phase Summary

Statuses match [phases/README.md](phases/README.md). Do not treat this table as a work-package archive.

| Phase | Name | Status |
|---:|---|---|
| 1–13, 15–18 | Platform foundation through early mobile (see phase index) | Complete (with documented risks/residuals where noted) |
| 14 | Production Deployment and Operations | **In progress** |
| 19 | Mobile POS Operations and Cashier Experience | **Open** |
| 20 | Global Product Catalog and Business Template Onboarding | **Open** |
| 21 | Privacy, Compliance, and Regulatory Readiness | **Open** |
| 22 | Production Readiness, Release & Operational Hardening | **Open** |
| 23 | Multi-Business Entitlements and Variable-Quantity Selling | **Open** |
| 24 | Linked Customer Statements and Personal Monetization | **Open** |
| 25 | Organization Web Admin / AntDesign hosts / SSO | **Open** |
| 26 | Sales Documents and Compliance Readiness | **Open** |
| 27 | Connected Supplier Commerce & Purchasing | **Open / In Progress** |
| 28 | Customer Ordering, Pickup & Delivery | **Open / In Progress** |
| 29 | Data Integrity, Query Performance & Database Hardening | **Open / Partial Closeout** |

## Current Open Areas

- **Phase 29:** WP11–WP14 evidence recorded (constraints, electronic payment reliability, concurrency/EXPLAIN, development backup/restore). Broader load and **Production** backup residuals remain.
- **Phase 14:** Packaging/TLS work in progress; Production backup/ops evidence (P14-WP04+) not started.
- **Phases 19–28:** Implementation slices exist at varying completeness; owner/device/browser verification and closeouts remain open where noted on each phase page. Phase 28 Personal linked-merchant storefront/cart is Code Complete / Validation Pending; **P28-WP10** E2E/closeout remains next.
- **Honesty gates:** Not Device Verified · Not Browser Verified · Not Production Ready · Not Production Payment Ready · Production Backup/Restore Proven = No.

## Authoritative Navigation

| Need | Location |
|---|---|
| Phase roadmap | [phases/README.md](phases/README.md) |
| Work-package / completion reports | [reports/README.md](reports/README.md) |
| Current phase detail | [Phase 29](phases/phase-29-data-integrity-query-performance-and-database-hardening.md) |
| Risks and issues | [risks-and-issues.md](risks-and-issues.md) |
| Architecture entry | [approved-architecture-summary.md](engineering/approved-architecture-summary.md) |
| Production readiness | [production-readiness-audit.md](engineering/production-readiness-audit.md) |
| Decisions | [decisions/README.md](decisions/README.md) |

## Workflow note

Agents and contributors should update **this dashboard** after meaningful delivery milestones, and record immutable evidence under `docs/reports/`. Do not begin unauthorized Production work packages. Platform Admin UI direction remains **Ant Design Blazor** (ADR-015).
