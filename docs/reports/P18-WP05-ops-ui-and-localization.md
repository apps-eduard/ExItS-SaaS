# P18-WP05 — Ops UI and localization

## Summary

Existing Phase 17 selling/shift/catalog/inventory/receipt/report screens remain the operational UI; Phase 18 wires role homes and More hub to them, removes DeferredPage placeholders, and adds EN/fil-PH keys for new journeys.

## Delivered

- `MoreHub` replaces `DeferredPage` for `/more`
- Localization keys for auth/personal/org/role/selling-mode strings (EN + fil-PH)
- Owner/Manager dashboards link to products, inventory, registers, shifts, sales, reports, voids via sale detail

## Tests

Updated Maui foundation/catalog/sale page guards; localization key presence via existing guard tests for catalog/sales.
