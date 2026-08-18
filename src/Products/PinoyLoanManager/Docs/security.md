# Pinoy Loan Manager — Security and Privacy

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)

| Field | Value |
|---|---|
| Product | Pinoy Loan Manager |
| Status | Draft — documentation baseline plus agreed operating-model direction; not product-owner approved |
| Implementation present | No |

## Authentication boundary

| Item | State |
|---|---|
| Trusted actor source | Platform identity. Production login/session **Open — R-091**. |
| Production auth (JWT/MFA/SSO/…) | **Open — R-091** — do not invent fake production login |
| Dev/Testing shortcuts | Document honestly; fail closed outside approved environments (D-P12-05). No PLM-specific Dev gate is designed in this package. |

## Product authorization

- Platform product access / commercial state / entitlements: **entry gate only**
- Product-local roles and grants: **operational authority** ([authorization-matrix.md](authorization-matrix.md)) — presets recorded; granular grants **Open / Product Owner Decision Required** (PLM-D-00-06)
- Both layers must allow the action; neither bypasses the other
- Platform entitlement does not replace Loan product-local authorization

Access intersection (required intent):

```text
trusted actor
+ trusted organization context
+ Platform product access
+ valid commercial state
+ required entitlement
+ active Loan product-local role
+ required Loan product-local grant
+ resource/workflow authorization
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
- mechanism **Status: Open / Product Owner Decision Required** (PLM-D-00-05)

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
| Product audit / immutable history | Intended product-owned audit/history. Posted disbursement, payment, penalty, waiver, reversal, collector cash movement, and remittance must not be silently edited or deleted. Detail **Status: Open / Product Owner Decision Required**. |
| Platform audit | Platform-owned; do not push operational payloads that violate boundary |

## Encryption

| At rest / in transit | Approach |
|---|---|
| TLS | Production TLS remains a portfolio risk until closed. Product-specific TLS design **Status: Open / Product Owner Decision Required**. |
| Data at rest | **Status: Open / Product Owner Decision Required** (no database yet) |
| Local/offline stores | Possible later MAUI/SQLite capability. Crypto approach **Status: Open / Product Owner Decision Required**. Not authorized. |

## Input / output controls

- Validation at boundary; ProblemDetails conventions (when an API exists)
- No EF entities as API/UI DTOs
- UI projects must not reference Infrastructure, EF Core, or Npgsql
- Domain remains persistence-independent; Application must not reference Infrastructure

## Concurrency and idempotency

| Operation class | Strategy |
|---|---|
| All Loan mutating operations | **Status: Open / Product Owner Decision Required** — do not invent posting or disbursement idempotency rules yet |

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
| PLM-D-00-06 | Missing product-local grant matrix (presets recorded) | Open |
| PLM-D-00-05 | Undesigned consent/linking mechanism | Open |
| PLM-D-00-11 | Legal/compliance validation not performed | Open |

Full register: [risks-and-decisions.md](risks-and-decisions.md).
