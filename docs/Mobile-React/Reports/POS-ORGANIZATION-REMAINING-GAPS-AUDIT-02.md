# POS-ORGANIZATION-REMAINING-GAPS-AUDIT-02

**Status:** COMPLETE  
**Branch:** `feat/organization`  
**TASK:** POS-ORGANIZATION-REMAINING-GAPS-AUDIT-02  
**START_SHA:** `fe7e3b852b05832cf39f84adfa7e62212dd26d0d`  
**MODE:** READ_ONLY_AUDIT (documentation only; no application code changed)

---

## EXECUTIVE_VERDICT

Organization POS on React is **substantially complete** for Philippines micro/small retail (sari-sari, small retailers, personal operators). The sell floor, catalog, inventory operations, purchasing, customers/utang, customer orders, cost/profitability reporting, expenses, shifts/devices, and seller order queue are implemented end-to-end in current code.

Since **AUDIT-01**, major previously-open items are **closed in code**: React Stock Count, React Inventory Transfers, Expenses React CRUD, branch report scoping (honest labeling), product profitability ranking, discount reporting, expired-stock quick flow, test harness repair (1256/1256), inventory permission polish, and CustomerOrder COGS hardening.

Remaining gaps are mostly **polish, validation depth, intentional deferrals, or enterprise features out of scope** for the target market — not missing core SMB operations.

| Metric | Value |
|--------|--------|
| **ORGANIZATION_POS_CORE_COMPLETENESS_PERCENT** | **~90%** |
| **ORGANIZATION_POS_PRODUCTION_READINESS** | **CONTROLLED_PILOT** — single-branch online pilot ready; multi-branch pilot viable with branch-report literacy; not broad production without payment-provider authorization and export/accountant workflows |

---

## CURRENT_ORGANIZATION_POS_MAP

```
Organization POS (React + POS API)
├─ Organization setup / onboarding / operational-setup
├─ Branches (+ fulfillment admin)
├─ Staff / roles / permissions (SMB model)
├─ Devices (org admin + register flow; enforcement optional on PWA)
├─ Registers / Cashier shifts (required for floor checkout)
├─ Catalog (products, categories, brands, units, barcode, import, BusinessUsage)
├─ Inventory
│  ├─ Detail, opening, adjust, lots/expiry, low-stock
│  ├─ Stock Count ………… React + API IMPLEMENTED
│  ├─ Transfers ………… React + API IMPLEMENTED
│  ├─ Stock Use / Production / Waste-Loss ………… React + API IMPLEMENTED
│  └─ Expired → Waste quick flow IMPLEMENTED
├─ Purchasing (Direct Buy, PO, GRN, partial receive, connected PO, prepare-products)
├─ Suppliers (manual + connected catalog sharing; NO supplier payables ledger)
├─ Customers (retail + Business Customers)
├─ Sell / Checkout (Cash, ManualGCash, Utang)
├─ Returns / voids / idempotency
├─ Cost / COGS / Gross Profit (LAST_AUTHORITATIVE snapshots)
├─ Reports (classic + operational + profitability + product profitability)
├─ Expenses ………… React CRUD + reports IMPLEMENTED (org-wide; no Expense.BranchId)
├─ Customer orders / storefront / delivery / seller queue
├─ Settlement Sale + COGS for completed orders (all payment methods; Personal Utang credit)
└─ Org Web ………… ONLINE_ONLY (intentional)
```

**Evidence:** `ExItS.PinoyBusinessPOS.React/src/app/router.tsx` (stock-counts, transfers, expenses routes); `Api/Program.cs` endpoint maps; domain aggregates under `Domain/`.

---

## STATUS_BY_AREA

