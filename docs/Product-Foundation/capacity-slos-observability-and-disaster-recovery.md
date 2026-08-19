# Capacity, SLOs, Observability, and Disaster Recovery

**Status:** Authoritative **planning** guidance (EXITS-SCALE-00). Not implemented.
**Decisions:** **D-SCALE-08**, **D-SCALE-09**
**Index:** [exits-scale-and-growth-architecture.md](exits-scale-and-growth-architecture.md)
**Related:** [postgresql-backup-and-restore.md](../runbooks/postgresql-backup-and-restore.md) (current Platform/POS backup drills; not Production-proven)

Do not invent contractual SLA percentages, RPO/RTO numbers, or fabricated benchmarks.

---

## 1. Capacity model

“1 million users” is **not** by itself a technical capacity requirement (**D-SCALE-08**).

Capacity planning must track actual workload dimensions such as:

- registered users
- organizations
- daily active users
- monthly active users
- concurrent sessions
- requests per second
- transaction writes per second
- payments per second
- orders / sales per second
- Loan operations per second (when that product exists)
- event throughput
- database size
- storage growth
- bandwidth
- peak / average ratio
- largest tenant share
- background-job volume

---

## 2. Conceptual capacity stages

No numeric cloud bill or hardware sizing. These stages are planning language only.

| Stage | Meaning | Metrics that matter | Escape hatches that become relevant | Do **not** introduce prematurely |
|---|---|---|---|---|
| **A — Launch / early production** | First Production (or equivalent) with limited tenants | availability, error rate, backup restore drills, auth honesty (R-091) | clear module/product boundaries; independent DBs | stamps, sharding, microservices, multi-region |
| **B — Growing multi-tenant production** | More orgs and daily use | RPS, write TPS, DB size, largest-tenant share, queue/job delay if any | horizontal app instances; connection pooling; async non-critical work | dedicated infra per customer; event sourcing everywhere |
| **C — High-volume regional product** | One product approaches operational limits | p95/p99 latency, DB saturation, noisy-neighbor, payment/disbursement failure rate | first product stamps; tenant placement lookup; partition-ready keys | active-active multi-region; service mesh |
| **D — Large-scale multi-product SaaS** | Many products, high event volume | per-product cost, cross-product blast radius, usage/billing lag, observability volume | independent product scale; durable events; control-plane module scale | shared operational DB; 2PC across products |
| **E — Multi-region / very-large-scale if required** | Legal, latency, or DR evidence demands it | residency, regional failover, cross-region consistency costs | regional stamps; placement by region | assuming all products share one residency policy |

Exact thresholds remain future business/ops decisions.

---

## 3. Load testing

Before major launch and major scale transitions, require capacity / load testing.

Future testing should include:

- normal load
- launch peak
- burst traffic
- largest-tenant behavior
- database contention
- queue backlog
- dependency slowdown
- retry storm
- cache failure
- partial outage

Exact targets remain tied to future business SLOs and forecasts. No fabricated results.

---

## 4. SLO / SLI model

Future service-level **indicators** (not contractual SLAs):

- availability
- request success rate
- p50 / p95 / p99 latency
- transaction latency
- queue age
- background-job delay
- DB saturation
- error rate
- failed payment / disbursement rate
- tenant-specific performance
- recovery time

Do **not** invent contractual SLA percentages yet.

---

## 5. Observability

Future telemetry must support correlation by:

- Product
- Organization
- Deployment / Stamp
- Region if applicable
- API / operation
- version / release
- correlation / trace ID

Sensitive / customer data must not be indiscriminately logged.

Avoid PHI and financial-secret leakage in logs.

Existing ops backup/restore for Platform and POS is **not** a complete observability platform. Do not claim OpenTelemetry or a specific vendor here.

---

## 6. Backup / restore at scale

Scale architecture must support future:

- product-specific backups
- Platform backup
- restore testing
- point-in-time recovery where supported
- per-stamp recovery
- tenant migration / recovery procedures
- recovery evidence

Today: Platform and POS can be dumped/restored **independently** in non-production drills. **Production Backup/Restore Proven = No** until Phase 14 production exit criteria are met.

Do **not** define exact RPO/RTO yet.

**RPO/RTO must be decided before Production.**

---

## 7. Disaster recovery

Do **not** require active-active multi-region at launch (**D-SCALE-09**).

Evolution options:

- single region + tested backups
- warm recovery environment
- secondary region
- regional stamps
- more advanced failover only when justified

Avoid application logic that unnecessarily prevents future regional deployment.
