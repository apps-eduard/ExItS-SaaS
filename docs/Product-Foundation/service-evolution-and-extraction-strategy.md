# Service Evolution and Extraction Strategy

**Status:** Authoritative **planning** guidance (EXITS-SCALE-00). Not implemented.
**Decisions:** **D-SCALE-03**
**Index:** [exits-scale-and-growth-architecture.md](exits-scale-and-growth-architecture.md)

ExItS will **not** adopt microservices merely because future scale is expected.

---

## 1. Modularity first

Initial architecture should prefer:

- clear domain boundaries
- clear modules
- clear contracts
- independently deployable Products
- independently owned databases
- stateless application/API design where practical

Extract a module into an independently deployed service **only when justified**.

Products are already independently deployable application planes. That is **not** the same as splitting every entity into a microservice.

---

## 2. Acceptable reasons to extract a service

Documented reasons that **may** justify extracting a capability:

- independently extreme scaling profile
- security / isolation requirement
- separate availability requirement
- independent deployment frequency
- operational ownership
- data residency
- failure-domain isolation
- materially different technology requirement
- measured bottleneck
- organizational / team boundary at sufficient scale

---

## 3. Invalid reasons to extract

Do **not** extract merely because:

- the codebase is large
- microservices are fashionable
- user count is high
- an architecture diagram looks cleaner

---

## 4. What must not be required at launch

Not required at initial launch solely because millions of users are a future goal:

- microservices everywhere
- Kubernetes
- service mesh
- active-active multi-region
- database sharding
- dozens of queues
- a separate service per entity
- distributed transactions
- event sourcing everywhere
- dedicated tenant infrastructure for every customer

Build these only when justified by measurement, isolation, residency, or an explicit authorized work package.

---

## 5. Platform Admin module evolution

The unified Platform Admin **experience** stays (**D-SCALE-01**).

Its backend may later evolve into independently scalable capabilities. That is module extraction **behind one admin UX**, not a new Admin app per product.
