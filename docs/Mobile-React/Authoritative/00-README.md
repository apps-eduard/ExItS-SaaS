# Authoritative Domain & React Migration Map

**Status:** AUTHORITATIVE for React migration planning
**Baseline branch:** `feat/pos-react-client`
**Baseline SHA:** `721cc946d61ccb193c8c69b76b6f1ff726526270`
**Package type:** Documentation only — no production code was changed to produce this set.

## Purpose

This documentation set reconstructs the **current effective** ExItS / PinoyBusinessPOS architecture and business domain from source, tests, and superseding implementation reports. It exists so React migration work packages stop jumping screen-to-screen without a proven domain contract.

These docs do **not** replace backend authorization or domain code.
They document the contract React must follow.

## What this is / is not

| Is | Is not |
|----|--------|
| Current-state reconstruction for React migration | A rewrite of Phases 1–29 |
| Evidence-backed capability inventory | Authorization to implement production features |
| Dependency-ordered migration roadmap | A claim that React already has POS parity |
| Separation of CURRENT vs OWNER-CONFIRMED CHANGE | Permission to invent missing backend contracts |

Historical phase documents, ADRs, and reports remain **historical evidence**. Later source and tests supersede earlier planning text when they conflict.

## Evidence hierarchy

1. Current backend / domain / application / API / database implementation
2. Current automated tests
3. Current MAUI implementation
4. Latest implementation reports / ADRs / engineering documents
5. Phase documents and historical reports
6. Current React implementation
7. Product-owner confirmed requirements recorded in [Migration/owner-decision-register.md](Migration/owner-decision-register.md)

Never mark something CURRENT solely because an old planning document proposed it.
Never mark something MISSING solely because an early phase excluded it if later source implemented it.

## Status taxonomy

Every important capability uses one primary status:

| Status | Meaning |
|--------|---------|
| `PROVEN_CURRENT` | Current source + tests prove the capability |
| `PROVEN_PARTIAL` | Some implementation exists; required behavior is incomplete |
| `PROVEN_MISSING` | Audit proves the capability does not currently exist |
| `SUPERSEDED` | Historical behavior replaced by later implementation |
| `OWNER_CONFIRMED_CHANGE` | Owner requires behavior different from (or beyond) current implementation |
| `DEFERRED` | Deliberately not part of the current migration target |
| `UNRESOLVED` | Evidence insufficient or contradictory |

## CURRENT vs OWNER-CONFIRMED CHANGE

- **CURRENT** = what the repository does today (source/tests).
- **OWNER-CONFIRMED CHANGE** = owner requirement that may match, extend, or differ from CURRENT.
- When they match, document both as aligned.
- When they differ, do **not** pretend CURRENT already equals the owner requirement. Schedule backend contract work before React UI that depends on the new contract.

## Path convention

Created under `docs/Mobile-React/Authoritative/` to match the existing `docs/Mobile-React/` casing and keep one React-migration documentation root. Do not scatter competing authoritative roots under historical phase folders.

## Index

### System context

| Doc | Role |
|-----|------|
| [01-system-context-and-scope-model.md](01-system-context-and-scope-model.md) | Platform / Personal / Organization / POS boundaries |
| [02-identity-personal-organization-lifecycle.md](02-identity-personal-organization-lifecycle.md) | Identity, sessions, staff aliases, Start a Business |
| [03-product-subscription-entitlement-lifecycle.md](03-product-subscription-entitlement-lifecycle.md) | Products, plans, subscriptions, entitlements, local roles |
| [04-organization-branches-staff-devices.md](04-organization-branches-staff-devices.md) | Org config, branches, staff, devices, fulfillment |
| [05-pos-domain-overview.md](05-pos-domain-overview.md) | POS bounded-context map |
| [06-react-ui-ux-and-responsive-foundation.md](06-react-ui-ux-and-responsive-foundation.md) | Mobile-first UI DoD, shared components, ListToolbar |

### POS domain

