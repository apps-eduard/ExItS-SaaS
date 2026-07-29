# ADR-013 — Build New Platform Foundation Before HealthCare Reconnection

[Decisions](README.md) | [Extraction sequence](../reuse/extraction-sequence.md) | [Rollback plan](../engineering/extraction-rollback-plan.md)

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-07-29 |
| Work package | P1-WP03 |
| Related | ADR-001, ADR-002, ADR-003, ADR-010, ADR-011, ADR-012 |

## Context

HealthCare is a completed nested repository (ignored by root Git) with reusable identity/org/permission/audit patterns, but missing portfolio billing/entitlements and unsuitable for wholesale copy into Platform. Phase 0 recommended building Platform in root without importing HealthCare first. P1-WP01/P1-WP02 defined ownership and contracts.

## Decision

1. **Platform foundation is built new** in the ExITS root Git repository (future phases).
2. **HealthCare remains separate and unchanged** initially (frozen/ignored).
3. HealthCare **patterns are adapted selectively**; there is **no wholesale code copy**.
4. **Reconnection** (identity/org mapping, adapters, cutover) happens **only after** contract, identity, security, regression, and rollback validation.
5. **Rollback remains possible** at documented levels (docs → foundation → auth → mapping → entitlements → DB → UI).
6. **PinoyBusinessPOS may start** after Platform foundations and contracts stabilize (**POS readiness gate**) **without waiting** for full HealthCare migration.
7. Password-hash / Identity database migration requires a **separately approved** plan; not part of early foundation.

## Consequences

### Positive

- Protects the completed HC MVP.
- Allows Platform and POS progress without import risk.
- Clear gates and rollback before cutover.

### Negative

- Temporary dual identity/org concepts until reconnection.
- Mapping and cutover complexity deferred to Phase 2+.

## Rejected alternatives

- Immediate monorepo import of HealthCare.
- Copying entire HC solution as Platform.
- Waiting for full HC cutover before any POS work.
- Editing HC migrations to “become” Platform.
