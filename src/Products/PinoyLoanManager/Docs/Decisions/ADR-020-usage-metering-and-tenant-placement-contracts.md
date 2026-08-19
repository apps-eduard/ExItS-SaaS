# ADR-020 — Usage metering and tenant placement contracts

**Status:** Accepted (PLM-DOC-10)
**Date:** 2026-08-19
**Decisions:** PLM usage metering and tenant placement **contract requirements** accepted; **D-P12-03 Open**; physical routing **deferred**

---

## Context

PLM product docs already preferred **LOAN DISBURSED** as the Platform billable event and recorded separate-database isolation with deferred stamp/partition routing. Without explicit contracts, implementation risk included:

- billing on approval instead of disbursement
- PII in usage payloads
- hard-coded PostgreSQL connections per environment
- direct writes to Platform billing tables

Portfolio scale and hosting packs ([hosted-saas-tenant-placement-model.md](../../../../docs/Product-Foundation/hosted-saas-tenant-placement-model.md)) define direction but not product-specific obligations.

---

## Decision

1. Accept [../Architecture/platform-usage-metering-contract.md](../Architecture/platform-usage-metering-contract.md):
   - primary event: **`LOAN_DISBURSED`**
   - additional events: **`LOAN_DISBURSEMENT_REVERSED`**, **`LOAN_DISBURSEMENT_CANCELLED`**
   - idempotency keys and at-least-once safe delivery required
   - **no PII** in metering payloads
   - Platform owns rating/billing; PLM owns disbursement truth; no direct Platform billing table writes
2. Accept [../Architecture/tenant-placement-and-routing-contract.md](../Architecture/tenant-placement-and-routing-contract.md):
   - lookup chain: **Organization + Product → Tenant Placement → Region / Stamp / Partition**
   - no hard-coded DB routing as the long-term strategy
   - fail closed on unknown placement
   - logical name `ExItS_PinoyLoanManager` unchanged (**PLM-D-00-02 Closed for name**)
3. Outbound usage delivery mechanism and inbound commercial-state transport remain **D-P12-03 Open**.
4. Placement service schema, stamp implementation, and migration ops remain **deferred** (not closed by this ADR).

---

## Consequences

Future PLM Infrastructure may implement outbox/handlers against stable event and placement fact requirements.

**Still open**

- **D-P12-03** — transport for usage events and commercial-state facts
- Platform billing schema and rating rules
- Physical stamp/shard provisioning and tenant movement tooling
- Backup/restore runbooks per partition

No message bus, queue, or placement microservice is authorized in this documentation package.

---

## Canonical references

- [../Architecture/platform-commercial-integration.md](../Architecture/platform-commercial-integration.md)
- [../Product/fees-and-net-proceeds-policy.md](../Product/fees-and-net-proceeds-policy.md)
- [../Reports/PLM-DOC-10-platform-personal-and-commercial-contracts.md](../Reports/PLM-DOC-10-platform-personal-and-commercial-contracts.md)
