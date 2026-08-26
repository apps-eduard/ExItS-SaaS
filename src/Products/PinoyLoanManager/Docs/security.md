# Pinoy Loan Manager — Security and Privacy

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)

| Field | Value |
|---|---|
| Product | Pinoy Loan Manager |
| Status | PLM MVP Product planning documentation complete (PLM-DOC-01–11); **PLM-D-00-10 Closed / Product Owner Accepted**; no implementation |
| Implementation present | No |

## Authentication boundary

| Item | State |
|---|---|
| Trusted actor source | Platform identity. **R-091 Closed for Phase 13 scope** — consume trusted Platform actor/context only. |
| Production auth | **R-091 Closed for Phase 13 scope** — passwords, sessions, Bearer, external login delivered; residuals (MFA enforcement, step-up, enterprise SSO/AD, outbound auth email) are separate gates |
| Dev/Testing shortcuts | Document honestly; fail closed outside approved environments (**D-P12-05 Closed / satisfied for authentication honesty**). No PLM-specific Dev gate is designed in this package. |

## Product authorization

- Platform product access / commercial state / entitlements: **entry gate only**
- Product-local roles and grants: **operational authority** ([authorization-matrix.md](authorization-matrix.md), [Security/authorization-grant-catalog.md](Security/authorization-grant-catalog.md)) — **PLM Authorization Policy v1**; **PLM-D-00-06 Closed for MVP**
- No implicit role hierarchy; no client-only authorization
- Resource / branch / assignment / session **scope** is a required layer
- Both layers must allow the action; neither bypasses the other
- Platform entitlement does not replace Loan product-local authorization

Access intersection (required intent):

```text
Authenticated Actor
+ Trusted Organization Context
+ Platform Product Access
+ Allowed Commercial State
+ Required Entitlement
+ Active PLM Role Assignment
+ Required PLM Grant
+ Valid Resource Scope
+ Valid Workflow State
+ Domain Invariants
= Authorized Action
```

## Organization isolation

- Org scope validated server-side (when implemented)
- Cross-org: conceal (Product Foundation default 404)
- Org id stored as Guid reference — no cross-DB FK to Platform
- No POS org-operational data access

## Consent and Personal linking

Optional Personal-to-Borrower linking, if implemented later:

- EX ID / QR resolution identifies only
- resolution alone never links
- explicit Personal consent is required before activating a relationship
- a borrower may exist without an ExItS Personal account
- MVP: organization-initiated link request (Owner/Manager grants); Personal self-claim not MVP
- product behavior and contract **Closed for PLM requirements** (**PLM-D-00-05**); Platform transport/persistence **PLM-D-00-04** external

Lifecycle and unlink: [Product/personal-linking-lifecycle-and-visibility.md](Product/personal-linking-lifecycle-and-visibility.md). Boundary: [Architecture/personal-integration-boundary.md](Architecture/personal-integration-boundary.md).

## Data classification

| Class | In scope? | Handling |
|---|---|---|
| PHI | **No** (default) | Not authorized. Do not add unless explicitly designed later. |
| PII | Expected later / not present | Classification and retention **architecture accepted** (**ADR-016**); numeric legal retention periods remain **PLM-D-00-11** |
| Operational financial | Intended later / not present | Remains in the Loan product database when implemented (**PLM-D-00-07 Closed for MVP policy**). Loan ledger and collector cash are separate facts. Never in Platform SaaS billing. |
| Secrets / credentials | Never in git | No PLM secret store is implemented. |

## Secrets

- [x] No secrets in source or docs (this package)
- [ ] Config via environment / secret store: **implementation/Production work** (Product Foundation direction documented)

## Logging and audit

| Concern | Approach |
|---|---|
| Application logs | **Required by Product Foundation** — tenant/product/org/correlation-aware observability; exact tooling deferred to implementation; no secrets/card/PHI dumps |
| Product audit / immutable history | Intended product-owned append-only operational history. Posted disbursement, payment, penalty, waiver, reversal, collector cash movement, remittance, and cash-variance records must not be silently edited or deleted. High-risk fields: [Security/role-and-grant-baseline.md](Security/role-and-grant-baseline.md), [Security/audit-and-history-baseline.md](Security/audit-and-history-baseline.md). Subledger principles: [Architecture/loan-ledger-and-balance-model.md](Architecture/loan-ledger-and-balance-model.md). Persistence schema is **implementation work**. |
| Platform audit | Platform-owned; do not push operational payloads that violate boundary |

## Encryption

| At rest / in transit | Approach |
|---|---|
| TLS | Portfolio/hosted deployment requirement; product-specific TLS wiring **implementation work** |
| Data at rest | **Implementation/Production work** (no database yet) |
| Local/offline stores | MVP: read-only cache and offline drafts in planning only; LocalStore not authorized; offline final posting deferred. Future crypto/device requirements: [Security/collector-device-security-policy.md](Security/collector-device-security-policy.md). See [Architecture/mobile-and-offline-operating-model.md](Architecture/mobile-and-offline-operating-model.md), [Architecture/mobile-offline-boundary.md](Architecture/mobile-offline-boundary.md). |

## Input / output controls

- Validation at boundary; ProblemDetails conventions (when an API exists)
- No EF entities as API/UI DTOs
- UI projects must not reference Infrastructure, EF Core, or Npgsql
- Domain remains persistence-independent; Application must not reference Infrastructure

## Concurrency and idempotency

| Operation class | Strategy |
|---|---|
| All Loan mutating operations | **Requirement recorded:** protect financial commands against duplicate submission (collector double-tap, mobile retry, API retry) via future idempotency / correlation. Especially: Disbursement, Payment, Float Transfer, Remittance, Penalty Waiver, Reversal. Implementation **not** designed. Detail: [Product/payment-and-allocation-model.md](Product/payment-and-allocation-model.md), [Product/disbursement-and-payment-controls.md](Product/disbursement-and-payment-controls.md). |

## Backup / restore

- Product DB backup independent of Platform DB (when a database exists)
- Independent of PinoyBusinessPOS backup
- Procedure remains **implementation/Production operations work** (Gate E)
- Destructive restore guards required for operator tools

## Production security risks (register)

| ID | Risk | Status |
|---|---|---|
| R-091 | Production authentication | **Closed for Phase 13 scope** — residuals (MFA, step-up, SSO/email) do not reopen |
| D-P12-03 | Commercial-state transport; risk of inventing Platform table reads or copying POS Dev headers as production design | Open |
| D-P12-05 | Dishonest Dev/Testing vs Production language | **Closed / satisfied for authentication honesty** |
| PLM-D-00-06 | Product-local grant catalog | **Closed for MVP** — PLM Authorization Policy v1 |
| PLM-D-00-05 | Personal linking Platform implementation | **Closed for PLM contract** — Platform transport external |
| PLM-D-00-11 | Legal/compliance validation not performed | Open |
| PLM-D-00-12 | Money rounding | **Closed** — To Even; PHP 2 dp; ≥8 intermediate |
| PLM-D-00-13 | High-risk maker/checker vs small-org Owner Override | **Closed** — distinct approver when another eligible user exists; controlled Owner Override for sole eligible Owner |

Full register: [risks-and-decisions.md](risks-and-decisions.md).