| Area | Status | Summary |
|------|--------|---------|
| **SELL_STATUS** | **IMPLEMENTED** | Cash, ManualGCash, Utang, weighted, overrides, discounts, returns, voids, shift/register gates, branch binding, idempotency. Card/provider GCash not on React floor (intentional). |
| **PRODUCT_STATUS** | **IMPLEMENTED** | Full catalog CRUD, BusinessUsage, sellability, units, barcode, expiry, import. |
| **INVENTORY_STATUS** | **IMPLEMENTED** | Opening, adjust, count, transfer, lots/expiry, stock use, waste/loss, production, permissions, React UX. **VALIDATION_GAP:** stock use/waste/production lack PostgreSQL integration tests. |
| **PURCHASING_STATUS** | **IMPLEMENTED** | Direct buy, PO, GRN, partial receive, connected supplier, cost capture, cancel. **DEFERRED:** post-receipt reversal/void. |
| **SUPPLIER_STATUS** | **IMPLEMENTED** | Manual + connected orgs, catalog sharing, BuyerSupplierProductLink, business customers. **DEFERRED:** retail B2B checkout at register. |
| **CUSTOMER_STATUS** | **IMPLEMENTED** | Walk-in, CRUD, history, business customers, linked personal. |
| **UTANG_STATUS** | **IMPLEMENTED** | Product-based Business Utang, repayment, statements; Personal Utang on Platform (separate). |
| **CUSTOMER_ORDER_STATUS** | **IMPLEMENTED** | Storefront, cart, checkout, seller queue, lifecycle, settlement sale + COGS (post-hardening). **PARTIAL:** Org-party + Utang completes without settlement sale. |
| **DELIVERY_STATUS** | **IMPLEMENTED** | Pickup + delivery fulfillment, branch/hours config, delivery fee in settlement lines. |
| **COST_PROFIT_STATUS** | **IMPLEMENTED** (accuracy **PARTIAL**) | LAST_AUTHORITATIVE snapshots; profitability + product ranking; unknown cost → null never zero. |
| **REPORTING_STATUS** | **PARTIAL** | Rich surfaces; branch-capable reports labeled; dashboard mixes branch sales with org-wide utang/expenses/low-stock. **DEFERRED:** export UI. |
| **EXPENSE_STATUS** | **IMPLEMENTED** | React list/create/detail/categories; void semantics; org-scoped (no branch dimension). |
| **DEVICE_STATUS** | **PARTIAL** | Registration + readiness gates; **EnforcementEnabled=false** on PWA by default. |
| **SHIFT_STATUS** | **IMPLEMENTED** | Open/close; CreateSale requires open shift + register. |
| **STAFF_RBAC_STATUS** | **IMPLEMENTED** | SMB intentional model: ViewInventory vs ManageInventory; ReportingUser view-only. |
| **BRANCH_STATUS** | **PARTIAL** | Ops (sell, inventory, transfers, counts) branch-bound; reports/dashboard mixed with honest labeling. |
| **OFFLINE_STATUS** | **DEFERRED / NOT_NEEDED (Web)** | Org Web ONLINE_ONLY; offline engine preserved for native. |
| **I18N_STATUS** | **PARTIAL** | Five locales; **mojibake/`?` corruption** in PH locale movement labels (en.ts core keys repaired). |
| **SECURITY_STATUS** | **IMPLEMENTED** | Org/branch fail-closed scoping; no customer cost leaks; FakePaymentGateway lab-only. |
| **PERFORMANCE_STATUS** | **IMPLEMENTED** (watch items) | Batch cost resolver; no proven N+1 in audited hot paths; unbounded report windows possible. |
| **RESPONSIVE_UX_STATUS** | **PARTIAL** | ExItS patterns on major screens; inventory nav crowded. |
| **TEST_CONFIDENCE_STATUS** | **IMPLEMENTED** | React 1256/1256; CustomerOrder integration 4/4 PASS this audit. |

### Classifications

