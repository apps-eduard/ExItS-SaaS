# POS-ORGANIZATION-PILOT-READINESS-AUDIT-03

**Status:** COMPLETE  
**Branch:** `feat/organization`  
**TASK:** POS-ORGANIZATION-PILOT-READINESS-AUDIT-03  
**START_SHA:** `27eea7ab71e986b3f5601081eca65105bdaf8b4f`  
**FINAL_SHA:** _(recorded after docs commit)_  
**MODE:** READ_ONLY_AUDIT (documentation only; no application code changed)

---

## EXECUTIVE_VERDICT

Organization POS is **controlled-pilot ready** for single-branch Philippines SMB (sari-sari / small staffed store) on online Web/PWA, using Cash / ManualGCash / Utang, org-level purchasing and supplier payables, and optional customer orders (pickup; delivery optional after fulfillment setup).

Since AUDIT-02, code has closed: report CSV export, locale parity repair, inventory-ops PostgreSQL coverage, purchase receipt reversal, and supplier payables (AP separate from Customer Utang). No **P0 pilot blockers** found in this audit.

| Metric | Value |
|--------|--------|
| **ORGANIZATION_POS_CORE_COMPLETENESS_PERCENT** | **~95%** |
| **ORGANIZATION_POS_PRODUCTION_READINESS** | **CONTROLLED_PILOT** — single-branch online pilot ready; multi-branch usable with org-wide ledger literacy; not broad production (no real payment gateway, no org offline-first, no GL/FIFO) |

**Authority:** CODE over docs. AUDIT-02 claims that supplier payables and report export were deferred are **superseded** by current implementation.

---

## DOC_VS_CODE_DISCREPANCIES (AUDIT-02 → CODE NOW)

| AUDIT-02 claim | Current code |
|----------------|--------------|
| Supplier payables DEFERRED / architecture forbid AP | **IMPLEMENTED** — `SupplierPayable` / ADR-023 / report+CSV |
| Report export DEFERRED | **IMPLEMENTED** — client CSV via `report-csv-export.ts` + `store-export` |
| PH locale mojibake IMPORTANT | **REPAIRED** — `POS-I18N-LOCALE-PARITY-02`; movement labels use `—` |
| Inventory ops Postgres VALIDATION_GAP | **COVERED** — `POS-INVENTORY-OPS-POSTGRES-INTEGRATION-TESTS-01` |
| Purchase receipt reversal DEFERRED | **IMPLEMENTED** — `POS-PURCHASE-RECEIPT-REVERSAL-01` |

---

## STATUS_BY_AREA

| Field | Status | Evidence / notes |
|-------|--------|------------------|
| **SELL_STATUS** | **IMPLEMENTED** | `SellFloorPage`, `CheckoutCashPage` (Cash/ManualGCash/Utang), barcode/weight, Today’s Prices, `SellReadinessGate` (device→shift→sell), returns/voids manager-gated |
| **PRODUCT_STATUS** | **IMPLEMENTED** | Catalog CRUD, BusinessUsage, units, barcode, expiry flag, supplier-linked catalog |
| **INVENTORY_STATUS** | **IMPLEMENTED** | Opening/adjust/count/transfer/Stock Use/Waste/Production/lots FEFO; Postgres integration suites present |
| **PURCHASING_STATUS** | **IMPLEMENTED** | PO, partial receive, direct purchase, reversal (blocked if payable payments posted) |
| **SUPPLIER_STATUS** | **IMPLEMENTED** | Manual + connected, catalog sharing, detail history; B2B path = PO not retail cart |
| **SUPPLIER_PAYABLE_STATUS** | **IMPLEMENTED** | Paid-at-receipt + later payments, due date, report AS_OF, CSV, org-scoped |
| **CUSTOMER_STATUS** | **IMPLEMENTED** | Optional on cash/GCash; CRUD; checkout-search for Cashier |
| **UTANG_STATUS** | **IMPLEMENTED** | CreditEntry AR; repayment/statements; separate from SupplierPayable |
| **CUSTOMER_ORDER_STATUS** | **IMPLEMENTED** | Personal storefront, cart, checkout, seller queue, settlement Sale + COGS |
| **DELIVERY_STATUS** | **IMPLEMENTED** (optional for pilot) | Entitlement + coords + policy + hours; pickup-only pilots OK |
| **COST_PROFIT_STATUS** | **IMPLEMENTED** (accuracy model PARTIAL by design) | LAST_AUTHORITATIVE snapshots; unknown cost null≠zero; no FIFO costing |
| **REPORTING_STATUS** | **IMPLEMENTED** | Classic + operational + payables + CSV; honest branch vs org scope labels |
| **EXPENSE_STATUS** | **IMPLEMENTED** | Org-wide (no BranchId) — acceptable for pilot |
| **DEVICE_STATUS** | **PARTIAL** | Registration + capacity; PWA enforcement often off — DEFERRED strict physical device |
| **SHIFT_STATUS** | **IMPLEMENTED** | Open/close; CreateSale requires open shift + register |
| **STAFF_RBAC_STATUS** | **IMPLEMENTED** | SMB matrix; Cashier cannot create customer/repay/void — Manager covers |
| **BRANCH_STATUS** | **PARTIAL** | Ops branch-bound; purchasing/payables/expenses/utang org-level |
| **OFFLINE_STATUS** | **DEFERRED / NOT_NEEDED (Web)** | `organizationWebRuntimePolicy` all offline flags `false` |
| **I18N_STATUS** | **IMPLEMENTED** | en + fil/ceb/hil/ilo parity guard; encoding hygiene repaired |
| **SECURITY_STATUS** | **IMPLEMENTED** | Org isolation fail-closed; idempotency; payable≠utang; export entitlement |
| **PERFORMANCE_STATUS** | **IMPLEMENTED** (watch) | Fine for SMB volumes; large unbounded report windows = watch later |
| **RESPONSIVE_UX_STATUS** | **PARTIAL** | Mobile cards on major reports; inventory nav still crowded |
| **TEST_CONFIDENCE_STATUS** | **IMPLEMENTED** (one known unit-suite lag) | React 1344/1344; CreateBuyerProductAndLinkTests 9/12 |

