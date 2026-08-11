# P23-WP01 — Current State and Domain Design

[Phase 23](../phases/phase-23-multi-business-entitlements-and-variable-quantity-selling.md) | [Portfolio](../portfolio-progress.md)

| Field | Value |
|---|---|
| Status | **Planned** (architecture contract; not Implemented) |
| Date | 2026-08-11 |
| Repo HEAD at audit | `c03894f` on `main` |
| Pushed | No (documentation commit only for this WP) |

## Status legend

This report is **Planned** for the phase contract. It is **not** Device Verified and **not** Production Ready.

## Confirmed exists today

### Business types and catalog

- Dynamic `BusinessType` + Admin CRUD (P20); join tags on global products/categories.
- `CatalogTemplate.PrimaryBusinessTypeId`.
- `PlatformOrganization.PrimaryBusinessTypeId` (Start Business; immutable) — migration `20260810205544_AddOrganizationBranchesAndPosDevices`.
- Merchant list/search endpoints accept optional BT filters when query params bind correctly.
- Platform Admin manages full global catalog without merchant entitlement gates.

### Commercial entitlements

- Plan grants: feature codes with boolean and numeric/quantity limits (branches, staff, POS devices, store/Utang features).
- Entitlement snapshots/overrides pipeline exists.

### Variable quantity / money (POS)

- Sale/inventory/purchasing/returns quantities are already `decimal` (`numeric(18,3)`).
- Money `numeric(18,2)` with `SaleMoney` AwayFromZero rounding.
- Measured UOMs include Kilogram/Gram (and L/ml/m); precision gated (3 dp measured / whole for countable).
- Sale lines snapshot unit price at checkout **on the server** from live catalog price.
- Local offline receipts snapshot qty+price for display; outbox sync sends ProductId+Quantity and **server re-prices**.

### Onboarding

- Start Business chooses plan + one Business Type; optional template import after device/PIN.

## Actually missing

| Capability | Gap |
|---|---|
| Multi-BT subscription entitlement | Plans cannot grant a set of Business Types |
| Org effective BT set | Only single primary; no activated add-ons |
| Server entitlement filtering | Discovery/import not gated by entitled BTs |
| Client filter wiring | POS clients send wrong query param names → filters no-op |
| SellingMode | No PerItem/ByWeight product flag |
| Weight entry UX | Stepper only; no grams/weight keypad workflow |
| Today’s Prices | No bulk current-price workflow |
| Offline price fidelity | Sync re-prices from live catalog (drift) |
| Future vendor types | Model supports codes, but not entitled/seeded for Fish/Vegetable/… packs |

## Recommended data-model changes

1. **Plan ↔ BusinessType entitlement** (association or structured grant) on plan versions; include in entitlement snapshots.
2. **Organization activated Business Types** ⊆ entitled set (primary always included).
3. **Effective BT resolver** used by merchant catalog/template APIs.
4. **CatalogProduct.SellingMode** (+ Platform global product mapping if imported): `PerItem` default, `ByWeight` for fresh goods; canonical kg for weight.
5. **Checkout/outbox contract** extension so offline/online sales preserve UnitPriceSnapshot + qty + UOM without silent re-price (authority-safe).
6. Optional: price-change audit via existing audit writer; avoid duplicate price-history table unless audit is insufficient.

## Migration impact

| Area | Expectation |
|---|---|
| Qty/money columns | **No type change** (already decimal 18,3 / 18,2) |
| Platform | Additive BT entitlement storage + org activation rows |
| POS PostgreSQL | Additive selling-mode (and any checkout snapshot fields if persisted beyond line) |
| LocalStore | Schema bump for selling mode + outbox payload |
| Backfill | Existing products → `PerItem`; existing orgs → effective `{Primary}` only |

## WP sequence (recommended)

WP01 (this) → WP02 entitlement domain → WP03 resolution/enforcement → WP04 catalog/template filtering → WP05 selling mode → WP06 confirm decimal foundation → WP07 purchasing/returns/reports → WP08 offline/sync fidelity → WP09 weighted UX → WP10 Today’s Prices → WP11 onboarding multi-BT → WP12 tests → WP13 docs closeout → WP14 device prep only.

## Risks / gaps

1. Offline re-price is a **correctness** issue for fresh goods; must be fixed before claiming weighted offline safety.
2. Client/API BT query param mismatch already breaks intended filtering.
3. Immutable primary BT + no add-on activation blocks mixed-business UX until WP02–WP03/WP11.
4. Extending FeatureGrantSpec vs new table — choose one in WP02; avoid N boolean features per type.
5. Platform `ProductUnit` vs POS `UnitOfMeasure` (`Meter` POS-only) remains an import mapping residual.

## Files / projects most affected (future WPs)

- `ExItS.Platform.Domain|Application|Infrastructure|Api|Admin` — catalog, organizations, entitlements, merchant discovery
- `ExItS.PinoyBusinessPOS.Domain|Application|Infrastructure|Api|ApiClient|LocalStore|Maui` — product mode, checkout, cart, offline outbox
- `ExItS.DesignSystem` — quantity entry controls
- Tests under `tests/ExItS.Platform.*` and `tests/ExItS.PinoyBusinessPOS.*`

## Checks performed (WP01)

- `git status` / branch `main` / HEAD `c03894f`
- Read P20/P22 docs pointers and phase index
- Code audit: BusinessType, org primary BT, plan grants, merchant discovery endpoints/clients, SaleMoney/SaleLine, LocalStore/outbox, MAUI cart
- No application code changes in WP01
- No migrations applied
- No physical device run

## Tests executed

None (documentation-only WP). Prior unrelated suite results not claimed for P23.

## Known gaps / deferred

- Implementation of WP02+
- Amount→weight, scale hardware, full vendor-type seeding, device verification

## Commit hash

Documentation commit for this WP: see `git log` message `docs(p23): add Phase 23 multi-BT entitlements and variable-quantity plan` (hash reported in the Cursor closing response).
