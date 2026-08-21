# RMAP-12 — Payments expansion + void

## Status

**COMPLETE**

## Baseline

starting SHA: `47af61a3` (Master Run 02 tip after RMAP-11b; verified clean)

## Contract

| Area | Finding |
|------|---------|
| Payment UI | Cash · **GCash** · Utang — never Card, never provider GCash |
| GCash API | `paymentMethod: "ManualGCash"`; UI label **GCash**; `gCashReference` required when Amount to Pay > 0 (max 64); no `amountTendered` |
| Zero total | Cash / ManualGCash allowed; UI “No payment required”; GCash reference **not** forced |
| Utang | `customerId` required; optional `dueDate` (`YYYY-MM-DD`); no tender; reject when Amount to Pay ≤ 0; debt = net Amount to Pay; CreateSale + CreateCredit |
| Void | `POST /api/v1/pos/sales/{saleId}/void` `{ reason }` — Owner/Admin/Manager (`VoidSale`); Utang also needs `ReverseCredit`; Cashier cannot void |
| Customers | `GET /api/v1/pos/customers?status=Active&search=` requires `ViewCustomersAndHistory` |

## Cashier customer gap (pre-existing — not changed)

`PosRoleMatrix` grants Cashier **CreateCredit** but **not** `ViewCustomersAndHistory`.

React behavior (no matrix mutation / no capability bypass):

- Customer search only when `canViewCustomers`
- Cashier selecting Utang sees a clear message that looking up customers requires Manager/Owner permission; confirm stays disabled
- Owner / Manager Utang works end-to-end with Active customer picker

## Implementation

- `pos-sales-client.ts` — Cash / ManualGCash / Utang checkout payload; `voidSale`; `formatPaymentMethodLabel`
- `pos-customers-client.ts` — minimal Active list/search
- `pos-capabilities.ts` — `canVoidSale`, `canViewCustomers`, `canCreateCredit`
- `CheckoutCashPage` — payment selector + method panels (file name retained for route stability)
- `TransactionSummaryPage` — void panel for authorized roles; GCash label (not ManualGCash)
- i18n en + fil-PH
- Vitest: capabilities, sales/customers clients, error mapping
- Playwright `e2e/rmap-12-payments-void.spec.ts`

## Exclusions

- Card UI / provider GCash
- Price Override (RMAP-12b / B01)
- RMAP-13 full customer CRUD / credit ledger / repayments
- Offline Utang / ManualGCash
- PosRoleMatrix mutation

## Implementation SHA

`7dcd3ab5` (feat)

## Validation

### React gates

| Gate | Result |
|------|--------|
| typecheck | PASS |
| lint | PASS (0 errors; existing react-refresh warnings only) |
| format:check | PASS |
| Vitest | 44 files / **193** tests passed |
| build | PASS |
| Playwright `rmap-11` | **9** passed |
| Playwright `rmap-11b` | **9** passed |
| Playwright `rmap-12` | **11** passed |

Responsive matrix (payment selector):

| Viewport | Result |
|----------|--------|
| 375×812 | PASS (e2e) |
| 768×1024 | PASS (e2e) |
| 1024×768 | PASS (e2e) |
| 1440×900 | PASS (e2e) |

### Proven behaviors

- Cash regression tender POST
- GCash UI → `ManualGCash` + reference; summary shows **GCash**
- No Card / provider GCash controls
- Owner Utang with customer; zero Utang blocked
- Discounted Utang payload (client test)
- Owner void; Cashier void denied (UI)
- Cashier Utang customer lookup message (matrix gap documented)

### Flags

- `RMAP_12_PASS=YES`
- `RMAP_12_CASH=YES`
- `RMAP_12_MANUAL_GCASH=YES`
- `RMAP_12_UTANG=YES`
- `RMAP_12_VOID=YES`
- `RMAP_12_NO_CARD=YES`
- `RMAP_12_NO_PROVIDER_GCASH=YES`
- `RMAP_12_CASHIER_UTANG_GAP=DOCUMENTED`
- `RMAP_12_NO_MATRIX_MUTATION=YES`
- `HARD_STOP=NO`

## Next

**RMAP-13 — Customers + Business Utang** when authorized. Do not start without authorization.