| Doc | Role |
|-----|------|
| [POS/product-catalog.md](POS/product-catalog.md) | Categories, products, import, global catalog |
| [POS/uom-selling-modes-and-conversions.md](POS/uom-selling-modes-and-conversions.md) | UOM, ByWeight, product units, shared base inventory |
| [POS/pricing-and-price-authority.md](POS/pricing-and-price-authority.md) | Selling price, Today’s Prices, override gaps |
| [POS/inventory.md](POS/inventory.md) | Tracking, movements, oversell rules |
| [POS/expiry-batches-and-stock-layers.md](POS/expiry-batches-and-stock-layers.md) | Lots, FEFO, TracksExpiration |
| [POS/suppliers-and-connected-suppliers.md](POS/suppliers-and-connected-suppliers.md) | Manual + connected suppliers |
| [POS/purchasing-and-receiving.md](POS/purchasing-and-receiving.md) | PO / GRN / receive-only inventory |
| [POS/registers-devices-and-shifts.md](POS/registers-devices-and-shifts.md) | Branch ≠ Register ≠ Device |
| [POS/sell-floor-cart-and-checkout.md](POS/sell-floor-cart-and-checkout.md) | Sell floor UX and cart |
| [POS/sales-returns-and-sales-documents.md](POS/sales-returns-and-sales-documents.md) | Sales, returns, Transaction Summary |
| [POS/customers-business-utang-and-linked-personal.md](POS/customers-business-utang-and-linked-personal.md) | Customers / Business Utang |
| [POS/customer-ordering-pickup-and-delivery.md](POS/customer-ordering-pickup-and-delivery.md) | Storefront, pickup, delivery |
| [POS/reports.md](POS/reports.md) | Operational reports |
| [POS/offline-local-first-and-device-behavior.md](POS/offline-local-first-and-device-behavior.md) | Offline matrix |

### Migration

| Doc | Role |
|-----|------|
| [Migration/backend-contract-map.md](Migration/backend-contract-map.md) | Capability → backend contract |
| [Migration/maui-capability-map.md](Migration/maui-capability-map.md) | MAUI current surface |
| [Migration/react-current-state.md](Migration/react-current-state.md) | React branch inventory |
| [Migration/capability-parity-matrix.md](Migration/capability-parity-matrix.md) | Backend / MAUI / React parity |
| [Migration/owner-decision-register.md](Migration/owner-decision-register.md) | Owner-confirmed requirements |
| [Migration/unresolved-domain-decisions.md](Migration/unresolved-domain-decisions.md) | True unresolved items only |
| [Migration/dependency-graph.md](Migration/dependency-graph.md) | Prerequisite chains |
| [Migration/react-migration-roadmap.md](Migration/react-migration-roadmap.md) | Complete proposed WP sequence |
| [Migration/validation-matrix.md](Migration/validation-matrix.md) | How future parity is proven |
| [Migration/master-run-execution-protocol.md](Migration/master-run-execution-protocol.md) | 10-WP batches, per-WP push, hard stops |

## Update policy

1. Update this set when backend/MAUI/React contracts change materially.
2. Prefer amending CURRENT status from source evidence, not from aspirational plans.
3. Record owner decisions in the Owner Decision Register; do not silently rewrite CURRENT.
4. Future React implementation work packages **must reference** these docs (parity matrix + dependency graph + roadmap WP id + UI foundation + master-run protocol).
5. Do not treat older `Implementation-Readiness/` or historical phase docs as higher authority than this set for CURRENT behavior.
6. Do not implement desired staff person-link React UX before **RMAP-B00**.
7. Do not bypass **RMAP-00** for visual packages unless the roadmap marks a WP non-UI.
8. First implementation batch order is **APPROVED PROPOSED MASTER RUN 01** in [Migration/react-migration-roadmap.md](Migration/react-migration-roadmap.md) (RMAP-00 → B00 → 01 → 01b → 02 → 03 → 04 → 05 → 06 → 07). Documenting it does not authorize starting implementation.

## Related non-authoritative Mobile-React docs

Planning and readiness docs under `docs/Mobile-React/` (README, Implementation-Readiness, Reports) remain useful history. When they conflict with this Authoritative set on **current** behavior, this set wins after source verification.
