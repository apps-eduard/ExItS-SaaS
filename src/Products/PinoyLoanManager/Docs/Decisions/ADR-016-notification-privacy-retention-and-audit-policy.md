# ADR-016 — Notification, privacy, retention, and audit policy

**Status:** Accepted product policy (PLM-DOC-08); not implemented
**Date:** 2026-08-19

---

## Context

PLM needed Product Owner rules for notification direction, delivery safety, data classification, retention architecture, audit coverage, and privacy/support boundaries. Prior baselines listed notification intent and high-risk history but left classification, retention schedule, notification provider, and full audit catalog open.

---

## Decision

1. **Notifications** — Primary customer channel is ExItS Personal in-app where linked. Optional future channels: SMS, email, push, printed notice. Provider choice deferred. Delivery does not change authoritative financial state; retries must not duplicate financial actions.
2. **Event catalog** — Approve MVP notification events for Personal and staff (linking, origination, disbursement, payment, delinquency, settlement, refund, collections, operational alerts).
3. **Safety** — Minimize sensitive data on insecure channels; link to authenticated detail; no cross-lender disclosure; no debt disclosure to unrelated contacts; record delivery attempts.
4. **Classification** — Approve PUBLIC, INTERNAL, CONFIDENTIAL, HIGHLY SENSITIVE classes with role/scope minimization.
5. **Retention** — Policy-driven retention architecture: no deletion of active obligations on request alone; financial history preserved while obligations/disputes/audit/legal hold exist; configurable periods from qualified guidance; audited disposal; legal hold; backup expiry; unlink does not erase Loan history. Do not invent numeric legal periods.
6. **Audit** — High-integrity audit covers identity/linking, origination, financial terms, disbursement, payments, refunds/settlement, penalties, restructuring, write-off/recovery, role assignments, Owner Override, cash operations, document generation, and privileged support. Audit is not editable notes; audit access is itself audited where appropriate.
7. **Privacy/support** — No cross-lender or unrelated product visibility; Platform support has no automatic PLM operational access; exports authorized and scoped.

Canonical: [../Product/notification-and-delivery-policy.md](../Product/notification-and-delivery-policy.md), [../Security/privacy-retention-and-audit-policy.md](../Security/privacy-retention-and-audit-policy.md).

---

## Consequences

Notification direction, classification, retention architecture, and audit coverage are approved for MVP planning.

**Still open:** notification provider integration, exact retention durations (PLM-D-00-11), legal/compliance validation (PLM-D-00-11), implementation.
