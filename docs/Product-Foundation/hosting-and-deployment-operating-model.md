# Hosting and Deployment Operating Model

**Status:** Authoritative **planning** guidance (EXITS-ARCH-01). Not implemented. Not a hosted-SaaS or Production-ready claim.
**Pack:** [README.md](README.md) · [hosting-readiness-checklist.md](hosting-readiness-checklist.md)
**Scale:** [exits-scale-and-growth-architecture.md](exits-scale-and-growth-architecture.md)
**On-prem topology:** [production-deployment-architecture.md](../engineering/production-deployment-architecture.md) (**D-P14-01**)

Do **not** treat this file as evidence that hosted multi-tenant SaaS infrastructure, stamps, sharding, or multi-region currently exist.

---

## 1. Purpose

Finalize the ExItS **portfolio hosting/deployment direction** for long-term growth.

| Decision | Meaning |
|---|---|
| **D-HOST-01** | **Primary / default** delivery model = **hosted multi-tenant SaaS** |
| **D-HOST-02** | Dedicated single-tenant hosting = optional mode |
| **D-HOST-03** | Customer on-prem = supported **special** mode, **not** the portfolio-wide default |

Same ExItS product architecture and source code across modes. **No customer-specific source forks** (**D-HOST-04**).

One logical Platform Control Plane / unified Platform Admin remains (**D-SCALE-01**). Products remain independently scalable and data-isolated (**D-SCALE-02**, **D-HOST-05**).

---

## 2. Relationship to D-P14-01 (history preserved)

**D-P14-01** remains the established **on-prem Production topology** (customer-operated host, reverse-proxy HTTPS, Platform + per-product apps/DBs). It is **not deleted**.

EXITS-ARCH-01 introduces a **broader portfolio hosting model**:

- **D-HOST-01** establishes hosted multi-tenant SaaS as the **portfolio default**.
- **D-P14-01** remains applicable to **Mode C (customer on-prem)**.
- **D-P14-01 is no longer the universal default** for every ExItS customer.

Existing P14 packaging, Compose templates, and on-prem evidence remain valid **for the on-prem deployment mode**. They do **not** prove hosted SaaS is implemented.

---

## 3. Formal hosting decisions

These IDs are portfolio-stable. They do **not** close **D-P12-03** or **R-091**. They do **not** implement infrastructure.

| ID | Decision | Status |
|---|---|---|
| **D-HOST-01** | ExItS primary portfolio delivery model is hosted multi-tenant SaaS | **Accepted architecture baseline** |
| **D-HOST-02** | Dedicated single-tenant hosting is an optional deployment mode for justified customers | **Accepted architecture baseline / implementation deferred** |
| **D-HOST-03** | Customer on-prem remains a supported special deployment mode, not the portfolio-wide default | **Accepted architecture baseline** |
| **D-HOST-04** | All deployment modes use the same logical product architecture and must avoid customer-specific source forks | **Accepted architecture baseline** |
| **D-HOST-05** | Deployment mode does not change Platform/Product data ownership boundaries | **Accepted architecture baseline** |
| **D-HOST-06** | Hosted tenant placement may evolve by Product + Organization + Region/Stamp/Partition | **Accepted architecture direction / implementation deferred** |
| **D-HOST-07** | Large tenants may later move to stronger physical isolation without becoming a different Product | **Accepted architecture direction / implementation deferred** |
| **D-HOST-08** | Platform/Product release compatibility must be explicit across independently deployed components | **Accepted architecture baseline** |
| **D-HOST-09** | On-prem continuity/commercial-state behavior requires an explicit future contract and does not close D-P12-03 | **Accepted architecture direction / mechanism open** |
| **D-HOST-10** | Multi-region and regional residency placement remain evolutionary options rather than initial-launch requirements | **Accepted architecture baseline** |

---

## 4. Document map

| Document | Subject |
|---|---|
| [deployment-mode-contract.md](deployment-mode-contract.md) | Same product across modes; no forks; compatibility; releases |
| [hosted-saas-tenant-placement-model.md](hosted-saas-tenant-placement-model.md) | Mode A placement, stamps, large-tenant move, residency |
| [dedicated-and-on-prem-deployment-model.md](dedicated-and-on-prem-deployment-model.md) | Modes B and C; connectivity; continuity; backup/support |
| [hosting-readiness-checklist.md](hosting-readiness-checklist.md) | Review checklist for future work |

---

## 5. Honesty gates

| Claim | Allowed? |
|---|---|
| Hosted multi-tenant SaaS is the **intended** portfolio default | Yes (this pack) |
| Hosted SaaS infrastructure currently exists | **No** |
| Million-user scale proven | **No** |
| Stamps / sharding / multi-region implemented | **No** |
| Production Ready / SLA / RPO/RTO proven | **No** |
| D-P12-03 closed | **No** |
| D-P14-01 erased | **No** — retained as on-prem topology |
