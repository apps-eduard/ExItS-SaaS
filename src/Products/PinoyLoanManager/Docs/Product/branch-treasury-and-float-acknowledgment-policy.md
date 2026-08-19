# Pinoy Loan Manager — Branch Treasury and Float Acknowledgment Policy

**Status:** Accepted product policy (PLM-DOC-09); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Branch-level cash custody and collector float two-step acknowledgment. Complements [cashier-and-collector-control-model.md](cashier-and-collector-control-model.md), [../Architecture/operational-subledger-and-accounting-boundary.md](../Architecture/operational-subledger-and-accounting-boundary.md), and [daily-operational-workflow.md](daily-operational-workflow.md).

Loan subledger and cash accountability remain separate ledgers.

---

## Branch Treasury concept

**Branch Treasury** is the branch-level physical cash custody concept that funds branch cash operations. It is **not** the Loan subledger and **not** a complete general ledger cash account.

Branch Treasury answers: *what branch cash pool is available to fund authorized cashier and field cash activity?*

Conceptual responsibilities:

- holds branch opening / replenishment cash declared by authorized staff
- funds Cashier Session opening balances
- receives returned cash from closed Cashier Sessions and collector remittances according to workflow
- remains visible in branch cash oversight reporting
- preserves auditable cash movement history

Branch Treasury does **not**:

- change borrower Loan balances
- replace external accounting / bank reconciliation (PLM-D-00-07 remainder)
- silently absorb unresolved variances without authorized review

Treasury amounts and movements use the same money precision rules as other PLM cash records ([money-precision-and-rounding-policy.md](money-precision-and-rounding-policy.md)).

---

## Cashier Session funding from treasury

Each **Cashier Session** represents an accountable cashier working period at a branch.

Opening flow:

```text
Branch Treasury (authorized balance)
        ↓
Authorized Cashier opens Cashier Session
        ↓
Opening Cash amount confirmed / drawn from Branch Treasury
        ↓
Cashier Session → Active
        ↓
Authorized cash operations (office payment, office disbursement, float issue, remittance receive)
```

Rules:

1. Opening Cash for a Cashier Session must be an explicit, auditable movement from Branch Treasury (or documented equivalent branch funding event approved by policy).
2. Opening Cash must **not** be silently edited after financial activity begins; corrections use auditable adjustment workflows ([cash-variance-and-session-close-policy.md](cash-variance-and-session-close-policy.md)).
3. Closing a Cashier Session returns accountable cash to Branch Treasury (or records variance) according to close policy.
4. Cashier Session expected cash derives from recorded movements, not manual balance overrides.

---

## Collector float — two-step acknowledgment

Collector float moves physical cash accountability from Cashier Session to Collector. It does **not** post Loan ledger entries.

### States

| State | Meaning |
|---|---|
| **Issued / Pending Receipt** | Cashier has recorded float handed over; collector accountability **not** yet increased |
| **Received / Active** | Collector has acknowledged receipt; collector accountability increased |
| **Cancelled / Expired** | Issuance cancelled or expired before receipt per policy; no collector increase |

### Workflow

```text
Active Cashier Session
        ↓
Cashier issues Float to assigned Collector
        ↓
Float record = Pending Receipt
        ↓
Collector confirms receipt (MAUI or authorized Web)
        ↓
Float record = Received / Active
        ↓
Cashier accountability decreases; Collector accountability increases
        (same correlated business event)
```

### Policy rules

1. **Two-step required** — float must not increase collector expected cash on Cashier action alone.
2. **Pending Receipt visibility** — Cashier, Manager, and affected Collector can see outstanding pending floats.
3. **Additional float** — each top-up is a **new** movement with its own Pending Receipt cycle; do not rewrite Opening Float retroactively.
4. **Denial / timeout** — if collector rejects or does not acknowledge within organization policy, issuance remains Pending Receipt or is cancelled through authorized workflow; cash accountability must remain explainable.
5. **No Loan impact** — float issue/receipt never changes Principal, Interest, Fees, or Penalties.
6. **Grants** — Cashier issues (`plm.collector-floats.issue`); Collector receives (`plm.collector-floats.receive`); oversight views per [../authorization-matrix.md](../authorization-matrix.md).

Partial remittance and end-of-day remittance flows remain as documented in [cashier-and-collector-control-model.md](cashier-and-collector-control-model.md).

---

## Relationship to collector cash formula

After float is **Received / Active**, collector expected cash follows:

```text
Opening Float (received)
+ Additional Float (each received)
+ Collections Received
− Loan Disbursements
− Partial Remittances
= Expected Collector Cash
```

Pending Receipt floats are **excluded** from Expected Collector Cash until acknowledged.

---

## Explicit non-goals

- Full vault / bank GL integration (PLM-D-00-07 remainder)
- Single-step float transfer without collector acknowledgment
- Netting float against collections without separate movement records
- Forcing zero variance at close by inventing treasury adjustments
- Legal / regulatory cash-handling compliance claims (PLM-D-00-11)