### Per-area classification detail

| Area | STATUS | REAL_GAP | PILOT_IMPACT | RECOMMENDED_ACTION |
|------|--------|----------|--------------|-------------------|
| Sell | IMPLEMENTED | ManualGCash = typed ref; no split tender | low | Operator training |
| Product | IMPLEMENTED | — | none | — |
| Inventory | IMPLEMENTED | Cost = latest acquisition not FIFO | low–med reports | Document cost model |
| Purchasing | IMPLEMENTED | Reversal blocked after payable payments | low | Document guard |
| Supplier | IMPLEMENTED | No org B2B retail checkout | none if PO-based | Defer B2B cart |
| Supplier payables | IMPLEMENTED | Payment reversal deferred | low | Defer |
| Customer/Utang | IMPLEMENTED | Cashier cannot create/repay | low (staffing) | Staff Manager for CRM |
| Customer orders | IMPLEMENTED | Delivery needs setup | low | Pickup default |
| Delivery | IMPLEMENTED / optional | Setup gate | none if optional | — |
| Cost/profit | PARTIAL (by design) | No FIFO COGS | med for accountants | Do not add FIFO now |
| Reports | IMPLEMENTED | Org-wide cards on dashboard | low if labeled | Keep literacy |
| Expenses | IMPLEMENTED | No BranchId | none for pilot | Accept org-wide |
| Device | PARTIAL | Enforcement optional on PWA | none | DEFERRED |
| Shift | IMPLEMENTED | Shifts not branch-true | low multi-branch | Document |
| RBAC | IMPLEMENTED | Intentional Cashier limits | none | — |
| Branch | PARTIAL | Org ledgers | med for multi-branch | Literacy |
| Offline | DEFERRED | Web online-only | none | LATER |
| I18n/UX | IMPLEMENTED / PARTIAL UX | Crowded inventory nav | low | Optional polish |
| Security | IMPLEMENTED | Dev bypass not prod | none | Discipline |
| Tests | PARTIAL suite | 3 stale unit tests | low CI | Repair package |

---

## CREATE_BUYER_PRODUCT_AND_LINK

| Field | Value |
|-------|--------|
| **CREATE_BUYER_PRODUCT_AND_LINK_STATUS** | **FAILING_UNIT_TESTS (3/12)** — production path OK |
| **CREATE_BUYER_PRODUCT_AND_LINK_PILOT_IMPACT** | **LOW** — isolated test lag |

**Root cause:** `CreateBuyerProductAndLinkRequest` now requires `BusinessUsage` (or sell/purchase/usage flags). Unit harness `Request(...)` omits them → `pos.catalog.bulk_validation: Choose how your business will use this product before creating it.`

