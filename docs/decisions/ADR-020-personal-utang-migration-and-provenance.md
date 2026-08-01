# ADR-020 — Personal Utang Migration and Provenance

[Decisions](README.md) | [Architecture](../architecture/saas-scopes-users-boundaries-navigation.md) | [Phase 16](../phases/phase-16-isolated-account-profiles-personal-utang-and-business-upgrade.md)

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-08-02 |
| Related | Architecture v1.5 §12, Phase 16 WP08, ADR-019 |

## Context

After Start a Business, users may want selected Personal Utang data available as organization Business Credit. Automatic conversion or continuous sync would blur ownership, duplicate risk, and consent. Architecture v1.5 requires optional, controlled migration with provenance.

## Decision

1. Personal-to-organization migration is **optional**. Default is not to migrate.
2. Migration must be:
   - **selective** (contacts, outstanding balances, history, due dates/notes, etc.)
   - **previewed** before commit
   - **destination-specific** (organization + product)
   - **idempotent** and protected against duplicates
   - **audited** (preview and execution)
   - protected against cross-organization import
3. **Continuous automatic synchronization is prohibited.** Migration is a point-in-time import, not a live mirror.
4. Linked participants require **consent** rules before their personal relationship data is transferred into an organization context (architecture v1.5 linked-participant consent).
5. Every migrated record carries provenance, at minimum:

```text
SourceType
SourceRecordId
ImportedByUserId
ImportedAt
DestinationOrganizationId
DestinationProduct
MigrationBatchId
```

6. Post-migration options remain: keep both ledgers active, archive personal source (read-only), or mark as transferred. Recommended default for balances: personal contact + outstanding balance → business customer + opening credit balance, with source handling explicit in the preview.
7. Migration does **not** grant Organization membership to linked Personal Utang participants and does not grant product roles from entitlement alone.

## Consequences

### Positive

- User control and auditability for financial data movement.
- Clear provenance for support and dispute handling.
- Preserves ADR-019 ownership boundaries.

### Negative / Follow-on

- WP08 must implement preview, consent, and idempotent batch machinery.
- Partial migrations increase UX complexity (selective checkboxes, archive vs retain).

## Rejected alternatives

- Silent auto-import on Organization creation.
- Bidirectional continuous sync between Personal Utang and Business Credit.
- Overwriting personal history without preview or provenance.
