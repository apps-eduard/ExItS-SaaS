# Pinoy Loan Manager — Platform Usage Metering Contract

**Status:** Accepted product contract requirements (PLM-DOC-10); event transport **not** selected
**Implementation present:** No
**Last updated:** 2026-08-19

How Pinoy Loan Manager reports **billable usage** to ExItS Platform. Defines event types, ownership, idempotency, and privacy — not message bus, queue, or Platform billing schema.

**D-P12-03 remains Open** for commercial-state **inbound** transport and for the outbound delivery mechanism. This document closes PLM **product behavior** for what must be metered and what must never be sent.

Related: [platform-commercial-integration.md](platform-commercial-integration.md), [../Product/fees-and-net-proceeds-policy.md](../Product/fees-and-net-proceeds-policy.md), [../Product/loan-lifecycle-model.md](../Product/loan-lifecycle-model.md), [../Decisions/ADR-020-usage-metering-and-tenant-placement-contracts.md](../Decisions/ADR-020-usage-metering-and-tenant-placement-contracts.md).

---

## Ownership

| Owner | Owns |
|---|---|
| ExItS Platform | Subscription, entitlement, SaaS billing, usage rating, invoices, Platform audit of billing |
| Pinoy Loan Manager | Loan operational truth, disbursement facts, when a billable disbursement occurred |
| Borrower | Not a party to Platform usage charges |

Platform usage charge is **Organization → ExItS Platform**. It is **not** a borrower fee and must not enter the Loan subledger ([fees-and-net-proceeds-policy.md](../Product/fees-and-net-proceeds-policy.md)).

PLM must **not** write directly to Platform billing tables. PLM emits **approved usage events** only.

---

## Primary billable event

| Event type | When emitted | Billable meaning |
|---|---|---|
| `LOAN_DISBURSED` | Loan disbursement is **posted** as released in PLM | Preferred usage increment for Platform billing |

**Loan Approved** is **not** the billable event. Approval ≠ disbursement.

Metering tracks **organizational** product usage, keyed to Organization + Product + correlation identifiers — not borrower PII.

---

## Additional event types

| Event type | When emitted | Purpose |
|---|---|---|
| `LOAN_DISBURSEMENT_REVERSED` | Disbursement reversal posted after prior billable disbursement | Offset or adjust prior usage; never silent delete |

**Pre-release cancellation** (loan cancelled before physical release): internal PLM lifecycle/audit fact only — **no Platform usage-metering event**, no charge, no compensating billing event.

Future Platform rating rules may map reversal events to credits, voids, or net counts. Mapping logic is Platform-owned. PLM must emit durable, idempotent facts for billable events only.

No other loan lifecycle event is authorized as billable in this contract unless a future ADR explicitly adds one.

---

## Required event payload facts (no PII)

Each usage event must include at minimum:

| Field | Requirement |
|---|---|
| Event type | One of the types above |
| Event identifier | Unique per logical business occurrence |
| Idempotency key | Stable across retries; duplicate delivery must not double-bill |
| Organization identifier | Guid |
| Product code | `pinoy-loan-manager` |
| Loan identifier | PLM-owned Guid (opaque to Platform billing) |
| Disbursement identifier | PLM-owned Guid when applicable |
| Occurred at UTC | Authoritative posting instant |
| Correlation / trace identifier | Cross-service audit |
| Amount metadata | **Non-PII** billing dimensions only (for example currency code, optional quantity=1); not borrower name, address, phone, government ID |

### Explicitly forbidden in metering payloads

- borrower name, address, phone, email, government IDs
- Personal login or contact email
- payment instrument details
- full loan terms snapshot unless later approved as non-PII billing metadata
- staff actor PII beyond opaque actor Guid if required for audit

---

## Idempotency and delivery

| Requirement | Detail |
|---|---|
| Idempotency key | Derived from stable business identity (for example disbursement posting id) |
| Duplicate detection | Platform and PLM must tolerate at-least-once delivery |
| Retries | Safe replays must not create duplicate billable usage |
| Durability | Critical events must use durable handoff (outbox or equivalent); no fragile post-commit fire-and-forget |
| Ordering | Per-loan causal ordering sufficient; global ordering not required |

Portfolio direction: [async-events-idempotency-and-resilience.md](../../../../../docs/Product-Foundation/async-events-idempotency-and-resilience.md).

Transport selection remains **D-P12-03 Open**.

---

## PLM internal responsibilities

Before emitting `LOAN_DISBURSED`, PLM must have:

- posted disbursement in the Loan subledger
- satisfied disbursement readiness and authorization rules
- recorded audit evidence

If disbursement is reversed, PLM emits the reversal/cancellation event in the same durable pattern. Platform usage must not remain overstated.

---

## Explicit non-goals

- Choosing queue, webhook, or outbox technology
- Platform invoice line schema
- Borrower fee calculation
- Closing **D-P12-03**
- Direct SQL/EF writes to Platform billing tables
