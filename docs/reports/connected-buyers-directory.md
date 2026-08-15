# Dedicated Connected Buyers Directory

Date: 2026-08-15  
Starting SHA: `02b924d6053d67d1f3aac65e63466d2d506b6029`  
Feature SHA: `a0f61da6102b8d7a95d1b36313bad8845eb4443d`  
Feature: polish/complete supplier-side Connected Buyers (MAUI + Org Web)

## Status

**Code complete (polish).** Feature already existed; this WP completes discoverability, copy, empty-state CTA, and customer-boundary tests.

## Delivered

- MAUI `/suppliers/connected/buyers` (+ detail): Active-only cards, direction copy, not-customer note, Review requests CTA when Pending > 0
- More hub → Connected buyers tile (in addition to Suppliers list)
- Org Web `/suppliers/buyers` tab: buyers-specific page header/subtitle
- Localization EN + fil-PH
- Tests: Active filter / Accept / Decline / Disconnect / ownership / Accept≠Customer / inventory-neutral Accept / MAUI + Org Web UI guards

## Explicit exclusions

- Add as customer (deferred)
- Catalog exposure UI for suppliers (separate gap)
- Phase closeout; Browser/Device Verified remain **NO**

## Domain

Reuses `ConnectedSupplierRelationship`. Supplier view = `SupplierOrganizationId == current org`. Active directory filters Status=Active client-side.

## Owner validation scenario

M01 buyer / M02 supplier: request → accept → Connected buyers shows buyer; Customers does not; no inventory change.

## Phases

Phase 21 / 25 / 26 remain **OPEN** — Owner Validation Pending.
