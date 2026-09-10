# POS-CUSTOMER-CREDIT-POLICY-APPROVAL-AUDIT-01

## TASK
POS-CUSTOMER-CREDIT-POLICY-APPROVAL-AUDIT-01 — Auditable Customer Credit / Utang Policy

## START_SHA
`18cfb89583ba2e538113498c6a2f7091cd9ff855`

## FINAL_SHA
_(recorded at commit)_

## BRANCH
`feat/organization`

## STATUS
PASS (targeted unit + React tests; Integration suites require migrated Testcontainers DB)

## Architecture

| Concern | Authority |
|---|---|
| POSCustomer | Profile / lifecycle only — **no** credit-policy fields |
| CustomerCreditPolicy | Org-owned current authorization (limit, term, status) |
| CustomerCreditPolicyChange | Append-only audit |
| CreditEntry / Repayment / WriteOff | Debt ledger — outstanding **derived**, never duplicated on policy |

### State transitions
`NotConfigured` (no row) → configure → `PendingApproval` → approve → `Approved` → disable → `Disabled` → reconfigure → `PendingApproval`.

Changing limit or term on `Approved` returns to `PendingApproval` and requires re-approval.

### Permissions
- `ManageCustomerCreditPolicy` / `ApproveCustomerCreditPolicy` — Owner, Admin, StoreManager
- Cashier: CreateCredit / CreateSale only when policy **Approved** and within limit — **cannot** manage/approve
- Same actor may configure and approve (two explicit audited actions; **no** four-eyes enforcement)

### Credit limit
`ProjectedOutstanding = Outstanding + NewCreditAmount ≤ CreditLimit` when `Status == Approved`.

### Default term
`DefaultTermDays` (1..365) applies to **new** Utang only via `saleDate.AddDays(term)`. Historical due dates unchanged.

### Concurrency
- Policy update/approve: optimistic on `UpdatedAtUtc` (+ `xmin`)
- NEW credit: `pg_advisory_xact_lock` keyed by org+customer inside ambient sale/create-credit transaction

### Checkout
Final `CheckoutSale` reloads policy + outstanding under lock. Cash/GCash unaffected.

## Explicit flags

| Flag | Value |
|---|---|
| CREDIT_POLICY_IS_LEDGER | NO |
| OUTSTANDING_DUPLICATED_IN_POLICY | NO |
| NEW_UTANG_REQUIRES_APPROVED_POLICY | YES |
| CREDIT_LIMIT_SERVER_ENFORCED | YES |
| FINAL_CHECKOUT_REVALIDATES | YES |
| POLICY_CHANGES_AUDITED | YES |
| APPROVAL_ACTOR_AUDITED | YES |
| CONFIGURATION_ACTOR_AUDITED | YES |
| HISTORY_APPEND_ONLY | YES |
| SAME_ACTOR_CONFIGURE_AND_APPROVE_ALLOWED | YES |
| FOUR_EYES_ENFORCED | NO |
| TERM_APPLIES_RETROACTIVELY | NO |
| CASHIER_CAN_APPROVE | NO |
| CROSS_ORG_FAIL_CLOSED | YES |
| CONCURRENT_LIMIT_OVERRUN_PROTECTED | YES |

## API
- `GET/PUT /api/v1/pos/customers/{id}/credit-policy`
- `POST .../credit-policy/approve`
- `POST .../credit-policy/disable`
- `GET .../credit-policy/history`

## Migration
`20260910170000_AddCustomerCreditPolicies` — tables `customer_credit_policies`, `customer_credit_policy_changes`

## UI
- Customer detail: CreditPolicySection (configure / approve / disable / history)
- Checkout Utang: compact eligibility panel; confirm blocked client-side when not approved / over limit

## Known residuals
- Broader OverdueQueryService large-page performance (pre-existing; not rewritten)
- Full 40-item concurrency matrix not all automated; core authorize + domain + use-case coverage added
- Integration Testcontainers must apply new migration before PosProductBasedUtangApiTests green on fresh CI
- Preserved prior WIP: Utang people idle browse / filter chip UX (included in this branch tip)

## Related WIP preserved
Utang checkout people list SQL filter + idle browse (CustomerUseCases / POSCustomerRepository / CheckoutCashPage) kept and extended with credit panel.
