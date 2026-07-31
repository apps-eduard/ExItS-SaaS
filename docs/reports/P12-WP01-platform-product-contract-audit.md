# P12-WP01 — Platform–Product Contract Audit

Phase marker: `P12-WP01-platform-product-contract-audit`

Package: **P12-WP01 — Platform–Product Contract Audit**  
Prior tip: `0812f01f00d352510fbbba91347675044e3562ad`  
Docs tip: *(recorded after docs commit)*

## Status

**Complete.** Documentation and architecture analysis only. No application code, migrations, APIs, UI, projects, packages, containers, or production infrastructure were added. Phase 11 Admin UI was not modified.

Exact next: **P12-WP02 — Product Foundation Reference** (do not begin until authorized).

Label legend used below:

| Label | Meaning |
|---|---|
| **Implemented** | Confirmed in current repository code, tests, or configuration |
| **Documented intent** | Stated in architecture/ops docs; not fully realized in runtime |
| **Recommendation** | Proposed authoritative rule for Phase 12; **not** claimed as newly implemented here |
| **Unresolved** | Ambiguity or decision still required before later WPs |

---

## 1. Current repository structure

### Implemented layout

```text
ExItS-SaaS/
├── ExItS.slnx
├── src/Platform/          # Admin, Api, Application, Domain, Infrastructure
├── src/Products/PinoyBusinessPOS/  # Api, ApiClient, Application, Domain, Infrastructure, LocalStore, Maui
├── src/Shared/            # DesignSystem, BackupRestore, Deployment
├── tests/                 # Platform, POS, Architecture, Admin, DesignSystem, Backup, Deployment
├── deploy/docker/         # NON-PRODUCTION pilot images + compose
├── ops/                   # backup + deploy operator scripts
└── docs/                  # portfolio architecture and tracking
```

### Solution / independence — Implemented

- `ExItS.slnx` contains Platform, PinoyBusinessPOS, Shared, tests, and tools projects only.
- No HealthCare product projects in the solution.
- No root `HealthCare/` directory; `git ls-files -- HealthCare/` empty.
- Platform `Integration/HealthCare/` holds **versioned contract abstractions only** (not a product source tree), guarded by `RepositorySafetyTests`.

### Only in-repo product today — Implemented

PinoyBusinessPOS is the sole operational product tree under `src/Products/`. Future products (Loan, Pawnshop, BNPL) are Phase 12 planning targets, not implemented.

---

## 2. Platform responsibility matrix

| Responsibility | Status | Evidence |
|---|---|---|
| Identity / users (domain + Admin) | **Implemented** (Dev/Testing identity) | Platform Domain `Identity/`; Admin users surfaces |
| Production authentication (JWT/passwords/MFA/SSO) | **Documented intent** / open **R-091** | Dev actor via `X-Dev-Platform-User-Id` / `DevelopmentPlatformActorAccessor`; not production-secure |
| Organizations + memberships | **Implemented** | Domain `Organizations/`; org membership roles |
| Product catalog (products, features, plans, plan versions, trials) | **Implemented** | Domain `Catalog/` |
| Independent product subscriptions + commercial state | **Implemented** | Domain `Subscriptions/`; per-product subscription model in use |
| SaaS billing / payment records | **Implemented** | Domain `Payments/SaaSPayment`; Admin Payments |
| Product entitlements + overrides | **Implemented** | Domain `Entitlements/` |
| Product access assignment (commercial entry) | **Implemented** | Domain `Products/ProductAccess`, `ProductAccessAssignment` |
| Platform Admin UI | **Implemented** | `ExItS.Platform.Admin` (Phase 11 design system) |
| Platform audit | **Implemented** | Domain `Audit/`; Admin Audit |
| Platform system roles / permissions | **Implemented** | `PlatformAuthz`, `PlatformPermission` catalog |
| Product discovery / launch metadata | **Partial Implemented** | Catalog + Admin portfolio/dashboard; unified multi-product launcher for future products is **Documented intent** |
| Product operational workflows | **Not Platform** | Explicit non-ownership |

