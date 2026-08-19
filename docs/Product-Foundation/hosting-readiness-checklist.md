# Hosting Readiness Checklist

**Status:** Planning review checklist (EXITS-ARCH-01). Not a hosted-SaaS or Production-ready claim.
**Index:** [hosting-and-deployment-operating-model.md](hosting-and-deployment-operating-model.md)

Checking an item means the **design** respects the rule. It does **not** mean hosted infrastructure exists.

---

## Portfolio default

- [ ] Hosted multi-tenant SaaS is treated as the portfolio **default** (D-HOST-01)
- [ ] Dedicated hosting is optional and justified (D-HOST-02)
- [ ] Customer on-prem remains optional/special, not the universal default (D-HOST-03)
- [ ] D-P14-01 is preserved as the **on-prem** topology, not erased

## Product identity

- [ ] Unified Platform Admin retained; no per-product Platform Admin apps
- [ ] Independent Product Application Planes retained
- [ ] Same Product identity across hosted / dedicated / on-prem
- [ ] No customer-specific source forks or customer product branches
- [ ] Customer-specific behavior is configuration/entitlement, not a fork

## Isolation and data

- [ ] Tenant isolation remains server-authoritative
- [ ] Deployment mode does not change Platform/Product data ownership
- [ ] Tenant placement abstraction exists in design before stamps/shards are introduced
- [ ] Large-tenant stronger isolation does not create a different Product

## Operations

- [ ] Platform/Product compatibility/versioning is explicit (windows not invented here)
- [ ] No premature multi-region or sharding implementation
- [ ] On-prem continuity does not invent D-P12-03 mechanisms
- [ ] D-P12-03 remains open
- [ ] Support access is never implied unrestricted by deployment mode
- [ ] No claim of hosted SaaS implemented, million-user scale, SLA, or Production Ready
