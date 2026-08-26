# Hosted SaaS Tenant Placement Model

**Status:** Authoritative **planning** guidance (EXITS-ARCH-01). Not implemented.
**Decisions:** **D-HOST-01**, **D-HOST-06**, **D-HOST-07**, **D-HOST-10**
**Scale:** [tenant-isolation-routing-and-partitioning.md](tenant-isolation-routing-and-partitioning.md), [deployment-stamps-and-data-scaling.md](deployment-stamps-and-data-scaling.md)
**Index:** [hosting-and-deployment-operating-model.md](hosting-and-deployment-operating-model.md)

No routing algorithm. No schema. No stamp implementation.

---

## 1. Mode A — hosted multi-tenant SaaS (default)

Conceptual:

```text
ExItS Hosted Platform
        |
Unified Control Plane
        |
Tenant Placement
        |
   +----+-------------------+
   |                        |
POS deployments/stamps   PLM deployments/stamps
   |                        |
many organizations       many organizations
```

Organizations remain **logically isolated**. Server-authoritative Actor + Organization + Product + access + entitlements + product grants + resource scope still apply. UI filtering is not isolation.

Hosted SaaS may operate (as a **future** capability, not a launch requirement):

- shared Platform infrastructure
- shared product deployment stamps/cells
- independently scalable Products
- product-specific databases/partitions
- tenant placement controlled by the Platform

**D-SCALE** decisions remain authoritative. Do **not** require multiple stamps, workers, caches, or partitions at launch.

---

## 2. Tenant placement (**D-HOST-06**)

```text
Organization
+
Product
        |
        v
Tenant Placement
        |
        v
Region / Stamp / Partition
```

Example **only**:

```text
Org A
  POS -> Region PH / POS Stamp 03
  PLM -> Region PH / PLM Stamp 01

Org B
  POS -> POS Stamp 05
```

Placement is invisible to ordinary users.

Do **not** implement routing. A placement **abstraction** is required before physical stamp/shard routing (**D-SCALE-05**).

---

## 3. Physical storage may evolve

Logical product authority remains stable.

```text
PinoyBusinessPOS
        |
   logical product
        |
  +-----+-----+-----+
  |           |     |
DB/Stamp A   B     C
```

Do **not** require one global physical POS (or PLM) database forever. Do **not** implement sharding. Do **not** choose sharding technology.

---

## 4. Large customer isolation (**D-HOST-07**)

A very large organization may later move from a **shared stamp** to a **dedicated stamp / dedicated partition / dedicated environment** without changing product identity or source code.

This is a scale **escape hatch**. Do **not** implement tenant movement here. Dedicated hosting is Mode B ([dedicated-and-on-prem-deployment-model.md](dedicated-and-on-prem-deployment-model.md)).

---

## 5. Data residency (**D-HOST-10**)

Hosting must support future placement constraints such as:

```text
Organization + Product
        |
allowed Region
```

Do not assume one global residency requirement. Requirements may vary by country, product, customer, regulation, or contract.

No legal claims. Multi-region remains evolutionary, not an initial-launch requirement.

---

## 6. Cost direction (no pricing)

Deployment mode materially affects cost. Shared hosted is usually most efficient per tenant. Dedicated costs more isolation. On-prem has different support/upgrade cost. Future Platform pricing **may** reflect deployment mode. **No pricing formula** in this package.