**Production:** `ConnectedCatalogPage.tsx` sends `businessUsage: "Resale"` on create-and-link.

**Classification:** TEST_HYGIENE / PRE_EXISTING_UNRELATED to floor pilot — not a production create-link bug.

---

## PILOT SCENARIOS

| Scenario | Status | Blockers |
|----------|--------|----------|
| **SCENARIO_A** Sari-sari | **READY** | none |
| **SCENARIO_B** Small staff store | **READY** | none (Cashier gaps covered by Manager/Owner) |
| **SCENARIO_C** Multi-branch | **PARTIAL** | No hard block; org-wide expenses/Utang/purchasing/payables; shifts not branch-dimensioned |
| **SCENARIO_D** Customer order / delivery | **PARTIAL** | Pickup+orders READY after fulfillment readiness; delivery optional (entitlement+coords+policy+hours) |

| Flag | Value |
|------|--------|
| **SINGLE_BRANCH_PILOT_READY** | **YES** |
| **MULTI_BRANCH_PILOT_READY** | **PARTIAL** |
| **CUSTOMER_ORDER_PILOT_READY** | **YES** (pickup); delivery optional |

---

## PAYMENTS / B2B / DEVICE

| Field | Value |
|-------|--------|
| **REAL_PAYMENT_PROVIDER_STATUS** | **DEFERRED** — ManualGCash on floor; FakePaymentGateway lab-only; Card/provider GCash not offered in React checkout |
| **REAL_PAYMENT_PROVIDER_PILOT_REQUIREMENT** | **NO** — ManualGCash is deliberate supported recording mode |
| **B2B_CHECKOUT_STATUS** | **DEFERRED** — org↔org via connected PO/receive/payables; `MerchantCheckoutPage` is personal→merchant storefront |
| **B2B_CHECKOUT_PILOT_REQUIREMENT** | **NO** |
| **DEVICE_OFFLINE_ARCHITECTURE** | **LATER** — Org Web ONLINE_ONLY |

---

## PRIORITY OUTPUT

### P0_BLOCKERS
*(none)*

### P1_BEFORE_PILOT
*(none required for single-branch controlled pilot)*  
Optional readiness hygiene (not blockers):
1. Operator briefing: ManualGCash ≠ verified gateway; report/dashboard branch vs org scope; payable reversal precondition.
2. If connected-supplier create-and-link is in pilot scope: repair `CreateBuyerProductAndLinkTests` before trusting that unit suite in CI.

### P2_AFTER_PILOT
1. Inventory / More navigation IA polish (crowded subtree).
2. CreateBuyerProductAndLink unit-test repair (if not done as hygiene).
3. Multi-branch report/shift dimension improvements if multi-branch pilots expand.
4. Delivery setup UX polish for delivery-first pilots.

### DEFERRED
- Real payment providers (GCash/Card gateway)
- Org B2B retail checkout
- Supplier payment reversal / partial receipt reversal
- Strict physical-device enforcement / Capacitor offline
- Organization offline-first Web

### NOT_NEEDED
- FIFO lot costing / GL / operating-profit accounting
- Expense.BranchId for single-branch pilot
- Broad translation rewrites

---

## PILOT_READY / REAL_BLOCKERS

| Field | Value |
|-------|--------|
| **PILOT_READY** | **YES** (controlled single-branch online pilot) |
| **REAL_BLOCKERS_BEFORE_PILOT** | **NONE** |

---

## VALIDATION EVIDENCE (this audit)

| Gate | Result |
|------|--------|
| React vitest | **1344 / 1344 PASS** |
| TYPECHECK | **PASS** |
| LINT | **PASS** |
| BUILD | **PASS** |
| CreateBuyerProductAndLinkTests | **9 passed / 3 failed** (BusinessUsage omission in harness) |
| NEW_TEST_SKIPS / ONLY / EXCLUSIONS | none added |

---

## NEXT

**NEXT:** `POS-CONNECTED-BUYER-CREATE-LINK-TEST-REPAIR-01`  
**NEXT_WHY:** Only code-proven automated failure found in this audit; production already sends `businessUsage: "Resale"`. Small repair restores unit confidence for connected-supplier create-and-link. **Do not** auto-pick device/offline, FIFO, GL, B2B checkout, or real payment providers. Parallel product action: start controlled Scenario A/B pilot and triage field feedback before large feature packages.

---

## FILES_CHANGED

- `docs/Mobile-React/Reports/POS-ORGANIZATION-PILOT-READINESS-AUDIT-03.md` (this document)
