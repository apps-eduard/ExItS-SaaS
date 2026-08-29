# POS-ORGANIZATION-REMAINING-GAPS-AUDIT-01

## Meta

| Field | Value |
|--------|--------|
| TASK | POS-ORGANIZATION-REMAINING-GAPS-AUDIT-01 |
| START_SHA | `ca849282b1cf2ef71841e87ec41ee39797ef978b` |
| BRANCH | `feat/organization` |
| MODE | READ-ONLY audit (code authoritative; docs cross-checked) |
| APPLICATION_CODE_CHANGED | NO |

---

## Executive verdict

Organization POS has a **usable operational core** on React for micro/small Philippine-style shops: catalog, sell (Cash / Manual GCash / Utang), inventory basics, purchasing (Direct Buy + PO + connected), customers/utang, stock use, production, waste/loss, period profitability, shifts/devices, seller customer-orders.

It is **not production-ready** casually: React still lacks stock-count and inventory-transfer UIs (API/Maui exist), many reports are organization-wide while the shell is branch-bound, cost accuracy is **LAST_AUTHORITATIVE** (not lot FIFO), Org Web is **ONLINE_ONLY**, and the React full suite has **88 failures** (mostly personal/platform/session harness; a few org UI/copy/encoding issues).

| Metric | Value |
|--------|--------|
| ORGANIZATION_POS_CORE_OPERATIONAL_COMPLETENESS | **~78%** — sell/inventory-ops/purchasing/customers/utang/stock-use/production/waste/cost snapshots present; multi-branch React inventory ops + branch-truthful reporting + React expenses CRUD + stock-count/transfer UI still missing |
| ORGANIZATION_POS_PRODUCTION_READINESS | **LIMITED_PILOT** — single-branch online pilot possible with trained operators; multi-branch / physical-count / branch-scoped reporting / suite green / real Card·GCash provider still required before broad production |

---

## CURRENT_ORGANIZATION_POS_MAP

```
Organization POS (React + POS API)
├─ Organization setup / onboarding / operational-setup
├─ Branches (+ fulfillment admin)
├─ Staff / roles / permissions
├─ Devices (org admin + register flow)
├─ Registers / Cashier shifts
├─ Catalog (products, categories, brands, units, prices, import)
├─ Inventory (detail, opening, adjust, lots/expiry, low-stock badges)
│  ├─ Stock Use
│  ├─ Production (setups + runs)
│  ├─ Waste / Loss
│  ├─ Transfers ………… API YES / React UI MISSING
│  └─ Stock Count ……… API YES / React UI MISSING
├─ Purchasing (Direct Buy, PO, Goods Receipt, connected PO, prepare-products)
├─ Suppliers (manual + connected catalog sharing)
├─ Customers (retail + Business Customers projection)
├─ Sell / Checkout (Cash, ManualGCash, Utang)
├─ Payments (manual; FakePaymentGateway for Card/GCash lab only)
├─ Utang (product-based Business Utang)
├─ Returns / refunds
├─ Cost / COGS / Gross Profit (sale snapshots + profitability report)
├─ Reports (classic + operational + profitability)
├─ Delivery / customer ordering (seller queue + storefront)
├─ Offline/native readiness (Org Web ONLINE_ONLY; engine preserved)
└─ Expenses ………… API YES / React CRUD routes MISSING (classic report only)
```

Evidence: `Api/Program.cs` Map* endpoints; `React/src/app/router.tsx`; `React/src/features/*`.

---

## Status by area

