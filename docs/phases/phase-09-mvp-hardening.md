# Phase 9 — MVP Hardening and Release

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-08-basic-store.md) | [Next](phase-10-full-pos.md)

## Status

**In Progress** — P9-WP01–P9-WP04 complete with documented risks. Do **not** begin P9-WP05 until explicitly authorized. **Not production-ready.**

## Objective

Prepare the first commercial MVP for secure production release.

## Work packages

### P9-WP01 — Security and Privacy Hardening

Status: **Complete** with documented risks

Report: [P9-WP01-security-and-privacy-hardening.md](../reports/P9-WP01-security-and-privacy-hardening.md)

Feature commit: de4fac64739f5b368a6b1f2490223fa032201b65

Phase marker: `P9-WP01-security-and-privacy-hardening`

#### Approved scope (clarified)

Harden Platform and PinoyBusinessPOS MVP against security, privacy, authorization, secret-handling, data-leakage, and unsafe-production-configuration risks. **No new business features.**

Deliver:

- Production-environment security guards (dev/test headers, probes, diagnostics unavailable outside approved environments)
- Authorization and tenant-isolation review (fail closed; conceal cross-org; no weakening)
- Privacy and data-minimization review
- Secret, token, header, and log hardening
- API and browser/mobile security controls (HTTPS outside Development, safe CORS, security headers, safe ProblemDetails)
- Local-device data protection review (Phase 7 AES-GCM; R-129 / SQLCipher remains open gate)
- Dependency and configuration security review
- Focused rate/abuse protection where supported
- Automated security/architecture tests
- Threat model and release-blocker documentation

HealthCare remains frozen.

#### Explicit exclusions (this WP)

- Production IdP / JWT/MFA/SSO unless already authorized separately (document as open blocker; do not invent fake auth)
- POS operational roles (unless separately assigned — not in this WP)
- Tax, refund, accounting, gateway, export, deferred business features
- Full SQLCipher migration without approved security decision
- Penetration-testing or compliance certification claims without evidence
- P9-WP02 or later

Existing Development/Testing identity, commercial, actor, feature-grant, and organization headers must remain unavailable outside approved environments.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (900 / 0 / 0; baseline 882).
- [x] Dashboard and phase page updated.
- [x] Completion report created (`docs/reports/P9-WP01-security-and-privacy-hardening.md`).
- [x] Focused commit created and hash recorded.
- [x] Working tree clean.
- [x] Exact next WP recorded: **P9-WP02 — Performance and Reliability** (do not begin).

### P9-WP02 — Performance and Reliability

Status: **Complete** with documented risks

Report: [P9-WP02-performance-and-reliability.md](../reports/P9-WP02-performance-and-reliability.md)

Feature commit: 46a4ac7bacfad0736fba4741817958862fadf9e2

Phase marker: `P9-WP02-performance-and-reliability`

#### Approved scope (clarified)

Harden Platform and PinoyBusinessPOS for predictable MVP-scale performance, resilience, recovery, and operational reliability. **No new business features.** Preserve all P9-WP01 security controls.

Deliver:

- Performance baselines and provisional budgets (not business SLAs unless documented otherwise)
- Database/query optimization (justified indexes only; no speculative indexing)
- API reliability controls (timeouts, cancellation, bounded retries where idempotent)
- Concurrency and transaction hardening (financial/stock invariants)
- Offline-sync reliability review (bounded processing; no silent discard)
- Health/readiness checks (liveness vs readiness; no secrets)
- Graceful failure behavior and failure-scenario matrix
- Load/soak evidence at representative or documented-scaled volumes
- Reliability risks and release blockers
- Observability: safe duration/outcome metrics without secrets/PHI/high-cardinality payloads

#### Explicit exclusions

- New business features; HealthCare changes
- Caching that exposes cross-org data or presents stale financial state as authoritative
- Redis/distributed cache without measured justification and roadmap approval
- Weakening authorization, org isolation, immutability, idempotency, or fail-closed behavior
- Automatic destructive offline cleanup
- P9-WP03 or later

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (915 / 0 / 0; baseline 900).
- [x] Dashboard and phase page updated.
- [x] Completion report created (`docs/reports/P9-WP02-performance-and-reliability.md`).
- [x] Focused commit created and hash recorded.
- [x] Working tree clean.
- [x] Exact next WP recorded: **P9-WP03 — Backup and Restore** (do not begin).

### P9-WP03 — Backup and Restore

Status: **Complete** with documented risks

Report: [P9-WP03-backup-and-restore.md](../reports/P9-WP03-backup-and-restore.md)

Feature commit: 3bbb0c716da60bd7d87a191c35bd0eced1bde380
Docs commit: 20ac81ac1cedc281a5c8e2d27ea8e8194e33a461

Phase marker: `P9-WP03-backup-and-restore`

#### Approved scope (clarified)

