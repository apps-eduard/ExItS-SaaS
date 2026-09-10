# POS-ORG-B2B-CUSTOMER-RELATIONSHIP-01

## Summary

Connected buyer organizations are modeled as **supplier-side Business Customers** that project over `ConnectedSupplierRelationship` (OrganizationConnection). No duplicate Organization or POSCustomer identity is created on accept.

One canonical ExItS Organization identity is shown as **B2B**. Two transaction mechanisms exist under that relationship:

1. **Direct** — seller-operated Sell → Checkout → select B2B Organization → Cash/GCash Sale
2. **Purchase order** — buyer-operated Suppliers → connected supplier → Connected Purchase Order

## Decisions

| Key | Value |
|---|---|
| ORG_TO_ORG_RELATIONSHIP_UI | B2B |
| ORGANIZATION_IDENTITY_MODEL | Single Platform Organization identity; connection references BuyerOrganizationId / SupplierOrganizationId |
| BUSINESS_CUSTOMER_MODEL | Supplier projection of Active (optional Disconnected) buyer relationships |
| BUSINESS_CUSTOMER_PERSISTENCE | CONNECTION_ONLY |
| RELATIONSHIP_DIRECTIONALITY | Directional; A→B and B→A may both exist as separate open rows |
| B2B_CONNECTION_SCOPE | ORGANIZATION |
| VISIBLE_RELATIONSHIP_TAG | B2B |
| DIRECT_TRANSACTION_PATH | SELL_CHECKOUT_SALE |
| PO_TRANSACTION_PATH | CONNECTED_PURCHASE_ORDER |
| DIRECT_USES_CUSTOMER_ORDER | NO |
| DIRECT_USES_STOREFRONT | NO |
| B2B_DUPLICATE_POSCUSTOMER | NO |
| CONNECTED_BUYER_SEPARATE_PRIMARY_UI | NO |
| SUPPLIER_CUSTOMER_VIEW | Customers → Businesses (`/customers?kind=businesses`) + `/customers/business/{connectionId}` |
| BUYER_SUPPLIER_VIEW | Existing Suppliers / relationships `view=buyer` (same connection id) |
| PERSONAL_CUSTOMER_SEPARATION | POSCustomer / Utang / Personal links remain separate |
| CATALOG_PRICING_SOURCE_OF_TRUTH | `ConnectedSupplierRelationship.CatalogSharingMode` + `CustomerDiscountPercent` + `ConnectedBuyerProductShare` |
| BUSINESS_CUSTOMER_RETAIL_CHECKOUT | SUPERSEDED — Direct B2B uses POS Sale buyer party Organization at Sell checkout |
| B2B_DIRECT_CHECKOUT | YES — checkout-search returns Active Business rows; Sale validates Active relationship via `BuyerConnectionId` |
| B2B_DIRECT_UTANG | BLOCKED — Product-Based Utang remains POSCustomer-only; PO payment terms unchanged |
| B2B_OFFLINE_CHECKOUT_SELECTION | NO — B2B Organization selection is online-only |
| BUSINESS_CUSTOMER_CODE | DEFERRED |
| PAYMENT_TERMS | DEFERRED |
| N_PLUS_ONE | NO — list uses batch share stats + one eligible-product count |

## Direct vs Purchase Order

| Path | Who enters | Aggregate | Pricing |
|---|---|---|---|
| Direct | Seller cashier (Sell → Checkout) | `Sale` with `BuyerPartyKind=Organization` | Normal retail / effective selling price |
| Purchase order | Buyer organization (Suppliers → Create PO) | `PurchaseOrder` / `ConnectedPurchaseOrder` | Connected B2B / PO pricing |

Do **not** use CustomerOrder / MerchantShop / storefront for B2B Direct.

## API

- `GET /api/v1/pos/connected-suppliers/business-customers`
- `GET /api/v1/pos/connected-suppliers/business-customers/{connectionId}`
- `GET /api/v1/pos/customers/checkout-search` — CreateSale; returns `kind=Customer|Business` (Active B2B only for Business)
- `POST /api/v1/pos/sales` — optional `buyerConnectionId` / Organization buyer party; server resolves Active relationship

Detail may refresh display name via Platform public organization resolve (snapshot retained as fallback). Supplier cannot edit buyer Organization identity.

## Explicit exclusions

- No BusinessCustomerAccount table
- No duplicated discount/catalog policy fields
- No CustomerOrder / storefront Direct B2B path
- No POSCustomer created merely for an ExItS Organization Direct sale
- No Utang merge with B2B Direct
- No separate Connected Buyers primary React directory
- Customer code / internal notes / payment terms deferred
- Future buyer-side read-only projection of seller Sales is out of scope here

### Follow-up / supersession

Organization buyer direct-sale history was implemented later by
**POS-B2B-DIRECT-PURCHASE-HISTORY-01** (buyer Direct Purchases unified history; seller Sale remains authoritative).
See [POS-B2B-DIRECT-PURCHASE-HISTORY-01.md](./POS-B2B-DIRECT-PURCHASE-HISTORY-01.md).

Seller-initiated Business Customer invitations, `InitiatedByParty`, unified Connection requests inbox,
Pending checkout visibility (non-selectable), and Accept/Decline consent gating were implemented later by
**POS-B2B-BUSINESS-CONNECTION-CONSENT-LIFECYCLE-01** (2026-09-10).
See [POS-B2B-BUSINESS-CONNECTION-CONSENT-LIFECYCLE-01.md](./POS-B2B-BUSINESS-CONNECTION-CONSENT-LIFECYCLE-01.md).

Historical note: this report's Active-only Business Customer / checkout-search language described the
then-current surface; Pending is now included for consent discoverability without selectable checkout.
