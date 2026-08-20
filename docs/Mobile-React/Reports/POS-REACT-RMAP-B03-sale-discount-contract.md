# RMAP-B03 — Commercial sale discount / adjustment backend contract

## Status

**FINAL CLOSED** (backend contract + payment-boundary closeout)

Closeout report: [POS-REACT-RMAP-B03-final-closeout.md](./POS-REACT-RMAP-B03-final-closeout.md)

## Baseline

starting SHA: `27ed884df2b4862770f8da5f39d7849952f07b8e`  
branch: `feat/pos-react-client`  
implementation SHA: `431e51040539bb4fcaba03e935df4b46c60fed3a`

## Permanent financial distinctions

| Concept | Meaning | This package |
|---------|---------|--------------|
| Today's Price | Changes catalog selling price | Out of scope (RMAP-06) |
| Cashier price override | Per-sale price policy | Out of scope (RMAP-B01) |
| **Commercial discount** | Preserves UnitPrice; records separate adjustment | **IMPLEMENTED** |
| Promotion / coupon | Automatic rule engine | Out of scope |
| Regulatory discount | Compliance-validated (e.g. statutory) | Out of scope |

## Contract discovery

| Area | Finding |
|------|---------|
| Prior Sale money | `LineTotal = RoundMoney(UnitPrice × qty)`; `Subtotal = Σ LineTotal`; tax on Subtotal; `Total` exclusive/inclusive |
| Authority | Online checkout ignores client prices; server prices from catalog |
| Tax | `OperationalSetupTaxCalculator` on subtotal; safe when fed **post-discount net** |
| Returns | Refund from snapshotted `UnitPrice` / `LineTotal` → **LineTotal must remain net** |
| Offline | Snapshot fidelity requires `LineTotal == RoundMoney(UnitPrice × Qty)` without discount fields |
| Zero total | Cash / ManualGCash allow ₱0 Completed; Utang rejects; **Card/GCash reject** (`pos.sale.electronic.total_must_be_positive`) |
| Contradictions | None requiring HARD STOP after additive design + electronic zero-total fail-closed |
| Owner decision | Not required (matrix documented; no ₱0 provider payment invented) |

## Discount model

- **Line** commercial discount: Percentage or FixedAmount
- **Sale** commercial discount: Percentage or FixedAmount
- Reason required (non-whitespace)
- Source: **Manual** only
- Actor: `AppliedBy` on adjustment row (= checkout actor)
- UnitPrice **never** mutated by discount
- Inventory `Quantity` **never** mutated by discount

### Allocation (sale-level)

Largest-remainder, deterministic:

1. Eligible base per line = GrossLineTotal − LineDiscountAmount  
2. Truncate exact proportional shares toward zero at 2 dp  
3. Distribute leftover centavos one at a time to largest discarded fraction  
4. Tie-break: lower `LineNumber` first  
5. Never allocate more than eligible base  
6. Recorded `SaleDiscountTotal` = sum of allocations (exact reconciliation)

## Money snapshots

| Field | Meaning |
|-------|---------|
| GrossLineTotal | Authoritative UnitPrice × selling quantity (pre-discount) |
| LineDiscountAmount | Direct line commercial discount |
| SaleDiscountAllocatedAmount | Allocated share of sale-level discount |
| LineTotal | **Net** (used by returns) = Gross − line − allocation |
| GrossSubtotal | Σ GrossLineTotal |
| LineDiscountTotal / SaleDiscountTotal / DiscountTotal | Aggregates |
| Subtotal | **Net pre-tax** after commercial discounts (DTO-compatible name) |
| TaxAmount / Total | Existing semantics on net Subtotal |
| Legacy rows | Discount columns = 0; Gross\* backfilled from prior Subtotal/LineTotal |

## Authorization

Capability: `UtangCapability.ApplyCommercialDiscount`  
Feature: `store-sales-apply-commercial-discount`

| Principal | Discount present | No discount |
|-----------|------------------|-------------|
| Owner | ALLOW | ALLOW (CreateSale) |
| StoreManager | ALLOW | ALLOW |
| Cashier | **DENY** | ALLOW |
| OrgAdmin alone (no POS discount grant) | DENY | CreateSale still role-gated |
| Wrong org / no product access | DENY | DENY |

