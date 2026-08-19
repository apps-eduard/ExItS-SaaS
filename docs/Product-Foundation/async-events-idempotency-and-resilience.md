# Async Events, Idempotency, and Resilience

**Status:** Authoritative **planning** guidance (EXITS-SCALE-00). Not implemented.
**Decisions:** **D-SCALE-06**, **D-SCALE-07**
**Index:** [exits-scale-and-growth-architecture.md](exits-scale-and-growth-architecture.md)

Do not select message-bus, queue, cache, or outbox technology here. Do not implement messaging.

---

## 1. Asynchronous work

Work that does **not** need to complete inside the user’s authoritative transaction should be capable of asynchronous execution (**D-SCALE-07**).

Examples:

- email / SMS / push notifications
- analytics
- reporting projections
- search indexing
- Platform usage metering
- exports
- non-critical integrations
- audit enrichment where appropriate

Authoritative product money and lifecycle state stay in the product transaction. Side effects leave through a durable handoff.

---

## 2. Durable event publication

When an authoritative transaction causes an event that must not be lost, future implementation should use a **durable publication** pattern.

Examples of **patterns** (not a technology choice):

- transactional outbox
- equivalent durable event handoff

The requirement is:

**No fragile “save DB then hope message send succeeds” architecture for critical events.**

Do not implement messaging in this package.

---

## 3. Idempotency

Idempotency is a portfolio-wide requirement for important retriable commands (**D-SCALE-06**).

Examples:

- POS payment
- POS order
- PLM payment
- PLM disbursement
- collector remittance
- subscription payment
- billing operation
- externally retried webhook / integration

Future design must support:

- idempotency key / correlation identity
- duplicate detection
- safe retries
- auditable result

Do not invent a single global key format here. Product and Platform contracts may differ; the **requirement** is shared.

---

## 4. Backpressure and rate limiting

Future public and product APIs must support layered protection concepts such as:

- per actor
- per organization
- per product
- per API / client
- infrastructure-level abuse protection

Do **not** choose numeric limits now.

Rate limits must account for tenant size and product usage. A large organization must not be treated identically to a tiny one by default.

---

## 5. Caching

Caching is a **performance tool**, not an authoritative data source.

Good future cache candidates may include:

- catalog metadata
- public product configuration
- safe read models
- computed reports
- non-sensitive reusable reference data

High-risk data requires explicit consistency rules.

Do not casually cache:

- authoritative financial balances
- authorization decisions indefinitely
- security revocation state
- mutable financial transaction state

Exact cache technology remains open. Redis (or any cache) is **not** required at launch.

---

## 6. Cross-boundary billing events

See [unified-control-plane-and-product-plane.md](unified-control-plane-and-product-plane.md) §7.

Product usage toward Platform billing must be retriable, idempotent, auditable, and reconcilable. It must not require a distributed transaction across Platform and product databases.