| Area | Status | Notes |
|------|--------|-------|
| SELL_STATUS | **IMPLEMENTED** | Floor + checkout; Cash/ManualGCash/Utang; discounts/overrides; FEFO; idempotency; void. Card/provider GCash not on React floor (intentional). |
| PRODUCT_STATUS | **IMPLEMENTED** | BusinessUsage Resale/Ingredient/InternalUse/ProducedItem; sell filter `CanBeSold`; brands/categories/units/expiry. Legacy `IngredientAndSellable` classifies as Resale. |
| INVENTORY_STATUS | **PARTIAL** | Opening/adjust/lots/low-stock badges IMPLEMENTED. **Stock Count + Transfers: API/Maui yes, React UI missing.** Valuation = estimated last UnitCost (no account-level stock-value field). |
| PURCHASING_STATUS | **IMPLEMENTED** | Direct Buy, PO, GRN, partial receive, connected PO, BuyerSupplierProductLink prepare. Update-PO still buyer-product oriented (partial polish). |
| SUPPLIER_STATUS | **PARTIAL** | Connected sharing AllEligible/SelectedOnly IMPLEMENTED. Business Customer list/detail identity = **SNAPSHOT_CONSISTENT** (POS-B2B-IDENTITY-DISPLAY-01; live list deferred — no Platform batch resolver). B2B retail checkout DEFERRED. |
| CUSTOMER_STATUS | **IMPLEMENTED** | Walk-in, CRUD, history, Business Customers list/detail. Not duplicated as Pinoy Loan. |
| UTANG_STATUS | **IMPLEMENTED** | Org product-based Utang + repay/statements. Personal Utang separate. Offline Utang on Org Web blocked (ONLINE_ONLY). |
| PAYMENTS_STATUS | **IMPLEMENTED** (manual) | Cash/ManualGCash/Utang real. `FakePaymentGateway` only for Card/GCash attempts — **do not ship as production GCash**. |
| STOCK_USE_STATUS | **IMPLEMENTED** | Domain/API/React/void/FEFO/cost snapshot; ONLINE_ONLY. Cost null when no acquisition. |
| PRODUCTION_STATUS | **IMPLEMENTED** | Setups/runs/void/material cost/output UnitCost; cycle LIMITED; nested Made→Made LATER; capacity DEFERRED. |
| WASTE_LOSS_STATUS | **IMPLEMENTED** | Exact lots, reasons, void, cost; expired quick-flow **IMPLEMENTED** (POS-EXPIRED-STOCK-WASTE-QUICK-FLOW-01). |
| COST_PROFIT_STATUS | **IMPLEMENTED** (accuracy PARTIAL) | SaleLine/Sale snapshots; profitability report; Waste/StockUse separate. **LAST_AUTHORITATIVE** (not lot FIFO). Product profitability ranking DEFERRED. Legacy sales Unavailable (no backfill). |
| REPORTING_STATUS | **PARTIAL** | Classic + operational + profitability present. Most aggregates **org-wide** while shell is branch-bound (misread risk). Discount period totals missing. Product GP ranking deferred. Export deferred. |
| DEVICE_STATUS | **PARTIAL** | Register/devices UI + runtime policy; sell readiness gate. Not a POS `UtangCapability` (admin experience). PWA device context optional. |
| SHIFT_STATUS | **IMPLEMENTED** | Open/close; CreateSale requires open shift + register. |
| STAFF_RBAC_STATUS | **PARTIAL** | Owner/Manager/Cashier/InventoryStaff/ReportingUser matrix. Stock Use/Production/Waste fold into ManageInventory (over-broad for some shops). Devices via admin experience. |
| BRANCH_STATUS | **PARTIAL** | Multi-branch entities + sale/inventory branch params. Reports mostly org-wide; profitability optional `branchId`. Transfer UI missing hurts multi-branch. |
| DELIVERY_STATUS | **IMPLEMENTED** | Seller orders queue + storefront + delivery marks; fulfillment settings. Not every edge of MAUI parity proven in this audit. |
| OFFLINE_NATIVE_READINESS | **DEFERRED / ONLINE_ONLY** | Org Web: no offline business mutations. Stock Use/Production/Waste/COGS use client IDs + server cost authority — native-friendly. Do not activate offline on Web. |
| I18N_STATUS | **PARTIAL** | Five locales present for Stock Use/Production/Waste/Profitability. **Encoding corruption** in `en.ts`: middle-dot became `?` (`Stock adjustment ? increase`, expiration status tests fail). |
| SECURITY_STATUS | **IMPLEMENTED** (residual risk MEDIUM) | Org-scoped endpoints + commercial capabilities. Cross-org ID rejection covered in recent packages. Fake gateway must stay out of production money path. |
| PERFORMANCE_STATUS | **MEDIUM** | Batch cost resolver PASS. Watch sell catalog, connected catalog, unbounded report windows. Avoid client-side sale profit aggregation (server report exists). |
| RESPONSIVE_UX_STATUS | **PARTIAL** | Newer Stock Use/Production/Waste/Profitability follow ExItS patterns; Inventory nav crowded; no transfer/stock-count entry points. |

