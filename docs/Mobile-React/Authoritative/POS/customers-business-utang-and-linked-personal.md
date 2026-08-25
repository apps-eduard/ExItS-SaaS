# Customers, Business Utang, and Linked Personal

## CURRENT — POS customers / Business Utang

| Topic | Status | Evidence |
|-------|--------|----------|
| POS customer aggregate | PROVEN_CURRENT | `customers` table + domain |
| Credit entries / repayments / due dates / overdue | PROVEN_CURRENT | credit APIs |
| Product-Based Utang | PROVEN_CURRENT | sale utang remarks / product utang |
| Statements / receipts | PROVEN_CURRENT | statement endpoints |
| Offline customer/credit | PROVEN_CURRENT | Queueable create/edit/credit/repay; encrypted local store |
| Staff ≠ customer | PROVEN_CURRENT | separate models |

API: `/api/v1/pos/customers` (+ credit/repayment/due-date/overdue/statements)

## CURRENT — Linked Personal projection

| Topic | Status | Evidence |
|-------|--------|----------|
| Customer link to Personal identity | PROVEN_CURRENT | Platform + POS correlation |
| Personal read of Business Utang | PROVEN_CURRENT | `/api/v1/pos/personal/linked-customers/...` |
| Not a copy into Personal Utang ledger | PROVEN_CURRENT | boundaries architecture doc |
| Link acceptance ≠ staff membership | PROVEN_CURRENT | |

## Personal Utang (Platform)

Separate Personal ledger under `/api/v1/personal/utang/*`. Status: **PROVEN_CURRENT** (Platform + MAUI). React: **MISSING**.

## OWNER-CONFIRMED

Personal ledger ≠ Organization Business Credit ledger. Preserve.

## React

Customers / Business Utang / statements: **CURRENT** (RMAP-13). Linked Personal purchase projection: **MISSING** (RMAP-B04 NOT STARTED). Personal Utang React: **MISSING**.
