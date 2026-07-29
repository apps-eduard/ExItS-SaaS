# ADR-011 — Platform Authority and Product-Local Projections

[Decisions](README.md) | [Capability boundary](../engineering/platform-product-capability-boundary.md) | [ADR-009 related](README.md)

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-07-29 |
| Work package | P1-WP01 |
| Related | ADR-003, ADR-009, ADR-010 |

## Context

ExITS products must remain operable when Platform is temporarily unavailable, while Platform remains the commercial and identity system of record. HealthCare assessment showed soft org limits exist but plans/subscriptions/entitlements do not. Products must not own SaaS billing ledgers or trust client-supplied organization identifiers.

## Decision

1. **Platform is authoritative** for global identity, organizations, product catalog, subscriptions, SaaS payments, and entitlements (including overrides, grace, suspension).
2. **Products own** operational data and operational permissions (HealthCare clinical; POS retail).
3. **Products use local entitlement projections** (versioned snapshots) for runtime enforcement.
4. **Cross-database foreign keys are prohibited.** References use stable IDs only.
5. **Normal product operations do not synchronously query Platform on every request.**
6. **Platform product access** (may use product) is distinct from **product operational permissions**.
7. **POS Customer ≠ Platform User**; optional future login link is deferred.
8. **SaaS subscription payments ≠ POS retail sale payments.**
9. This ADR **accepts** the intent of ADR-009 (local entitlement snapshots).

## Consequences

### Positive

- Offline-friendly POS and resilient HealthCare ops.
- Clear extraction boundary for identity/billing.
- Prevents accidental shared DbContext / mega-platform.

### Negative

- Projection staleness and conflict handling must be designed (P1-WP02 / Phase 3).
- Temporary dual identity during HealthCare extraction.

## Rejected alternatives

- Synchronous entitlement check on every sale/appointment.
- Products as billing system of record.
- Shared database with cross-schema FKs.
- Merging Customer/Patient into ApplicationUser.
