# Pinoy Loan Manager — Tenant Placement and Routing Contract

**Status:** Accepted product contract requirements (PLM-DOC-10); routing implementation **deferred**
**Implementation present:** No
**Last updated:** 2026-08-19

How Pinoy Loan Manager resolves **where** an Organization’s product data lives in hosted SaaS without hard-coded database connections. Aligns with portfolio scale guidance; does not implement stamps, shards, or placement services.

Related: [persistence-and-database-boundary.md](persistence-and-database-boundary.md), [platform-access-context-contract.md](platform-access-context-contract.md), [../../../../docs/Product-Foundation/hosted-saas-tenant-placement-model.md](../../../../docs/Product-Foundation/hosted-saas-tenant-placement-model.md), [../Decisions/ADR-020-usage-metering-and-tenant-placement-contracts.md](../Decisions/ADR-020-usage-metering-and-tenant-placement-contracts.md).

---

## Problem statement

PLM must remain deployable across:

- shared hosted SaaS stamps
- future dedicated stamps / partitions
- dedicated or on-prem modes

…without embedding `Host=...;Database=ExItS_PinoyLoanManager` (or stamp-specific names) in application source as the long-term routing strategy.

**Logical database name** remains **`ExItS_PinoyLoanManager`** (**PLM-D-00-02 Closed for name**). Physical placement is resolved at runtime through an approved abstraction.

---

## Required lookup chain

Hosted SaaS default model:

```text
Organization
+
Product (pinoy-loan-manager)
        |
        v
Tenant Placement (Platform-controlled)
        |
        v
Region / Stamp / Partition
        |
        v
PLM connection target (resolved, not hard-coded)
```

Portfolio reference: [hosted-saas-tenant-placement-model.md](../../../../docs/Product-Foundation/hosted-saas-tenant-placement-model.md) (**D-HOST-06** accepted direction; implementation deferred).

---

## Contract facts PLM may consume

Future approved placement contract must expose at minimum:

| Fact | Requirement |
|---|---|
| Organization identifier | Guid |
| Product code | `pinoy-loan-manager` |
| Deployment mode | Hosted shared, dedicated hosted, on-prem, or other approved mode |
| Region | Residency / latency region identifier |
| Stamp identifier | Opaque deployment cell identifier |
| Partition identifier | Opaque data partition within stamp when applicable |
| Placement version | Change marker for cache invalidation |
| Connection resolution reference | Opaque handle or secret reference — not a checked-in connection string |

Ordinary users and Personal borrowers must not see placement metadata.

---

## PLM application rules

| Rule | Requirement |
|---|---|
| No hard-coded DB | Application code must not embed environment-specific PostgreSQL host/database routing as the primary strategy |
| Fail closed | Unknown placement → deny product DB access; do not guess |
| Isolation | One Organization’s PLM data must not leak across placement boundaries |
| Independent backup | PLM backup/restore scope follows placement partition; procedure remains open |
| Migrations | Applied per placement target under controlled ops; no automatic Production `Migrate()` at API startup |
| Cross-product | No FK to POS or Platform operational tables; OrganizationId is identity only |

On-prem and dedicated modes may collapse Region/Stamp/Partition to a single resolved target while preserving the same abstraction interface.

---

## Relationship to access context

Tenant placement resolves **data plane** routing. [platform-access-context-contract.md](platform-access-context-contract.md) resolves **control plane** identity, org, product access, and commercial facts. Both are required before authoritative PLM work.

Placement lookup failure and commercial-state unknown both fail closed for write authority.

---

## Large-tenant movement

A large Organization may later move from a shared stamp to a dedicated stamp/partition without changing product code (**D-HOST-07** direction). PLM docs record the requirement; movement tooling is not designed here.

---

## Explicit non-goals

- Sharding algorithm or technology choice
- Platform placement service schema
- Creating databases, partitions, or migrations in this package
- Closing **D-P12-03** (orthogonal but often co-deployed)
- Requiring multi-stamp infrastructure at initial launch
