# Phase 9 — MVP Hardening and Release

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-08-basic-store.md) | [Next](phase-10-full-pos.md)

## Status

**In Progress** — P9-WP01 complete with documented risks. Phase 8 accepted complete at `4a9ed5c4ac5ccaa7d96f04bfc68b9950b0ab1c79`. Do **not** begin P9-WP02 until explicitly authorized. **Not production-ready.**

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

Status: Not Started (do not begin until authorized)

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

### P9-WP03 — Backup and Restore

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

### P9-WP04 — Accessibility, Localization and Theme QA

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
