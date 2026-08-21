# RMAP-13 — Customers + Business Utang

## Status

**COMPLETE**

## Baseline

starting SHA: `17569653` (feat/pos-react-client tip after RMAP-12; verified clean)

## Contract

| Area | Finding |
|------|---------|
| Customers | List/search/create/edit/deactivate/reactivate via `/api/v1/pos/customers` |
| Business Utang | Credit summary, credit entries, repayments, ledger, statement |
| Wording | **Amount owed**, **Payment**, **Remaining balance** (not engineering jargon) |
| Checkout | Cash/GCash optional customer (`ViewCustomersAndHistory`); Utang requires customer — Cashier uses narrow checkout-search (Review Repair 01) |
| Discounted Utang | Credit amount displayed from server equals net **Amount to Pay** |
| Link status | Read-only ExItS Personal link chip when `linkedPersonalPublicUserId` present |
| Capabilities | `canCreateCustomer`, `canEditCustomer`, `canRecordRepayment`, `canViewStatement` (+ existing `canViewCustomers`) |

## Implementation

- Expanded `pos-customers-client.ts` — get/create/update/deactivate/reactivate, credit summary, credits, repayments, ledger, statement
- Routes under AppShell with guards: `/customers`, `/customers/new`, `/customers/:id`, `/customers/:id/edit`, `/customers/:id/repay`, `/customers/:id/statement`
- Checkout optional customer panel for Cash/GCash when `canViewCustomers`
- Role home Customers link when `canViewCustomers`
- i18n en + fil-PH
- Vitest: capabilities + customers client (incl. discounted credit = net Amount to Pay)
- Playwright `e2e/rmap-13-customers-utang.spec.ts`

## Exclusions

- RMAP-B04 linked ExItS buyer purchase projection — **NOT STARTED** (`RMAP_B04_STARTED=NO`)
- Personal Utang ledger UI
- PosRoleMatrix mutation
- Card / provider GCash
- Offline customer/credit queue
- Manual credit create outside sale (sale Utang path remains RMAP-12)

## Implementation SHA

`adf634ee` (feat)

## Validation

### React gates

| Gate | Result |
|------|--------|
| typecheck | PASS |
| lint | PASS (0 errors; existing react-refresh warnings only) |
| format:check | PASS |
| Vitest | 44 files / **198** tests passed |
| build | PASS |
| Playwright `rmap-12` | **11** passed |
| Playwright `rmap-13` | **9** passed |

Responsive matrix (customers list):

| Viewport | Result |
|----------|--------|
| 375×812 | PASS (e2e) |
| 768×1024 | PASS (e2e) |
| 1024×768 | PASS (e2e) |
| 1440×900 | PASS (e2e) |

### Proven behaviors

- Owner customer list/search/detail with Amount owed
- Owner create customer
- Owner record payment + Remaining balance preview
- Owner statement view
- Cashier denied `/customers` (ViewCustomers)
- Discounted Utang credit amount = net Amount to Pay (client + e2e display from server)

### Flags

- `RMAP_13_PASS=YES`
- `RMAP_13_CUSTOMERS=YES`
- `RMAP_13_UTANG=YES`
- `RMAP_13_REPAYMENT=YES`
- `RMAP_13_STATEMENT=YES`
- `RMAP_13_CHECKOUT_OPTIONAL_CUSTOMER=YES`
- `RMAP_13_DISCOUNTED_CREDIT_EQUALS_NET=YES`
- `RMAP_B04_STARTED=NO`
- `RMAP_13_NO_PERSONAL_UTANG=YES`
- `RMAP_13_NO_MATRIX_MUTATION=YES`
- `HARD_STOP=NO`

## Next

**RMAP-14 — Returns / refunds** when authorized. Do not start RMAP-B04 without authorization.
