# RMAP-09 — Sell floor + cart parity (units/weight/stock)

## Status

**COMPLETE** (review repairs applied)

## Baseline

starting SHA: `4ff88ca15127b872abd6abe4bfd98b56e351b8a9` (RMAP-08 docs)

## Contract review

| Area | Finding |
|------|---------|
| Catalog sell read | Products include `sellingMode`, `units[]` (Sell kind), prices per unit |
| Session cart | In-memory only; client preview prices; server still prices sale later |
| Add decision tree | ByWeight → weight; 1 sell + custom → measured qty dialog; 1 sell + !custom → direct add with unit identity; >1 → unit selector; else base |
| ByWeight | Canonical quantity = kilograms; g input normalizes to kg (3 dp) |
| Custom measured | Liter/Meter/etc. use unit label, ≤3 dp, **no kg conversion** |
| Whole qty | `requiresWholeEnteredQuantity` = `!allowsCustomQuantity` (not multiplier-based) |
| Multi-UOM | Separate cart lines per `productUnitId`; unit price from catalog unit |
| Stock hints | Advisory only — catalog on-hand; for `tracksExpiration` prefer `sellableQuantity` via `getInventoryProduct` |
| Checkout / Pay | Explicitly disabled — no sale POST; no fake success |
| FEFO allocation | Out of scope (RMAP-11 checkout) |
| Camera barcode | Deferred — see below |
| Owner decision | NO |

## Implementation

- Extended `SessionCartProvider` lines: sellingMode, productUnitId, unitLabel, multiplierToBase, unitPrice, quantity, **allowsCustomQuantity**
- Sell-unit entry dialog (multi-UOM / pack), weight entry dialog (kg/g), **SellCustomQuantityDialog** for measured non-weight units
- Sell floor: search, categories, barcode auto-add, locked add-flow, stock advisory, clear-with-confirm, line qty/weight/custom edit, remove
- Pay remains disabled with “checkout not ready” copy
- i18n en + fil-PH

## Exclusions

- Checkout / payment / sale create (RMAP-10/11)
- FEFO lot allocation on sell (RMAP-11)
- Camera barcode scanning (deferred — see below)
- Offline cart persistence / outbox
- Price override / commercial discount UX

## Deferred — camera barcode

Keyboard and hardware wedge/scanner input via the sell search field remain supported (exact barcode → auto-add). Device **camera** barcode scanning is **not** implemented in this package.

Future camera scanning is planned as a **PWA** capability (`getUserMedia` / a `ScannerService` abstraction). It is **not** Capacitor-required and must not be gated on a Capacitor plugin package.

## Implementation SHA

`ae433fd2b0f6c88d4eb1d6696f53b7c16960711e` (initial); review repairs on `feat/pos-react-client`

## Validation

### React gates

| Gate | Result |
|------|--------|
| Vitest | 39 files / **163** tests passed (decision-tree + qty matrix) |
| typecheck | PASS |
| Playwright `rmap-09` | Passed (single sell unit, custom liter, whole qty, weight, responsive) |

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
- `RMAP_09_CAMERA_BARCODE_DEFERRED=YES` (future PWA `getUserMedia` / ScannerService — not Capacitor-required)
- `RMAP_09_FEFO_EXCLUDED=YES`

## Next

RMAP-10 — Registers + open shift gate — **COMPLETE**. Next: RMAP-11 checkout/sale. Do not fake checkout success from the sell floor.
