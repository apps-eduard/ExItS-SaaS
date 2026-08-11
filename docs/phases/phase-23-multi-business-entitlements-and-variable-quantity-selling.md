# Phase 23 — Multi-Business Entitlements and Variable-Quantity Selling

[Phases](README.md) | [Portfolio](../portfolio-progress.md) | [WP01 audit](../reports/P23-WP01-current-state-and-domain-design.md)

| Field | Value |
|---|---|
| Status | **Open** — WP01 ✓ · WP02 ✓ · WP03 ✓ · WP04 ✓ · WP05 ✓ · WP06 ✓ · WP07 ✓ · WP08 ✓ · WP09 ✓ · WP10 ✓ · **WP10A Done** · **WP10B Done** · **WP11 Done** · WP12+ not started |
| Branch / HEAD at open | `main` @ `c03894f` (WP01 docs later @ `4e5cb99`) |
| Physical device target | `R58R61E3CAZ` (validation not in WP01–WP03) |

## Problem statement

Merchants increasingly operate **mixed businesses** (e.g. Sari-Sari + Vegetable Vendor) and sell **fresh goods by weight** with **prices that change daily**. Today ExItS has:

- one immutable organization **PrimaryBusinessTypeId**
- commercial entitlements that are **boolean / numeric limits only** (no Business Type packs)
- catalog/template discovery that is **optional query-param filtering**, not entitlement-enforced
- **decimal** sale/inventory quantities with measured UOMs (Kg/g/…), but **no product selling-mode**, poor weight entry UX, and **offline sync that re-prices from live catalog**

Without Phase 23, mixed-business discovery leaks unrelated catalogs, and weighted/fresh selling cannot be trusted offline or across price changes.

## Goals

1. Subscription plans can grant **one or more Business Types** without hard-coding every combination as a plan type.
2. Organization keeps **PrimaryBusinessTypeId** for defaults/onboarding/reporting and gains **effective entitled Business Types** from subscription (+ optional activation of allowed add-ons).
3. **Server-enforced** filtering of Global Catalog and Catalog Templates to the org’s effective Business Types (Platform Admin unrestricted).
4. Downgrade never deletes merchant-owned catalog or history; only blocks **new** discovery/import for removed types.
5. Product **SellingMode** (PerItem / ByWeight) drives cart behavior in **one** POS — no per-industry “modes”.
6. Decimal quantity + money already exist; extend with selling-mode, kg/g UX, immutable sale-line snapshots, and a **Today’s Prices** workflow that remains authority-safe online and offline.
7. Preserve local-first offline grants, device registration gates, barcode rules, and identity model.

## Non-goals

- Hard-coded plan SKUs per Business Type combination.
- Separate POS apps or cart modes per industry.
- Scale hardware integration (document only).
- Amount→weight (“PHP 50 of tomato”) in first implementation unless it drops out naturally (default: **deferred**).
- Seeding every future vendor type in WP01–WP02 (add when consistent with seed patterns).
- Redesigning Personal/Staff identity, Platform Admin Ant Design stack, or production auto-`Migrate()`.
- Claiming Device Verified or Production Ready from unit tests alone.
- Destructive DB reset unless explicitly requested.

## Confirmed current architecture (WP01)

### Business Type / catalog

- Dynamic `BusinessType` in `catalog.business_types` (P20); seeds via `LegacyBusinessTypeSeeds`.
- `GlobalProduct` / `GlobalCategory` M2M business-type tags.
- `CatalogTemplate.PrimaryBusinessTypeId` (single).
- `PlatformOrganization.PrimaryBusinessTypeId` — set at Start Business, **immutable**.
- Merchant APIs: optional `businessTypeId` / `businessTypeCode` filters; **auth-only**, not entitlement-enforced.
- POS discovery clients often send mismatched param names (`businessType`) → filters largely **no-op**.

### Subscription / entitlements

- `Plan` → `PlanVersion.Grants` (`FeatureCode` + bool + optional `int?` limit) → `Subscription` → `EntitlementSnapshot`.
- Capacities: branches / staff / POS devices.
- **No** plan↔BusinessType grant model.

### Selling / quantity / money (POS)

