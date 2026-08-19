# ExItS Scale and Growth Architecture

**Status:** Authoritative **planning** guidance for future ExItS product and Platform work (EXITS-SCALE-00). Not implemented. Not a production-capacity claim.
**Pack:** [README.md](README.md) · [scale-readiness-checklist.md](scale-readiness-checklist.md)
**Foundation:** [exits-product-foundation-reference.md](exits-product-foundation-reference.md)
**Related:** [production-deployment-architecture.md](../engineering/production-deployment-architecture.md) (P14 on-prem direction) · [approved-architecture-summary.md](../engineering/approved-architecture-summary.md)

Do **not** treat this file as evidence that millions of users, multi-region, sharding, or microservices are currently supported.

---

## 1. Purpose

ExItS must remain a multi-product SaaS portfolio whose **ownership and security boundaries stay stable** while usage grows from thousands toward hundreds of thousands or millions of registered users.

That growth must **not** require redesigning:

- Platform vs Product ownership
- independent product subscriptions and entitlements
- organization tenancy
- product-local authorization
- operational-money vs SaaS-billing money
- database authority (Platform vs each Product)

It also must **not** require building million-user infrastructure before measured demand exists.

**Principle:** Design boundaries early. Split physical infrastructure only when measured scale requires it.

---

## 2. Core scale principle

| Do | Do not |
|---|---|
| Keep one **logical** Platform Control Plane and one unified Platform Admin **experience** | Assume one process, one server, or one database forever |
| Keep independently owned Product Application Planes | Couple products at runtime or in one operational database |
| Provide **scale-out escape hatches** (stamps, routing, async, idempotency) | Prematurely implement stamps, shards, queues, or microservices |
| Decide capacity from **workload metrics** | Treat “1 million users” as a technical requirement by itself |
| Isolate failure domains | Promise zero cascading failures |

Escape hatches are **documented options**. They are not launch deliverables.

---

## 3. Document map

| Document | Subject |
|---|---|
| [unified-control-plane-and-product-plane.md](unified-control-plane-and-product-plane.md) | Control plane, product planes, Personal, Admin split, billing at scale |
| [tenant-isolation-routing-and-partitioning.md](tenant-isolation-routing-and-partitioning.md) | Tenant isolation, routing, partition readiness, large tenants |
| [deployment-stamps-and-data-scaling.md](deployment-stamps-and-data-scaling.md) | Stamps/cells, physical vs logical databases, cost |
| [async-events-idempotency-and-resilience.md](async-events-idempotency-and-resilience.md) | Async work, durable events, idempotency, rate limits, cache |
| [capacity-slos-observability-and-disaster-recovery.md](capacity-slos-observability-and-disaster-recovery.md) | Capacity stages, SLOs, telemetry, backup/DR, multi-region |
| [service-evolution-and-extraction-strategy.md](service-evolution-and-extraction-strategy.md) | Modularity-first; when (not) to extract services |
| [scale-readiness-checklist.md](scale-readiness-checklist.md) | Review checklist for future work packages |

---

## 4. Formal scale decisions

These IDs are portfolio-stable. They do **not** close **D-P12-03** or **R-091**.

| ID | Decision | Status |
|---|---|---|
| **D-SCALE-01** | One logical Platform Control Plane and one unified Platform Admin experience | **Accepted architecture baseline** |
| **D-SCALE-02** | Independent Product Application Planes and data authorities | **Accepted architecture baseline** |
| **D-SCALE-03** | Modularity-first; no premature microservices | **Accepted architecture baseline** |
| **D-SCALE-04** | Deployment stamps/cells are the preferred future horizontal isolation/scale mechanism when measured demand requires them | **Accepted architecture baseline / implementation deferred** |
| **D-SCALE-05** | Tenant routing/placement abstraction is required before physical sharding or stamp routing | **Accepted architecture direction / implementation deferred** |
| **D-SCALE-06** | Critical retriable commands require idempotency and duplicate safety | **Accepted architecture baseline** |
| **D-SCALE-07** | Non-critical cross-boundary work should support durable asynchronous processing | **Accepted architecture baseline** |
| **D-SCALE-08** | Capacity decisions must use measured workload metrics, not registered-user count alone | **Accepted architecture baseline** |
| **D-SCALE-09** | Multi-region is an evolution option, not a launch requirement | **Accepted architecture baseline** |
| **D-SCALE-10** | Control Plane and Product Plane failure domains should be isolated where practical | **Accepted architecture baseline** |

---

## 5. Relationship to existing architecture

This pack **extends** Product Foundation isolation and P14 production deployment direction. It does not replace them.

- **Today’s Production packaging direction** remains customer on-prem (or equivalent operator-controlled host) with separate Platform and product databases ([production-deployment-architecture.md](../engineering/production-deployment-architecture.md)).
- **Stamps, routing, and multi-region** are future hosted/SaaS and large-scale options. They are not required to launch, and they are not implemented here.
- **Development backup/restore** for Platform and POS exists as ops capability. Production RPO/RTO, stamp-level recovery, and contractual SLAs remain **unset**.
- **Commercial-state transport (D-P12-03)** and **production authentication (R-091)** remain **open**. This pack records scale *considerations* (availability, revocation, fail-closed, continuity) without inventing the mechanism.
- **Privileged support access** into product data must be explicit, time-bound, audited, and minimized. Silent unrestricted support access is forbidden. This is not implemented here.

---

## 6. Explicitly not required at initial launch

Do **not** introduce the following solely because millions of users are a future goal:

- microservices everywhere
- Kubernetes or a service mesh
- active-active multi-region
- database sharding
- dozens of queues
- a separate service per entity
- distributed transactions / 2PC across Platform and products
- event sourcing everywhere
- dedicated tenant infrastructure for every customer

Build these only when justified by measurement, isolation, residency, or an explicit authorized work package.

---

## 7. Honesty gates

| Claim | Allowed? |
|---|---|
| Architecture *can evolve* toward large scale without redesigning product ownership | Yes (this pack) |
| Millions of users currently supported | **No** |
| Product or Platform is Production Ready | **No** (R-091 and other blockers remain) |
| Fabricated benchmarks, SLAs, or cloud bills | **No** |
| Stamps / sharding / multi-region implemented | **No** |
| D-P12-03 or R-091 closed by this pack | **No** |