---

## Gap matrix (selected)

| Area | Feature | Status | Backend | UI | Tests | Risk | Priority | Evidence | Action |
|------|---------|--------|---------|-----|-------|------|----------|----------|--------|
| Inventory | Stock Use | IMPLEMENTED | PASS | PASS | targeted PASS | Low | MVP_REQUIRED done | `StockUseEndpoints`, `/inventory/stock-use` | Maintain |
| Inventory | Production | IMPLEMENTED | PASS | PASS | targeted PASS | Low | MVP_REQUIRED done | production routes/API | Maintain |
| Inventory | Waste/Loss | IMPLEMENTED | PASS | PASS | targeted PASS | Low | MVP_REQUIRED done | waste-loss routes/API | Maintain |
| Inventory | Stock Count | PARTIAL | PASS | MISSING React | API/Maui | Med | **MVP_REQUIRED** | `InventoryEndpoints` stock-counts; no React route | React stock-count package |
| Inventory | Transfers | PARTIAL | PASS | MISSING React | API/Maui | Med | **MVP_REQUIRED** (multi-branch) | transfers API; no React pages | React transfer package |
| Cost | Sale COGS / GP | IMPLEMENTED | PASS | PASS | 15 unit + report UI | Med accuracy | MVP_REQUIRED done | cost snapshots + profitability | Honest LAST_AUTHORITATIVE labels |
| Cost | Lot FIFO layers | DEFERRED | N/A | N/A | — | — | **LATER** | lots lack UnitCost | Do not build ERP costing now |
| Cost | Product profitability rank | DEFERRED | no | no | — | Low | IMPORTANT | cost report | Later package |
| Reports | Branch-scoped aggregates | PARTIAL | profitability optional branch | most org-wide | — | **P1** | **MVP_REQUIRED** | RMAP-20 + ReportingEndpoints | Branch report contract |
| Reports | Discount period totals | **IMPLEMENTED** | sale snapshots | overview/summary/classic/product | targeted PASS | Low | IMPORTANT | POS-DISCOUNT-REPORTING-HARDENING-01 | Maintain |
| B2B | List vs live identity | PARTIAL | by design | asymmetric | — | Low | IMPORTANT | BusinessCustomerUseCases | Reconcile display |
| B2B | Retail checkout of Business Customers | DEFERRED | — | — | — | — | LATER | B2B report | After identity polish |
| Payments | Real Card/GCash | PARTIAL/lab | FakePaymentGateway | not on floor | — | **P0 if mis-shipped** | LATER (auth) | DI FakePaymentGateway | Keep ManualGCash |
| Expenses | CRUD | PARTIAL | API | classic report only | — | Low | IMPORTANT | ExpenseEndpoints; no feature CRUD routes | Expenses React CRUD |
| Waste | Expired quick review | **IMPLEMENTED** | Waste/Loss create prefill | Expiration → Write off → exact lot | targeted PASS | Low | IMPORTANT | this report | Maintain |
| Offline | Org Web mutations | DEFERRED | ONLINE_ONLY | ONLINE_ONLY | policy | — | NOT_NEEDED on Web | offline matrix | Preserve for native |
| Accounting | GL / operating profit | DEFERRED | — | — | — | — | NOT_NEEDED | cost report | Out of scope |
| I18n | Middle-dot encoding | UNSAFE/PARTIAL | — | `?` in strings | inventory tests fail | Low | **P2** | `en.ts` movement labels | Fix encoding |
| Tests | Sell-floor suite | FAIL harness | — | — | 9 fail | Med | **P1** harness | account-class gate in tests | Repair session mocks |