| Field | Value |
|-------|--------|
| **SUPPLIER_PAYABLE_STATUS** | **DEFERRED / NOT_NEEDED (MVP)** — No AP ledger, supplier payments, or supplier invoices. Architecture tests forbid AP concepts. Connected PO `Utang` is a term label only. |
| **REAL_GCASH_STATUS** | **DEFERRED** — ManualGCash on floor; provider GCash via PaymentAttempt + FakePaymentGateway (lab). |
| **CARD_STATUS** | **DEFERRED** — Domain/API lab path only; not on React checkout floor. |
| **FIFO_STATUS** | **NOT_NEEDED** — Lots exist for FEFO consumption; COGS uses LAST_AUTHORITATIVE acquisition cost, not lot-layer FIFO. |
| **GL_STATUS** | **NOT_NEEDED** — No general ledger, double-entry, or BIR engine. |
| **COST_MODEL_STATUS** | **IMPLEMENTED** |
| **COST_ACCURACY_MODEL** | **LAST_AUTHORITATIVE** — OpeningStock / PurchaseReceipt / DirectPurchaseReceipt / ProductionOutput; immutable SaleLine snapshots. |
| **CUSTOMER_ORDER_INTEGRATION_VALIDATION_STATUS** | **PASS** — `PosCustomerOrderUtangLedgerApiTests` 4/4 passed (Release, this audit). |
| **REACT_FULL_SUITE_STATUS** | **PASS** — Trusted baseline 1256/1256 (POS-REACT-TEST-HARNESS-ORG-SESSION-REPAIR-01). Not re-run this audit. |

---

## OPERATOR_QUESTION_COVERAGE

| Question | Can owner answer today? | Evidence |
|----------|-------------------------|----------|
| How much did I sell today? | **Yes** | Dashboard / sales-summary; branch filter on sale metrics |
| How much profit did I make? | **Yes** | Profitability report (COGS completeness shown) |
| Which products make the most profit? | **Yes** | Product profitability ranking |
| Which products sell most? | **Yes** | Sales-by-product |
| What did I purchase? | **Yes** | PO list, direct purchases, purchasing summary report |
| What stock is low? | **Yes** | Inventory list badges + inventory-status report |
| What expired? | **Yes** | Expiration settings / inventory expiration views |
| What was wasted/lost? | **Yes** | Waste/Loss list + operational report |
| What was used internally? | **Yes** | Stock Use list + report separation |
| What was produced? | **Yes** | Production runs |
| What is my inventory quantity? | **Yes** | On-hand per product (branch-bound workspace) |
| What expenses did I record? | **Yes** | Expenses CRUD + expense summary report |
| What customer Utang is outstanding? | **Yes** | Classic utang report + dashboard (org-wide) |
| What happened per branch? | **Partial** | Branch-capable sales/profitability; shifts/purchasing/expenses org-wide |
| What happened per cashier? | **Partial** | Sales-by-cashier; shifts-summary (no device-level report) |
| What orders came from customers? | **Yes** | Seller order queue + customer order lifecycle |
| What supplier balances do I owe? | **No** | Supplier payables not implemented; PO history only |

---

## OLD_AUDIT_RECONCILIATION