Create a safe, repeatable, documented backup-and-restore capability for the delivered MVP. Prove **recoverability**, not merely that dump files can be created. **No new business features.** Preserve P9-WP01 security and P9-WP02 health/reliability. Platform and POS databases remain separate and independently restorable.

Deliver:

- PostgreSQL-native logical backups (`pg_dump`) per database with manifests and SHA-256 checksums
- Safe restore into new/empty databases with explicit destructive confirmation for overwrite
- Structural and business-integrity validation after restore
- Retention cleanup (dry-run by default; never delete latest valid)
- Encryption-at-rest guidance and safe integration point (keys never beside artifacts; no secrets in repo)
- Operational runbooks and recovery-drill evidence
- Provisional RPO/RTO engineering targets (not SLAs)
- Local/offline SQLite limitations documented (not authoritative; R-129)
- PITR explicitly deferred unless WAL evidenced
- Automated tests (Testcontainers/disposable DBs)

#### Explicit exclusions

- New business features; HealthCare changes
- Combining Platform+POS into one non-independently-restorable artifact
- Committing dumps, secrets, tokens, or keys
- Claiming production DR/PITR beyond tested scenarios
- External paid backup services
- Mobile SQLite as server backup source
- P9-WP04 or later

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (preserve 915 baseline; suite now 931 / 0 / 0).
- [x] Dashboard and phase page updated.
- [x] Completion report created (`docs/reports/P9-WP03-backup-and-restore.md`).
- [x] Focused commit created and hash recorded.
- [x] Working tree clean.
- [x] Exact next WP recorded: **P9-WP04 — Accessibility, Localization and Theme QA** (do not begin).

### P9-WP04 — Accessibility, Localization and Theme QA

Status: **Complete** with documented risks

Report: [P9-WP04-accessibility-localization-theme-qa.md](../reports/P9-WP04-accessibility-localization-theme-qa.md)

Feature commit: f7b3aecec614eea8b1de601cd08e843f4aea91f8
Docs commit: a28adb46b95e8a38651a0b8f32119a06f102aae2

Phase marker: `P9-WP04-accessibility-localization-theme-qa`

#### Approved scope (clarified)

Perform a complete accessibility, localization, responsive-layout, and theme-quality review across delivered Platform and PinoyBusinessPOS MVP. **QA and hardening only — no new business features.** Preserve P9-WP01–P9-WP03 security, isolation, idempotency, immutability, and reliability controls. HealthCare remains frozen.

Deliver:

- Accessibility review and fixes (WCAG 2.2 AA engineering target; no formal certification claim)
- English and Filipino (`fil-PH`) localization reconciliation
- System, Light, and Dark theme validation and token-consistency fixes
- Phone/tablet responsive-layout validation
- Keyboard, focus, screen-reader, and touch-target checks
- Reduced-motion and contrast review
- Localization and theme regression tests
- Documented remaining limitations and release blockers
- Manual QA matrix (EN/fil × Light/Dark/System)
- Android Release evidence or honest R-109 limitation

#### Explicit exclusions

- New business workflows; production auth; POS operational roles
- Tax, refund, accounting, gateway, receipt printing, report export
- New languages beyond existing locales; new UI framework; full visual redesign
- RTL implementation (document as unsupported unless already present)
- P9-WP05 or later

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (preserve 931 baseline).
- [x] Dashboard and phase page updated.
- [x] Completion report created (`docs/reports/P9-WP04-accessibility-localization-theme-qa.md`).
- [x] Focused commit created and hash recorded.
- [x] Working tree clean.
- [x] Exact next WP recorded: **P9-WP05 — Pilot and Deployment** (do not begin).

### P9-WP05 — Pilot and Deployment

Status: Not Started

#### Required outcomes

- Implement only the approved scope described by the architecture and product documents.
- Add required tests and documentation evidence.
- Preserve security, tenant isolation and product boundaries.

#### Definition of Done

- [ ] Approved outcomes complete.
- [ ] Applicable tests pass with exact evidence.
- [ ] Dashboard and phase page updated.
- [ ] Completion report created.
- [ ] Focused commit created and hash recorded.
- [ ] Working tree clean.

### P9-WP06 — Commercial MVP Closeout

Status: Not Started

#### Required outcomes

- Implement only the approved scope described by the architecture and product documents.
- Add required tests and documentation evidence.
- Preserve security, tenant isolation and product boundaries.

#### Definition of Done

- [ ] Approved outcomes complete.
- [ ] Applicable tests pass with exact evidence.
- [ ] Dashboard and phase page updated.
- [ ] Completion report created.
- [ ] Focused commit created and hash recorded.
- [ ] Working tree clean.

## Phase exit criteria

- [ ] Every work package is complete or explicitly deferred.
- [ ] Risks and decisions are recorded.
- [ ] Required regression/security tests pass.
- [ ] Next phase is explicitly approved.
