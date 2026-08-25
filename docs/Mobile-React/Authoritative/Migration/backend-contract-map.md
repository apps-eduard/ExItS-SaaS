# Backend Contract Map

Maps major capabilities to current backend contracts. Status uses the Authoritative taxonomy.

| Capability | Bounded Context | Domain Entity/Aggregate | Application Use Case/Service | API Route | Persistence/Table | Authorization | Concurrency/Idempotency | Primary Tests | Status | Evidence |
|------------|-----------------|-------------------------|------------------------------|-----------|-------------------|---------------|-------------------------|---------------|--------|----------|
| Login / session | Platform | `PlatformUser`, `PlatformAuthSession`, `AccountProfile` | `LoginPlatformUser`, profile select, `ValidateAndRenewPlatformSession` | `/api/v1/platform/auth/login`, me, logout, profiles | `platform_users`, `platform_auth_sessions`, `account_profiles` | Credentials + AccountClass | Session token hash | Identity/session tests; ApiSessionAuthTests | PROVEN_CURRENT | `/me` includes `accountClass`, `homeOrganizationId`, `organizationContextLocked` (RMAP-01) |
| Org-scoped staff login (CURRENT) | Platform | `PlatformUser` staff + `StaffLoginNameRules` | Invite accept + login | `/organization-invitations/accept`, staff-invitations | `platform_users.normalized_email` | Org invite token | Unique email | `OrganizationScopedStaffIdentityTests` | PROVEN_CURRENT | Separate staff PlatformUser |
| Staff existing-person link | Platform | `PlatformUser.LinkedPersonalUserId` | `AcceptOrganizationInvitation.ExecuteForAuthenticatedPersonalAsync` (Personal profile proof; org lock) | `/invitations/accept-as-personal` | `platform_users.linked_personal_user_id` | Active Personal `AccountProfile` + verified email + token | Org advisory lock; audit `person_link.established` | `OrganizationScopedStaffIdentityTests`; parallel accept integration | PROVEN_CURRENT | RMAP-B00 Option C + Review Repair 03 |
| Start a Business | Platform | Organization + membership | `StartBusinessForPersonalUser` | `POST /api/v1/personal/start-business` | orgs, memberships, branches | Personal session | Tx create | StartBusiness use cases/tests | PROVEN_CURRENT | PersonalEndpoints |
| Product local role | Platform | `ProductLocalRoleGrant` | Grant use cases | org product role APIs | `product_local_role_grants` | Org admin/owner | Unique grants | Role grant tests | PROVEN_CURRENT | ProductLocalRoleGrant.cs |
| Branches / fulfillment | Platform | `OrganizationBranch`, delivery policy | Branch use cases | `/organizations/{id}/branches`, hours, fulfillment-* | branch tables | Org membership | Optimistic updates | Branch/delivery tests | PROVEN_CURRENT | BranchAndDeviceEndpoints |
| POS devices | Platform | `PosDevice` | Device registration | `/organizations/{id}/pos-devices` | device tables | Org admin | Tokens | Device tests | PROVEN_CURRENT | BranchAndDeviceEndpoints |
| Catalog products | POS | `CatalogProduct`, units | `CatalogProductUseCases` | `/api/v1/pos/catalog/products` | `pos.products`, `product_units` | POS catalog roles | Product UpdatedAt | CatalogDomainTests | PROVEN_CURRENT | React admin RMAP-04 uses CRUD + image; units/prices still RMAP-05/06 |
| Today’s Prices | POS | `CatalogProduct` / units | price update | `POST .../catalog/products/prices` | products/units | Manager/Owner | `ExpectedUpdatedAtUtc` | PosCatalogTodaysPricesApiTests | PROVEN_CURRENT | |
| Multi-UOM conversion | POS | `CatalogProductUnit` | conversion helpers + checkout | catalog + sales | `pos.product_units` | Catalog/sales roles | Snapshots | ProductUnitConversionTests, RiceSellUnitCheckoutSemanticsTests | PROVEN_CURRENT | engineering units doc |
| Sale price policy | POS | — | — | — | — | — | — | — | PROVEN_MISSING | no SalePricePolicy |
| Inventory tracking | POS | `InventoryAccount`, `StockMovement` | `InventoryUseCases` | `/api/v1/pos/inventory` | `inventory_accounts`, `stock_movements` | Inventory roles | xmin | InventoryAccountDomainTests | PROVEN_CURRENT | |
| Lots / FEFO | POS | `InventoryLot` | lot allocation | `/inventory/lots` | `inventory_lots` | Inventory roles | Lot qty | InventoryLotDomainTests | PROVEN_CURRENT | |
| Sales checkout | POS | `Sale`, `SaleLine` | `SaleUseCases` | `/api/v1/pos/sales` | `sales`, `sale_lines` | CreateSale + device/shift | Idempotency keys | SaleDomainTests | PROVEN_CURRENT | |
| Commercial discount | POS | `SaleCommercialDiscount*` | Checkout + Quote | `/api/v1/pos/sales` + `/quote` | discount columns + `sale_commercial_discount_adjustments` | ApplyCommercialDiscount | Intent only; server money | SaleCommercialDiscountDomainTests; PosSaleCommercialDiscountApiTests | PROVEN_CURRENT | RMAP-B03; offline discount fail-closed |
| Sale quote preview | POS | calculator | `CheckoutSale.QuoteAsync` | `POST /api/v1/pos/sales/quote` | none (non-persisting) | CreateSale (+ discount cap) | — | PosSaleCommercialDiscountApiTests | PROVEN_CURRENT | Not a recorded sale |
| Buyer purchase projection | POS+Platform | SaleBuyerParty | — | — | — | — | — | — | PROVEN_MISSING | RMAP-B04 NOT STARTED |
| Final tax activation | Platform+POS | capability + OperationalSetup | — | — | — | — | — | — | PROVEN_PARTIAL | RMAP-TAX NOT STARTED |
| Returns | POS | `SaleReturn` | return use cases | `/api/v1/pos/sale-returns` | sale_returns* | Return roles | Concurrency | SaleReturnDomainTests | PROVEN_CURRENT | Refunds use net LineTotal |
| Customers / credit | POS | Customer, CreditEntry | customer/credit use cases | `/api/v1/pos/customers` | customers, credit_* | Customer roles | Idempotent repay | credit/offline tests | PROVEN_CURRENT | |
| Manual suppliers | POS | `Supplier` | supplier use cases | `/api/v1/pos/suppliers` | `suppliers` | Purchasing roles | | Supplier tests | PROVEN_CURRENT | |
| Connected suppliers | POS | relationship/share/PO | ConnectedSupplier use cases | `/api/v1/pos/connected-suppliers` | connected_* tables | Org POS roles | Status machine | Connected supplier tests | PROVEN_CURRENT | EXPOSABLE≠SHARED |
| Purchasing / GRN | POS | PO, GoodsReceipt | purchasing use cases | `/purchase-orders`, `/goods-receipts` | po/grn tables | Purchasing roles | Receive-only stock | ReceiveStockInventorySemanticsTests | PROVEN_CURRENT | |
| Registers / shifts | POS | Register, CashierShift | shift use cases | `/registers`, `/cashier-shifts` | registers, shifts | Shift roles | One active shift | Shift tests | PROVEN_CURRENT | |
| Expenses | POS | Expense | ExpenseUseCases | `/expenses` | expenses | Expense roles | Void pattern | Expense tests | PROVEN_CURRENT | OnlineRequired |
| Reports | POS | Read models | reporting queries | `/reports`, `/dashboard` | read queries | Reporting roles | | Report aggregate tests | PROVEN_CURRENT | OnlineRequired |
| Customer orders | POS | CustomerOrder | ordering use cases | `/customer-orders` | customer order tables | Buyer/seller auth | Reservation | Ordering tests | PROVEN_CURRENT | |
| Transaction Summary | POS | Sale document policy | SalesDocumentPolicy | sales document request | sale + platform compliance | Org capability | | SalesDocumentFoundationTests | PROVEN_CURRENT | TaxDocument unavailable |
| Offline cash sale | POS+LocalStore | outbox commands | offline dispatchers | sync + sales | SQLite + server | Device grant | Idempotency | OfflineSaleSnapshotFidelityTests | PROVEN_CURRENT | MAUI only |

## Notes

- Prefer this map over phase checklists when planning React API clients.
- `PROVEN_MISSING` rows block React UI that depends on them until a backend domain package exists.
