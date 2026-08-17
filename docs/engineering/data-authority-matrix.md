# Data Authority Matrix

[Capability boundary](platform-product-capability-boundary.md) | [Data ownership](data-ownership.md) | [Contracts](platform-product-contracts.md)

| Data | Authoritative Owner | Stable ID | Referenced By | Replicated Data | Update Authority | Audit Owner | Deletion Owner |
|---|---|---|---|---|---|---|---|
| Platform user | Platform | PlatformUserId (Guid) | Products, Admin | Display name/email cache | Platform | Platform | Platform (+ retention policy) |
| Organization | Platform | PlatformOrganizationId | Products | Name, status, slug | Platform | Platform | Platform |
| Membership | Platform | OrganizationMembershipId | Products (access) | Role codes for access | Platform | Platform | Platform |
| Product | Platform | ProductCode | All | Name, status | Platform | Platform | Platform |
| Plan | Platform | PlanCode + PlanVersion | Products | Limits/features list | Platform | Platform | Platform |
| Subscription | Platform | SubscriptionId | Products | Status, period, plan | Platform | Platform | Platform |

> **P3-WP02:** Organization (minimal ownership fields) and Subscription rows are persisted. SaaS payment rows are **not**. Entitlement snapshot tables are **not**.
| Entitlement | Platform | EntitlementVersion + org+product | Products | Feature map, limits, grace | Platform | Platform | Platform |
| SaaS payment | Platform | SaaSPaymentId | Admin, org billing UI | Status, reference | Platform | Platform | Platform |
| POS business | POS | POSBusinessId | POS; links OrgId | — | POS | POS | POS |
| Organization branch (master) | Platform | OrganizationBranchId | POS / Admin (opaque GUID refs) | Name/status/coords via Platform APIs | Platform | Platform | Platform |
| Branch-scoped POS operations | POS | uses Platform OrganizationBranchId | POS inventory/orders/transfers | — | POS | POS | POS |
| Customer | POS | POSCustomerId | POS; optional UserId later | — | POS | POS | POS |
| CustomerCredit / entries / payments | POS | CreditId / EntryId / CreditPaymentId | POS | — | POS | POS | POS |
| Catalog product / barcode | POS | ProductId (POS) | POS | Platform GTIN snapshot as `PlatformBarcode` only | POS (org SKU/barcode); Platform template is reference | POS | POS |
| Platform shared product image | Platform | `global_product_images` + server storage key | POS/storefront by `PlatformGlobalProductId` | version/reference only; **one file reused by all orgs** | Platform | Platform | Platform |
| Catalog product image metadata (merchant override) | POS | `product_images` row (org+product) | POS storefront/catalog | version/dimensions only | POS | POS | POS |
| Catalog product image files (merchant override) | POS object store (local/dev filesystem V1) | server `storage_key` + versioned WebP paths | authorized image GET | not in PostgreSQL / not in catalog JSON | POS | POS | POS |
| Sale | POS | SaleId | POS | — | POS | POS | POS |
| POS payment attempt (Card/GCash simulated / manual transfer) | POS | PaymentAttemptId | POS sale | Provider/external refs, safe card metadata; **no** PAN/CVV/wallet secrets | POS (webhook/simulation authoritative for Paid) | POS | POS |
| Inventory balance | POS | org+product `InventoryAccount` (sellable on-hand) | POS | — | POS | POS | POS |
| Branch inventory balance | POS | org+branch+product `InventoryBranchBalance` | POS | Platform `OrganizationBranchId` as opaque GUID | POS | POS | POS |
| Inventory transfer | POS | `InventoryTransferId` | POS | Platform branch GUIDs | POS | POS | POS |
| Supplier | POS | SupplierId | POS | — | POS | POS | POS |
| POS retail payment | POS | RetailPaymentId | POS | Method: cash \| gcash \| customer-credit; GCash ref when gcash | POS | POS | POS |
| POS credit payment | POS | CreditPaymentId | POS | Method: cash \| gcash; GCash ref when gcash | POS | POS | POS |
| Offline device state | POS | DeviceId (later) | POS | — | POS | POS | POS |
| Entitlement projection row | Product (storage) / Platform (facts) | (OrgId, ProductCode, EntitlementVersion) | Product runtime | Copy of Platform snapshot | Product apply from Platform | Product (+ Platform source) | Product |

**Replication:** Platform → product projections only for commercial/identity facts needed locally. No product → Platform replication of operational domain data. No cross-DB FKs. Detail: [data-ownership.md](data-ownership.md), [platform-product-contracts.md](platform-product-contracts.md).
