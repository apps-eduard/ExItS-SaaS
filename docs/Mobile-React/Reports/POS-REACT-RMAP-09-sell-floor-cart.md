# RMAP-09 — Sell floor + cart parity (units/weight/stock)

## Status

**COMPLETE**

## Baseline

starting SHA: `4ff88ca15127b872abd6abe4bfd98b56e351b8a9` (RMAP-08 docs)

## Contract review

| Area | Finding |
|------|---------|
| Catalog sell read | Products include `sellingMode`, `units[]` (Sell kind), prices per unit |
| Session cart | In-memory only; client preview prices; server still prices sale later |
| ByWeight | Canonical quantity = kilograms; g input normalizes to kg (3 dp) |
| Multi-UOM | Separate cart lines per `productUnitId`; unit price from catalog unit |
| Stock hints | Advisory only — catalog on-hand; for `tracksExpiration` prefer `sellableQuantity` via `getInventoryProduct` |
| Checkout / Pay | Explicitly disabled — no sale POST; no fake success |
| FEFO allocation | Out of scope (checkout later) |
| Camera barcode | Deferred — keyboard / wedge scanner path retained |
| Owner decision | NO |

## Implementation

- Extended `SessionCartProvider` lines: sellingMode, productUnitId, unitLabel, multiplierToBase, unitPrice, quantity (entered qty or kg)
- Sell-unit entry dialog (multi-UOM / pack) and weight entry dialog (kg/g)
- Sell floor: search, categories, barcode auto-add, unit/weight flows, stock advisory, clear-with-confirm, line qty/weight edit, remove
- Pay remains disabled with “checkout not ready” copy
- i18n en + fil-PH

## Exclusions

- Checkout / payment / sale create (RMAP-10/11)
- FEFO lot allocation on sell
- Camera barcode scanning (deferred)
- Offline cart persistence / outbox
- Price override / commercial discount UX

## Deferred — camera barcode

Keyboard and hardware wedge/scanner input via the sell search field remain supported (exact barcode → auto-add). Device camera barcode scanning is **not** implemented in this package and is deferred to a later device/Capacitor package.

## Implementation SHA

`ae433fd2b0f6c88d4eb1d6696f53b7c16960711e`

## Validation

### React gates

| Gate | Result |
|------|--------|
| Vitest | 37 files / **146** tests passed |
| typecheck | PASS |
| lint | 0 errors (pre-existing fast-refresh warnings only) |
| format:check | PASS (after prettier) |
| build | PASS |

### Playwright

| Spec | Result |
|------|--------|
| `e2e/rmap-09-sell-floor-cart.spec.ts` | Passed **7** / Failed 0 |

Responsive matrix:

| Viewport | Result |
|----------|--------|
| 375×812 | PASS |
| 768×1024 | PASS |
| 1024×768 | PASS |
| 1440×900 | PASS |

### Flags

- `RMAP_09_PASS=YES`
- `RMAP_09_RESPONSIVE_MATRIX_PROVEN=YES`
- `RMAP_09_CHECKOUT_EXCLUDED=YES`
- `RMAP_09_CAMERA_BARCODE_DEFERRED=YES`
- `RMAP_09_FEFO_EXCLUDED=YES`

## Next

RMAP-10 — Registers + open shift gate. Do not fake checkout success from the sell floor.
