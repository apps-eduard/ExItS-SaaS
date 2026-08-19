# Dedicated and On-Prem Deployment Model

**Status:** Authoritative **planning** guidance (EXITS-ARCH-01). Not implemented as new infrastructure here.
**Decisions:** **D-HOST-02**, **D-HOST-03**, **D-HOST-09**
**On-prem topology (existing):** [production-deployment-architecture.md](../engineering/production-deployment-architecture.md) (**D-P14-01**)
**Index:** [hosting-and-deployment-operating-model.md](hosting-and-deployment-operating-model.md)

**D-P12-03 remains OPEN.** Do not invent offline entitlement tokens, leases, caches, license files, or cryptographic mechanisms.

---

## 1. Mode B — dedicated single-tenant hosting (optional)

Possible reasons:

- very large organization
- stronger isolation
- contractual requirement
- performance requirements
- regulated environment
- premium deployment
- regional / data residency needs

Still uses normal ExItS product architecture. **No customer-specific source fork.**

May be realized later as a dedicated stamp, dedicated partition, or dedicated environment (**D-HOST-07**). Implementation deferred.

---

## 2. Mode C — customer on-prem (special, not default)

Possible for:

- offline-sensitive environments
- customer infrastructure policy
- regulatory requirements
- private networks
- enterprise contracts

On-prem is **not** the default architecture for the whole portfolio (**D-HOST-03**).

**D-P14-01** remains the established on-prem Production topology (customer host, reverse-proxy HTTPS, Platform + licensed product apps/DBs). Existing P14 packaging evidence applies to this **mode**, not as proof that hosted SaaS exists.

---

## 3. Control-plane connectivity

Hosted SaaS normally has **direct connectivity** to its Platform Control Plane.

Dedicated / on-prem deployments may have different connectivity characteristics.

Future commercial/auth transport must support appropriate:

- availability
- authorization freshness
- expiry
- revocation
- entitlement changes
- audit
- reconnect / reconciliation

Exact mechanism remains a future Platform decision (**D-P12-03**, **D-HOST-09**).

---

## 4. On-prem continuity concerns (architecture only)

For future on-prem deployments, document concerns — **not** rules:

- temporary Internet outage
- Platform connectivity outage
- entitlement freshness
- local operational continuity
- revocation
- update availability
- backup ownership
- support access
- telemetry / privacy
- synchronization after reconnect

Exact rules remain future decisions. **D-HOST-09** does not close **D-P12-03**.

---

## 5. Backup ownership (conceptual)

| Mode | Conceptual operations |
|---|---|
| Hosted SaaS | ExItS / operator owns backup/restore according to **future** production policy |
| Dedicated hosted | ExItS / operator normally owns backup unless contract states otherwise |
| On-prem | responsibility may be shared or customer-operated depending on deployment agreement |

Exact contractual responsibility remains future operations/legal work.

Do **not** invent SLA / RPO / RTO numbers. RPO/RTO must still be decided before Production.

Current Platform/POS `pg_dump` drills remain valid tooling evidence; they are **not** Production-proven hosted SaaS backup.

---

## 6. Observability

Hosted environments should ultimately allow central operational visibility by Product, Organization, Stamp, Region, and release/version.

On-prem telemetry may require customer consent/configuration and must respect privacy.

Do **not** assume unrestricted remote telemetry.

---

## 7. Support access

Deployment mode must **never** imply unrestricted support access.

Hosted or on-prem support access must eventually require:

- explicit authorization
- appropriate role/grant
- purpose
- audit
- time limitation where applicable
- sensitive-data minimization

Do not implement.

---

## 8. Security (planning; not readiness)

Hosted SaaS will eventually require strong production authentication, MFA for privileged administration, secret management, encryption, tenant isolation, rate limiting, abuse detection, audit, backup protection, and deployment security.

Do **not** claim these are complete. Keep current security risks honest. **R-091** is Closed for Phase 13 scope. Residual MFA enforcement, enterprise SSO/AD, outbound auth delivery, step-up authentication, and overall portfolio Production-readiness work remain separate gates and do not reopen R-091.