---

## Risk matrix

### P0
1. **Never ship FakePaymentGateway as real GCash/Card** — React correctly omits Card/provider GCash; keep it that way until an authorized provider exists.
2. **Cross-org / tenancy** — current packages assert org scoping; any new endpoint must keep fail-closed validation (ongoing discipline, not a known open leak).

### P1
1. **React Stock Count missing** — operators may misuse ManualDecrease / Waste for physical counts.
2. **React Inventory Transfer missing** — multi-branch ops blocked or forced into unsafe workarounds.
3. **Branch vs org report confusion** — shell shows branch; most totals are organization-wide.
4. **React full-suite org surfaces red** — sell-floor tests hit account-class gate (“different account class”); inventory copy/encoding failures. Not domain COGS regressions, but blocks confidence.

### P2
1. B2B Business Customer list snapshot vs detail live name.
2. ~~Commercial discount period report fields.~~ **DONE** (POS-DISCOUNT-REPORTING-HARDENING-01).
3. Expenses React CRUD.
4. Expired-stock → Waste quick review.
5. i18n middle-dot `?` corruption (`inventory.movementType.manualIncrease`, expiration status).
6. CustomerOrder SaleDeduction without UnitCost (fulfillment COGS trail).
7. Over-broad ManageInventory for Stock Use / Production / Waste write-off.

### P3
1. Product profitability ranking.
2. Nested production / capacity estimates.
3. Account-level estimated stock value on inventory list.
4. Navigation crowding under Inventory.
5. Report export files.

### P4 / NOT_NEEDED for target market
1. General ledger / double-entry / BIR engine / operating profit.
2. Labor/overhead production costing.
3. Enterprise FIFO/weighted-average inventory accounting (unless lot UnitCost is added later).
4. Org Web offline money queue.
5. Automatic production-variance waste.

---

## Full React suite failure classification (1184 tests)

Re-ran `npm test` for this audit: **1096 passed / 88 failed / 215 files**.

| Bucket | Approx count | Examples |
|--------|--------------|----------|
| ORGANIZATION_POS_RELATED | **~12** | `sell-floor.test.tsx` (9) — fail with Personal/Org **account-class** gate, not sell logic; `InventoryDetailPage*.test.tsx` (2) — middle-dot/`?` label expectations; `checkout-personal-customer-picker` (1) QR class |
| SESSION_HARNESS_RELATED | **~20+** | sign-in antiforgery, sign-out, account-shell, workspace-grant-hint, remote-logout, connectivity-ux |
| PERSONAL_RELATED | **~35+** | personal-shell-home, people-lifecycle, personal-switch-to-business, PersonalGuidePage |
| PLATFORM_RELATED | **~25+** | platform-*-client tests (PascalCase/credentials/links/utang/todo/start-business) |
| UNKNOWN | remainder | QR scanner edge cases overlapping harness |

**Conclusion:** Failures are **not** primarily caused by Stock Use / Production / Waste / COGS domain packages. Highest org-adjacent signal is **session/account-class harness breaking sell-floor tests** plus **i18n encoding drift** on inventory labels. Treat suite red as **HIGH priority harness/repair**, not as evidence that inventory packages are broken.

ORGANIZATION_POS_RELATED_TEST_FAILURES ≈ 12 (mostly harness/copy)  
UNRELATED_TEST_FAILURES ≈ 76 (personal/platform/session)

---

## Operator question coverage

