# {{PRODUCT_NAME}} — Security and Privacy

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../exits-product-foundation-reference.md)

| Field | Value |
|---|---|
| Product | {{PRODUCT_NAME}} |
| Status | Draft / Approved |

## Authentication boundary

| Item | State |
|---|---|
| Trusted actor source | {{ACTOR_SOURCE}} |
| Production auth (JWT/MFA/SSO/…) | **Open — R-091** — do not invent fake production login |
| Dev/Testing shortcuts | Document honestly; fail closed outside approved environments |

## Product authorization

- Platform product access / commercial state / entitlements: **entry gate only**
- Product-local roles and grants: **operational authority** (`authorization-matrix.md`)
- Both layers must allow the action; neither bypasses the other

## Organization isolation

- Org scope validated server-side
- Cross-org: {{CROSS_ORG_BEHAVIOR}}
- Org id stored as Guid reference — no cross-DB FK to Platform

## Data classification

| Class | In scope? | Handling |
|---|---|---|
| PHI | **No** (default) / Yes if authorized | {{PHI_HANDLING}} |
| PII | {{PII}} | {{PII_HANDLING}} |
| Operational financial | {{FIN}} | {{FIN_HANDLING}} |
| Secrets / credentials | Never in git | {{SECRETS_HANDLING}} |

## Secrets

- [ ] No secrets in source or docs
- [ ] Config via environment / secret store: {{SECRET_STORE}}

## Logging and audit

| Concern | Approach |
|---|---|
| Application logs | {{LOG_POLICY}} — no secrets/card/PHI dumps |
| Product audit / immutable history | {{AUDIT_POLICY}} |
| Platform audit | Platform-owned; do not push operational payloads that violate boundary |

## Encryption

| At rest / in transit | Approach |
|---|---|
| TLS | {{TLS}} — Production TLS remains a portfolio risk until closed |
| Data at rest | {{AT_REST}} |
| Local/offline stores | {{LOCAL_CRYPTO}} |

## Input / output controls

- Validation at boundary; ProblemDetails conventions
- No EF entities as API/UI DTOs
- {{IO_EXTRA}}

## Concurrency and idempotency

| Operation class | Strategy |
|---|---|
| {{OP_CLASS_1}} | {{IDEM_STRATEGY_1}} |

## Backup / restore

- Product DB backup independent of Platform DB
- {{BACKUP_NOTES}}
- Destructive restore guards required for operator tools

## Production security risks (register)

| ID | Risk | Status |
|---|---|---|
| R-091 | Production authentication | Open |
| {{RISK_ID}} | {{RISK_DESC}} | {{RISK_STATUS}} |

Full register: `risks-and-decisions.md`.
