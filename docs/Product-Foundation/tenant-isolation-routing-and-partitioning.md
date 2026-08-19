# Tenant Isolation, Routing, and Partitioning

**Status:** Authoritative **planning** guidance (EXITS-SCALE-00). Not implemented.
**Decisions:** **D-SCALE-05** (routing/placement direction); isolation is a **Required** Product Foundation invariant
**Index:** [exits-scale-and-growth-architecture.md](exits-scale-and-growth-architecture.md)

Do not implement routing, sharding, or tenant movement in this package.

---

## 1. Tenant isolation (mandatory)

Tenant isolation is a mandatory architectural invariant.

Every operational product request must ultimately be associated with trusted:

1. Actor
2. Organization
3. Product
4. Product access
5. Entitlement / commercial state
6. Product-local grants
7. Resource scope

Organization scoping must be **server authoritative**.

UI filtering is **never** sufficient isolation.

No request may depend on a client simply supplying an unchecked `OrganizationId`.

This restates Product Foundation access intersection. It is not a new authorization model.

---

## 2. Tenant routing / placement (future)

Prepare for future tenant placement (**D-SCALE-05**).

Conceptual future capability:

```text
Organization + Product
        →
Tenant Placement / Routing
        →
Deployment Stamp / Cell / Partition
```

Example **only** (not a schema, not implemented):

```text
Organization A
  POS → POS Stamp 03
  PLM → PLM Stamp 01

Organization B
  POS → POS Stamp 07
```

Users must not need to know physical placement.

Placement metadata is a **Platform control-plane** concern. Products consume placement through approved contracts when that capability exists.

Do **not** implement routing now. A tenant-routing/placement **abstraction** is required **before** physical sharding or stamp routing is introduced.

---

## 3. Logical data authority vs physical instances

Preserve Product Foundation:

- Platform has its own authoritative database.
- Each Product owns its own authoritative operational data.
- No cross-product foreign keys.
- No direct product reads of Platform tables.
- No direct Platform reads of product operational tables.

Clarify:

**“One database per product” is a logical ownership rule.**

It does **not** require one physical database instance forever.

Future product scale may permit:

```text
Product
  →
multiple physical partitions / shards / databases / stamps
```

while retaining **one logical product data authority**.

Do **not** finalize database technology or a sharding algorithm here.

---

## 4. Shard / partition readiness (design later; do not implement now)

Do **not** implement sharding now.

When product schemas and contracts are later designed, they should make organization-scoped partitioning **possible**. Future design must consider:

- `OrganizationId` as a core tenant key
- stable identifiers
- avoiding cross-tenant transactions
- avoiding hidden cross-shard assumptions
- tenant placement lookup
- migrations across stamps
- tenant movement
- reporting across partitions
- backup / restore per partition
- disaster recovery

Cross-tenant or cross-stamp transactions must not become an implicit requirement of ordinary operational workflows.

---

## 5. Very large tenants

Unusually large organizations may later receive stronger isolation **if required**.

Possible future models (none mandatory):

- dedicated stamp
- dedicated database / partition
- dedicated resources
- premium isolation tier

Do **not** make dedicated infrastructure mandatory for every customer. Do **not** invent a pricing SKU here.
