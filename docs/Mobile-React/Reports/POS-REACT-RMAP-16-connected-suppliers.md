# RMAP-16 — Connected suppliers

## Status

**PASS** (pending parent commit + native-speaker review)

| Flag | Value |
|------|-------|
| `RMAP_16_AUTHORIZED` | YES (authorized after RMAP-15 PASS) |
| `RMAP_16_PASS` | PASS |
| `RMAP_16_CLIENT` | PASS |
| `RMAP_16_CAPABILITIES` | PASS |
| `RMAP_16_UI` | PASS |
| `RMAP_16_I18N` | PASS |
| `RMAP_16_VITEST` | PASS |
| `RMAP_16_E2E` | PASS |
| `RMAP_16_TYPECHECK` | PASS |
| `RMAP_16_NATIVE_SPEAKER` | PENDING |
| `RMAP_17_STARTED` | NO |
| `HARD_STOP` | NO (await RMAP-17 authorization separately) |

## Contract

| Area | Finding |
|------|---------|
| API | `/api/v1/pos/connected-suppliers/*` — relationships, exposures L1, buyer-product-shares L2, catalog/readiness/match, links create/create-and-link/unlink |
| Connection request | Business QR / ORG###### only; Guid-alone rejected client-side and by backend |
| EXPOSABLE ≠ SHARED | Accept connection does **not** share products; share filter `shared` returns only `isShared` rows; post-approve share prompt is explicit |
| Inventory invariant | Share / expose / link / connection clients never call inventory mutation endpoints — Vitest + Playwright URL assertions **PASS** |
| Capabilities | View/Manage Suppliers for relationship/share; View/Manage Purchasing for catalog/links; ManageCatalog required with ManagePurchasing for create-and-link |
| Manual suppliers | RMAP-15 CRUD unchanged; connected actions only when `ConnectedOrganization` + Active/Pending as appropriate |

## Implementation

- `pos-connected-suppliers-client.ts` + unit tests (zod + inventory invariant helpers)
- Features: request, incoming requests (+ share prompt), connected buyers, shared products + buyer price, connected catalog, linked products
- Extended `SuppliersListPage` / `SupplierDetailPage` with connected entry points
- Routes under `/suppliers/connected/...`, `/suppliers/:id/connected-catalog`, `/suppliers/:id/linked-products`
- Guards: existing supplier guards + `RequireViewPurchasing` / purchasing capability helpers
- i18n `connected.*` in en, fil-PH, ceb-PH, ilo-PH, hil-PH
- Playwright `e2e/rmap-16-connected-suppliers.spec.ts`
- Report + roadmap status update

## Exclusions

- RMAP-17 PO receive / inventory receive
- Incoming connected purchase-order inbox/fulfillment UI
- Migrations / backend changes
- Offline linked-product LocalStore sync UI
- Native-speaker i18n sign-off

## Validation

### React gates

| Gate | Result |
|------|--------|
| prettier (touched) | PASS |
| typecheck | PASS |
| Vitest (connected client + capabilities + message-parity) | PASS |
| Playwright `rmap-16-connected-suppliers` | PASS |

Responsive matrix (suppliers list with connected CTAs):

| Viewport | Result |
|----------|--------|
| 375×812 | PASS (e2e) |
| 768×1024 | PASS (e2e) |
| 1024×768 | PASS (e2e) |
| 1440×900 | PASS (e2e) |

### Proven behaviors

- Request connection with ORG######; Guid-alone rejected
- Approve incoming → share prompt (EXPOSABLE ≠ SHARED messaging)
- Share selected products + apply buyer price
- Empty shared catalog; search no-results; readiness chips
- Create-and-link; link existing; unlink
- No inventory URLs on share/link flows
- Wrong-org supplier detail not found
- Cashier denied `/suppliers`
- Locale smoke (Filipino buyers title)
- Responsive 4 viewports

### Inventory invariant evidence

- Unit: `assertNotInventoryMutationUrl` + path marker checks on request/share/link/unlink URLs
- E2E: `tracker.inventoryCalls === 0` and no `/inventory` or `/stock-counts` in observed fetch paths

## Exact next

Do **not** start RMAP-17 until authorized. Native-speaker i18n review remains PENDING.