| Concern | Actual type / rule |
|---|---|
| Quantity (sale, inventory, PO, returns) | `decimal` / `numeric(18,3)` |
| Money | `decimal` / `numeric(18,2)`, `SaleMoney.RoundMoney` AwayFromZero |
| Measured UOMs | Kg, g, L, ml, m — max **3** decimal places; over-precision **rejected** |
| Whole UOMs | Piece/Pack/… — whole numbers only |
| Line total | `RoundMoney(unitPrice * quantity)` with unit price snapshot on line |
| Checkout API | Client sends `ProductId` + `Quantity` only; **server loads live `SellingPrice`** |
| Offline local receipt | Snapshots qty + price in local store |
| Offline outbox | Sends qty only → **server re-prices** on sync (**price drift risk**) |
| MAUI sell UX | `QuantityStepper` (step 1 or 0.001); no free weight keypad / Today’s Prices |

### Onboarding

Register → Personal → Start Business → plan + **required** Business Type → org + Main Branch → device → PIN → optional template → ready. No multi-BT activation step.

## Invariants (Phase 23)

1. **Classification ≠ commerce ≠ starter pack:** Business Type, Subscription Plan, Catalog Template, Global Catalog, Merchant Catalog remain distinct.
2. Primary Business Type remains required once set; additional types come only from **entitled** set.
3. Effective Global/Template visibility requires: eligible status ∧ BT intersection ∧ commercial catalog entitlement ∧ permission. **Server enforces.**
4. Merchant-owned products/history are never auto-deleted on entitlement downgrade.
5. Completed `SaleLine` quantity, UOM, and unit-price snapshot are immutable; product current price may change freely.
6. Money and measured quantities use `decimal` only (never `float`/`double` in domain/API contracts).
7. Offline sale remains transactionally local-first; sync must not rewrite historical qty/UOM/price snapshots (requires deliberate contract change vs today’s re-price behavior).
8. Owner does not bypass registered POS-device requirements for money operations.
9. Barcode rules unchanged: optional GS1 digits; SKU independent; weighted goods may have null barcode.
10. Platform Admin retains unfiltered Platform catalog management.

## Domain model (recommended)

### Entitlements

- Extend commercial catalog so a plan version can grant a **set of Business Type ids/codes** (prefer explicit association table or structured grant value — not N boolean fake features per type).
- Resolve **EffectiveOrganizationBusinessTypes** = `{ Primary } ∪ (activated entitled add-ons ⊆ plan-allowed)`.
- Keep snapshotting consistent with existing `EntitlementSnapshot` so POS/offline can cache effective BT codes when needed.

### Selling

- Add product **SellingMode**: `PerItem` | `ByWeight` (extensible later for `ByMeasure`).
- ByWeight: canonical inventory/sale unit **Kilogram**; gram entry normalizes to kg (`350 g → 0.350 kg`).
- Reuse existing `numeric(18,3)` qty / `numeric(18,2)` money; document precision in WP05.
- SaleLine already snapshots unit price; close the offline gap by carrying trusted snapshots on sync (WP08) under existing authority rules.

### Today’s Prices

- Update `CatalogProduct.SellingPrice` (current price) via authorized manage path; audit via existing audit writer if sufficient.
- Do not clone products per price change.

## Entitlement rules

| Actor / case | Visibility |
|---|---|
| Platform Admin | All Platform catalog/templates |
| Org with effective `{SariSari}` | Only SariSari-tagged Active globals + SariSari primary templates |
| Org with `{SariSari, VegetableVendor}` | Union of both |
| Downgrade removes VegetableVendor | Existing merchant veg products remain; new veg global discovery/import/templates blocked |

## Weighted-selling rules

- One cart; mode from product, not from org BT.
- Reject qty ≤ 0; enforce UOM decimal rules; line total = rounded money(product of snapshots).
- Price change after sale must not alter historical lines.
- Amount→weight deferred (WP note).

## Precision / rounding

| Kind | Rule |
|---|---|
| Money | 2 dp, AwayFromZero (`SaleMoney`) |
| Measured qty | ≤ 3 dp; reject excess precision |
| Whole qty | integer only |
| g→kg | divide by 1000 exactly in decimal |
| kg→g display | optional UI; storage remains kg |

## Downgrade rules

- No cascade delete of merchant catalog, inventory, or sales.
- Discovery/import/template APIs return 403/empty for removed BT.
- Historical reports unchanged.

## Offline / local-first

