# Validation Matrix

How future parity will be proven.
**Do not** claim Device Verified or Browser Verified from automated tests alone.

UI WPs must also satisfy [06-react-ui-ux-and-responsive-foundation.md](../06-react-ui-ux-and-responsive-foundation.md) (phone / tablet / desktop + a11y).

| Capability | Unit Tests | Integration Tests | MAUI Regression | React Unit | React E2E | Offline | Authorization | Cross-org | Device/Browser | Owner Validation | Notes |
|------------|------------|-------------------|-----------------|------------|-----------|---------|---------------|-----------|----------------|------------------|-------|
| Shared UI foundation (RMAP-00) | Component tests | N/A | N/A | Vitest | Viewport E2E 375/768/1024/1440 | N/A | N/A | N/A | Phone+tablet+desktop visual | Spot-check ListToolbar | COMPLETE: `shared-ui-foundation.test.tsx` + `e2e/rmap-00-responsive.spec.ts` (375×812, 768×1024, 1024×768, 1440×900; overflow/focus/SearchField/QuantityStepper/MoneyDisplay/LoadingSkeleton/StickyActionBar) |
| Staff person-link backend (RMAP-B00) | Person-link + accept-as-personal + org-lock + audit | Anonymous no-Personal; Platform-only same email unlinked | MAUI compat path | N/A | N/A | N/A | Membership on staff only | Multi-org + removal + parallel accept | N/A | Owner Option C + Review Repair 03 | COMPLETE; Repair 03 |
| Account/session post-B00 (RMAP-01) | Platform identity | Auth integration | Sign-in | Auth client | Playwright login | N/A | Session class | | Browser | PASS | Master Run 01 #3 |
| Desired staff person-link UI (RMAP-01b) | | Invite/accept API | MAUI after B00 | Invite/accept UI | E2E Personal accept + alias | N/A | Membership rules | Multi-org | Browser | PASS | Master Run 01 #4 |
| Workspace/roles post-B00 (RMAP-02) | | | | Session guards | E2E denial/access | N/A | Product roles | Cross-class | Browser | Spot-check | Master Run 01 #5; reconciled by RMAP-02R |
| Role/experience reconciliation (RMAP-02R) | OrgWebShell + PosRoleMatrix | | | pos-capabilities | E2E Owner/Manager/Cashier/admin | N/A | Admin≠Ops | | Browser + viewports | PASS | Experience ≠ role mutation; StoreManager Org Web denied |
| Branch/device context (RMAP-03) | OperationalBranch | | | workspace + deferred device | E2E zero/one/multi/no-location | N/A | Bound org+branch | HomeOrg lock | Browser + viewports | PASS | Device not invented |
| Catalog admin (RMAP-04) | CatalogEndpoints | | | `/catalog*` CRUD | E2E manager/cashier/conflict + viewports | N/A | ManageCatalog gate | Org isolation via headers | Browser + viewports | PASS | UOM/prices deferred |
| Product units (RMAP-05) | CatalogProductUnit | | | form packages editor | E2E UOM/ByWeight/packages + viewports | conversion unit tests | ManageCatalog | PascalCase API codes | Browser + viewports | PASS | Milligram/Open Sack excluded |
| Today's Prices (RMAP-06) | prices endpoint + PosCatalogTodaysPricesApiTests 3/3 | | | `/catalog/todays-prices` | E2E 9/9: dirty+token, partial fail, conflict, cashier/OrgAdmin deny | N/A | ManageCatalog | required ExpectedUpdatedAtUtc | 375/768/1024/1440 sticky save + overflow | PASS — validation closeout | Override excluded; RMAP-B03 not started |
| Inventory (RMAP-07) | InventoryEndpoints + PosInventoryApiTests 7/7 | InventoryAccountDomainTests 9/9 | | `/inventory*` | E2E 8/8: untracked/enable0/enable+/in/out/disable rules + deny | N/A | View/ManageInventory | Not tracked ≠ zero; disable requires zero | 375/768/1024/1440 list+detail | PASS — validation closeout | Lots RMAP-08 complete |
| Lots / expiry (RMAP-08) | InventoryLotDomainTests 13/13; PosInventoryLotApiTests 6/6 | | | `/inventory/expiration` + detail lots | E2E: totals/lots/windows/adjust + Load more >50 + RMAP-07 regression | N/A | ViewInventory | Expired write-off via Out + lot | 375/768/1024/1440 expiration+detail | PASS | Checkout FEFO = RMAP-11; React does not FEFO |
| Sell floor + cart (RMAP-09) | Session cart helpers + qty matrix | | | units/weight/custom qty dialogs; stock advisory | E2E: multi-UOM, single sell unit, Liter custom, whole qty, ByWeight, barcode, clear, 375/768/1024/1440 | N/A | CreateSale UI gate later | Org via workspace | Browser + viewports | PASS | Pay/checkout + camera (PWA later) + FEFO excluded |
| Registers + open shift (RMAP-10) | readiness + register/shift clients | | | `/registers`, `/shifts*`, ShiftContextProvider | E2E 15/15: no-shift/open/closed/denied/branch/org/cashier/refresh + 375/768/1024/1440 | N/A | View/ManageShifts; ViewRegisters; Cashier≠admin | Org + branch headers | Browser + viewports | PASS | PosDevice not invented; sale POST = RMAP-11 |
| Checkout cash sale (RMAP-11 / 11b) | pos-sales-client quote+checkout + cart map + error map | | | `/sell/checkout`, `/sell/sales/:id/summary` | E2E rmap-11 + rmap-11b (discount/quote/zero/cashier) + viewports | N/A | CreateSale + ApplyCommercialDiscount + moneyPostReady | Org + branch + installation device | Browser + viewports | PASS | Cash only; discount UX RMAP-11b; no GCash/Utang/Card |
| CURRENT staff alias login (pre-B00 evidence) | Platform identity | Auth integration | Sign-in staff | | | N/A | Session class | Staff lock | Browser | | Historical CURRENT; superseded as final after B00 |
| Account scope isolation | Domain | Middleware integration | Org switch wipe | Session guards | E2E denial | N/A | AccountScopeGuard | Cross-class API deny | Browser | Spot-check | |
| ByWeight | WeightedSale* | Sales API | Weight dialog | Qty helpers + RMAP-09 weight dialog | E2E weight cart (RMAP-09); sale later | Snapshot fidelity | CreateSale | | Device later | | Pay excluded on RMAP-09 |
| Product units / rice pool | ProductUnitConversion, RiceSell* | API catalog/sales | Sell-as checkout | Unit math helpers + RMAP-09 unit picker | E2E multi-unit cart (RMAP-09); sale later | LocalStore v9 | Catalog/sales roles | Org isolation | Device later | Owner rice scenario | Checkout next |
| Today’s Prices | CatalogDomain UpdateSellingPrice | PosCatalogTodaysPricesApi | Todays Prices page | Price form | E2E update+sell | N/A | Manager/Owner | | Browser | | |
| Price override policy | Domain `SalePriceOverride*` | API quote/checkout | MAUI optional | RMAP-12b UI | E2E RMAP-12b | N/A | Cashier DENY; Manager ≤100%; Owner unlimited | | Browser | Locked PO matrix | Backend + React UI DONE |
| Inventory untracked/tracked | InventoryAccountDomain | Inventory API | Inventory pages | | E2E enable+opening | OnlineRequired | Inventory roles | | Browser | | |
| Oversell | Sale/inventory unit | Sales API | Checkout stock UI | | E2E insufficient stock | Offline tracked rules | | | Device later | | |
| Lots/FEFO | InventoryLotDomain 13/13 | PosInventoryLotApi 4/4 | Expiration UI | lot-status helpers | E2E rmap-08 7/7 | | ViewInventory | | Browser | | Checkout FEFO later |
| Connected EXPOSABLE≠SHARED | Connected domain | Connected API | MAUI share UI | | E2E expose without share | Linked cache only | | Cross-org | Browser | | |
| Receive-only inventory | ReceiveStock* | PO/GRN API | Purchasing receive | | E2E submit≠stock; receive=stock | OnlineRequired | | | Browser | | |
| Checkout cash online | SaleDomain | Sales API | SaleCheckout | Sale client | E2E pay | Later offline | CreateSale+shift | | Browser first | | |
| Commercial discount (RMAP-B03) | SaleCommercialDiscount* | PosSaleCommercialDiscountApiTests | No MAUI discount UI | Quote + checkout discount UX (RMAP-11b) | E2E rmap-11b quote/zero/cashier deny | Offline discount fail-closed | ApplyCommercialDiscount | Allocation persisted | FINAL CLOSED backend | UX = RMAP-11b COMPLETE |
| Offline cash | OfflineSaleSnapshot* | Sync | LocalStore dispatch | Future LocalStore | Limited | Required | Device grant | | **Device Verified manual** | Not auto |
| Customer ordering/delivery | Ordering domain | CustomerOrder API | BranchEdit+shop | | E2E pickup/delivery | OnlineRequired | Linked merchant | Cross-org | Browser + geo manual | Owner delivery setup |
| Transaction Summary wording | SalesDocumentFoundation | | Phase26 wording guards | Copy tests | E2E disclaimer | | Compliance capability | | Browser | Never claim BIR certified; RMAP-TAX future |
| Buyer purchase projection | — | — | — | — | — | — | — | — | NOT STARTED | RMAP-B04 |
| Final tax activation | — | Platform capability | — | — | — | — | — | — | NOT STARTED | RMAP-TAX |
| Reports | Report aggregates | Reports API | Reports pages | | Smoke E2E | OnlineRequired | Viewer+ | | Browser | No fake P&L; tax reports gated later |
| UI responsive quality | N/A | N/A | N/A | Visual regression where practical | Phone/tablet/desktop screenshots | N/A | | | **Manual owner UX review** | Poor UX = PARTIAL |

## Evidence rules

1. Unit/integration green ≠ Device Verified.
2. Playwright green ≠ production PWA offline POS.
3. Playwright green ≠ Mobile/Tablet UX PASS.
4. Owner Validation column is required for rice multi-unit, delivery readiness, price-policy acceptance, and staff person-link acceptance.
5. Record exact test project names and commit SHAs in future WP closeouts.
6. Master-run batches follow [master-run-execution-protocol.md](master-run-execution-protocol.md).
