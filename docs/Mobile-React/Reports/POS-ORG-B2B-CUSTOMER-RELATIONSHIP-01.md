# POS-ORG-B2B-CUSTOMER-RELATIONSHIP-01

## Summary

Connected buyer organizations are modeled as **supplier-side Business Customers** that project over `ConnectedSupplierRelationship` (OrganizationConnection). No duplicate Organization or POSCustomer identity is created on accept.

## Decisions

| Key | Value |
|---|---|
| ORGANIZATION_IDENTITY_MODEL | Single Platform Organization identity; connection references BuyerOrganizationId / SupplierOrganizationId |
| BUSINESS_CUSTOMER_MODEL | Supplier projection of Active (optional Disconnected) buyer relationships |
| BUSINESS_CUSTOMER_PERSISTENCE | CONNECTION_ONLY |
| RELATIONSHIP_DIRECTIONALITY | Directional; A→B and B→A may both exist as separate open rows |
| B2B_CONNECTION_SCOPE | ORGANIZATION |
| SUPPLIER_CUSTOMER_VIEW | Customers → Businesses (`/customers?kind=businesses`) + `/customers/business/{connectionId}` |
| BUYER_SUPPLIER_VIEW | Existing Suppliers / relationships `view=buyer` (same connection id) |
| PERSONAL_CUSTOMER_SEPARATION | POSCustomer / Utang / Personal links remain separate; Business Customers are not checkout parties |
| CATALOG_PRICING_SOURCE_OF_TRUTH | `ConnectedSupplierRelationship.CatalogSharingMode` + `CustomerDiscountPercent` + `ConnectedBuyerProductShare` |
| BUSINESS_CUSTOMER_RETAIL_CHECKOUT | DEFERRED |
| BUSINESS_CUSTOMER_CODE | DEFERRED |
| PAYMENT_TERMS | DEFERRED |
| N_PLUS_ONE | NO — list uses batch share stats + one eligible-product count |

## API

- `GET /api/v1/pos/connected-suppliers/business-customers`
- `GET /api/v1/pos/connected-suppliers/business-customers/{connectionId}`

Detail may refresh display name via Platform public organization resolve (snapshot retained as fallback). Supplier cannot edit buyer Organization identity.

## Explicit exclusions

- No BusinessCustomerAccount table
- No duplicated discount/catalog policy fields
- No retail checkout selection of Business Customers
- No Utang merge with B2B
- Customer code / internal notes / payment terms deferred