Checkout and quote require CreateSale; when discount intents are present they additionally require ApplyCommercialDiscount.

## API

| Endpoint | Behavior |
|----------|----------|
| `POST /api/v1/pos/sales/quote` | Non-persisting authoritative preview |
| `POST /api/v1/pos/sales` | Additive `Discounts` intent list; server recomputes all money |
| Client authoritative money | **NO** |

Offline checkout **with** discount intents: **fail closed** (`pos.sale.discount.offline_not_supported`).  
Legacy offline without discounts: unchanged.

## Migration

`AddPosCommercialSaleDiscounts` (`20260820214748`)

- Additive columns on `sales` / `sale_lines`
- Table `pos.sale_commercial_discount_adjustments`
- Backfill gross = prior net; discount = 0
- Check constraints for reconciliation

## Tests (this closeout)

| Suite | Result |
|-------|--------|
| `SaleCommercialDiscount*` + `SaleDomainTests` | 63 passed |
| UOM/weight conversion suites | 31 passed |
| `PosSaleCommercialDiscountApiTests` + migration | 9 passed |
| `PosSaleApiTests` | 16 passed |
| `PosSalesScopeArchitectureTests` | 8 passed |
| `SaleReturnDomainTests` + `OperationalSetupTaxCalculatorTests` | 8 passed |
| MAUI SaleCartService filter | no matching tests |
| MAUI Checkout UI string guards | 2 failed — **pre-existing**, unrelated to discount (markup/localization substrings) |

## Current payment product rule

| User-facing | Internal domain | Status |
|-------------|-----------------|--------|
| Cash | `Cash` | CURRENT |
| GCash | `ManualGCash` | CURRENT (manual confirmation) |
| Utang | `Utang` | CURRENT |
| — | `Card` | FUTURE provider infrastructure only |
| — | `GCash` (provider/API) | FUTURE provider infrastructure only |

Do not label ordinary checkout UX as “ManualGCash”. Do not expose Card / provider GCash as current React checkout choices.

## Zero-total / payment matrix (closed)

| Method | Total = ₱0 after commercial discount | Result |
|--------|--------------------------------------|--------|
| Cash | Allowed | Completed immediately; tendered/change 0 |
| GCash (ManualGCash) | Allowed | Completed immediately (no payment attempt) |
| Utang | Rejected | `pos.sale.utang.total_must_be_positive` |
| Card / provider GCash | Rejected | `pos.sale.electronic.total_must_be_positive` |

**Positive discounted Utang:** remaining Amount to Pay > ₱0 is **supported**. Linked credit amount must equal net `Sale.Total` (not GrossSubtotal). Proven in closeout API tests.

**Why Card/provider GCash reject:** electronic checkout creates `AwaitingPayment` + stock reservation, then payment attempts require `amount > 0`. A ₱0 Card/provider-GCash sale would reserve stock and never be payable — fail closed at checkout. This is defensive backend safety, not a current product feature claim.

## Explicit exclusions

- React discount UX (future **RMAP-11b**, after checkout)
- RMAP-08 lots/expiry
- RMAP-B04 buyer purchase projection (documented NOT STARTED)
- RMAP-TAX final controlled tax activation (documented NOT STARTED)
- Promotions, coupons, regulatory discounts
- Cashier configurable limits / approval workflow
- Card / provider GCash checkout UX

## Related future packages

- **RMAP-B04** — Linked ExItS buyer purchase projection — NOT STARTED  
- **RMAP-TAX** — Final controlled tax activation — NOT STARTED (after RMAP-23, before RMAP-24)  
- **RMAP-11b** — Commercial Discount UX — defined; not started (depends on RMAP-11 Checkout)

## Legal / compliance language

ExItS does not determine a merchant's legal BIR obligations. Transaction Summary is not a substitute for a legally required invoice. Platform approval/capability is product authorization, not BIR certification. Final tax + discount interaction validation belongs to RMAP-TAX with BIR/legal/accounting review. Buyer purchase projection requires privacy/retention review (RMAP-B04).

## Next

**HARD STOP.** Do not start RMAP-08, RMAP-11b, RMAP-B04, RMAP-TAX, or React discount UI.
