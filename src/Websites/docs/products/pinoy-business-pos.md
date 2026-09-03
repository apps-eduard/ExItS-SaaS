# Product Truth: Pinoy Business POS

> This document describes what Pinoy Business POS **actually is**, based on repository code inspection.
> It is the source of truth for all marketing claims about this product.
> See [pages/pos.md](../pages/pos.md) for how `/pos` presents it.

---

## Product Identity

| Field | Value |
|---|---|
| Product name | Pinoy Business POS |
| Platform product code | `pinoy-business-pos` (inferred from codebase) |
| Readiness | **CONFIRMED** — substantial working implementation |
| Implementation | ExItS.PinoyBusinessPOS.Api, .Application, .Domain, .Infrastructure, .React (web client), .Maui (mobile scaffold) |
| Web client | React (Vite + TypeScript, offline-capable PWA) |

---

## Evidence Classification

### CONFIRMED Capabilities (verified in codebase)

**Selling**
- Real-time POS cart and sale recording
- Offline-capable selling (local store, outbox sync, offline operating grant)
- Cashier shift management (open/close/cash tracking)
- Register management
- Sale returns
- Payment recording
- Idempotent mutation design (duplicate transaction protection)
- Stock guard (prevent selling without stock where applicable)

**Catalog**
- Product catalog management (categories, items, variants)
- Product images
- Branch-specific pricing overrides
- Catalog import
- Product business usage tracking
- Catalog lookup / search

**Inventory**
- Stock tracking per branch
- Inventory movements / adjustments
- Expiration tracking (FIFO-capable)
- Branch stock — branch owns stock (not Area)

**Customers**
- Customer list management per branch
- Customer credit (Utang) — customer credit limit, balance tracking
- Personal customer linking (ExItS Personal platform users linked as customers)
- Linked customer management

**Customer Ordering (Storefront)**
- Public store landing page (dynamic ordering readiness evaluation)
- Customer ordering enabled/disabled per branch
- Online orders paused per branch
- Customer order accept / reject workflow
- Order status notifications to Personal buyers (in-app notification system)
- Personal merchant cart (browser-local durable cart)
- Ordering available dynamically evaluated against branch readiness, entitlements, operating hours, and delivery policy

**Suppliers**
- Supplier management (list, detail)
- Purchase orders to suppliers
- Direct purchase receipts
- Supplier payables tracking

**Connected ExItS Suppliers**
- Connect to other ExItS platform organizations as suppliers
- Supplier connection request / approval workflow
- Connected supplier can push catalog to buyer organization
- Branch-scoped connected supplier authorization

**Branches**
- Multi-branch organization support
- Branch-specific operations (stock, staff, pricing, ordering)
- Branch readiness checks (customer ordering readiness)
- Branch operating hours and delivery policy

**Areas**
- Area grouping of branches
- Area-level staff oversight access
- Area-level inventory rollup reporting (read-only rollup — Area does NOT own stock)

**Staff and Roles**
- Owner / Manager / Cashier role presets
- Grant-based authorization (not role-name hard-coded)
- Staff invitation and setup
- Per-branch staff access scoping
- Area Manager access level

**Reporting**
- Cashier shift reports
- Sales reporting
- Inventory reports
- Supplier payable statements

**Account and Auth**
- ExItS Platform-based identity (no product-local auth)
- Product access entitlement checked server-side
- Offline PIN (dev/testing — not production auth)
- Device management

**Onboarding**
- Guided onboarding flow
- Business setup presets
- Branch readiness wizard

---

### IN DEVELOPMENT or Partially Implemented

- MAUI mobile client — scaffold exists (`ExItS.PinoyBusinessPOS.Maui`); production readiness unknown from code inspection alone
- Some reporting areas may have partial implementation — verify before detailed marketing
- Expense tracking — API folder present (`Expenses`); extent of implementation TBD

---

### UNKNOWN (insufficient evidence from inspection alone)

- Specific payment method integrations (beyond cash recording)
- BIR compliance / official receipts
- Specific SLA or uptime commitment
- Production hosting infrastructure

---

## Audience

| Segment | Description |
|---|---|
| Solo / personal seller | Single branch, small catalog, Utang management |
| Small retail business | Multi-staff, inventory control, purchase orders |
| Multi-branch retailer | Area grouping, branch stock rollups, connected suppliers |
| Personal buyer | Uses ExItS Personal to browse/order from linked merchants |

---

## Architecture Facts (relevant to marketing)

- Web client runs offline (service worker, local store) — continues selling when internet is unavailable
- Stock is branch-owned — Area is a reporting/organizational grouping only
- Supplier connections are between ExItS-registered organizations
- Ordering availability is dynamically evaluated — not permanently on or off
- Customer link (Personal ↔ Merchant) requires explicit approval by the merchant

---

## Prohibited Marketing Claims

Do not claim:
- Any specific ₱ prices without WEB-D-01 resolution
- BIR accreditation / official receipt compliance without legal verification
- Specific uptime/SLA figures without operational data
- Payment gateway integrations without verified implementation
- Mobile app availability without confirming MAUI production readiness
- "AI-powered" or automation claims not in codebase
- Customer counts, transaction volumes, or business statistics without verified data
