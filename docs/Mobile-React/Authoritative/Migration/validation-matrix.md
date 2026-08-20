# Validation Matrix

How future parity will be proven.
**Do not** claim Device Verified or Browser Verified from automated tests alone.

| Capability | Unit Tests | Integration Tests | MAUI Regression | React Unit | React E2E | Offline | Authorization | Cross-org | Device/Browser | Owner Validation | Notes |
|------------|------------|-------------------|-----------------|------------|-----------|---------|---------------|-----------|----------------|------------------|-------|
| Staff login alias | Platform identity unit | Auth integration | Sign-in staff | Auth client | Playwright login | N/A | Session class | Staff lock | Browser manual | Confirm alias format | |
| Account scope isolation | Domain | Middleware integration | Org switch wipe | Session guards | E2E denial | N/A | AccountScopeGuard | Cross-class API deny | Browser | Spot-check | |
| Product units / rice pool | ProductUnitConversion, RiceSell* | API catalog/sales | Sell-as checkout | Unit math helpers | E2E multi-unit sale | LocalStore v9 | Catalog/sales roles | Org isolation | Device later | Owner rice scenario | |
| ByWeight | WeightedSale* | Sales API | Weight dialog | Qty helpers | E2E weight sale | Snapshot fidelity | CreateSale | | Device later | | |
| Today’s Prices | CatalogDomain UpdateSellingPrice | PosCatalogTodaysPricesApi | Todays Prices page | Price form | E2E update+sell | N/A | Manager/Owner | | Browser | | |
| Price override policy | **New** domain tests | **New** API | MAUI after backend | Policy UI | E2E override deny/allow | N/A | Cashier vs Owner | | Browser | Owner policy matrix | Blocked on RMAP-B01 |
| Inventory untracked/tracked | InventoryAccountDomain | Inventory API | Inventory pages | | E2E enable+opening | OnlineRequired | Inventory roles | | Browser | | |
| Oversell | Sale/inventory unit | Sales API | Checkout stock UI | | E2E insufficient stock | Offline tracked rules | | | Device later | | |
| Lots/FEFO | InventoryLotDomain | Inventory lots API | Expiration UI | | E2E expired blocked | | | | Browser | | |
| Connected EXPOSABLE≠SHARED | Connected domain | Connected API | MAUI share UI | | E2E expose without share | Linked cache only | | Cross-org | Browser | | |
| Receive-only inventory | ReceiveStock* | PO/GRN API | Purchasing receive | | E2E submit≠stock; receive=stock | OnlineRequired | | | Browser | | |
| Checkout cash online | SaleDomain | Sales API | SaleCheckout | Sale client | E2E pay | Later offline | CreateSale+shift | | Browser first | | |
| Offline cash | OfflineSaleSnapshot* | Sync | LocalStore dispatch | Future LocalStore | Limited | Required | Device grant | | **Device Verified manual** | Not auto |
| Customer ordering/delivery | Ordering domain | CustomerOrder API | BranchEdit+shop | | E2E pickup/delivery | OnlineRequired | Linked merchant | Cross-org | Browser + geo manual | Owner delivery setup |
| Transaction Summary wording | SalesDocumentFoundation | | Phase26 wording guards | Copy tests | E2E disclaimer | | Compliance capability | | Browser | Never claim BIR certified |
| Reports | Report aggregates | Reports API | Reports pages | | Smoke E2E | OnlineRequired | Viewer+ | | Browser | No fake P&L |

## Evidence rules

1. Unit/integration green ≠ Device Verified.
2. Playwright green ≠ production PWA offline POS.
3. Owner Validation column is required for rice multi-unit, delivery readiness, and price-policy acceptance.
4. Record exact test project names and commit SHAs in future WP closeouts.
