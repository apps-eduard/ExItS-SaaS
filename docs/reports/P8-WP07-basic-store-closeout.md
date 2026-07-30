# P8-WP07 — Basic Store Closeout

Phase marker: `P8-WP07-basic-store-closeout`

## Status

**Complete with documented risks. Phase 8 closed.** Reconciled P8-WP01 through P8-WP06 as one coherent Basic Store MVP subsystem. Closeout added consolidated capability-matrix, Phase 8 migration-chain, and deferred-scope architecture guards. **No new business capability.** **Not production-ready** while R-109, production authentication/roles, online-only Basic Store restrictions, Manual GCash verification, report export, and related documented blockers remain open. **Phase 9 was not started.**

Feature commit: 0bc5ebb999c0708e6ac76b04a30d522037eec3cb

## Phase 8 closeout decision

Mark Phase 8 **complete with documented risks** when:

- P8-WP01–P8-WP06 form one subsystem (catalog → sales → Product-Based Utang → inventory → expenses → dashboard/reports)
- No critical Phase 8 defect remains in the reconciled scope
- Organization isolation and `store-*` authorization are proven
- Transaction, idempotency, and inventory invariants hold
- Reports reconcile with source records (read-only projections; no second balance model)
- Migration chain apply / stepwise rollback / re-apply succeeds
- Full `ExItS.slnx` Release tests pass (**882** / **0** / **0**; baseline 851)
- Android Release APK builds
- Documentation matches implementation
- HealthCare remains frozen
- Git is clean; `main` matches `origin/main` after push

**Do not claim production readiness** while documented blockers remain open.

## Final delivered Basic Store scope

| Area | Delivered |
|---|---|
| Catalog / barcode | Org-owned categories/products; SKU/barcode uniqueness (inactive reserved); deterministic barcode validation; UOM lock after inventory activity; soft lifecycle (no hard delete of history) |
| Sales | Cash / Manual GCash; server totals; immutable completed sale + line snapshots; org-scoped sale numbers; idempotent checkout; void preserves history |
| Product-Based Utang | Atomic sale + linked credit; amount = sale total; optional due date; linked reverse only via sale void; subsequent Utang activity blocks invalid void |
| Inventory | Movement-derived on-hand; tracked checkout non-negative; untracked skip movements; sale deduction / void restore atomic; low-stock derived |
| Expenses | Immutable Cash / Manual GCash; void + replacement; summaries exclude voided from net; no effect on sales/inventory/Utang/Platform billing |
| Dashboard / reports | Read-only org-scoped projections; 366-day inclusive max; FIFO aging reused; voided separated; category-label caveat documented; no report tables / export files |

## Final business invariants (proven)

### Catalog

- Products and categories are organization-owned
- SKU and barcode uniqueness remain organization-scoped; inactive identifiers reserved
- Barcode validation is deterministic
- Product UOM cannot change after inventory activity
- No hard delete of historical catalog records

### Sales

- Completed sale and line snapshots are immutable; totals server-side
- Quantity precision follows UOM; Cash and Manual GCash rules remain distinct
- Exact replay creates one sale; sale numbers concurrency-safe per org
- Void preserves history and cannot run twice

### Product-Based Utang

- One sale creates exactly one linked credit atomically; amount = sale total
- Direct linked-credit reversal blocked; sale void + credit reverse succeed or fail together
- Subsequent Utang activity prevents invalid void/reversal

### Inventory

- On-hand derived from immutable movements; tracked checkout cannot go negative
- Untracked products create no stock movements
- Sale deduction atomic with completion; void restores stock exactly once
- Utang void coordinates sale, credit, and stock atomically
- Low-stock status is derived

### Expenses

- Expenses immutable; corrections via void + replacement
- Summaries exclude voided from net totals
- No expense operation affects sales, inventory, Utang, or Platform billing

### Dashboard and reports

- Read-only, organization-scoped; totals reconcile with source records
- Voided records separated; FIFO aging reused (not reimplemented)
- No persisted report totals; category-label caveat documented
- 366-day inclusive range enforced consistently

## Final capability matrix

| Code | Role |
|---|---|
| `store-catalog-view` | Continuity / read |
| `store-catalog-manage` | Full commercial mutation |
| `store-sales-view` | Continuity / read |
| `store-sales-create` | Full commercial mutation |
| `store-sales-void` | Full commercial mutation |
| `store-inventory-view` | Continuity / read |
| `store-inventory-manage` | Full commercial mutation |
| `store-expenses-view` | Continuity / read |
| `store-expenses-manage` | Full commercial mutation |
| `store-dashboard-view` | Continuity / read |
| `store-reports-view` | Continuity / read |

| Commercial state | Behavior |
|---|---|
| Trialing / Active / GracePeriod | Grant-controlled full capabilities |
| PastDue / Cancelled / Expired | Continuity: view grants only (mutations denied) |
| Suspended / missing / stale / unknown | Fail closed |

Additional gates:

- Product-Based Utang create requires `store-sales-create` **and** `customer-credit-create`
- Utang void requires `store-sales-void` **and** credit reverse capability
- Stock validation is part of checkout (cannot bypass by omitting inventory manage grant)
- Development/Testing commercial and actor headers ignored outside approved environments
- Platform product access does not assign POS operational roles
- No `store-reports-export` (file export deferred)

Consolidated proof: `BasicStoreCapabilityMatrixTests` + existing per-WP commercial tests.

## Migration inventory

| Migration | Purpose |
|---|---|
| `AddPosCatalogAndBarcodes` | Categories / products |
| `AddPosSimpleSales` | Sales / lines / sale numbers |
| `AddProductBasedUtang` | Sale↔credit linkage + Utang payment |
| `AddPosBasicInventory` | Accounts / stock movements |
| `AddPosExpenses` | Expense categories / expenses / sequences |

