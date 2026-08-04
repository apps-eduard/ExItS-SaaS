# Catalog Background Jobs and Imports

**Purpose**  
Define reliable, idempotent background processing for template imports, selected-product imports, and Platform bulk catalog imports.

---

| Field | Value |
|---|---|
| Status | Proposed |
| Phase | Phase 20 |
| Work Package | P20-WP05 / P20-WP06 |
| Runtime | .NET background processing |

---

## 1. Why Background Processing

- First batches may contain 150–300 products.
- Bulk CSV/XLSX imports may contain thousands of rows.
- Progress, retry, partial success, and error reporting are required.
- Long-running work must not block HTTP requests.

---

## 2. Technology Rule

Reuse the project’s existing background-job approach.

Preferred order:

1. Existing project job infrastructure.
2. PostgreSQL-backed hosted worker.
3. Hangfire or Quartz.NET only when justified.

Do not introduce Redis solely for this phase.

---

## 3. Job Types

| Job | Owner | Trigger |
|---|---|---|
| `ImportCatalogTemplateBatch` | POS | Merchant confirms template or loads next batch |
| `ImportSelectedCatalogProducts` | POS | Merchant selects products |
| `ImportGlobalCatalogFile` | Platform | Admin confirms CSV/XLSX import |
| `GenerateCatalogImportErrorReport` | Owning product | Import completes with failures |

---

## 4. POS Template Import Logic

### Input

```json
{
  "organizationId": "trusted-context",
  "platformTemplateId": "uuid",
  "batchNumber": 1,
  "requestedBy": "user-id",
  "idempotencyKey": "uuid"
}
```

### Steps

1. Validate entitlement and POS permissions.
2. Resolve authenticated organization context.
3. Fetch published template snapshot from Platform.
4. Exclude already imported external product IDs.
5. Normalize barcode, SKU, unit, and category mapping.
6. Validate conflicts against local catalog.
7. Create/map local categories.
8. Create local product snapshots.
9. Create opening inventory only through existing inventory rules.
10. Record item-level result.
11. Update progress.
12. Complete with success, warnings, or failure.

---

## 5. Idempotency

Required controls:

- unique idempotency key per organization and command
- unique external product mapping when business rules require it
- safe retry after timeout
- no duplicate local product on repeated job execution
- item-level status persisted before advancing progress

---

## 6. Partial Success

Prefer partial success when safe.

Example:

```text
Requested: 200
Imported: 188
Skipped duplicates: 9
Failed validation: 3
Result: CompletedWithWarnings
```

The user must be able to inspect failed/skipped items.

---

## 7. Retry Policy

Retry only transient failures:

- database timeout
- temporary Platform API failure
- temporary storage failure

Do not blindly retry:

- duplicate barcode
- invalid unit
- unauthorized request
- archived template
- malformed source data

Recommended transient retry:

```text
Maximum attempts: 3
Backoff: exponential with jitter
```

Follow existing project resilience conventions when present.

---

## 8. Progress Model

```text
Status
TotalCount
ProcessedCount
ImportedCount
SkippedCount
FailedCount
CurrentStage
StartedAt
CompletedAt
LastHeartbeatAt
```

Progress must never exceed total count.

---

## 9. Security

- Worker revalidates organization scope and permissions where required.
- Never trust organization ID only from serialized job input.
- Do not log sensitive tokens or secrets.
- Audit requestor, organization, source template/file, and result.
- Error reports must be accessible only to authorized users.

---

## 10. Performance

- Use server-side pagination.
- Use bulk insert where compatible with domain invariants.
- Avoid N+1 Platform and database queries.
- Process large imports in chunks.
- Mobile clients poll progress at a reasonable interval.
- Images are referenced, not downloaded synchronously into each import transaction unless explicitly designed.

---

## 11. Acceptance Criteria

- [ ] Jobs are idempotent.
- [ ] Partial success is supported.
- [ ] Progress is queryable.
- [ ] Transient retries are bounded.
- [ ] Permanent failures are not repeatedly retried.
- [ ] Item-level error details are preserved.
- [ ] Organization isolation and audit are tested.

---

**Document Owner**: Engineering  
**Last Updated**: 2026-08-04
