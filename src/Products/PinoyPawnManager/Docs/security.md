# Pinoy Pawn Manager — Security

> Detail folders: [Security/](Security/README.md) · Compliance: [Compliance/](Compliance/README.md)

| Field | Value |
|---|---|
| Status | PPM-00 planning |
| Implementation | None |
| Last updated | 2026-08-27 |

## Security posture (required intent)

- Least privilege via product-local grants (not hard-coded role-name checks)
- Organization and branch isolation
- Object-level authorization on tickets, items, payments, custody moves
- No cross-product DB access
- Immutable operational history + append-only audit intent
- Sensitive evidence (photos, serials, IMEI, jewelry marks) minimized and access-controlled
- High-risk actions support separation of duties / supervisor thresholds (**PPM-D-00-06/07/18** Open for amounts)

## Threat themes

| Theme | Examples | Mitigations (planning) |
|---|---|---|
| Insider fraud | Wrong-item release; fake appraisal; duplicate cash release | Dual control, custody confirmations, idempotency, audit |
| Cross-tenant leakage | Org A reads Org B tickets | Server-side org scoping |
| Cross-branch leakage | Branch staff sees other vault | Branch scope + explicit transfer |
| Evidence tampering | Overwriting intake photos | Immutable evidence versions; discrepancy workflow |
| Ambiguous retries | Double redemption payment | Idempotency keys + reconciliation |
| Privacy | Customer KYC/photos oversharing | Purpose limitation; retention Open (**PPM-D-00-19**) |
| Stolen goods | Accepting prohibited items | Configurable restrictions + legal review (**PPM-D-00-05**, Compliance) |

## Authentication

Platform owns production authentication (**R-091** portfolio). PPM must not invent a parallel login system. Staff act under Platform identity + PPM grants within selected Organization/product context.

## Authorization summary

See [authorization-matrix.md](authorization-matrix.md) and [Security/role-and-grant-baseline.md](Security/role-and-grant-baseline.md).

High-risk capabilities (planning labels):

- Approve high-value appraisal / principal
- Release funds
- Release collateral
- Override custody discrepancy
- Mark disposition eligible
- Transfer for Commerce disposition

## Privacy

- Default: no PHI
- Customer identity verification fields are a **legal/product decision**, not invented KYC schema
- Photos/serials are sensitive operational evidence — not public marketing assets
- Data Privacy Act considerations: [Compliance/philippines-regulatory-review.md](Compliance/philippines-regulatory-review.md)

## Audit

Business history (ticket snapshots) ≠ mutable audit stream.  
Required event classes: [Security/audit-and-history.md](Security/audit-and-history.md).

## Explicit non-claims

- No claim that ExItS or PPM is a licensed pawnshop operator
- No claim of AML/KYC completeness without separate compliance program
- Development-stage unauthenticated shortcuts must never be described as production-secure
