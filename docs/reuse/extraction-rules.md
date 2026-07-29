# Extraction and Migration Rules

[Home](../index.md) | [Reuse Matrix](reuse-classification-matrix.md) | [Extraction sequence](extraction-sequence.md) | [Capability boundary](../engineering/platform-product-capability-boundary.md)

1. Preserve the completed HealthCare MVP in a working state.
2. Create a baseline tag or commit before structural extraction.
3. Never copy HealthCare migrations into PinoyBusinessPOS.
4. Do not rename healthcare concepts into POS concepts.
5. Platform extraction requires contract tests and HealthCare regression tests.
6. Shared libraries must be generic by evidence, not by name alone — require **two verified consumers**, product-neutral code, and no framework-specific UI (P1-WP01 governance).
7. Each product keeps its own API, database, migrations and deployment. No cross-database foreign keys. Cross-boundary data uses versioned contracts (P1-WP02 / ADR-012).
8. Platform outages must not block ordinary product transactions (local entitlement projections — ADR-011 / ADR-012).
9. Ant Design Blazor remains only in existing HealthCare Staff Web. New ExITS Platform Admin and PinoyBusinessPOS use native CSS/Razor — Ant is not required and must not be introduced there.
10. Extraction proceeds in focused work packages with rollback instructions ([extraction-sequence.md](extraction-sequence.md), [extraction-rollback-plan.md](../engineering/extraction-rollback-plan.md)).
11. Do not put product-specific domain (clinical or POS operational) into Platform; do not duplicate Platform identity/billing inside products as system of record.
12. Do not put clinical-sensitive or POS operational payloads into Platform commercial contracts; consumers must be idempotent under at-least-once delivery.
13. Build **new** Platform foundations in root Git before HealthCare reconnection (ADR-013). Do not wholesale-copy the HealthCare solution. Do not import HealthCare until an approved WP.
14. POS may begin after the POS readiness gate without waiting for full HealthCare cutover.