| Gap (AUDIT-01) | OLD_STATUS | CURRENT_STATUS | Evidence |
|----------------|------------|----------------|----------|
| React Stock Count | MISSING | **IMPLEMENTED** | `router.tsx` L568–570; `StockCount*Page.tsx`; `POS-REACT-STOCK-COUNT-01` |
| React Inventory Transfers | MISSING | **IMPLEMENTED** | `router.tsx` L571–573; `InventoryTransfer*Page.tsx`; `POS-REACT-INVENTORY-TRANSFER-01` |
| Branch report scoping | org-wide / P1 | **PARTIAL (honest)** | `report-branch-scope.ts`, `ReportScopeControls.tsx`, `POS-REPORTS-BRANCH-SCOPING-01` |
| Expenses React CRUD | MISSING | **IMPLEMENTED** | `ExpenseListPage`, `ExpenseCreatePage`, `ExpenseDetailPage`; `POS-EXPENSES-REACT-CRUD-01` |
| B2B identity/display | PARTIAL | **IMPLEMENTED** (list snapshot by design) | `POS-B2B-IDENTITY-DISPLAY-01`; BusinessCustomer detail vs list asymmetry intentional |
| Discount reporting | MISSING | **IMPLEMENTED** | `POS-DISCOUNT-REPORTING-HARDENING-01`; operational metrics |
| Expired stock quick flow | MISSING | **IMPLEMENTED** | `expired-waste-quick-flow.ts`; `POS-EXPIRED-STOCK-WASTE-QUICK-FLOW-01` |
| Product profitability ranking | DEFERRED | **IMPLEMENTED** | `ProductProfitabilityTable.tsx`; `POS-PRODUCT-PROFITABILITY-RANKING-01` |
| Test harness / session failures | 88 FAIL | **IMPLEMENTED** | 1256/1256; `POS-REACT-TEST-HARNESS-ORG-SESSION-REPAIR-01` |
| i18n encoding (en.ts) | PARTIAL | **PARTIAL** | en.ts repaired for harness; **PH locales still corrupted** on movement labels |
| ManageInventory clarity | PARTIAL | **IMPLEMENTED** | `canViewInventory` vs `canManageInventory`; `POS-INVENTORY-PERMISSION-I18N-POLISH-01` |
| CustomerOrder UnitCost/COGS | P2 gap | **IMPLEMENTED** | Cash/GCash settlement sale + snapshots; `POS-CUSTOMER-ORDER-COGS-HARDENING-01` |
| React suite red | 88 failures | **GREEN** | 1256/1256 baseline |
| Supplier payables | (implied future) | **DEFERRED** | Architecture guards; no AP tables |
| Report export | DEFERRED | **DEFERRED** | `canExportData()` reserved; no UI |
| Real Card/GCash | LATER | **LATER** | FakePaymentGateway; floor excludes provider methods |
| B2B retail checkout | DEFERRED | **DEFERRED** | `SaleBuyerParty.Organization` exists; no React checkout UX |
| Org Web offline | DEFERRED | **NOT_NEEDED (Web)** | `organization-web-runtime-policy.ts` |

---

## OPEN_GAP_MATRIX

| Area | Feature | Status | Backend | UI | Tests | Risk | Priority | Evidence | Recommended action |
|------|---------|--------|---------|-----|-------|------|----------|----------|-------------------|
| Reporting | Dashboard branch vs org mix | PARTIAL | Mixed queries | Labeled partially | — | Med misread | **IMPORTANT** | `DashboardQueryService.cs` L114–115 | Unify or clearly separate branch/org cards |
| Reporting | CSV/export | DEFERRED | entitlement only | none | — | Low | **IMPORTANT** | `pos-capabilities.ts` `canExportData` | Report export package |
| I18n | PH locale mojibake | PARTIAL | — | corrupted labels | — | Low UX | **IMPORTANT** | `fil-PH.ts` movement keys vs `en.ts` | Locale parity repair |
| CustomerOrder | Org + Utang no settlement | PARTIAL | early return | N/A (Personal-only storefront) | unit gap | Low edge | **LATER** | `CustomerOrderUtangLedgerService.cs` L83–86 | Block at place or post org settlement |
| Purchasing | GRN/direct receipt reversal | DEFERRED | immutable GRN | — | — | Med | **LATER** | `P10-WP02-purchasing.md` | Authorized reversal design |
| Inventory | Stock use/waste/production integration | VALIDATION_GAP | API yes | yes | unit only | Med | **IMPORTANT** | no `PosStockUse*ApiTests` | PostgreSQL integration tests |
| Device | PWA enforcement off | PARTIAL | optional | policy | — | Low pilot | **LATER** | `PosDeviceAuthorizationOptions` | Native/production policy |
| Payments | Real GCash/Card | DEFERRED | lab | not on floor | — | P0 if mis-shipped | **LATER** | `FakePaymentGateway.cs` | Authorized provider only |
| Suppliers | Supplier payables / AP | DEFERRED | forbidden | none | arch guards | — | **NOT_NEEDED (MVP)** | `PosPurchasingScopeArchitectureTests.cs` | Do not build without ADR |
| B2B | Retail checkout to business org | DEFERRED | domain ready | no UX | — | — | **LATER** | `SaleBuyerParty.cs`, checkout client | After B2B demand |
| Accounting | FIFO / lot COGS | DEFERRED | — | — | — | — | **NOT_NEEDED** | cost resolver | Out of scope |
| Accounting | GL / operating profit | DEFERRED | — | — | — | — | **NOT_NEEDED** | — | Out of scope |
| UX | Inventory nav crowding | PARTIAL | — | many sub-routes | — | Low | **LATER** | `router.tsx` inventory subtree | IA polish |
| Cost | Legacy pre-snapshot sales | PARTIAL | Unavailable status | — | — | Low | **NOT_NEEDED** | migration backfill forbidden | Accept Unavailable |
| Offline | Org Web mutations | DEFERRED | ONLINE_ONLY | blocked | policy | — | **NOT_NEEDED** | `organization-web-runtime-policy.ts` | Native path only |

