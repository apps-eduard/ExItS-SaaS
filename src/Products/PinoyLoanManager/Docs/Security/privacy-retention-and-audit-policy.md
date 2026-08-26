# Pinoy Loan Manager — Privacy, Retention, and Audit Policy

**Status:** Accepted product policy (PLM-DOC-08); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Data classification, retention architecture, audit coverage, and privacy/support boundaries. Not a schema design, SIEM specification, or legal retention schedule.

**Canonical companions:** [../security.md](../security.md), [audit-and-history-baseline.md](audit-and-history-baseline.md), [resource-scope-and-data-minimization-policy.md](resource-scope-and-data-minimization-policy.md), [privileged-access-and-owner-recovery-policy.md](privileged-access-and-owner-recovery-policy.md), [../Product/personal-linking-lifecycle-and-visibility.md](../Product/personal-linking-lifecycle-and-visibility.md). ADR: [../Decisions/ADR-016-notification-privacy-retention-and-audit-policy.md](../Decisions/ADR-016-notification-privacy-retention-and-audit-policy.md).

---

## Data classification

PLM data is classified conceptually as follows. Role/scope/data minimization applies per [resource-scope-and-data-minimization-policy.md](resource-scope-and-data-minimization-policy.md).

| Class | Examples | Handling |
|---|---|---|
| **PUBLIC** | Approved public product/offer information published to eligible audiences | May appear in marketing/publishing surfaces; no borrower-specific data |
| **INTERNAL** | Organization configuration; ordinary operational metadata; non-customer workflow state | Organization-scoped; staff access by grant |
| **CONFIDENTIAL** | Borrower contact/profile; Loan balances; schedules; payments; collection notes; operational reports | Strict grant + scope; minimized in UI, exports, logs, and notifications |
| **HIGHLY SENSITIVE** | Identity-document images/numbers; income/affordability records; authentication/security data; privileged audit evidence; financial correction evidence | Highest restriction; enhanced audit; no unnecessary logging or notification exposure |

PHI is **not** in scope by default. Do not add PHI handling unless explicitly designed later.

Classification direction is **approved**. Exact field-level mapping to classes is an implementation concern guided by this policy.

---

## Privacy boundaries

Preserve:

- **No cross-lender visibility** — one Organization cannot see another Organization's Borrower/Loan data
- **No unrelated POS/product data** — PLM does not read PinoyBusinessPOS or other product operational tables
- **Personal boundary** — Personal uses PLM APIs only; never PLM tables directly
- **Unlink does not erase history** — revoking Personal link changes access/relationship only; Loan and payment history remain in PLM per retention rules
- **Export authorization** — exports require explicit grant and scope; export events are auditable

Platform support has **no automatic PLM operational access**. Privileged support requires explicit recovery/support workflow with enhanced audit.

---

## Retention architecture

Retention is **policy-driven**. Do **not** invent numeric legal retention periods in this package.

### Approved principles

| Principle | Rule |
|---|---|
| Active records | Active Loan/Borrower records cannot be deleted merely on user request while obligations exist |
| Financial history | Remains while obligations, disputes, audit requirements, legal hold, or configured retention requirements exist |
| Configuration | Retention periods must be configured from qualified legal/records guidance (PLM-D-00-11) |
| Disposal | Expired records may be securely deleted or anonymized where permitted; disposal is audited |
| Legal hold | Legal hold suspends disposal regardless of default retention |
| Backups | Backups follow their own controlled expiry; restore must respect legal hold |
| Personal unlink | Unlinking Personal does **not** delete Loan history |

Exact retention **durations** remain **Open** pending qualified legal/records input (PLM-D-00-11).

---

## Audit coverage

High-integrity audit must cover the following. Audit records are **not** an editable notes table.

| Domain | Actions |
|---|---|
| Identity / linking | Personal link request, consent, decline, revoke, suspend, identity correction |
| Origination | Applications, assessment facts presented, approval/rejection, material revision |
| Financial terms | Term snapshots, schedule versions, policy versions |
| Disbursement | Release, cancellation, reversal |
| Payments | Payment posting, allocation, reversal |
| Refunds / settlement | Cash Refund, Refund Payable, Settlement Quote, settlement, principal prepayment |
| Penalties / exceptions | Penalty assessment, waiver, exception, reversal |
| Lifecycle | Restructuring, Write-Off, Recovery |
| Authorization | Role assignments, grant changes, scope changes |
| Privileged access | Owner Override usage |
| Cash operations | Cashier sessions, float, remittance, variance, resolution |
| Documents | Authoritative document generation, reprint/copy |
| Support / recovery | Privileged support/recovery actions |

### Audit record expectations (when implemented)

High-risk fields include: actor, organization, branch, time, action, target resource, amount where relevant, reason where required, approval actor where applicable, correlation/reference, original transaction reference for reversals, device/channel where useful.

### Audit access

- Audit view follows grants and scope.
- Collectors do not receive default unrestricted audit browse.
- **Audit access is itself audited** where appropriate.
- Platform audit remains Platform-owned; do not push operational payloads that violate product boundary.

This policy **extends** [audit-and-history-baseline.md](audit-and-history-baseline.md) with approved coverage catalog.

---

## Logging minimization

Application logs must avoid unnecessary sensitive data (full identity numbers, complete payment instrument data, verbose borrower PII). Logging approach remains implementation-defined; the **requirement** is approved.

---

## Support and privileged access

| Rule | Detail |
|---|---|
| Platform support | No automatic PLM operational browse |
| Privileged support | Explicit workflow; enhanced audit; time-bound where possible |
| Owner recovery | Platform emergency Owner recovery boundary per [privileged-access-and-owner-recovery-policy.md](privileged-access-and-owner-recovery-policy.md) |
| Exports | Authorized, scoped, auditable |

---

## Legal / compliance boundary

| Item | Status |
|---|---|
| Classification model approved | Yes |
| Retention architecture (no numeric periods) approved | Yes |
| Audit coverage list approved | Yes |
| Privacy/support boundaries approved | Yes |
| Legal/compliance validation | **Open** (PLM-D-00-11) |
| Exact retention durations | **Open** (PLM-D-00-11) |
| Implemented | **No** |

---

## Honesty gates

| Claim | Allowed? |
|---|---|
| Data classification PUBLIC/INTERNAL/CONFIDENTIAL/HIGHLY SENSITIVE approved | Yes |
| Retention architecture approved (no invented periods) | Yes |
| Audit coverage catalog approved | Yes |
| Legally compliant retention schedule | **No** (PLM-D-00-11 Open) |
| Production privacy certification | **No** |
| Implemented | **No** |