| Question | Status |
|----------|--------|
| How much did I sell today? | IMPLEMENTED (dashboard/sales-summary; org-wide caveat) |
| How much gross profit? | IMPLEMENTED (profitability; Complete only) |
| How much inventory do I have? | PARTIAL (OnHand per product; no portfolio valuation dashboard) |
| Which products are low stock? | IMPLEMENTED (list badges + inventory-status report) |
| What did I purchase / receive? | IMPLEMENTED |
| What was used internally? | IMPLEMENTED (Stock Use + report separation) |
| What was wasted/lost? | IMPLEMENTED |
| What was produced? | IMPLEMENTED (production runs history) |
| Which products sell most? | IMPLEMENTED (sales-by-product) |
| Which generate most gross profit? | DEFERRED |
| Utang outstanding? | IMPLEMENTED |
| Branch performance? | PARTIAL (branchId on profitability only; most reports org-wide) |
| Cashier/device/shift? | PARTIAL (sales-by-cashier, shifts-summary; device reporting light) |
| Inventory changed today? | PARTIAL (movements report by type; no unified ops timeline UI) |

---

## Target-market filter summary

| Class | Items |
|-------|--------|
| MVP_REQUIRED | React Stock Count; React Transfers (if multi-branch); branch-report honesty/contract; suite harness repair for sell-floor; keep ManualGCash-only payments |
| IMPORTANT | Expenses CRUD UI; inventory i18n encoding fix; clearer ManageInventory vs write-off permission story |
| LATER | Product profitability ranking; lot-layer FIFO COGS; nested BOM; real Card/GCash provider; B2B retail checkout; native offline activation |
| NOT_NEEDED | GL, operating profit, labor/overhead, enterprise manufacturing, Org Web offline money |

Exact cost-layer/FIFO costing is **safely deferred** for micro/small POS if UI honestly says “estimated / last purchase or production cost” and forces acquisition costs on receive/opening.

---

## Recommended roadmap

### NEXT_01 = POS-REACT-STOCK-COUNT-01
- **WHY_NOW:** Physical count is core store ops; API exists; React gap pushes unsafe ManualDecrease/Waste misuse.
- **SCOPE:** Stock count create/complete/history UI; variance movements; branch-scoped; permissions View/ManageInventory.
- **DO_NOT_INCLUDE:** Waste auto-classification; GL; full cycle count analytics.

### NEXT_02 = POS-REACT-INVENTORY-TRANSFER-01
- **WHY_NOW:** Multi-branch inventory integrity; API exists; without UI, branch OnHand drifts operationally.
- **SCOPE:** Transfer create/ship/receive/cancel UI; lot-aware where API supports; history.
- **DO_NOT_INCLUDE:** Inter-org transfers; advanced WMS.

### NEXT_03 = POS-REPORTS-BRANCH-SCOPING-01
- **WHY_NOW:** Operators bind a branch then read org-wide totals — money/ops misread risk (P1).
- **SCOPE:** Contract for classic/operational reports: filter by branch **or** hard-label “All branches”; wire `branchId` consistently; profitability already optional.
- **DO_NOT_INCLUDE:** Fake P&L; tax engine; product profitability ranking.

### NEXT_04 = POS-REACT-TEST-HARNESS-ORG-SESSION-REPAIR-01
- **WHY_NOW:** 9 sell-floor + shell/session failures block CI confidence for Organization work.
- **SCOPE:** Fix account-class / grant mocks so org sell/inventory tests mount; fix i18n `·` encoding; triage InventoryDetail expectations.
- **DO_NOT_INCLUDE:** Rewriting Personal suite (separate package if needed).

### NEXT_05 = POS-EXPENSES-REACT-CRUD-01 **or** POS-B2B-IDENTITY-DISPLAY-01
- **WHY_NOW (pick by product priority):** Expenses API without CRUD leaves cost ops incomplete; B2B snapshot/live asymmetry confuses supplier-facing identity.
- **SCOPE (Expenses):** List/create/void expense UI + categories. **SCOPE (B2B):** List shows snapshot + optional live badge; persist refresh on detail.
- **DO_NOT_INCLUDE:** Operating profit dashboard; B2B retail checkout; payment terms.