---

## RISK_MATRIX

### P0
1. **Never ship FakePaymentGateway as production GCash/Card** — React floor correctly omits provider methods.
2. **Tenancy discipline on new endpoints** — existing paths fail-closed; ongoing discipline required.

### P1
1. **Dashboard/report branch literacy** — operator may misread org-wide utang/expenses beside branch sales.
2. **Stock use/waste/production integration test gap** — unit coverage exists; PostgreSQL proof missing.

### P2
1. PH locale mojibake on inventory movement labels.
2. Report export absent (accountant workflow).
3. Org-party + Utang customer order edge (API-only).

### P3
1. Inventory navigation crowding.
2. Purchase receipt reversal.
3. Device enforcement policy for production Web.

### P4 / NOT_NEEDED
1. Supplier payables / AP (explicitly forbidden in architecture).
2. FIFO / weighted-average / GL / operating profit.
3. Org Web offline business mutations.
4. B2B retail checkout (until demand).

---

## TARGET_MARKET_FILTER

### MVP_REQUIRED
- None blocking single-branch online pilot (core ops implemented).

### IMPORTANT
- Dashboard/report branch clarity for multi-branch operators.
- PH locale i18n parity (movement labels).
- Report export (CSV) for owner/accountant workflow.
- PostgreSQL integration tests for stock use / waste / production.

### LATER
- Real GCash/Card provider integration (authorized).
- Supplier payables / AP (requires ADR + architecture change).
- B2B retail checkout at register.
- Purchase receipt reversal.
- Org-party + Utang order settlement edge case.
- Device enforcement on production Web.
- Inventory nav IA polish.

### NOT_NEEDED (target market)
- FIFO / lot-layer COGS accounting.
- General ledger / double-entry / BIR engine.
- Operating profit / expense allocation engine.
- Org Web offline money queue.
- Enterprise per-operation RBAC explosion.

---

## RECOMMENDED_ROADMAP

### NEXT_01 = POS-DASHBOARD-REPORT-BRANCH-CLARITY-01

| Field | Value |
|-------|--------|
| **WHY_NOW** | Multi-branch ops and transfers are implemented; dashboard still mixes branch sale metrics with org-wide utang/expenses/low-stock — highest misread risk for pilots. |
| **BUSINESS_VALUE** | Owners trust branch vs whole-business numbers; reduces support confusion during multi-branch rollout. |
| **RISK_IF_DEFERRED** | Branch-bound operators make wrong restock/credit decisions from blended dashboard. |
| **DEPENDENCIES** | None; reporting services already branch-capable for sales family. |
| **ESTIMATED_SCOPE** | **MEDIUM** |

### NEXT_02 = POS-I18N-LOCALE-PARITY-02

