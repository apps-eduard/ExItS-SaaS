# Phase 7 — Offline Synchronization

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Previous](phase-06-utang-mvp.md) | [Next](phase-08-basic-store.md)

## Objective

Deliver safe offline-first operation and synchronization.

## Status

**In Progress** — P7-WP01 complete with documented risks. Do **not** begin P7-WP02 until explicitly authorized.

Authoritative design: [offline-sync-design.md](../engineering/offline-sync-design.md)

Report: [P7-WP01-sqlite-and-device-identity.md](../reports/P7-WP01-sqlite-and-device-identity.md)

## Work packages

### P7-WP01 — SQLite and Device Identity

Status: **Complete with documented risks**

Phase marker: `P7-WP01-sqlite-and-device-identity`

Feature commit: _(recorded after push)_

#### Approved scope (foundation only)

- SQLite local-store infrastructure and schema migrations (`Microsoft.Data.Sqlite`)
- Per-user / per-organization / per-product database isolation (hashed filenames)
- Durable DeviceId via SecureStorage (`IDeviceIdentityProvider`)
- Local-context open/close lifecycle after online access validation
- Persistent sync-status shell indicator (Online / Offline / Reconnect only)
- Development/Testing diagnostics `/dev/offline-foundation`
- Tests, Android Release APK, documentation, Git evidence

**Does not enable offline business operations.**

#### Explicit exclusions (P7-WP02+)

Offline queue/outbox, idempotency processing, business-data cache, offline mutations, sync workers, conflict resolution, server device registration, entitlement snapshot cache, offline grace window, pending-op retention (OD-10), SQLCipher for business data, sales/inventory/gateways.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence (563 passed / 0 failed / 0 skipped).
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [ ] Focused commit created and hash recorded.
- [ ] Working tree clean.

### P7-WP02 — Offline Queue and Idempotency

Status: Not Started — **do not begin**

#### Required outcomes

- Offline mutation queue/outbox with explicit sync states
- Idempotency keys and duplicate-safe processing
- Retryable vs permanent failure handling
- Wire Pending Sync / Syncing / Sync Failed / Last Synced shell states
- Add required tests and documentation evidence
- Preserve security, tenant isolation and product boundaries

#### Definition of Done

- [ ] Approved outcomes complete.
- [ ] Applicable tests pass with exact evidence.
- [ ] Dashboard and phase page updated.
- [ ] Completion report created.
- [ ] Focused commit created and hash recorded.
- [ ] Working tree clean.

### P7-WP03 — Customer and Credit Sync

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

### P7-WP04 — Payment Sync and Recovery

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

### P7-WP05 — Offline Closeout

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
