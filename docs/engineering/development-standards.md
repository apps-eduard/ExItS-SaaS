# Development Standards

[Home](../index.md) | [Cursor Workflow](../cursor/README.md)

- One approved work package per Cursor prompt.
- Inspect before changing.
- Preserve existing product behavior during platform and product changes.
- No unrelated refactoring.
- Server-side security and tenant isolation are mandatory.
- No direct cross-product database access.
- Each product owns its API, database, migrations, and deployment; cross-boundary data uses versioned contracts and no cross-database foreign keys.
- Shared libraries require two verified consumers, product-neutral code, clear ownership, and no framework-specific UI coupling.
- Platform outages must not block ordinary product transactions; use validated local entitlement projections.
- Keep product-specific operational domains and sensitive payloads out of Platform commercial contracts.
- Contract consumers must be idempotent under at-least-once delivery.
- No hard-coded user-facing strings in new UI.
- New POS UI supports both themes from first implementation.
- New reusable components include accessibility, loading, error and empty states.
- Run applicable build/tests and record exact evidence.
- Update dashboard, phase page and completion report.
- Create one focused commit and report the hash.
- **P2-WP02:** Domain uses `DomainException` + stable error codes; Application returns `ApplicationResult`; timestamps are UTC `DateTimeOffset` supplied via `IClock` at the use-case boundary (domain methods do not call `DateTime.UtcNow`).
- **P2-WP03:** Commercial composition is deterministic via `EntitlementSnapshotComposer`; no generic rules engine; published plan versions are immutable.
- **P2-WP04:** Outbound contracts live in Application (`Contracts` / `Projections`); no Shared/ project; no transport packages.
- **P2-WP05:** Migration validation lives in Application (`MigrationValidation`); structured findings; no EF/SQL/generic repositories.
