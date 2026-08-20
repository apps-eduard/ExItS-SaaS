# MAUI Capability Map

Host: `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui`
Offline policy: `PosOfflineCapabilityPolicy` (unknown → OnlineRequired).

| Capability | MAUI Route/Page | Component/Service | Backend Route | Offline Policy | Roles | Current UX behavior | Status | Evidence | React migration notes |
|------------|-----------------|-------------------|---------------|----------------|-------|---------------------|--------|----------|----------------------|
| Sign-in (email + CURRENT staff alias) | `/signin` | `SignIn.razor`, `IAuthenticationService` | `/api/v1/platform/auth/login` | OfflineCapable shell | n/a | Accepts real email or CURRENT `local@ORG######` staff principal | PROVEN_CURRENT | Auth pages | Desired person-link staff model is OWNER_CONFIRMED_CHANGE (RMAP-B00); do not treat CURRENT duplicate PlatformUser as final |
| Workspace / org select | `/workspace-select`, `/organization-select` | workspace services | auth orgs/context | OnlineRequired | membership | Chooses profile/org | PROVEN_CURRENT | | React has workspace chooser |
| Start a Business | `/start-business`, `/personal/explore-pos` | `StartBusiness.razor` | `/personal/start-business`, `/commercial/plans` | OnlineRequired | Personal | Plan → create org | PROVEN_CURRENT | | React MISSING |
| Staff invite/assign | `/org/staff*` | Staff pages | staff-invitations + roles | OnlineRequired | Owner/Admin | Invite → accept creates staff identity | PROVEN_CURRENT | | React MISSING |
| Branches / fulfillment | `/organization/branches`, `BranchEdit` | `BranchEdit.razor` | Platform branches + fulfillment | OnlineRequired | Org admin | Address, coords, hours, pickup/delivery | PROVEN_CURRENT | | React MISSING admin |
| Devices | `/devices/register`, `/organization/devices` | device pages | Platform pos-devices | OnlineRequired | Org admin | Register/revoke | PROVEN_CURRENT | | React PARTIAL context only |
| Catalog admin | `/catalog*` | Catalog* pages | `/pos/catalog/*` | OnlineRequired (create Queueable) | Owner/Manager | CRUD, units, import | PROVEN_CURRENT | | React read-only |
| Today’s Prices | `/catalog/todays-prices` | `CatalogTodaysPrices` | `POST .../prices` | OnlineRequired | Owner/Manager | Bulk current price | PROVEN_CURRENT | | React MISSING |
| Weighted / sell units | `/sales/new` | `SaleCheckout`, SellingUnit dialogs | sales + catalog units | Queueable cash | Cashier+ | Sell-as + ByWeight | PROVEN_CURRENT | | React MISSING |
| Inventory | `/inventory*` | Inventory* | `/pos/inventory` | OnlineRequired | Owner/Manager | Track/adjust/counts/transfers/lots | PROVEN_CURRENT | | React MISSING |
| Suppliers | `/suppliers*` | Suppliers pages | `/pos/suppliers` | OnlineRequired | purchasing roles | Local suppliers | PROVEN_CURRENT | | React MISSING |
| Connected suppliers | connected routes under suppliers | Connected UI | `/pos/connected-suppliers` | mixed; linked OfflineCapable | purchasing | Expose/share/PO | PROVEN_CURRENT | | React MISSING |
| Purchasing | `/purchasing*` | Purchasing pages | PO/GRN APIs | OnlineRequired; new Queueable | purchasing | Draft→receive | PROVEN_CURRENT | | React MISSING |
| Registers | `/registers*` | Registers | `/pos/registers` | OnlineRequired | Owner/Manager | Station lifecycle | PROVEN_CURRENT | | React MISSING |
| Shifts | `/shifts*` | Shifts | `/cashier-shifts` | OnlineRequired | Cashier+ | Open/close/cash | PROVEN_CURRENT | | React MISSING |
| Sell / checkout | `/sales/new` | `SaleCartService` | `/pos/sales` | Queueable cash | CreateSale | Full checkout | PROVEN_CURRENT | | React cart only |
| Sales history | `/sales`, detail/receipt | Sales pages | `/pos/sales` | OnlineRequired | roles | History | PROVEN_CURRENT | | React MISSING |
| Returns | `/sales/{id}/return` | `SaleReturn.razor` | `/sale-returns` | OnlineRequired | return roles | Partial/restock | PROVEN_CURRENT | | React MISSING |
| Customers / Utang | `/customers*` | Customers + local credit store | `/pos/customers` | Queueable list/credit | roles | Credit/repay | PROVEN_CURRENT | | React MISSING |
| Reports | `/reports*`, `/dashboard` | Reporting | `/pos/reports` | OnlineRequired | Viewer+ | Ops reports | PROVEN_CURRENT | | React MISSING |
| Customer ordering | `/orders*`, personal shop | Seller/Buyer order pages | customer-orders | OnlineRequired | | Storefront + ops | PROVEN_CURRENT | | React MISSING |
| Offline PIN/grant | `/offline-pin*` | OfflinePin + LocalStore | sync/grant | OfflineCapable | device | PIN unlock | PROVEN_CURRENT | | React MISSING |
| Expenses | expense pages | Expenses | `/pos/expenses` | OnlineRequired | | Record/void | PROVEN_CURRENT | | React MISSING |

## Summary

MAUI remains the **reference client** for operational parity. React must copy contracts from MAUI+backend, not invent navigation order from React’s current thin route list.