Pre-Phase-8 tip for chain tests: `AddPosIdempotencyRecords`.

**No** dashboard/report migration. **No** supplier, tax, accounting, gateway, report-cache, Platform, or HealthCare tables in `pos`.

Closeout proof: `PosPhase8MigrationChainTests` — apply latest → stepwise rollback Expenses→…→idempotency → re-apply; schema + deferred-table bans.

No new migration required (no confirmed schema defect).

## Phase 8 endpoint inventory (Basic Store)

| Area | Routes (prefix `/api/v1/pos`) |
|---|---|
| Catalog | `/catalog/categories`, `/catalog/products` (+ by-sku / by-barcode, deactivate/reactivate) |
| Sales | `/sales` (list/create/get/void; Cash/ManualGCash/Utang) |
| Inventory | `/inventory` (+ low-stock, enable/disable, adjustments, movements) |
| Expenses | `/expense-categories`, `/expenses` (+ summary, void) |
| Dashboard / reports | `/dashboard`; `/reports/sales`, `/sales/by-product`, `/sales/by-category`, `/utang`, `/inventory`, `/expenses` |

Utang MVP customer/credit/repayment/statement/sync routes remain as delivered in Phases 6–7 (not re-scoped here).

Contracts: typed DTOs; server-authoritative totals; stable `ProblemDetails.errorCode`; org concealment; cancellation supported on query paths; no raw EF entities.

## Transaction / idempotency design

| Path | Guarantee |
|---|---|
| Simple sale checkout | Atomic sale + lines (+ stock when tracked) |
| Product-Based Utang | Atomic sale + linked credit (+ stock when tracked) |
| Sale void | Atomic void (+ stock restore once when tracked) |
| Utang void | Atomic sale void + credit reverse + stock restore |
| Expense create | Idempotent via established POS idempotency records |
| Exact replay | One mutation; changed payload → conflict; concurrent duplicates converge |

Proven via existing PostgreSQL/Testcontainers integration suites plus Phase 8 chain migration test. Source idempotency on stock deduction/restoration preserved from P8-WP04.

## MAUI Basic Store experience

Online-only: catalog/barcode, Cash/Manual GCash checkout, Product-Based Utang checkout, sales history/void, inventory/adjustments/low-stock, expenses/summaries, dashboard/reports. Established DesignSystem + PosResources (en / fil-PH). No Phase 9 screens. Reconnect-required when offline for authoritative store surfaces.

## Closeout hardening delivered

1. Consolidated `store-*` capability matrix unit suite (`BasicStoreCapabilityMatrixTests`)
2. Full Phase 8 migration-chain integration test (`PosPhase8MigrationChainTests`)
3. Architecture guards: no `Migrate()` on POS Program; phase marker; deferred-concept bans; no report-cache tables; no `store-reports-export`; Platform↔POS store code literal parity
4. Phase marker updated to `P8-WP07-basic-store-closeout`

**No confirmed runtime business defect** required a production code fix beyond marker/docs/tests.

## Explicit deferred functionality

- Suppliers and purchasing
- Warehouses, branches, transfers, batches, lots, serials, expiry
- Cost accounting, valuation, COGS, profit, margin, P&L, journals
- Tax/VAT/fiscal invoicing
- Discounts and promotions
- Refunds, returns, exchanges, partial voids
- Split payments
- Gateway/card/QR integration and GCash verification
- Receipt/label printing and file export (CSV/PDF/Excel)
- Offline catalog, sales, inventory, expenses, and reports
- Operational POS roles and production authentication
- Phase 9 functionality

## Tests and Android

| Suite | Passed | Failed | Skipped |
|---:|---:|---:|---:|
| Full `ExItS.slnx` Release | **882** | **0** | **0** |

Baseline **851** preserved and exceeded (+31 focused closeout tests: capability matrix theories/facts, architecture guards, migration chain).

Android Release APK:

`src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/bin/Release/net10.0-android/com.exits.pinoybusinesspos-Signed.apk`

Interactive device/emulator validation **not** claimed (`adb` unavailable) — **R-109 remains open**.

## Risks and production limitations

| ID / topic | Status |
|---|---|
| R-109 interactive Android validation | **Open** — Release APK only; no interactive Basic Store E2E on device |
| Dev/Testing commercial and actor headers | **Open** — not production-secure |
| Production authentication / operational POS roles | **Open** |
| Online-only catalog/sales/inventory/expenses/reports | **By design** for Phase 8 — offline Basic Store deferred |
| Manual GCash not independently verified | **Open** (R-025 related) |
| Report export deferred | **Open** — no CSV/PDF/Excel generation |
| Category-label caveat | **Documented** — sales-by-category labels use current catalog assignment; money/qty snapshots remain immutable |
| Inventory scale / concurrency beyond MVP | **Open** — MVP-scale proven only |
| Tax, refund, accounting workflows | **Absent** — deferred |

Do **not** mark these resolved without evidence.

## HealthCare freeze

Root `HealthCare/` remains ignored, untracked, outside `ExItS.slnx`.

## Documentation and Git

Updated Phase 8, portfolio, README, FILE-MANIFEST, release-plan, risks, contracts, security, data-ownership, testing-strategy, phases index, reports index, this report.

| Field | Value |
|---|---|
| Feature commit | 0bc5ebb999c0708e6ac76b04a30d522037eec3cb |
| Docs hash-record commit | edf80ed370b000a0caaf1786ec4fde8208b1afb3 |
| Final working tree | clean after push |

## Exact next authorized phase / work package

**Phase 9 — MVP Hardening and Release** / **P9-WP01 — Security and Privacy Hardening** (do not begin until explicitly authorized).
