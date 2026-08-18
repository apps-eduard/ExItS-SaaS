# Pinoy Loan Manager — Disbursement and Payment Controls

**Status:** Planning / product-rule baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Office and field disbursement / payment workflows, receipts, failed collection attempts, and disbursement assignment safety.

Related: [loan-lifecycle-model.md](loan-lifecycle-model.md), [payment-and-allocation-model.md](payment-and-allocation-model.md), [cashier-and-collector-control-model.md](cashier-and-collector-control-model.md), [disbursement-readiness-model.md](disbursement-readiness-model.md), [../Security/role-and-grant-baseline.md](../Security/role-and-grant-baseline.md).

---

## Approval vs disbursement

Approval and disbursement remain **separate**. Approval alone never proves cash was released. Preferred Platform usage-billing event remains **LOAN DISBURSED**, not Loan Approved.

```text
Approved → Awaiting Disbursement → Disbursed → Active
```

---

## Office disbursement flow

```text
Loan = Approved
        ↓
Loan = Awaiting Disbursement
        ↓
Borrower / customer verified
        ↓
Authorized Cashier
        ↓
Cashier Session active
        ↓
Sufficient accountable cash
        ↓
Confirm exact snapshotted Loan terms
        ↓
Release Net Proceeds
        ↓
Record Disbursement
        ↓
Loan becomes operationally Disbursed / Active
        ↓
Cashier accountable cash decreases
```

No silent editing of Loan amount during disbursement. If amount / terms are wrong, do **not** “fix” them during cash release. Use the authorized loan correction / cancellation process.

Exact disbursement cancellation / reversal workflow remains **OPEN**.

---

## Field / collector disbursement flow

```text
Approved Loan
  → Awaiting Disbursement
  → Assigned to authorized Collector
  → Collector sees field-disbursement task
  → Collector verifies borrower
  → Collector confirms exact Net Proceeds
  → Collector has sufficient accountable cash
  → Collector releases money
  → Disbursement recorded
  → Collector accountable cash decreases
  → Loan becomes Disbursed / Active
```

Collector **cannot** change approved amount, rate, terms, net proceeds, or schedule policy. If anything disagrees: Collector must **stop and escalate**.

---

## Disbursement assignment safety

Field disbursement assignment should include enough authoritative context:

- Loan identity
- borrower
- branch
- approved amount
- exact net proceeds
- assignment
- expiration / validity if applicable
- status
- authorized disbursement channel

Collector may execute it. Collector may **not** edit its financial terms.

---

## Failed / not-completed disbursement

If the Loan is approved but money is **not** actually released, do **not** mark it Disbursed.

Examples: borrower does not come to office; collector cannot locate borrower; customer changes mind; insufficient authorized cash; verification fails.

Loan remains in an appropriate **pre-disbursement** state. No Platform usage-charge event should be generated merely because approval existed.

---

## Office payment flow

```text
Borrower pays Cashier
        ↓
Cashier identifies correct Loan
        ↓
System shows valid collectible amount
        ↓
Cash received
        ↓
Payment command posted
        ↓
Loan Subledger updated
        +
Cashier Cash Accountability updated
        ↓
Receipt / reference produced
```

Both sides must correlate to the same business transaction. Do **not** record cash without loan posting, or loan posting without accountable cash, when the payment method is **cash**.

Partial payments remain supported. Allocation follows snapshotted Loan policy. See [payment-and-allocation-model.md](payment-and-allocation-model.md).

---

## Field collection flow

```text
Collector receives assigned work
        ↓
Visits borrower
        ↓
System shows current due, past due, collectible amount, relevant Loan summary
        ↓
Customer pays
        ↓
Collector enters actual amount received
        ↓
Payment posted
        ↓
Loan Subledger updated
        +
Collector accountable cash increases
        ↓
Receipt / reference recorded
```

Collector must **not** manually decide hidden allocation outside the Loan policy.

---

## Payment receipt principle

Every posted customer payment should eventually receive a unique durable reference / receipt identity.

Receipt should be **reproducible from authoritative posted data**. Do not make receipt existence depend only on a successfully printed paper receipt.

Future channels may include printed receipt, Personal receipt / history, SMS / email notification, digital receipt. Receipt numbering / format remains **OPEN**. No implementation in this package.

---

## Failed collection attempt

If the customer does not pay, Collector should record a **Collection Attempt**, not fabricate a Payment.

Possible factual outcomes: customer unavailable; insufficient funds; refused payment; business / home closed; collection prevented by weather / service issue; other.

Collector records **facts**. Penalty / exception policy determines financial consequence separately. See [penalty-exception-and-waiver-model.md](penalty-exception-and-waiver-model.md), [exception-reversal-and-variance-workflow.md](exception-reversal-and-variance-workflow.md).

---

## Idempotency

Reaffirm WP04. Especially protect: Disbursement, Payment, Float Transfer, Remittance, Penalty Waiver, Reversal.

Example: Collector presses Record Payment twice because of slow network. Future system must prevent duplicate authoritative financial posting. Implementation is **not** designed here.

---

## Explicit non-goals

- Silent term edits at cash release
- Marking Disbursed without cash release
- Customer wallet for true overpayment (MVP)
- Receipt format / numbering
- Offline authoritative posting

## Legal / compliance boundary

No disbursement, payment, or collection workflow in this document is claimed legally compliant. External qualified legal/compliance review remains required before Production (PLM-D-00-11). This package does not invent Philippine regulations.
