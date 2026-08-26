# Payment Baseline

**Status:** Planning baseline (PSP-00)  
**Implementation present:** No  
**Related decisions:** PSP-D-00-06, PSP-D-00-07, PSP-D-00-19, PSP-D-00-15, PSP-D-00-16

## Money boundary

| Flow | Owner |
|---|---|
| Organization → ExItS (SaaS subscription) | Platform |
| Customer → Service Organization (service charges, labor, parts, deposits, payments, refunds) | PinoyServicePro |

Operational money must **never** become Platform `SaaSPayment*` records.

## Likely payment categories (no providers assumed)

| Category | Notes |
|---|---|
| Cash | Common |
| Manual electronic payment reference | Recorded reference only |
| Future integrated electronic payment provider | Not authorized in PSP-00 |
| Split payment | Decision required (PSP-D-00-07) |
| Deposit | Decision required (PSP-D-00-06) |
| Refund / reversal | Decision required (PSP-D-00-19) |

## Rules of honesty

- Decimal monetary concepts
- Do not invent tax/legal/accounting compliance
- Do not copy PinoyBusinessPOS payment domain directly
- Exact accounting/ledger design not authorized yet
- Documents/receipts: operational receipt intent; not tax invoice by default (PSP-D-00-15)

## Safe defaults

- Single tender per completion until split policy exists
- Deposits off until policy exists
- Refunds off until policy exists; no silent deletes of financial events
