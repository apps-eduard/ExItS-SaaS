# Connected supplier Browse / Linked products UX

Date: 2026-08-16  
Related design: [connected-exits-suppliers.md](../engineering/connected-exits-suppliers.md)  
Phase 1 baseline: [connected-exits-suppliers-phase-1.md](connected-exits-suppliers-phase-1.md)

## Status

**Code Complete (MAUI UX follow-up).** Not Device Verified. Not Browser Verified. **Not Production Ready.**

## Implementation commits

| SHA | Message |
|---|---|
| `3a63a015` | `feat(maui): improve connected supplier browse catalog UX` |
| `c6e669fc` | `feat(maui): polish browse and linked products UX` |
| _(docs tip)_ | `docs(maui): record browse/linked products UX feature hashes` |

## Delivered capability

- **Browse products** (`ConnectedSupplierCatalog.razor`): stacked search chrome, loading / error / empty / no-match states, auto-load first shared page online, Link and use, EN + fil-PH copy clarifying that empty means the supplier has not shared exposures
- **Linked products** (`LinkedSupplierProducts.razor`): stacked search + Update sync, offline banner with local list, empty / no-match states, Browse products CTA, relationship resolve when query missing
- UI guards: `ConnectedSupplierCatalogUiGuardTests`, `LinkedSupplierProductsUiGuardTests`

## Explicit exclusions

- Supplier-side catalog **exposure** management UI (supplier must still share products before Browse shows items)
- Full supplier catalog offline download
- Device / Browser Verified claims

## Persistence / migrations

Unchanged. Selective linked-product LocalStore projection and online catalog search rules unchanged.

## Build / test evidence

- `dotnet test tests/ExItS.PinoyBusinessPOS.Maui.Tests/... --filter FullyQualifiedName~SupplierProductsUiGuard|FullyQualifiedName~ConnectedSupplierCatalog` — passed (2)

## Security / portfolio independence

- No credentials or PHI committed
- No HealthCare tree; Platform/Product DB boundaries unchanged

## Exact next

Owner device retest of Browse + Linked products on MAUI. Supplier exposure UI remains deferred unless authorized.