| Field | Value |
|-------|--------|
| **WHY_NOW** | en.ts harness keys fixed; PH locales (`fil-PH`, `ceb-PH`, `hil-PH`, `ilo-PH`) still show `?` for movement separators — visible on inventory detail/history. |
| **BUSINESS_VALUE** | Professional operator-facing copy in local languages. |
| **RISK_IF_DEFERRED** | Low functional risk; credibility/UX debt in non-English locales. |
| **DEPENDENCIES** | None. |
| **ESTIMATED_SCOPE** | **SMALL** |

### NEXT_03 = POS-REPORT-EXPORT-01

| Field | Value |
|-------|--------|
| **WHY_NOW** | Report surfaces are rich; owners routinely need CSV for accountant/review; entitlement stub exists with no UI. |
| **BUSINESS_VALUE** | Closes owner→accountant handoff without manual re-entry. |
| **RISK_IF_DEFERRED** | Operators export via screenshots/manual copy; not a pilot blocker. |
| **DEPENDENCIES** | Report queries already server-side; Owner/Manager capability gating. |
| **ESTIMATED_SCOPE** | **MEDIUM** |

### LATER
- **POS-SUPPLIER-PAYABLES-01** — only after explicit ADR; architecture currently **forbids** AP. Manual supplier tracking + PO history suffices for most micro retailers today.
- **POS-PAYMENT-PROVIDER-GCASH-01** — authorized real provider; keep ManualGCash on floor until then.
- **POS-B2B-RETAIL-CHECKOUT-01** — org buyer at register; domain prepared, no product flow.
- **POS-PURCHASE-RECEIPT-REVERSAL-01** — immutable GRN today.
- **POS-INVENTORY-OPS-INTEGRATION-01** — stock use / waste / production PostgreSQL tests.

### NOT_NEEDED
- FIFO / GL / operating profit / Org Web offline mutations / enterprise RBAC granularity.

**Note:** Supplier Payables is **not** recommended as NEXT_01. For Philippines micro/small retail MVP, branch-report clarity and locale/export polish deliver higher pilot value with lower architectural risk than introducing AP.

---

## FINAL_VERDICT

1. **Is Organization POS now usable for a real small-store pilot?**  
   **Yes** — single-branch, online, with Cash/ManualGCash/Utang, inventory ops, purchasing, utang, customer orders, profitability, and expenses.

2. **Is it safe for multi-branch pilot?**  
   **Partially yes** — transfers and branch-scoped ops exist; operators must understand mixed dashboard/report scoping (labeled but not uniform).

3. **Single best next feature/package?**  
   **POS-DASHBOARD-REPORT-BRANCH-CLARITY-01** — closes the largest remaining operator-trust gap without forbidden architecture (AP/FIFO/GL).

4. **What should explicitly NOT be built yet?**  
   Supplier payables/AP, FIFO/lot COGS, GL, real Card/GCash without authorization, B2B retail checkout, Org Web offline mutations.

5. **Real blockers before pilot?**  
   **None for single-branch online pilot** with trained operators and ManualGCash (not provider GCash). Multi-branch pilots should include branch-report training. Ensure Platform + POS APIs running (local validation launcher).

---

## VALIDATION EVIDENCE (this audit)

| Check | Result |
|-------|--------|
| Git START_SHA | `fe7e3b852b05832cf39f84adfa7e62212dd26d0d` |
| Worktree at audit start | **DIRTY** (unrelated local tooling: `tools/Start-PlatformApiOnly.ps1`, `tools/Start-PosApiOnly.ps1` — not part of this task) |
| `PosCustomerOrderUtangLedgerApiTests` | **4/4 PASS** (Release, filtered run) |
| React full suite | **Not re-run**; trusted baseline **1256/1256** per harness repair package |
| Application code changed | **NO** |

---

## APPLICATION_CODE_CHANGED

**NO** — audit documentation only.
