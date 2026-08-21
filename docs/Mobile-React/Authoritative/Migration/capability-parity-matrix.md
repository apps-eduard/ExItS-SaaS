# Capability Parity Matrix

Statuses: `PROVEN_CURRENT` | `PROVEN_PARTIAL` | `PROVEN_MISSING` | `SHELL_ONLY` | `MISSING` | `SUPERSEDED` | `OWNER_CONFIRMED_CHANGE` | `UNRESOLVED` | `N/A`

Current Contract Status: `CURRENT` | `OWNER_CONFIRMED_CHANGE` | `MIXED` | `UNRESOLVED`

| Capability | Owner Requirement | Backend | MAUI | React | Current Contract Status | React Parity Status | Dependencies | Required Action | Evidence / Notes |
|------------|-------------------|---------|------|-------|-------------------------|---------------------|--------------|-----------------|------------------|
| Platform/Personal/Org scope model | Preserve P16/ADR-017 sessions | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_CURRENT | CURRENT | PASS (RMAP-01) | none | RMAP-01b staff invite UI | ADR-017, AccountScopeGuard, RequireAccountClass |
| Org-scoped staff login alias format | Preserve alias availability | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_PARTIAL | CURRENT | PARTIAL (CURRENT login) | Auth | Keep format as real login | StaffLoginNameRules |
| Staff existing-person link / Personal accept | One human proven by formal link; Personal may accept | PROVEN_CURRENT | PROVEN_CURRENT (compat) | PROVEN_CURRENT (RMAP-01b) | CURRENT after RMAP-B00 | PASS | none | Late link deferred | LinkedPersonalUserId; accept-as-personal UI |
| Separate staff PlatformUser per job | Approved Option C | PROVEN_CURRENT | PROVEN_CURRENT | N/A | CURRENT | Formal link when Personal accepts | — | Preserve isolation | P19 + RMAP-B00 |
| Start a Business | Personal→Org Owner | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Personal session | Later foundation WP | StartBusinessUseCases |
| Product access + local roles | Distinct from membership | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_CURRENT (RMAP-02R) | CURRENT | PASS (RMAP-02R) | Session/org | Experience ≠ role mutation | PosRoleMatrix; experience guards |
| Staff invite authority | Owner membership manage | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_CURRENT (RMAP-02R) | CURRENT | PASS | Org Owner | Manager/Cashier denied | EnsureCanManageMemberships; canInviteOrganizationStaff |
| Shared React UI foundation | Mobile-first DoD | N/A | N/A | PROVEN_CURRENT (foundation) | OWNER_CONFIRMED (UI std) | PROVEN_CURRENT (foundation) | RMAP-00 COMPLETE | Reuse in later WPs | 06-react-ui-ux doc; shared-ui-foundation.test.tsx |
| Branch context binding | Required for ops | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_CURRENT (RMAP-03) | CURRENT | PASS | Org | Device deferred | WorkspaceProvider + operational-branch |
| Branch fulfillment/delivery config | Required for delivery | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Branch, RMAP-00 | After org parity | BranchEdit, Platform APIs |
| Catalog read (sell) | Needed for sell floor | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_CURRENT | CURRENT | CURRENT (RMAP-09) | Product access | Checkout next | pos-catalog-client; units/weight on sell |
| Catalog admin CRUD | Owner/Manager | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_CURRENT | CURRENT | CURRENT | Catalog, RMAP-00, RMAP-04 | Units/prices next | CatalogEndpoints + `/catalog*` |
| UOM enum | Controlled list | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_CURRENT | CURRENT | CURRENT (RMAP-05/09) | Catalog | — | UnitOfMeasure.cs |
| ByWeight selling | Required | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_CURRENT | CURRENT | CURRENT (RMAP-09; Pay excluded) | UOM, RMAP-00 | Checkout WP | SellingMode; weight dialog |
| Multi-UOM shared pool (rice etc.) | Required | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_CURRENT | CURRENT | CURRENT | Product units, RMAP-05/09 | Checkout next | CatalogProductUnit |
| Milligram UOM | Decision item | PROVEN_MISSING | PROVEN_MISSING | MISSING | UNRESOLVED | N/A | UD-01 | Owner decision; optional backend | No enum member |
| Today’s Prices | Daily price change | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_CURRENT | CURRENT | CURRENT | Catalog admin, RMAP-06 | Cashier override later (RMAP-B01) | prices endpoint; validation closeout `cb91145b` |
| Commercial sale discount | Preserve UnitPrice; separate adjustment | PROVEN_CURRENT | PROVEN_CURRENT (no discount UI) | MISSING (no React UX) | CURRENT | Backend FINAL CLOSED; UI BLOCKED | RMAP-B03 | Discount UX after checkout (RMAP-11b) | ApplyCommercialDiscount; quote+checkout |
| Sale-line price override policy | Owner policy | PROVEN_MISSING | PROVEN_MISSING | MISSING | OWNER_CONFIRMED_CHANGE | BLOCKED | UD-02 / RMAP-B01 | Backend domain first | No SalePricePolicy |
| Linked buyer purchase projection | Read-only buyer history | PROVEN_MISSING | PROVEN_MISSING | MISSING | OWNER_CONFIRMED_CHANGE (future) | N/A | RMAP-B04 NOT STARTED | Backend-first later | SaleBuyerParty counterparty only |
| Controlled tax activation UX | TAX_NOT_AVAILABLE→ACTIVE | PROVEN_PARTIAL (capability) | PROVEN_PARTIAL | MISSING | OWNER_CONFIRMED_CHANGE (future) | N/A | RMAP-TAX NOT STARTED | After RMAP-23 | Not BIR certification |
| Inventory default untracked | Default untracked | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_CURRENT | CURRENT | CURRENT | Catalog, RMAP-07 | — | CreateUntracked; validation closeout `cb91145b` |
| Inventory track/adjust/movements | Tracked authority | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_CURRENT | CURRENT | CURRENT | Product, RMAP-07 | — | InventoryUseCases; validation closeout `cb91145b` |
| Oversell prevention | Required | PROVEN_CURRENT | PROVEN_CURRENT | MISSING (enforced at checkout) | CURRENT | MISSING (advisory hints only on RMAP-09) | Inventory+checkout | Enforce on checkout | insufficient_stock |
| Expiry lots + FEFO | Optional expiry | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_CURRENT (inventory surfaces) | CURRENT | CURRENT (RMAP-08; checkout FEFO = RMAP-11) | Tracked inventory, RMAP-08 | Checkout FEFO at RMAP-11 | InventoryLotFefo; React inventory only |
| Manual suppliers | Local suppliers | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Org, RMAP-00 | React supplier WP | Supplier |
| Connected suppliers EXPOSABLE≠SHARED | Preserve | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Suppliers | After local suppliers | ConnectedSuppliers |
| Purchasing receive-only stock | Preserve invariant | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Suppliers+inventory | React purchasing WP | GRN movements |
| Registers | Station mgmt | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Branch | Before shift UX | Register |
| Devices | Device auth | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_PARTIAL (deferred) | CURRENT | PARTIAL — no browser PosDevice | Org | Later device contract | Honest deferred; money APIs server-gated |
| Shifts | Cash authority | PROVEN_CURRENT | PROVEN_CURRENT | MISSING | CURRENT | MISSING | Register/device | Gate checkout | CashierShift |
| Sell floor browse | Required | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_CURRENT | CURRENT | CURRENT (RMAP-09) | Catalog, RMAP-00 | Checkout next | SellFloorPage |
| Cart | Required | PROVEN_CURRENT | PROVEN_CURRENT | PROVEN_CURRENT | CURRENT | CURRENT (RMAP-09; session only) | Sell floor | Checkout next | SessionCart |
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
- `READY_FOR_REACT_STAFF_IDENTITY_PARITY` = **YES** (backend RMAP-B00 PASS; RMAP-01 session PASS). React staff invite/accept UI is RMAP-01b.
