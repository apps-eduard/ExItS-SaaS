# RMAP-15 — Manual suppliers

## Status

**PASS** (pending parent commit + native-speaker review)

| Flag | Value |
|------|-------|
| `RMAP_15_AUTHORIZED` | YES (implementation authorized for this run) |
| `RMAP_15_PASS` | PASS |
| `RMAP_15_CLIENT` | PASS |
| `RMAP_15_CAPABILITIES` | PASS |
| `RMAP_15_UI` | PASS |
| `RMAP_15_I18N` | PASS |
| `RMAP_15_VITEST` | PASS |
| `RMAP_15_E2E` | PASS |
| `RMAP_15_TYPECHECK` | PASS |
| `RMAP_15_NATIVE_SPEAKER` | PENDING |
| `RMAP_16_STARTED` | NO |
| `HARD_STOP` | NO (await RMAP-16 authorization separately) |

## Contract

| Area | Finding |
|------|---------|
| API | `/api/v1/pos/suppliers` list/create/get/update/activate/deactivate |
| List search | Term starting with `SUP` → `supplierCode`; otherwise `name` |
| Status filter | Active / Inactive / All |
| Pagination | `page` / `pageSize` 20 |
| Form | Name required; remaining fields optional; edit sends `expectedUpdatedAtUtc` |
| Connection badge | Manual (`External`) vs Connected (`ConnectedOrganization`) — no IDs shown |
| Capabilities | `canViewSuppliers` (Owner/Admin/StoreManager + InventoryStaff + ReportingUser); `canManageSuppliers` (Owner/Admin/StoreManager only); Cashier DENY |
| Conflicts | Friendly mapping for name/email/mobile/tax concurrency codes |

## Implementation

- `pos-suppliers-client.ts` + unit tests (zod + `posRequest`)
- Features: `SuppliersListPage`, `SupplierDetailPage`, `SupplierFormPage`
- Routes: `/suppliers`, `/suppliers/new`, `/suppliers/:supplierId`, `/suppliers/:supplierId/edit`
- Guards: `RequireViewSuppliers`, `RequireManageSuppliers`
- Role home **Suppliers** link when `canViewSuppliers`
- i18n `suppliers.*` in en, fil-PH, ceb-PH, ilo-PH, hil-PH
- Playwright `e2e/rmap-15-suppliers.spec.ts`
- Report + roadmap status update

## Exclusions

- RMAP-16+ connected supplier flows (connect, expose, buyer prices, share)
- Purchasing / goods receipt
- Migrations / backend changes
- Offline supplier queue
- PosRoleMatrix mutation

## Validation

### React gates

| Gate | Result |
|------|--------|
| prettier (touched) | PASS |
| typecheck | PASS |
| Vitest (suppliers + capabilities + message-parity) | PASS |
| Playwright `rmap-15-suppliers` | PASS |

Responsive matrix (suppliers list):

| Viewport | Result |
|----------|--------|
| 375×812 | PASS (e2e) |
| 768×1024 | PASS (e2e) |
| 1024×768 | PASS (e2e) |
| 1440×900 | PASS (e2e) |

### Proven behaviors

- Owner list / SUP code search / pagination
- Owner create
- Edit concurrency friendly error
- Deactivate / activate
- Duplicate name friendly error
- Wrong-org detail → not found
- Cashier denied `/suppliers`
- Locale smoke (Filipino title)
- Responsive 4 viewports

## Exact next

Do **not** start RMAP-16 until authorized. Native-speaker i18n review remains PENDING.
