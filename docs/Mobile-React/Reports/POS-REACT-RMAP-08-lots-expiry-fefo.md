# RMAP-08 — Lots / expiry / FEFO (optional track)

## Status

**COMPLETE**

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
| FEFO sell allocation | Out of scope (RMAP-09+) — inventory surfaces only |
| Owner decision | NO |

## Implementation

- Inventory client: lot DTOs, `listProductLots`, `listExpiringLots`, enable/adjust expiry fields
- Catalog create/edit: Track expiration + warning days
- Inventory detail: expiry totals + lot list + expiry-aware enable/adjust
- `/inventory/expiration`: window filters, search, link to product detail
- Manager home + inventory list: **Expiring stock** links
- i18n en + fil

## Exclusions

- Checkout / sell-floor FEFO allocation engine (RMAP-09+)
- Card / provider payments
- Branch transfer lot UX
- Purchase goods-receipt lot UI

## Implementation SHA

`4c38bb0e6e72549fa7641d301c2e0f7885d9f604`

## Validation

### Backend contract (existing CURRENT)

| Suite | Result |
|-------|--------|
| `InventoryLotDomainTests` | Passed **13** / Failed 0 / Skipped 0 |
| `PosInventoryLotApiTests` | Passed **4** / Failed 0 / Skipped 0 (Testcontainers available) |

### React gates

| Gate | Result |
|------|--------|
| Vitest | 37 files / **140** tests passed |
| typecheck | PASS |
| lint | 0 errors (8 pre-existing fast-refresh warnings) |
| format:check | PASS |
| build | PASS |

### Playwright

| Spec | Result |
|------|--------|
| `e2e/rmap-08-lots-expiry.spec.ts` | Passed **7** / Failed 0 |
| `e2e/rmap-07-inventory.spec.ts` (regression) | Passed **8** / Failed 0 |

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
- Checkout FEFO still OUT OF SCOPE (RMAP-09+)

## Next

RMAP-09 — Sell floor + cart parity (units/weight/stock). Do not start checkout FEFO allocation here.
