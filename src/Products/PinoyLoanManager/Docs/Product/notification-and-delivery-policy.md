# Pinoy Loan Manager — Notification and Delivery Policy

**Status:** Accepted product policy (PLM-DOC-08); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Customer and staff notification direction, channels, events, and delivery safety. Not a provider selection, template copy pack, or delivery-SLA specification.

**Canonical companions:** [notification-model.md](notification-model.md), [personal-loan-experience.md](personal-loan-experience.md), [personal-linking-lifecycle-and-visibility.md](personal-linking-lifecycle-and-visibility.md), [document-and-receipt-policy.md](document-and-receipt-policy.md). ADR: [../Decisions/ADR-016-notification-privacy-retention-and-audit-policy.md](../Decisions/ADR-016-notification-privacy-retention-and-audit-policy.md).

---

## Core principle

**Notification delivery does not change authoritative financial state.**

Posted Disbursement, Payment, Settlement, Refund, Write-Off, and other financial events remain valid regardless of notification success or failure. Retries must not create duplicate financial actions.

---

## Primary customer channel

**ExItS Personal in-app notification** is the primary future customer channel where a linked Personal/Borrower relationship exists and consent permits delivery.

Unlinked Borrowers rely on organization-operated channels (in-person, print, SMS/email where configured) according to organization policy.

---

## Optional future channels

Additional channels may be supported per Organization configuration:

| Channel | Notes |
|---|---|
| SMS | Provider choice **deferred** |
| Email | Provider choice **deferred** |
| Push notification | Mobile/platform push where Personal or MAUI supports it |
| Printed notice | Physical delivery; not SoR |

No SMS/email/push **provider** is selected in this package. Product **direction** for optional channels is approved; provider integration remains **Open**.

---

## Notification events (MVP plan)

### Personal (linked, authorized)

| Event | Purpose |
|---|---|
| Personal link request | Consent workflow |
| Quick Loan offer | Published eligible offer |
| Request submitted | Application/request acknowledgment |
| Approval / rejection | Decision delivery |
| Approval expiry | Expired approval before Disbursement |
| Ready for release | Awaiting Disbursement |
| Disbursement | Funds released |
| Payment posted | Payment confirmation |
| Receipt available | Document/receipt ready |
| Due reminder | Upcoming obligation |
| Past Due | Delinquency notice |
| Penalty Assessment / Waiver | Where appropriate and authorized |
| PTP reminder | Promise-to-pay follow-up |
| Restructuring proposal / approval | Lifecycle change |
| Settlement quote | Quote issued |
| Settlement | Loan settled |
| Refund available / paid | Refund Payable or cash refund |

### Organization staff

| Event | Purpose |
|---|---|
| Pending approvals | Workflow queue |
| Pending disbursements | Release queue |
| Collection exceptions | Exception/waiver queue |
| Variance | Unresolved cash variance |
| Reconciliation | Session/remittance issues |
| Overdue portfolio alerts | Manager/Owner operational alert |

Staff delivery is scoped by grants and assignment. Collectors do not receive unrestricted org-wide financial notifications by default.

Personal must **not** receive another lender's private operational data.

---

## Delivery behavior

- Record delivery attempt and status (conceptual; implementation deferred).
- Support retry without duplicate financial posting (idempotent notification dispatch).
- Failed delivery must **not** roll back posted financial events.
- Link to authenticated detail in Personal or Org Web where appropriate instead of embedding full sensitive content in insecure channels.

---

## Notification safety and minimization

Notifications must:

- **minimize sensitive data** — avoid full account numbers, identity document details, or complete balance breakdowns on insecure channels by default
- **link to authenticated detail** where appropriate (Personal or authorized Org session)
- **record delivery attempt/status** for operational follow-up
- **support retry** without duplicate financial action
- **not disclose** one lender's data to another
- **not disclose debt** to unrelated contacts or third parties through the system
- **respect consent/preferences** where applicable (channel opt-out, link status)

Message copy and legal wording remain subject to PLM-D-00-11. Do not claim legally sufficient collection or disclosure language.

---

## Relationship to prior planning baseline

This policy **supersedes** open notification-direction items in [notification-model.md](notification-model.md). Provider selection remains **Open**.

---

## Honesty gates

| Claim | Allowed? |
|---|---|
| Personal primary channel and optional SMS/email/push direction approved | Yes |
| Delivery does not change financial state | Yes |
| Notification event catalog approved for MVP planning | Yes |
| Provider selected or integrated | **No** |
| Legally sufficient message content | **No** (PLM-D-00-11 Open) |
| Implemented | **No** |
