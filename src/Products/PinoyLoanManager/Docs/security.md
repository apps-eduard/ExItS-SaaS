# Pinoy Loan Manager — Security and Privacy

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)

| Field | Value |
|---|---|
| Product | Pinoy Loan Manager |
| Status | PLM-00 baseline accepted (PLM-D-00-10); PLM-DOC-01 linking rules recorded; no implementation |
| Implementation present | No |

## Authentication boundary

| Item | State |
|---|---|
| Trusted actor source | Platform identity. Production login/session **Open — R-091**. |
| Production auth (JWT/MFA/SSO/…) | **Open — R-091** — do not invent fake production login |
| Dev/Testing shortcuts | Document honestly; fail closed outside approved environments (D-P12-05). No PLM-specific Dev gate is designed in this package. |

## Product authorization

- Platform product access / commercial state / entitlements: **entry gate only**
- Product-local roles and grants: **operational authority** ([authorization-matrix.md](authorization-matrix.md), [Security/role-and-grant-baseline.md](Security/role-and-grant-baseline.md)) — presets and grant **intent** recorded; identifiers **Open** (PLM-D-00-06)
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
+ Active PLM Product Role
+ Required PLM Grant
+ Resource / Branch / Workflow Scope
= Authorized Operational Action
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
- product behavior defined (PLM-DOC-01); Platform transport/persistence/integration **Open** (PLM-D-00-05)

Lifecycle and unlink: [Product/personal-linking-lifecycle-and-visibility.md](Product/personal-linking-lifecycle-and-visibility.md). Boundary: [Architecture/personal-integration-boundary.md](Architecture/personal-integration-boundary.md).

## Data classification

| Class | In scope? | Handling |
|---|---|---|
| PHI | **No** (default) | Not authorized. Do not add unless explicitly designed later. |
| PII | Expected later / not present | Handling, retention, and minimization **Status: Open / Product Owner Decision Required**. |
| Operational financial | Intended later / not present | Remains in the Loan product database when defined (PLM-D-00-07). Loan ledger and collector cash are separate facts. Never in Platform SaaS billing. |
| Secrets / credentials | Never in git | No PLM secret store is implemented. |

## Secrets

- [x] No secrets in source or docs (this package)
- [ ] Config via environment / secret store: **Status: Open / Product Owner Decision Required**

## Logging and audit

| Concern | Approach |
|---|---|
| Application logs | **Status: Open / Product Owner Decision Required** — no secrets/card/PHI dumps |
| Product audit / immutable history | Intended product-owned append-only operational history. Posted disbursement, payment, penalty, waiver, reversal, collector cash movement, remittance, and cash-variance records must not be silently edited or deleted. High-risk fields: [Security/role-and-grant-baseline.md](Security/role-and-grant-baseline.md), [Security/audit-and-history-baseline.md](Security/audit-and-history-baseline.md). Subledger principles: [Architecture/loan-ledger-and-balance-model.md](Architecture/loan-ledger-and-balance-model.md). Schema **Open**. |
| Platform audit | Platform-owned; do not push operational payloads that violate boundary |

## Encryption

| At rest / in transit | Approach |
|---|---|
| TLS | Production TLS remains a portfolio risk until closed. Product-specific TLS design **Status: Open / Product Owner Decision Required**. |
| Data at rest | **Status: Open / Product Owner Decision Required** (no database yet) |
| Local/offline stores | Possible later MAUI/SQLite capability. Crypto approach **Status: Open / Product Owner Decision Required**. Not authorized. See [Architecture/mobile-offline-boundary.md](Architecture/mobile-offline-boundary.md). |

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
- Procedure **Status: Open / Product Owner Decision Required**
- Destructive restore guards required for operator tools

## Production security risks (register)

| ID | Risk | Status |
|---|---|---|
| R-091 | Production authentication | Open |
| D-P12-03 | Commercial-state transport; risk of inventing Platform table reads or copying POS Dev headers as production design | Open |
| D-P12-05 | Dishonest Dev/Testing vs Production language | Open |
| PLM-D-00-06 | Missing product-local grant identifiers (presets and intent recorded) | Open |
| PLM-D-00-05 | Undesigned consent/linking **transport** (product behavior defined) | Open |
| PLM-D-00-11 | Legal/compliance validation not performed | Open |
| PLM-D-00-12 | Money rounding | **Closed** — To Even; PHP 2 dp; ≥8 intermediate |

Full register: [risks-and-decisions.md](risks-and-decisions.md).
