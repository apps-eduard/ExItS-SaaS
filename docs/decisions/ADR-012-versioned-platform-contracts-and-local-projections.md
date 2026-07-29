# ADR-012 — Versioned Platform Contracts and Local Product Projections

[Decisions](README.md) | [Contracts](../engineering/platform-product-contracts.md) | [ADR-011](ADR-011-platform-authority-and-product-local-projections.md)

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-07-29 |
| Work package | P1-WP02 |
| Related | ADR-003, ADR-009, ADR-011 |

## Context

P1-WP01 established Platform authority and product-local entitlement projections. Implementers still need explicit rules for identifiers, versioning, idempotency, event envelopes, projection states, reconciliation, and privacy exclusions before building APIs or messaging.

## Decision

1. Cross-boundary references use **stable IDs** (UUID/Guid or immutable codes); **no cross-database foreign keys**.
2. Platform↔product integration uses **versioned, additive contracts** (events, snapshots, later APIs).
3. Delivery is assumed **at-least-once**; consumers must be **idempotent** and tolerate duplicates/out-of-order updates.
4. Products enforce commercial rules via **local entitlement projections** with explicit projection states.
5. Platform commercial/identity contracts **must not** carry clinical-sensitive or POS operational payloads (remarks, sale lines, inventory, medical notes).
6. Projection failures use **manual/admin reconciliation** that replaces commercial projection only and preserves operational data.
7. **Transport selection is deferred** (OD-03); this ADR does not choose a broker or poll mechanism.
8. SaaSPayment, RetailPayment, and CreditPayment remain separate concepts.
9. This ADR **extends** ADR-011 with contract mechanics; it does not replace Platform authority decisions.

## Consequences

### Positive

- Clear implementation checklist for Phase 2–3.
- Safer multi-product evolution and offline-tolerant POS behavior.
- Reduced risk of PHI/PII leakage into Platform.

### Negative

- Projection staleness durations and transport still open (R-022, OD-03).
- Reconciliation UX and tooling deferred to later phases.

## Rejected alternatives

- Sync Platform entitlement check on every sale/appointment.
- Unversioned shared database rows with cross-schema FKs.
- Untyped arbitrary key-value entitlement bags as the primary model.
- Selecting Kafka/RabbitMQ/Service Bus in this documentation WP.
