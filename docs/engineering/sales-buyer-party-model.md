# Sales buyer party model

[Identity boundaries](../architecture/personal-organization-identity-boundaries.md) · [QR spec](../specs/identity/public-user-id-and-qr.md) · [Connected suppliers](connected-exits-suppliers.md)

## Rule

Every POS sale is owned by the **selling organization** (`Sale.OrganizationId`).

The buyer is a **counterparty**, never the transaction owner.

| Buyer kind | Meaning |
|---|---|
| WalkIn | No customer / anonymous checkout |
| ExternalCustomer | Seller-owned `POSCustomer` without ExItS identity on the sale |
| Personal | ExItS Personal public identity (`EX-…`) as buyer |
| Organization | ExItS Business identity (`ORG######`) as buyer |

Actor (`RecordedBy`) is who operated the till. It does not own the sale.

Operational mutations (sales, inventory, orders, shifts) store actor GUIDs on authoritative aggregates — not client-supplied ids. Fulfillment handoffs store `ReadyBy`, `DeliveredBy`, etc. Provider payment finalization uses `ProviderFinalizedBySystem` instead of attributing gateway work to a fake user. See [P28-WP15D operational actor traceability](../reports/P28-WP15D-operational-actor-traceability.md).

## Customer record vs ExItS identity

A seller may keep an org-owned `POSCustomer` profile and optionally link:

- Personal public user id, or
- Buyer organization id + public organization id

Those links do **not** grant the seller access to Personal Utang, Personal contacts, or the buyer organization's private POS data.

Do not auto-merge customers by name/phone/email.

## QR purpose matrix (checkout / suppliers)

| Flow | Personal QR | Business QR | Device QR |
|---|---|---|---|
| Sale customer selection | Allow | Allow | Reject |
| Connected supplier | Reject | Allow | Reject |
| Device registration | Reject | Reject | Allow |
| Personal contact flow | Allow | Reject / explicit business action | Reject |

Server-side connected-supplier requests require Business QR / `ORG######` and reject Personal/device payloads even if a Guid is forged by the client.

## Business vs Personal Utang

POS Product-Based Utang remains seller-organization owned via `POSCustomer` + `CreditEntry`.

Linking a Personal ExItS buyer on a credit sale does **not** write Personal Utang.

## Offline / LocalStore

LocalStore remains **v9** (file per user/org/product). Buyer party fields travel on `CheckoutSaleRequest` / receipt snapshots; no LocalStore schema bump.

Org switch continues to clear SaleCart and selling/device context so Org A buyer selection cannot bleed into Org B.

## Ownership transfer readiness

Historical sales keep `OrganizationId` and buyer snapshots. Buyer Organization identity stays on `BuyerOrganizationId` / public org id, not the current owner user.

## Linked ExItS buyer purchase projection

### Personal (RMAP-B04 — implemented)

A Completed/Voided sale with Personal (`EX-…`) `SaleBuyerParty` may be projected **read-only** into the authenticated Personal linked-customer statement / receipt APIs (`/api/v1/pos/personal/linked-customers/...`). Seller `Sale` remains authoritative.

### Organization buyer Direct Purchases (POS-B2B-DIRECT-PURCHASE-HISTORY-01 — implemented)

A sale with `BuyerPartyKind = Organization` and `BuyerOrganizationId =` the authenticated buyer organization is projected **read-only** into that organization's Direct Purchases history.

| Rule | Requirement |
|------|-------------|
| Authority | Seller `Sale` remains authoritative; do not transfer ownership or duplicate as buyer `DirectPurchaseReceipt` |
| Unified page | `/purchasing/direct-purchases` shows Local (`DirectPurchaseReceipt`) + B2B (projected Sale) |
| APIs | `GET /api/v1/pos/purchasing/direct-purchases` and `GET .../b2b/{saleId}` |
| Scope | Buyer sees only Sales where `BuyerOrganizationId` matches session organization |
| Privacy | Seller cost / profit / margin / internal notes / actor ids not exposed |
| Inventory | No automatic buyer inventory mutation, goods receipt, or product-id mapping |
| Relationship | Current Active connection is **not** required for historical visibility |
| Status | Voided Sales remain visible as Voided |
| Isolation | No cross-org shortcut; authorization uses POS workspace organization |

UI CTA **Record direct purchase** continues the existing local `DirectPurchaseReceipt` write flow. Purchase Orders remain a separate Purchasing module.
