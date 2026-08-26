# PinoyServicePro — Security and Privacy

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)

| Field | Value |
|---|---|
| Product | PinoyServicePro |
| Status | Draft — PSP-00 documentation; Implementation Not Started |
| Last updated | 2026-08-20 |
| Implementation present | No |

## Authentication boundary

| Item | State |
|---|---|
| Trusted actor source | Platform identity / session context (consume only) |
| Production auth (JWT/MFA/SSO/…) | **Open — R-091** — do not invent fake production login |
| Dev/Testing shortcuts | Document honestly; fail closed outside approved environments (D-P12-05) |

## Product authorization

- Platform product access / commercial state / entitlements: **entry gate only**
- Product-local roles and grants: **operational authority** ([authorization-matrix.md](authorization-matrix.md))
- Both layers must allow the action; neither bypasses the other
- Configuration / templates must not weaken authorization

## Organization isolation

- Org scope validated server-side
- Cross-org: conceal (planning default 404)
- Org id stored as Guid reference — no cross-DB FK to Platform
- Branch scope rules required for bookings, jobs, money, and reports when multi-branch is enabled (PSP-D-00-12)

## Data classification

| Class | In scope? | Handling |
|---|---|---|
| PHI | **No** (default) | Not authorized. Do not introduce medical/health records under generic notes. Future PHI industries need separate authorized compliance design. |
| PII | Expected (customers, contacts, addresses) | Minimize; retention open (PSP-D-00-17); access via grants |
| Operational | Bookings, jobs, assets, staff assignments, history | Tenant-isolated; grant-scoped |
| Operational financial | Service amounts, payments, refunds, deposits if enabled | Product DB only; ≠ SaaS billing |
| Secrets / credentials | Never in git | Environment / secret store when implemented |

## Compliance posture

PinoyServicePro must **not** claim:

- BIR accredited / BIR compliant / BIR certified
- industry licensed
- tax compliant
- accounting compliant

unless separately implemented, validated, and approved.

Future Philippine tax-document integration must use the controlled ExItS compliance architecture rather than hard-coded ServicePro assumptions (PSP-D-00-16).

## Secrets

- [x] No secrets in source or docs (PSP-00 docs-only)
- [ ] Config via environment / secret store: when packaging is authorized

## Logging and audit

| Concern | Approach |
|---|---|
| Application logs | No secrets / card / PHI dumps; minimize PII |
| Product audit / immutable history | Product-owned operational audit when implemented — [Security/audit-and-history-baseline.md](Security/audit-and-history-baseline.md) |
| Platform audit | Platform-owned; do not push operational payloads that violate boundary |

## Encryption

| At rest / in transit | Approach |
|---|---|
| TLS | Portfolio Production TLS risk remains open until closed upstream |
| Data at rest | Product DB controls when authorized |
| Local/offline stores | Only if offline is authorized (PSP-D-00-04); do not inherit POS offline crypto assumptions |

## Input / output controls

- Validation at boundary; ProblemDetails conventions when API exists
- No EF entities as API/UI DTOs
- Server-authoritative scheduling and money rules — UI slot availability is not sole authority

## Concurrency and idempotency

| Operation class | Strategy (planning) |
|---|---|
| Booking create/reschedule | Server-authoritative conflict checks; exact policy open (PSP-D-00-20) |
| Payments / refunds | Idempotent posting intent; exact rules open (PSP-D-00-19) |
| Estimate acceptance | Explicit transition; no silent overwrite of accepted terms |

## Backup / restore

- Product DB backup independent of Platform DB (when DB exists)
- Destructive restore guards required for operator tools
- Retention policy open (PSP-D-00-17)

## Production security risks (register)

| ID | Risk | Status |
|---|---|---|
| R-091 | Production authentication | Open |
| D-P12-03 | Commercial-state transport | Open / provisional |
| PSP-D-00-13 | Anonymous/public booking identity & abuse | Open |
| PSP-D-00-04 | Offline mutable surface risk | Open |
| PSP-D-00-16 | Tax/compliance activation without false claims | Open |

Full register: [risks-and-decisions.md](risks-and-decisions.md).
