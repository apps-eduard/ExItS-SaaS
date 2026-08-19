# Deployment Stamps and Data Scaling

**Status:** Authoritative **planning** guidance (EXITS-SCALE-00). Not implemented.
**Decisions:** **D-SCALE-04**, **D-SCALE-05**, **D-SCALE-09**
**Index:** [exits-scale-and-growth-architecture.md](exits-scale-and-growth-architecture.md)
**Related:** [production-deployment-architecture.md](../engineering/production-deployment-architecture.md) (P14 on-prem Production direction)

Stamps, cells, sharding, Kubernetes, and multi-region are **not** launch requirements and are **not** implemented here.

---

## 1. Relationship to current Production direction

P14 production packaging direction remains **customer on-prem** (or equivalent operator-controlled host) with separate Platform and product databases.

This scale pack does **not** replace that direction.

Deployment stamps/cells are the preferred **future** horizontal isolation and scale mechanism **when measured demand requires them** (**D-SCALE-04**), especially for hosted/multi-tenant growth. They are not required for initial launch and must not be built solely because millions of users are a future goal.

---

## 2. Stamps / cells

Conceptually:

```text
Product
   |
Tenant Router
   |
   +-- Stamp A
   +-- Stamp B
   +-- Stamp C
   +-- ...
```

A stamp may contain an appropriate set of:

- API instances
- background workers
- cache
- product database / partition
- observability resources
- supporting infrastructure

depending on future deployment architecture.

Users and organization staff must not need to know which stamp serves them.

Do **not** require stamps for initial launch. Do **not** implement a tenant router in this package.

---

## 3. Stamp creation triggers

Do **not** use registered-user count alone (**D-SCALE-08**).

Future reasons to introduce additional stamps may include:

- database saturation
- sustained latency
- throughput limits
- noisy-neighbor risk
- large-tenant isolation
- blast-radius reduction
- geographic requirements
- data residency
- maintenance constraints
- recovery objectives
- operational scale

Exact thresholds must come from measured production or load-test data. Do not invent numeric cloud capacity here.

---

## 4. Blast radius

Design toward bounded failures:

- One bad tenant should not bring down all tenants.
- One Product incident should not automatically bring down other Products.
- One Stamp failure should not bring down other Stamps.
- A Control Plane incident should not automatically corrupt Product operational state.

No architecture can promise zero cascading failures. Document this as a resilience **goal**.

---

## 5. Stateless horizontal scale

Future APIs and web-facing server components should prefer **stateless request processing** where practical.

Avoid architecture that requires one specific server instance to retain authoritative session or business state.

Authoritative state belongs in appropriate durable systems (Platform DB, product DB, approved token/session stores — as later authorized).

Do **not** implement distributed session infrastructure in this package.

---

## 6. Multi-region and data residency

**D-SCALE-09:** Multi-region is an evolution option, not a launch requirement.

Do **not** require active-active multi-region at launch.

Do **not** implement multi-region here.

Future architecture must be capable of associating tenant/product placement with a **region** when required.

Do **not** assume all products require the same residency policy. Data-residency requirements must be product-, legal-, and market-specific.

Avoid designing application logic that unnecessarily prevents future regional deployment (for example, hidden global singletons, undocumented cross-region data gravity, or client-hardcoded placement).

---

## 7. Cost

See [unified-control-plane-and-product-plane.md](unified-control-plane-and-product-plane.md) §8. Stamp sprawl without measured demand is an anti-pattern.