---

## 3. Product responsibility matrix (PinoyBusinessPOS as reference)

| Responsibility | Status | Evidence |
|---|---|---|
| Operational domain entities | **Implemented** | Domain: Catalog, Sales, Inventory, Customers, Credit (utang), Expenses, Suppliers, Purchasing, CashierShifts, Returns, Registers, Payments, Permissions |
| Product workflows | **Implemented** | Application + Api handlers/endpoints |
| Product-local roles and grants | **Implemented** | `PosRole`, `PosRoleAssignment`, `PosRoleMatrix`, capability grants |
| Product API | **Implemented** | `ExItS.PinoyBusinessPOS.Api` |
| Product UI / mobile | **Implemented** | MAUI Android-first; LocalStore for offline |
| Product reports | **Implemented** | POS API reports + MAUI report surfaces (Admin does **not** host POS ops reports) |
| Product database + migrations | **Implemented** | `PosDbContext`, schema `pos`, `ExItS_PinoyBusinessPOS` |
| Product operational money | **Implemented** | Sales, expenses, shifts, returns/refunds, utang/credit — separate from SaaS |
| Product-domain audit | **Partial / product-local** | Operational history via immutable domain records + POS audit patterns; not Platform audit SoR |
| Platform SaaS administration | **Not product** | Consumes commercial facts; does not own catalog/billing SoR |

---

## 4. Data ownership matrix

| Data | Authoritative owner | Storage | Cross-boundary rule |
|---|---|---|---|
| Platform user / org / membership | Platform | `ExItS_Platform` / schema `platform` | Products may reference Guid IDs only |
| Product catalog, plans, subscriptions, entitlements, SaaS payments | Platform | Platform DB | Products consume via contracts/headers/APIs — **not** direct table reads |
| Platform audit | Platform | Platform DB | No clinical/POS operational payloads as Platform SoR |
| POS operational entities | POS | `ExItS_PinoyBusinessPOS` / schema `pos` | Platform must not read operational tables |
| POS `organization_id` | POS column referencing Platform Org Guid | POS DB | **Guid reference only** — no cross-DB FK |
| Offline device state | POS LocalStore | Device SQLite | Not Platform DB; not shared authoritative ops DB |
| Future product ops data | That product | Dedicated DB (naming convention `ExItS_<Product>`) | Same isolation rules |

### Isolation confirmation — Implemented

| Rule | Evidence |
|---|---|
| Separate Platform DB | `PlatformDbContext`, `ConnectionStrings:PlatformDatabase`, `ExItS_Platform` |
| Separate POS DB | `PosDbContext`, `ConnectionStrings:PosDatabase`, `ExItS_PinoyBusinessPOS` |
| No cross-product FKs | Intra-`pos` FKs only; org id is Guid column |
| No product reading Platform tables directly | No Platform Infra/EF refs from POS Domain/App/Maui; commercial gate via headers |
| No Platform reading POS operational tables | No POS project refs from Platform; Admin architecture guards forbid PinoyBusinessPOS coupling |
| No shared authoritative operational DB | Separate compose DB services (`platform-db`, `pos-db`) |
| Trusted org/product context | Server-side org scoping + commercial middleware (Dev-stage headers; Production fail-closed for commercial headers) |

---

## 5. Authorization ownership matrix

| Layer | Owner | Status | Notes |
|---|---|---|---|
| Platform system role | Platform | **Implemented** | Admin/billing/support permissions |
| Organization membership role | Platform | **Implemented** | Owner/Admin/Member — **not** POS Cashier powers |
| Product access + commercial state + entitlements | Platform (facts) | **Implemented** | Entry gate; POS currently receives commercial snapshot via Dev headers, not live Platform DB read |
| Product-local role + grant | Product | **Implemented** (POS) | `PosRoleMatrix` + assignments |
| Resource ownership / workflow invariants | Product | **Implemented** | Fail closed cross-org (404) |

