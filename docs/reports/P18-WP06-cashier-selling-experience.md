# P18-WP06 — Cashier Selling Experience

| Field | Value |
|---|---|
| Status | **Code Complete and Build Verified** |
| Phase | [Phase 18](../phases/phase-18-mobile-personal-organization-and-pos-experience.md) |
| Implementation commit | `4b8b727` |
| Device validation | **Pending User Validation (phase-level; see P18-WP08)** |
| Date | 2026-08-03 |

## 1. Objective

Cashier home and selling experience: shift lifecycle, New Sale, cart/checkout, receipt, own sales history, Cashier restrictions.

## 2. Scope

Cashier-facing Mobile UX. Owner/Manager may enter the same selling UI via Start Selling without becoming Cashier.

## 3. Existing functionality reused

- Phase 17/8 sale checkout, cart service, shifts open/close, registers, sale detail/receipt fields, inventory reduction on completed sale
- Cashier capability matrix (void/return restrictions already hardened in Phase 17)

## 4. Backend / API work completed

- No new Cashier sale APIs in Phase 18; existing POS sales/shifts/registers/catalog endpoints reused

## 5. MAUI screens and flows completed

- `/cashier` — current register, start/continue/close shift, New Sale, own sales history link
- `/sales/new` — product search/lookup, cart, quantity changes, subtotal/tax/total (where setup/tax mode applies), cash tender/change, complete sale with `_saving` duplicate-submit guard, navigate to sale detail/receipt
- Shift open/close screens (existing)
- Sales list/detail for history and receipt display

## 6. Files / components changed (representative)

- `Maui/Components/Pages/Dashboards/CashierHome.razor`
- Existing `Sales/SaleCheckout.razor`, `SaleDetail.razor`, `Shifts/*`, `Registers/*` (wired from Cashier home)
- Selling-mode exit behavior on checkout cancel

## 7. Authorization and organization-isolation behavior

Cashier lacks capabilities denied by POS role matrix (e.g. returns/process return already restricted). Sales and shifts are organization-scoped. Selling mode for Owner/Manager does not change the effective role code.

## 8. Tests executed and totals

| Suite | Result |
|---|---|
| POS UnitTests | **339 passed** |
| POS IntegrationTests | **135 passed** |
| MAUI.Tests (sale page guards) | included in **73 passed** |

## 9. MAUI build result

**Build Verified**.

## 10. Emulator / device validation result

**Pending User Validation (phase-level; see P18-WP08)**.

## 11. Known limitations

- Barcode input depends on existing catalog lookup support (exact/search), not a separate scanner SDK integration claim
- Reprint/share only where existing document handoff capability exists
- Online-only checkout for core cash sale path (offline sale productization not claimed)

## 12. Deferred items

Device E2E Cashier journey; gateway payments; split tender.

## 13. Current status

Implemented · Tested · Build Verified · Pending User Validation (phase-level; see P18-WP08)

## 14. Commit reference

Implementation: `4b8b727`. Documentation reconciliation: Phase 18 docs tip on `main`.
