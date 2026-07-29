# Extraction and Migration Rules

[Home](../index.md) | [Reuse Matrix](reuse-classification-matrix.md) | [Capability boundary](../engineering/platform-product-capability-boundary.md)

1. Preserve the completed HealthCare MVP in a working state.
2. Create a baseline tag or commit before structural extraction.
3. Never copy HealthCare migrations into PinoyBusinessPOS.
4. Do not rename healthcare concepts into POS concepts.
5. Platform extraction requires contract tests and HealthCare regression tests.
6. Shared libraries must be generic by evidence, not by name alone — require **two verified consumers**, product-neutral code, and no framework-specific UI (P1-WP01 governance).
7. Each product keeps its own API, database, migrations and deployment. No cross-database foreign keys.
8. Platform outages must not block ordinary product transactions (local entitlement projections — ADR-011).
9. Ant Design Blazor remains only in existing HealthCare Staff Web. New ExITS Platform Admin and PinoyBusinessPOS use native CSS/Razor — Ant is not required and must not be introduced there.
10. Extraction proceeds in focused work packages with rollback instructions.
11. Do not put product-specific domain (clinical or POS operational) into Platform; do not duplicate Platform identity/billing inside products as system of record.