### Authoritative intersection (recommended and largely implemented)

```text
Trusted actor
  → trusted organization context
  → Platform product access
  → allowed commercial state
  → required entitlement
  → product-local role
  → product-local grant
  → resource / workflow rules
```

No single check bypasses another. **Platform product access does not imply product operational permission** — **Implemented** in POS role model + documented in `ProductAccess` / authorization docs.

**Unresolved:** Production identity (R-091). Until closed, Dev/Testing headers must not be described as production-secure.

---

## 6. Financial ownership matrix

| Money kind | Owner | Status | Must not |
|---|---|---|---|
| SaaS subscription / billing payments | Platform (`SaaSPayment*`) | **Implemented** | Be reused as product ledgers |
| POS sale tenders | POS (`SalePaymentMethod`: Cash, ManualGCash, Utang) | **Implemented** | Be written into Platform SaaS payment tables |
| POS expenses | POS | **Implemented** | — |
| Cashier shift cash movements | POS | **Implemented** | Become Platform billing |
| Returns / refunds | POS | **Implemented** | — |
| Product-Based Utang / credit / repayments | POS | **Implemented** | Share Platform GCash SaaS semantics |
| Future product operational finance | That product | **Documented intent** | Reuse Platform billing records |

Naming guard: Platform SaaS `GCash` vs POS `ManualGCash` (unverified) — intentional separation (**Implemented** + architecture tests on `SaaSPayment*` naming).

---

## 7. Deployment / versioning boundary

| Concern | Status | Evidence |
|---|---|---|
| One Platform API image | **Implemented** (pilot) | `deploy/docker/Dockerfile.platform-api` |
| Platform Admin image | **Implemented** (pilot) | `Dockerfile.platform-admin` |
| Independently versioned product API image | **Implemented** (POS pilot) | `Dockerfile.pos-api` |
| Separate persistent DB per product | **Implemented** (pilot compose) | `platform-db` + `pos-db` |
| Customer-specific deployment configuration | **Documented intent** / pilot ops | Env templates + `ops/deploy`; not production-complete |
| Deploy only licensed products | **Documented intent** | Commercial model supports per-product subscription; automated license-gated compose profiles not fully productized for multi-product |
| Avoid customer-specific source forks | **Recommendation** + portfolio practice | Single codebase; configure per customer |
| Production TLS / readiness | **Open risks** | Not Production-ready |

WP01 does **not** add Dockerfiles or deployment profiles.

---

## 8. Confirmed compliant patterns

1. Separate Platform vs product project trees and databases.
2. Guid org references without cross-DB FKs.
3. SaaS money types distinct from retail/operational money types.
4. Platform product access ≠ product-local roles.
5. Admin and MAUI UI do not reference Infrastructure/EF.
6. Shared libraries limited to DesignSystem / BackupRestore / Deployment (technical primitives).
7. HealthCare present only as Platform integration contracts.
8. Architecture tests enforce isolation (`RepositorySafetyTests`, `PosFoundationArchitectureTests`, `LayerDependencyTests`, `AdminArchitectureGuardTests`, `PilotDeploymentArchitectureTests`).
9. Phase 11 Admin UI remains a Platform surface only — no POS operational UI merge.

---

## 9. Inconsistencies and ambiguities