- Preserve device registration + offline grant model.
- Local receipt already snapshots; **outbox must stop silent re-price** for fidelity (WP08).
- Explicit server rejection ≠ network unreachable remains distinct.

## Authorization / security

- Keep product-local POS permissions + commercial feature codes.
- Today’s Prices / price edit: ManageCatalog (or narrower price permission if introduced) + online/offline authority.
- Server filters discovery; UI hide is insufficient.

## Migration strategy

- Prefer additive tables/columns; backfill SellingMode=`PerItem` for existing products.
- Qty/money columns already decimal — **no widening** expected for core qty.
- New plan–BT grant storage + optional org activated-BT table.
- LocalStore schema bump for selling mode + sync payload fields.
- No production auto-migrate; no destructive reset in phase flow.

## Backward compatibility

- Existing piece products behave as PerItem.
- Existing sales history valid.
- Single-primary orgs keep working; multi-BT is opt-in via plan + activation.
- Fix client query-param names as part of filtering WP.

## Work packages

| WP | Title | Notes |
|---|---|---|
| **WP01** | Architecture audit + phase contract | **Done** — [P23-WP01](../reports/P23-WP01-current-state-and-domain-design.md) |
| **WP02** | Subscription Business Type entitlement domain/persistence | **Done** — [P23-WP02](../reports/P23-WP02-business-type-entitlement-model.md) |
| **WP03** | Effective entitlement resolution + server enforcement | **Done** — [P23-WP03](../reports/P23-WP03-entitlement-enforcement.md) |
| **WP04** | Template + Global Catalog entitlement filtering | **Done** — [P23-WP04](../reports/P23-WP04-catalog-template-filtering.md) |
| **WP05** | Product selling mode / unit model | **Done** — [P23-WP05](../reports/P23-WP05-variable-quantity-product-domain.md) |
| **WP06** | Decimal quantity foundation across sales/inventory | **Done** — [P23-WP06](../reports/P23-WP06-sales-inventory-decimal-propagation.md) |
| **WP07** | Purchasing/returns/reporting propagation | **Done** — [P23-WP07](../reports/P23-WP07-purchasing-returns-reporting.md) |
| **WP08** | SQLite/offline/outbox/sync compatibility | **Done** — [P23-WP08](../reports/P23-WP08-offline-sale-snapshot-fidelity.md) |
| **WP09** | Weighted-sale MAUI UX | **Done** — [P23-WP09](../reports/P23-WP09-weighted-sale-maui-ux.md) |
| **WP10** | Today’s Prices workflow | **Done** — [P23-WP10](../reports/P23-WP10-todays-prices.md) |
| **WP10A** | Philippine default Business Types & starter templates | **Done** — [P23-WP10A](../reports/P23-WP10A-philippine-default-business-types-and-templates.md) |
| **WP10B** | POS commercial plan refresh (Starter/Growth/Pro + BT capacity) | **Done** — [P23-WP10B](../reports/P23-WP10B-pos-commercial-plan-refresh.md) |
| **WP11** | Onboarding multi-BT entitlement UX | **Done** — [P23-WP11](../reports/P23-WP11-onboarding-multi-business-type-ux.md) |
| **WP12** | Regression/security/edge-case tests | Matrix below |
| **WP13** | Documentation/closeout | Final report; no Device Verified claim |
| **WP14** | Physical Android validation prep only | Device `R58R61E3CAZ` — not run unless asked |

**Boundary revision vs prompt:** WP06 is thinner than originally implied because decimal qty already exists; effort shifts to WP05 (mode) + WP08 (offline price fidelity) + WP09 (UX). WP07 remains because returns/purchasing must be regression-proven for ByWeight, not greenfield types.

## Validation matrix (target)

See [P23-WP01](../reports/P23-WP01-current-state-and-domain-design.md) and later `P23-validation-matrix.md`. Must cover entitlement, weighted product, mixed cart, backward compatibility cases listed in the Phase 23 request.

## Explicit deferred work

- Amount→weight calculator (unless WP09 proves trivial).
- Scale device integration.
- Additional vendor niches beyond the WP10A Philippine defaults (extend via Ensure pattern).
- Effective-dated price lists beyond current price + audit.
- Physical device verification (WP14 prep only unless requested).

## Stop line for this Cursor run

WP01 only: investigation + phase contract + WP01 report + documentation commit. **Do not start WP02+.**
