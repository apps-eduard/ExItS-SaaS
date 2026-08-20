# Sales, Returns, and Sales Documents

## CURRENT — Sales

| Topic | Status | Evidence |
|-------|--------|----------|
| `Sale` / `SaleLine` | PROVEN_CURRENT | Domain |
| Statuses | Completed, Voided, AwaitingPayment (and related) | PROVEN_CURRENT |
| Number generation | PROVEN_CURRENT | sequences |
| Payments | Cash, ManualGCash, Utang (+ payment attempts) | PROVEN_CURRENT |
| Snapshots | Price, UOM, selling unit, qty, multiplier, selling mode | PROVEN_CURRENT |
| Inventory deduction | Tracked only; base qty | PROVEN_CURRENT |
| Void | PROVEN_CURRENT | `POST .../void` |
| History/detail | PROVEN_CURRENT | GET sales |
| Offline cash + outbox | PROVEN_CURRENT | LocalStore |
| Receipt / Transaction Summary | PROVEN_CURRENT | Document kind TransactionSummary |

API: `/api/v1/pos/sales`

## CURRENT — Returns / refunds

| Topic | Status | Evidence |
|-------|--------|----------|
| Sale returns (incl. partial) | PROVEN_CURRENT | `SaleReturn*` |
| Restock disposition | PROVEN_CURRENT | return lines |
| Inventory restoration | PROVEN_CURRENT | movements |
| Utang effects | PROVEN_CURRENT where product-based/credit paths apply | |
| Reason / audit | PROVEN_CURRENT | return fields |
| Offline | OnlineRequired (fail-closed) | policy |

API: `/api/v1/pos/sale-returns`

## Compliance / sales document boundary (Phase 26)

| Rule | Status | Evidence |
|------|--------|----------|
| One Sale engine | PROVEN_CURRENT | Sale use cases |
| Current document = Transaction Summary | PROVEN_CURRENT | `SalesDocumentKind.TransactionSummary` |
| TaxDocument | Unavailable / not issuable | `TaxDocumentIssuanceNotAvailable`; `ImplementationAvailable = false` |
| Tax settings ≠ issuance authority | PROVEN_CURRENT | SalesDocumentPolicy |
| Compliance capability on organization | PROVEN_CURRENT | Platform org compliance profile |
| Ownership transfer preserves compliance profile | PROVEN_CURRENT | Phase 26 / ownership transfer |
| Historical sales not retroactively reinterpreted | PROVEN_CURRENT | snapshots + policy |
| BIR/NPC certified claims | PROVEN_MISSING | Explicit non-claim in historical docs; do not claim |

Tests: `SalesDocumentFoundationTests`, Phase26 wording guards.

## React

Sales POST, void, returns, receipts: **MISSING**. Cart shell only.