| Finding | Classification | Disposition |
|---|---|---|
| `approved-architecture-summary.md` still says next WP is P3-WP02 | Stale doc | Corrected in this WP to point at Phase 12 |
| `authorization-matrix.md` P6 note still says product-local Cashier/Store roles “not implemented” | Stale vs P10-WP06 | Corrected with a dated note |
| `platform-product-capability-boundary.md` embeds early Phase 2 “not implemented yet” status lines for catalog/subscriptions | Historical status text | **Unresolved** for full rewrite; still directionally correct on ownership; do not treat status lines as current delivery board |
| `data-ownership.md` / some matrices list later POS items as “not yet” while Phase 8–10 delivered them | Stale | Track for doc hygiene in later Phase 12 WPs; ownership columns remain correct |
| POS commercial evaluation via Dev headers vs live Platform entitlement projection | **Documented intent** for stronger Platform-backed evaluation | **Unresolved** transport/projection hardening (not WP01 scope) |
| Draft Product-Foundation path/name mismatch: roadmap draft uses `Docs/Product-Foundation/exits-product-foundation.md`; untracked draft is `docs/Product-Foundation/exits-product-foundation-reference.md` | Draft inconsistency | Resolve in **P12-WP02**; leave draft untracked until WP02 |
| Roadmap draft lists many Product-Foundation templates not yet present on disk | Planning only | WP03 scope; do not finalize in WP01 |
| Unified multi-product launcher UX | Intent | Not required for WP01 contract |

---

## 10. Recommended authoritative contract

These rules are **Recommendations** for Phase 12 (and match Implemented behavior unless noted). They are **not** newly coded in WP01.

1. Platform owns SaaS administration, not operational product workflows.
2. Each product has an independent subscription.
3. Each product has an independent database.
4. Products never query Platform tables directly.
5. Platform never queries product operational tables directly.
6. Products use product-local roles and grants.
7. Platform product access does not grant operational permission.
8. Product operational money is separate from Platform billing.
9. Products may share contracts, design primitives, and infrastructure abstractions, but not authoritative domain state.
10. Future products must be independently deployable and versioned.
11. Customer deployments use one standard codebase and configuration, never customer forks.
12. Context-loading for product development must avoid scanning unrelated products and historical reports (detail in P12-WP02 / P12-WP04).

---

## 11. Decisions required before later Phase 12 work packages

| ID | Decision | Needed by |
|---|---|---|
| D-P12-01 | Canonical Product-Foundation file name and path under `docs/Product-Foundation/` | P12-WP02 |
| D-P12-02 | Canonical per-product docs root (`src/Products/<Name>/Docs/` vs `docs/products/...`) | P12-WP02 / WP03 |
| D-P12-03 | How future products obtain commercial state (header injection vs Platform API projection vs shared contract package) without violating “no direct Platform table reads” | P12-WP02+ / future product WPs |
| D-P12-04 | Whether stale engineering matrices get a dedicated hygiene WP or incremental updates during WP02–WP05 | Portfolio maintainers |
| D-P12-05 | Production identity approach remains blocked on **R-091** — product bootstrap must keep Dev/Testing vs Production language honest | All future products |

No business-policy invention for Loan/Pawnshop/BNPL is authorized here.

---

## 12. Validation

| Check | Result |
|---|---|
| Contract matches repository boundaries | **Pass** |
| No HealthCare product dependency | **Pass** |
| Platform and POS remain independent | **Pass** |
| Phase 11 UI untouched | **Pass** (no Admin/app code changes) |
| No application / infrastructure code in WP01 | **Pass** |
| Full Release tests | **1186 passed / 0 failed / 0 skipped** |
| Phase 12 roadmap WP01 status accurate | Updated this WP |
| Product-Foundation drafts not accidentally finalized | Left untracked pending WP02 |

---

## 13. Files changed (this WP)

- `docs/reports/P12-WP01-platform-product-contract-audit.md` (this report)
- `docs/phases/phase-12-product-foundation-and-bootstrap.md` (tracked + status)
- `docs/portfolio-progress.md`
- `docs/phases/README.md`
- `docs/reports/README.md`
- `README.md`
- `FILE-MANIFEST.md`
- `docs/engineering/approved-architecture-summary.md` (stale “next WP” correction)
- `docs/engineering/authorization-matrix.md` (stale roles note correction)
- `docs/risks-and-issues.md` (Phase 12 note)

Intentionally **not** committed: `docs/Product-Foundation/**` (deferred to P12-WP02+).

---

## Exact next work package

**P12-WP02 — Product Foundation Reference** when explicitly authorized. Do not begin P12-WP02 in this package. Do not create `_ProductTemplate` scaffold.
