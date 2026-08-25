# RMAP-08 — Lots / expiry / FEFO (optional track)

## Status

**COMPLETE** (review repairs applied)

## Baseline

starting SHA: `cba1cd7277183ffe1c1d25ab5366b284cc57e2f2` (post RMAP-B03 / pre-master lint clear)

## Contract review

| Area | Finding |
|------|---------|
| Product flag | `TracksExpiration` + `ExpirationWarningDays` (default 7 when on) |
| Lots | `GET .../inventory/{productId}/lots`; detail totals sellable / expired / near-expiry |
| Expiring list | `GET .../inventory/lots?window=Expired\|Days7\|Days14\|Days30` |
| Enable / Adjust In | Expiration date required when tracked + qty > 0 / In |
| Adjust Out | Lot selection (or expiry when no lots); Expired write-off wording |
| NearExpiryCount | Per-product `EffectiveExpirationWarningDays` (catalog join); ExpiredCount remains date-based |
| Branch scope | Bound branch header scopes lot list; adjust rejects wrong-branch `LotId` |
| Pagination | React detail + expiration: pageSize **50** + Load more (not 500/1000) |
| FEFO sell allocation | Out of scope here — validated at **RMAP-11 checkout**, not RMAP-09+. React does **not** FEFO. |
| Owner decision | NO |

## Implementation

- Inventory client: lot DTOs, `listProductLots`, `listExpiringLots`, enable/adjust expiry fields
- Catalog create/edit: Track expiration + warning days
- Inventory detail: expiry totals + infinite lot list (pageSize 50) + expiry-aware enable/adjust; selection stable across pages
- `/inventory/expiration`: window filters, search, Load more, reset on window/search change, de-dupe by lotId
- Manager home + inventory list: **Expiring stock** links
- Backend: `CountExpiryAsync` product-aware near count; `ListLots` passes optional branch header
- i18n en + fil

## Exclusions

- Checkout / sell-floor FEFO allocation engine (**RMAP-11** checkout ownership; React never allocates FEFO)
- Card / provider payments
- Branch transfer lot UX
- Purchase goods-receipt lot UI

## Implementation SHA

`4c38bb0e6e72549fa7641d301c2e0f7885d9f604` (initial); review repairs on `feat/pos-react-client` after RMAP-09

## Validation

### Backend contract

| Suite | Result |
|-------|--------|
| `InventoryLotDomainTests` | Passed **13** / Failed 0 / Skipped 0 |
| `PosInventoryLotApiTests` | Passed **6** / Failed 0 / Skipped 0 (includes near-count + branch-scope proofs) |

### React gates

| Gate | Result |
|------|--------|
| Vitest | 39 files / **163** tests passed |
| typecheck | PASS |
| Playwright `rmap-08` + `rmap-07` regression | Passed (incl. >50 lot Load more) |

Responsive matrix (expiration list + detail):

| Viewport | Result |
|----------|--------|
| 375×812 | PASS |
| 768×1024 | PASS |
| 1024×768 | PASS |
| 1440×900 | PASS |

### Flags

- `RMAP_08_PASS=YES`
- `RMAP_08_RESPONSIVE_MATRIX_PROVEN=YES`
- `RMAP_08_BACKEND_CONTRACT_REVALIDATED=YES`
- `RMAP_08_PAGINATION_PAGE50_PROVEN=YES`
- Checkout FEFO OUT OF SCOPE until **RMAP-11** (not RMAP-09+)

## Next

RMAP-09 — Sell floor + cart parity (units/weight/stock). Do not start checkout FEFO allocation here.
