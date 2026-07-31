# ReferenceLoan — Security and Privacy

> **FICTIONAL** P12-WP06. Foundation: [exits-product-foundation-reference.md](../exits-product-foundation-reference.md)

| Field | Value |
|---|---|
| Product | ReferenceLoan |
| Status | Draft — fictional validation only |

## Authentication boundary

| Item | State |
|---|---|
| Trusted actor source | Platform-trusted actor when available; Dev/Testing patterns only until R-091 closes |
| Production auth (JWT/MFA/SSO/…) | **Open — R-091** — do not invent fake production login |
| Dev/Testing shortcuts | Document honestly; fail closed outside approved environments |

## Product authorization

- Platform product access / commercial state / entitlements: **entry gate only**
- Product-local roles and grants: **operational authority** (`authorization-matrix.md`)
- Both layers must allow the action; neither bypasses the other

## Organization isolation

- Org scope validated server-side
- Cross-org: 404 concealment
- Org id stored as Guid reference — no cross-DB FK to Platform

## Data classification

| Class | In scope? | Handling |
|---|---|---|
| PHI | **No** (default) | Not authorized for this fictional product |
| PII | Yes | Minimize; no logging of raw identity dumps |
| Operational financial | Yes | Product DB only; not Platform SaaS payment tables |
| Secrets / credentials | Never in git | Env / secret store |

## Secrets

- [x] No secrets in source or docs
- [x] Config via environment / secret store: portfolio conventions when implemented

## Logging and audit

| Concern | Approach |
|---|---|
| Application logs | Safe metadata only — no secrets/card/PHI dumps |
| Product audit / immutable history | Product-owned when implemented |
| Platform audit | Platform-owned; do not push operational payloads that violate boundary |

## Encryption

| At rest / in transit | Approach |
|---|---|
| TLS | Follow portfolio Production TLS risk until closed |
| Data at rest | Product DB encryption per future packaging WP |
| Local/offline stores | None in this dry run |

## Input / output controls

- Validation at boundary; ProblemDetails conventions
- No EF entities as API/UI DTOs

## Concurrency and idempotency

| Operation class | Strategy |
|---|---|
| Money-affecting commands | Idempotency keys / server authority (when implemented) |

## Backup / restore

- Product DB backup independent of Platform DB
- Destructive restore guards required for operator tools

## Production security risks (register)

| ID | Risk | Status |
|---|---|---|
| R-091 | Production authentication | Open |
| D-P12-03 | Commercial-state transport | Open |

Full register: `risks-and-decisions.md`.
