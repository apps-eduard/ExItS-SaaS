# PLM-DOC-08 — Documents, Reporting, Privacy & Notifications

**Status:** Documentation package complete (planning only)
**Implementation present:** No
**Last updated:** 2026-08-19

---

## Scope

Finalize MVP authoritative document catalog, document/receipt identity, receipts for posted financial events, account statements, template versioning, operational KPI formulas, PAR and aging buckets, notification direction, data classification, retention architecture, audit coverage, and privacy/support boundaries.

**Out of scope:** code, legal sufficiency claims, notification provider selection, numeric retention periods, statutory accounting certification.

---

## Delivered

| Doc | Subject |
|---|---|
| [document-and-receipt-policy.md](../Product/document-and-receipt-policy.md) | Document types, identity, receipts, statements |
| [reporting-kpi-and-aging-policy.md](../Product/reporting-kpi-and-aging-policy.md) | KPI formulas, PAR, aging, report catalog |
| [notification-and-delivery-policy.md](../Product/notification-and-delivery-policy.md) | Channels, events, delivery safety |
| [privacy-retention-and-audit-policy.md](../Security/privacy-retention-and-audit-policy.md) | Classification, retention, audit, privacy |
| ADR-015, ADR-016 | Decision records |

---

## Resolved product directions

- Authoritative document catalog
- Durable receipt identity for Payment, Disbursement, Cash Refund, Settlement, Principal Prepayment, Recovery
- Template versioning and snapshot behavior
- Account statement component breakdown
- GROSS OUTSTANDING PRINCIPAL, PAST-DUE SCHEDULED AMOUNT, COLLECTION RATE, PAR-X formulas
- PAR 1 / 7 / 30 / 60 / 90 standard views
- Aging buckets Current / 1–7 / 8–30 / 31–60 / 61–90 / 91+
- Scope-filtered operational report catalog
- Personal primary notification channel; optional SMS/email/push direction
- Delivery does not change financial state
- Data classification PUBLIC / INTERNAL / CONFIDENTIAL / HIGHLY SENSITIVE
- Retention architecture (policy-driven; no invented periods)
- Audit coverage catalog
- Privacy and support boundaries

---

## Remains open

| Item | ID / note |
|---|---|
| Legal sufficiency of documents, disclosures, notifications | PLM-D-00-11 |
| Exact legally mandated receipt/document format | PLM-D-00-11 |
| Numeric retention durations | PLM-D-00-11 |
| Notification provider / integration | Product rule resolved; provider Open |
| GL / statutory reporting | PLM-D-00-07 remainder |
| Implementation | Future authorized work |

**PLM-D-00-11 remains Open.**

---

## No-code statement

Documentation only. Implementation paused. Parked scaffold unmerged.

---

## Exact next

**PLM-DOC-09 — MAUI Field Operations, Offline Boundary, Routes, Device Security, Branch Treasury & UI Sharing**
