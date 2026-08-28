# POS-EXPIRATION-SETTINGS-PAGE-01

Dedicated Inventory expiration settings page for tracking lifecycle, near-expiry warning, and legacy lot repair.

| Field | Value |
| --- | --- |
| Status | **Complete** |
| Package | `ExItS.PinoyBusinessPOS.React` (+ enable-tracking repair in Application) |
| Branch | `feat/organization` |
| Start SHA | `314dcc64512a73c4223df07a2bdf34531fc505c6` |

## Flags

| Flag | Value |
| --- | --- |
| `EXPIRATION_SETTINGS_ROUTE` | `/inventory/:productId/expiration` |
| `INVENTORY_DETAIL_OWNS_LOTS` | **YES** |
| `PRODUCT_EDIT_DUPLICATE_SETTINGS` | **NO** |
| `PRODUCT_CREATE_EXPIRATION_CONFIG` | **YES** |
| `TRACKED_ONHAND_WITHOUT_LOTS_ROOT_CAUSE` | `legacy enable-before-init (pre POS-EXPIRATION-TRACKING-INITIALIZATION-01)` — tracking could be ON with positive OnHand and zero lot coverage |
| `LEGACY_EXPIRY_REPAIR` | **PASS** (same `EnableExpirationTracking` command allocates lots when already ON + lotTotal = 0; OnHand unchanged) |

## Page ownership

| Surface | Owns |
| --- | --- |
| Product Create | Initial Track expiration + Near-expiry warning (+ opening stock) |
| Product Edit | Catalog master data; expiration **summary + Manage link only** |
| Expiration Settings | Enable / disable / warning days / repair assign-dates |
| Inventory Detail | OnHand, Good/Near/Expired, stock lots, adjustment, movements |
| Receiving | Actual expiry on incoming stock |

## Delivered

- Route `:productId/expiration` under inventory (sibling of org-wide `expiration` list).
- `ExpirationSettingsPage` reuses `EnableExpirationTrackingDialog`.
- Inventory Detail: compact `ON · {n}-day warning` + Manage link; disable removed; setup-required banner when ON + OnHand &gt; 0 + no lots.
- Catalog edit: no duplicate editable expiration form.
- Backend repair path on enable when already tracking and lot total is zero.

## Explicit exclusions

- No product-level “expiry date” setting.
- No duplicate Stock Lots UI on settings page.
- PinoyBusinessPOS Blazor UI not in scope.

## Validation

- Backend: `EnableExpirationTrackingUseCaseTests` (incl. legacy repair).
- React: inventory feature tests + typecheck / lint / test / build as recorded in commit evidence.
