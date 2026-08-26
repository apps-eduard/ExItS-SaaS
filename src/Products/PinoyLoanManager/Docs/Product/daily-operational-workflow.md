# Pinoy Loan Manager — Daily Operational Workflow

**Status:** Planning / product-rule baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Common operating day for Owner / Manager, Cashier, and Collector. Not a UI or scheduling specification.

Related: [cashier-and-collector-control-model.md](cashier-and-collector-control-model.md), [disbursement-and-payment-controls.md](disbursement-and-payment-controls.md), [exception-reversal-and-variance-workflow.md](exception-reversal-and-variance-workflow.md), [cash-variance-and-session-close-policy.md](cash-variance-and-session-close-policy.md), [schedule-and-collection-calendar-policy.md](schedule-and-collection-calendar-policy.md), [../Security/role-and-grant-baseline.md](../Security/role-and-grant-baseline.md).

---

## Assignment model

Collector work should be assignable. Possible dimensions:

- branch
- customer / borrower
- loan
- collection route
- approved disbursement task
- date / day

Collector visibility should derive from authorized assignment / scope. Do **not** let every Collector browse every organization’s borrower financial information by default.

Route planning and optional event-based GPS are resolved in [collector-route-and-location-policy.md](collector-route-and-location-policy.md) (PLM-DOC-09): assignment-based routes, no auto optimization, optional event GPS with policy/permission/disclosure, no continuous tracking.

---

## Morning

**Manager / Owner**

- reviews operational dashboard
- confirms collector assignments
- reviews approved loans awaiting release

**Cashier**

- opens Cashier Session
- confirms opening cash
- issues collector float

**Collector**

- receives assignments
- receives / acknowledges float (Pending Receipt → Received; see [branch-treasury-and-float-acknowledgment-policy.md](branch-treasury-and-float-acknowledgment-policy.md))
- begins route

Float acknowledgment uses two-step **Pending Receipt** workflow. Canonical: [branch-treasury-and-float-acknowledgment-policy.md](branch-treasury-and-float-acknowledgment-policy.md).

---

## During the day

**Collector**

- collects payments
- records failed attempts
- requests exceptions
- releases authorized approved loans (field disbursement)
- may request additional float
- may partially remit cash

**Cashier**

- handles office payments
- handles office disbursement
- issues additional float
- receives partial remittance

**Manager**

- reviews approvals
- reviews operational exceptions
- supervises collections

---

## End of day

**Collector**

- stops route
- submits cash / remittance
- cashier counts
- collector reconciliation performed

**Cashier**

- receives remittances
- reconciles own session
- declares cash
- closes session according to policy

**Manager / Owner**

- reviews exceptions / variances requiring authorization

Cashier Session may close as Closed Balanced when variance is zero, or Closed With Variance after authorized review when variance is nonzero. Unresolved variance remains **visible**. Canonical: [cash-variance-and-session-close-policy.md](cash-variance-and-session-close-policy.md).

---

## Surfaces

Organization Web is the full operational application. MAUI Hybrid is the limited field / collector surface. Platform Admin is **not** the loan operations console. Personal is borrower presentation only. See [../Architecture/application-surface-model.md](../Architecture/application-surface-model.md).

---

## Offline mobile boundary

MAUI Collector offline capability: MVP allows read-only cache and offline drafts only; offline **final posting** is deferred. Do **not** treat offline activity as immediately authoritative.

**Server remains authoritative** for final financial authorization / posting.

Future offline posting must explicitly address queued operations, idempotency, duplicate protection, stale balances, revoked authorization, cash accountability, conflicts, receipt status, and reconciliation. See [../Architecture/mobile-and-offline-operating-model.md](../Architecture/mobile-and-offline-operating-model.md).

No offline posting implementation is authorized yet.

---

## Explicit non-goals

- Route / GPS product requirements beyond [collector-route-and-location-policy.md](collector-route-and-location-policy.md)
- Forcing a day to close at zero by inventing fake cash movements
- Offline-first financial posting

## Legal / compliance boundary

No operational workflow in this document is claimed legally compliant. External qualified legal/compliance review remains required before Production (PLM-D-00-11). This package does not invent Philippine regulations.
