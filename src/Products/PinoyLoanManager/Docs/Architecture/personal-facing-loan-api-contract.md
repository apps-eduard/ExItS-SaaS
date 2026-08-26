# Pinoy Loan Manager — Personal-Facing Loan API Contract

**Status:** Accepted product contract requirements (PLM-DOC-10); routes and OpenAPI **not** defined
**Implementation present:** No
**Last updated:** 2026-08-19

Customer-facing **operations** ExItS Personal may invoke against Pinoy Loan Manager through approved PLM APIs. This is a contract checklist, not an endpoint catalog.

**Personal must never read PLM database tables.** All access is server-authoritative, organization-scoped, and filtered to the authenticated Personal identity and its permitted Borrower link(s).

Related: [personal-integration-boundary.md](personal-integration-boundary.md), [personal-link-and-consent-contract.md](personal-link-and-consent-contract.md), [../Product/personal-loan-experience.md](../Product/personal-loan-experience.md), [api-and-contract-boundary.md](api-and-contract-boundary.md), [../Decisions/ADR-019-platform-personal-contract-requirements.md](../Decisions/ADR-019-platform-personal-contract-requirements.md).

---

## Access prerequisites

Every operation assumes:

1. Valid Platform-authenticated Personal actor ([platform-access-context-contract.md](platform-access-context-contract.md))
2. Active or permitted Personal ↔ Borrower relationship for the target Organization ([personal-link-and-consent-contract.md](personal-link-and-consent-contract.md)), except where noted for consent prompts
3. PLM commercial gate satisfied for the Organization (**D-P12-03 Open** for transport of commercial facts)
4. Operation allowed by current link state (for example, blocked Personal-delivered Quick Loan actions after revoke)

Unlinked Borrowers may still exist in PLM for organization-operated workflows; they are **out of scope** for Personal-facing reads except pending consent UX owned by Platform/Personal.

---

## Required customer operations

Logical operation groups. Final naming, pagination, and versioning are implementation work.

### Lender relationships

| Operation | Purpose |
|---|---|
| List my lender relationships | Organizations where Personal has Linked (or historically linked per visibility rules) Borrower context |
| Get lender relationship summary | Organization display name, link state, blocking flags |

### Quick Loan discovery and requests

| Operation | Purpose |
|---|---|
| List available Quick Loan offers | Published templates eligible for linked Borrower / group targeting |
| Get Quick Loan offer detail | Snapshot terms preview; eligibility ≠ approval |
| Submit Quick Loan request | Creates PLM request from Personal; requires Linked state |
| List my Quick Loan requests | Submitted requests for linked Borrower context |
| Get Quick Loan request status | Approval / rejection / pending states |

### Traditional and shared application visibility

| Operation | Purpose |
|---|---|
| List my loan applications | Traditional and Quick paths converging to shared core |
| Get application status | Including rejection reason where policy permits customer display |

### Active and historical loans

| Operation | Purpose |
|---|---|
| List my loans | Active and settled Loans for linked Borrower context |
| Get loan summary | Balances, status, maturity, delinquency indicators permitted for customer display |
| Get loan schedule | Installment schedule from PLM authoritative state |
| Get payment history | Posted payments; append-only history |
| Get receipt | Durable receipt identity independent of print success |

### Notifications and documents

| Operation | Purpose |
|---|---|
| List loan notifications | Personal-channel notifications sourced from PLM events |
| Get loan document metadata | Authorized document references; binary delivery mechanism TBD |
| Acknowledge notification read state | Optional; must not roll back financial events |

### Consent-adjacent reads (Personal hub)

| Operation | Purpose |
|---|---|
| List pending link consent requests | Delivered through Platform/Personal; PLM may expose request metadata |
| Get pending link consent detail | Minimum context to accept/decline |

Accept/decline/revoke link operations belong to [personal-link-and-consent-contract.md](personal-link-and-consent-contract.md), not this loan-data contract.

---

## Authorization and data boundaries

| Rule | Requirement |
|---|---|
| Table access | **Forbidden** — Personal never queries PLM DB |
| Cross-organization reads | **Forbidden** |
| Cross-lender leakage | **Forbidden** |
| Authoritative money | PLM server state only; Personal cache is non-authoritative |
| P2P Personal lending | **Separate** — not merged with organizational PLM Loans |
| Write scope | Personal-facing writes limited to consented actions (requests, consent responses via link contract); no staff operations |

DTOs must not expose EF entities or internal staff-only fields.

---

## Idempotency and safety

Customer-initiated writes that create financial or request records must support:

- idempotency key or equivalent correlation identity
- safe retry semantics
- auditable duplicate rejection

Aligns with portfolio async/idempotency direction; exact key format is implementation work.

---

## Explicit non-goals

- OpenAPI / route design
- Authentication mechanism (**R-091 Closed for Phase 13 scope**; transport **D-P12-03 Open**)
- Platform relationship schema (**PLM-D-00-04 Open**)
- Staff / Organization Web / MAUI operational APIs (separate surfaces)
- Closing **D-P12-03** transport
