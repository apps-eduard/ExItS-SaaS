# Scale-Readiness Checklist

**Status:** Planning review checklist (EXITS-SCALE-00). Not a production-readiness claim.
**Index:** [exits-scale-and-growth-architecture.md](exits-scale-and-growth-architecture.md)

Use this when reviewing future Platform or product work packages. Checking an item means the **design** respects the rule. It does **not** mean the capability is implemented or that ExItS currently supports millions of users.

---

## Boundaries

- [ ] Platform remains one logical control plane with one unified Platform Admin experience
- [ ] Product remains an independent application plane with its own operational data authority
- [ ] Personal does not become a shared product ledger or authorization authority
- [ ] Normal product operations are not moved into Platform Admin
- [ ] No cross-product foreign keys or direct cross-database table reads
- [ ] “One database per product” treated as **logical** ownership (physical partitions later only if measured)

## Isolation and routing

- [ ] Server-authoritative Actor + Organization + Product + access + entitlements + product grants + resource scope
- [ ] Client-supplied OrganizationId is never trusted isolation
- [ ] Tenant placement/routing abstraction considered before any stamp or shard routing
- [ ] Users are not required to know physical placement

## Resilience

- [ ] Control-plane vs product-plane failure domains considered (D-P12-03 still open — no invented lease/cache)
- [ ] Critical events use a durable publication *intent* (not fire-and-forget)
- [ ] Important retriable commands have idempotency/duplicate-safety *intent*
- [ ] Authoritative product transactions are not blocked on synchronous Platform billing unless an approved invariant says so
- [ ] Caching is not treated as source of truth for balances, authz, or revocation

## Scale-out discipline

- [ ] No microservices introduced without extraction criteria
- [ ] No stamps/shards/multi-region introduced without measured demand or legal/residency need
- [ ] Capacity arguments use workload metrics, not registered-user count alone
- [ ] Large-tenant isolation is optional, not mandatory for every customer
- [ ] Cost per product/tenant is visible to Platform pricing thinking

## Honesty

- [ ] R-091 remains open until production authentication is actually Production-ready
- [ ] D-P12-03 remains open until commercial-state transport is decided
- [ ] No claim of millions of users supported
- [ ] No fabricated SLA, benchmark, or RPO/RTO
- [ ] No “Production Ready” claim from this checklist
