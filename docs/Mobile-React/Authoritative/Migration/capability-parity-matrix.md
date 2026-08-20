# Capability Parity Matrix

Statuses: `PROVEN_CURRENT` | `PROVEN_PARTIAL` | `PROVEN_MISSING` | `SHELL_ONLY` | `MISSING` | `SUPERSEDED` | `OWNER_CONFIRMED_CHANGE` | `UNRESOLVED` | `N/A`

Current Contract Status: `CURRENT` | `OWNER_CONFIRMED_CHANGE` | `MIXED` | `UNRESOLVED`

| Capability | Owner Requirement | Backend | MAUI | React | Current Contract Status | React Parity Status | Dependencies | Required Action | Evidence / Notes |
|------------|-------------------|---------|------|-------|-------------------------|---------------------|--------------|-----------------|------------------|
| Platform/Personal/Org scope model | Preserve P16/ADR-017 sessions | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_PARTIAL | CURRENT | PARTIAL | none | RMAP-01 session parity | ADR-017, AccountScopeGuard |
| Org-scoped staff login alias format | Preserve alias availability | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_PARTIAL | CURRENT | PARTIAL (CURRENT login) | Auth | Keep format; RMAP-B00 may reattach | StaffLoginNameRules |
| Staff existing-person link / Personal accept | One human; Personal may accept | PROVEN_MISSING | uses CURRENT model | MISSING | OWNER_CONFIRMED_CHANGE | BLOCKED | RMAP-B00 | Backend first; marker ORGANIZATION_STAFF_EXISTING_PERSON_LINK_CONTRACT_MISSING | CreateOrganizationStaff; invite accept |
| Separate staff PlatformUser per job | P19 CURRENT | PROVEN_CURRENT | PROVEN_CURRENT | N/A | CURRENT (may SUPERSEDE after B00) | Do not ship as final desired | — | Document until B00 | P19 |
| Start a Business | Personal→Org Owner | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Personal session | Later foundation WP | StartBusinessUseCases |
| Product access + local roles | Distinct from membership | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_PARTIAL | CURRENT | PARTIAL | Session/org | Harden role guards | ProductLocalRoleGrant |
| Shared React UI foundation | Mobile-first DoD | N/A | N/A | PROVEN_PARTIAL | OWNER_CONFIRMED (UI std) | PARTIAL | RMAP-00 | Inventory + fill gaps | 06-react-ui-ux doc |
| Branch context binding | Required for ops | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_CURRENT | CURRENT | PARTIAL (bind only) | Org | Keep; add admin later | WorkspaceProvider |
| Branch fulfillment/delivery config | Required for delivery | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Branch, RMAP-00 | After org parity | BranchEdit, Platform APIs |
| Catalog read (sell) | Needed for sell floor | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_PARTIAL | CURRENT | PARTIAL | Product access | Extend units/weight | pos-catalog-client |
| Catalog admin CRUD | Owner/Manager | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Catalog, RMAP-00 | React admin WP | CatalogEndpoints |
| UOM enum | Controlled list | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_PARTIAL | CURRENT | PARTIAL | Catalog | Surface enum | UnitOfMeasure.cs |
| ByWeight selling | Required | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | UOM, RMAP-00 | React sell WP | SellingMode |
| Multi-UOM shared pool (rice etc.) | Required | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Product units | Migrate CURRENT contract | CatalogProductUnit |
| Milligram UOM | Decision item | PROVEN_MISSING | PROVEN_MISSING | MISSING | UNRESOLVED | N/A | UD-01 | Owner decision; optional backend | No enum member |
| Today’s Prices | Daily price change | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Catalog admin, RMAP-00 | React pricing WP | prices endpoint |
| Sale-line price override policy | Owner policy | PROVEN_MISSING | PROVEN_MISSING | MISSING | OWNER_CONFIRMED_CHANGE | BLOCKED | UD-02 backend | Backend domain first | No SalePricePolicy |
| Inventory default untracked | Default untracked | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Catalog | Migrate CURRENT | CreateUntracked |
| Inventory track/adjust/movements | Tracked authority | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Product, RMAP-00 | React inventory WP | InventoryUseCases |
| Oversell prevention | Required | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Inventory+checkout | Enforce on checkout | insufficient_stock |
| Expiry lots + FEFO | Optional expiry | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Tracked inventory | Later inventory WP | InventoryLotFefo |
| Manual suppliers | Local suppliers | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Org, RMAP-00 | React supplier WP | Supplier |
| Connected suppliers EXPOSABLE≠SHARED | Preserve | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Suppliers | After local suppliers | ConnectedSuppliers |
| Purchasing receive-only stock | Preserve invariant | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Suppliers+inventory | React purchasing WP | GRN movements |
| Registers | Station mgmt | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Branch | Before shift UX | Register |
| Devices | Device auth | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_PARTIAL | CURRENT | PARTIAL | Org | Harden device grant | Platform devices |
| Shifts | Cash authority | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Register/device | Gate checkout | CashierShift |
| Sell floor browse | Required | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_PARTIAL | CURRENT | PARTIAL | Catalog, RMAP-00 | Continue | SellFloorPage |
| Cart | Required | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_PARTIAL | CURRENT | PARTIAL | Sell floor | Add unit/weight | SessionCart |
| Checkout/sale | Required | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Shift+cart+inventory | Major React WP | SaleUseCases |
| Returns/void | Required | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Sales | After sales | SaleReturn |
| Customers / Business Utang | Required | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Sales optional | Customer WP | CustomerEndpoints |
| Personal Utang | Personal surface | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Personal session | Personal WP | personal/utang |
| Expenses | Ops | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Org | Later | ExpenseEndpoints |
| Reports | Ops | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Sales data | Later | ReportingEndpoints |
| Customer ordering/pickup/delivery | Delivery significant | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Branch fulfillment | Extended commerce | CustomerOrderEndpoints |
| Transaction Summary docs | Current doc | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Sales | Wording/disclaimer | SalesDocumentPolicy |
| TaxDocument issuance | Future | PROVEN_MISSING | PROVEN_MISSING | MISSING | DEFERRED | N/A | Compliance | Do not claim | TaxDocumentIssuanceNotAvailable |
| Offline cash/outbox | Selective offline | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Checkout online first | Late hardening | LocalStore |
| PWA static shell | Delivery channel | N/A | N/A | PROVEN_CURRENT | CURRENT | CURRENT for shell | none | Keep | pwa/* |
| Pre-P19 personal-email staff attach | Forbidden as CURRENT | SUPERSEDED | SUPERSEDED | N/A | SUPERSEDED | Do not reintroduce as CURRENT | — | Owner desires new person-link via RMAP-B00 (not old attach) | P19 |
| Dev-era unauthenticated APIs as prod-secure | Forbidden | SUPERSEDED | SUPERSEDED | N/A | SUPERSEDED | Do not claim | — | Phase 13 auth | |

## Reading guide

- **React Parity Status BLOCKED** means backend contract gap, not merely UI gap.
- Prefer this matrix over old Implementation-Readiness matrices for CURRENT truth.
- `READY_FOR_REACT_STAFF_IDENTITY_PARITY` = **NO** until RMAP-B00 PASS.