**Not automatic next:** `POS-INVENTORY-OPERATIONS-REPORTING-HARDENING-01` as a giant mixed package — split into Stock Count, Transfers, and Branch Reporting as above.

---

## Area scoreboard (for FINAL REPORT fields)

```
SELL_STATUS=IMPLEMENTED
PRODUCT_STATUS=IMPLEMENTED
INVENTORY_STATUS=PARTIAL
PURCHASING_STATUS=IMPLEMENTED
SUPPLIER_STATUS=PARTIAL
CUSTOMER_STATUS=IMPLEMENTED
UTANG_STATUS=IMPLEMENTED
PAYMENTS_STATUS=IMPLEMENTED
STOCK_USE_STATUS=IMPLEMENTED
PRODUCTION_STATUS=IMPLEMENTED
WASTE_LOSS_STATUS=IMPLEMENTED
COST_PROFIT_STATUS=IMPLEMENTED
REPORTING_STATUS=PARTIAL
DEVICE_STATUS=PARTIAL
SHIFT_STATUS=IMPLEMENTED
STAFF_RBAC_STATUS=PARTIAL
BRANCH_STATUS=PARTIAL
DELIVERY_STATUS=IMPLEMENTED
OFFLINE_NATIVE_READINESS=ONLINE_ONLY_DEFERRED
I18N_STATUS=PARTIAL
SECURITY_STATUS=IMPLEMENTED
PERFORMANCE_STATUS=MEDIUM
RESPONSIVE_UX_STATUS=PARTIAL
```

```
P0_GAPS=FakePaymentGateway must not be production GCash/Card; tenancy fail-closed discipline
P1_GAPS=React Stock Count; React Transfers; branch vs org report honesty; org session/sell-floor harness
P2_GAPS=Expenses CRUD; i18n encoding; CustomerOrder COGS UnitCost; inventory write-off permission coarseness
P3_GAPS=Product profitability rank; nested production; list stock valuation; nav crowding; exports
```

```
MVP_REQUIRED_GAPS=Stock Count UI; Transfer UI (multi-branch); branch-report contract; harness repair; payment honesty
IMPORTANT_GAPS=Expenses CRUD; i18n middle-dot fix; ManageInventory write-off permission story
LATER_GAPS=Product GP ranking; lot FIFO; real Card/GCash; B2B retail checkout; native offline
NOT_NEEDED_FOR_TARGET_MARKET=GL; operating profit; labor/overhead; Org Web offline money; enterprise manufacturing
```

```
ORGANIZATION_POS_RELATED_TEST_FAILURES=~12 (sell-floor harness + inventory label/encoding + checkout QR class)
UNRELATED_TEST_FAILURES=~76 (personal/platform/session)
```

---

## Explicit non-defects (intentional deferrals)

- Full accounting / GL / operating profit  
- Lot-layer FIFO / weighted-average formal valuation  
- Production capacity / auto waste from variance / nested Made→Made without ingredient flag  
- Org Web offline business mutations  
- Product profitability ranking (deferred after cost package)  
- B2B retail checkout / payment terms  
- Real payment provider until authorized  

---

## Evidence sources (non-exhaustive)

- `ExItS.PinoyBusinessPOS.Api/Program.cs` endpoint map  
- `ExItS.PinoyBusinessPOS.React/src/app/router.tsx`  
- `docs/Mobile-React/Authoritative/Offline/react-pwa-offline-capability-matrix.md`  
- Reports: Stock Use, Production, Waste/Loss, Cost/Profit, ProductBusinessUsage, B2B relationship, Buyer onboarding, RMAP-07/17/20  
- Domain: `StockMovementType`, `ProductUsage`, Sale cost snapshots  
- Vitest full suite 2026-08-29: 1096/88/1184; sell-floor failure HTML shows account-class gate  

---

## Audit limitations

- Did not run full backend suite or local E2E browser matrix.  
- Delivery “complete” based on routes/endpoints + authoritative docs; deep fulfillment edge cases not re-proven.  
- Maui parity used only to mark API-without-React gaps, not as a migration mandate.  
