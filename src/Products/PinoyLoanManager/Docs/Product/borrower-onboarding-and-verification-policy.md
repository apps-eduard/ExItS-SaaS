# Pinoy Loan Manager — Borrower Onboarding and Verification Policy

**Status:** Accepted product policy (PLM-DOC-07); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

MVP natural-person Borrower profile minimum, identity verification, status lifecycle, and data minimization. Not complete KYC/legal compliance.

**Canonical companions:** [borrower-model.md](borrower-model.md), [borrower-identity-and-duplicate-policy.md](borrower-identity-and-duplicate-policy.md), [personal-linking-lifecycle-and-visibility.md](personal-linking-lifecycle-and-visibility.md). ADR: [../Decisions/ADR-013-borrower-onboarding-and-application-minimums.md](../Decisions/ADR-013-borrower-onboarding-and-application-minimums.md).

---

## Borrower type (MVP)

PLM MVP supports **natural-person Borrowers** only.

Business/entity Borrowers are **deferred** to a future separately approved product version.

A natural-person Borrower may remain **unlinked** from ExItS Personal.

---

## Minimum Borrower profile

MVP Borrower requires:

- full legal name
- date of birth
- primary contact method
- primary residential/contact address
- identity-verification status
- at least one organization-approved identity-verification method
- organization
- operational Branch where applicable
- borrower status
- privacy/notice acknowledgment record
- created/updated/audit metadata

Do **not** claim this is complete KYC/legal compliance (PLM-D-00-11 Open).

Approved identity document types and required evidence remain **configurable** and subject to legal/compliance review.

---

## Optional / configurable profile data

May include:

- alternate contact
- email
- employment/business information
- income/source-of-funds information
- references
- emergency contact
- household information
- supporting documents
- organization-specific borrower number
- route/area
- notes

Sensitive fields require role-based minimization per [../Security/resource-scope-and-data-minimization-policy.md](../Security/resource-scope-and-data-minimization-policy.md).

---

## Borrower status (conceptual)

- Prospect
- Active
- Suspended
- Blocked
- Inactive
- Deceased
- Archived

Do not finalize code enum.

Suspension/blocking prevents new applications as configured. It does **not** delete or erase existing Loans.

---

## Verification

Identity verification status and method are organization-controlled. Verification completion is required before Disbursement readiness (see [approval-revision-and-disbursement-readiness-policy.md](approval-revision-and-disbursement-readiness-policy.md)).

Identity corrections are high-risk per PLM-DOC-05 (`plm.personal-links.correction-request` / `correction-approve`).

---

## Honesty gates

| Claim | Allowed? |
|---|---|
| MVP Borrower minimum categories approved | Yes |
| Legally sufficient KYC | **No** (PLM-D-00-11 Open) |
| Implemented | **No** |
