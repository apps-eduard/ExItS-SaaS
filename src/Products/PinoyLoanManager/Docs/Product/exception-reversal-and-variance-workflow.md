# Pinoy Loan Manager — Exception, Reversal, and Variance Workflow

**Status:** Planning / product-rule baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Collection exceptions, penalty waivers, payment reversals vs cash refunds, remittance-after-correction, and cash variance.

Related: [penalty-exception-and-waiver-model.md](penalty-exception-and-waiver-model.md), [penalty-assessment-and-cap-policy.md](penalty-assessment-and-cap-policy.md), [schedule-and-collection-calendar-policy.md](schedule-and-collection-calendar-policy.md), [payment-and-allocation-model.md](payment-and-allocation-model.md), [cashier-and-collector-control-model.md](cashier-and-collector-control-model.md), [reversal-refund-and-correction-policy.md](reversal-refund-and-correction-policy.md), [cash-variance-and-session-close-policy.md](cash-variance-and-session-close-policy.md), [../Security/role-and-grant-baseline.md](../Security/role-and-grant-baseline.md).

---

## Exception request flow

For a collector-reported event requiring approval:

```text
Collector records event
        ↓
Exception Request / Pending Review
        ↓
Manager or Owner with grant reviews
        ↓
Approve / Reject
        ↓
Policy consequence applied
```

Collector must **not** convert their own unsupported reason directly into an approved waiver / exception.

---

## Organization-wide collection exception

Owner / Manager with required grant may declare an organization / branch / area collection exception (example: severe flooding).

Record: date / time window; affected branch / area; affected collectors / customers according to scope; reason; approver; penalty treatment; schedule treatment according to applicable policy; audit history.

Do **not** delete original due dates / history. Schedule treatment uses the Loan’s snapshotted exception policy: [schedule-and-collection-calendar-policy.md](schedule-and-collection-calendar-policy.md).

---

## Penalty waiver flow

Penalty Waiver is a high-risk financial action.

```text
Existing Penalty
        ↓
Waiver requested or initiated
        ↓
Reason required
        ↓
Authorized Manager / Owner reviews
        ↓
Approve / Reject
        ↓
If approved: Waiver financial event posted
```

Original Penalty remains historically visible.

Collector **cannot** approve their own waiver request. Cashier does **not** receive default waiver-approval authority.

---

## Payment reversal

Payment reversal is **not** deleting a payment.

```text
Original Payment
        ↓
Correction / Reversal Request
        ↓
Reason required
        ↓
Authorized review if required
        ↓
Reversal financial event
        ↓
Loan balance recalculated from events
```

Original payment remains visible. If corrected, a **separate** correct payment is posted.

Exact payment reversal approval threshold remains **OPEN** for grant identifiers (PLM-D-00-06). Policy: [reversal-refund-and-correction-policy.md](reversal-refund-and-correction-policy.md).

---

## Loan reversal ≠ physical cash refund

This distinction is **mandatory**.

Illustrative: a PHP 100 Loan payment was posted incorrectly.

- Reversing the Loan payment changes **borrower financial history**.
- It does **not** automatically prove that PHP 100 physical cash was returned to the customer.

If physical cash is refunded / returned: record a separate authorized **CASH MOVEMENT**.

```text
Loan Payment Reversal  ≠  Cash Refund / Cash Return
```

They may be correlated but must remain distinguishable. Canonical cash refund workflow: [reversal-refund-and-correction-policy.md](reversal-refund-and-correction-policy.md).

---

## Reversal after remittance

Do **not** rewrite prior collector remittance history.

Example: Collector collected PHP 100, remitted it to Cashier, then the payment is found wrong.

Do **not** erase:

- Customer → Collector
- Collector → Cashier

Instead: financial correction / reversal and any physical refund / reimbursement must be represented through **new auditable events**. This is critical for reconciliation.

---

## Cash variance

```text
Variance = Actual Counted Cash − Expected Cash
```

Possible result: Zero, Overage, Shortage.

Do **not** silently modify expected balance to make variance zero.

Record: expected, actual, amount of variance, direction, actor, time, notes / reason, related Collector / Cashier session.

---

## Variance resolution

Cash variance is a controlled exception.

- Collector **cannot** resolve their own variance.
- Cashier may record / count the variance but should **not** receive default authority to silently erase it.
- Manager / Owner with appropriate grant handles resolution.

Possible future resolution categories (not accounting entries): counting error corrected; missed cash movement identified; approved shortage; approved overage; employee repayment; cash adjustment; investigation pending; other documented reason.

Do **not** define accounting entries yet.

---

## Do not force hidden variance

The system should be able to preserve a **real unresolved variance**.

Do **not** force users to invent a fake cash movement simply so a day can close at zero.

Possible operational state:

- Reconciliation Complete
- Variance Pending Resolution

This is preferable to corrupting financial history. Canonical close-with-variance: [cash-variance-and-session-close-policy.md](cash-variance-and-session-close-policy.md).

---

## Idempotency

Protect Penalty Waiver and Reversal (and related cash movements) against duplicate posting. See [payment-and-allocation-model.md](payment-and-allocation-model.md).

---

## Legal / compliance

No exception, waiver, reversal, or variance workflow in this document is claimed legally compliant. External qualified legal/compliance review remains required before Production (PLM-D-00-11). This package does not invent Philippine regulations.

---

## Explicit non-goals

- Silent expected-cash edits
- Erasing remittance history after a later correction
- Equating Loan reversal with cash refund
- Collector self-approval of waiver / variance
- GL journal design
