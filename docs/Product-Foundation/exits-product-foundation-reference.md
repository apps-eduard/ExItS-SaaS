# ExItS Product Foundation Reference

**Status:** Authoritative for future ExItS product work (P12-WP02)
**Path:** `docs/Product-Foundation/exits-product-foundation-reference.md`
**Companion audit:** [P12-WP01](../reports/P12-WP01-platform-product-contract-audit.md)
**Permanent rules:** `.cursor/rules/exits-workflow.mdc`

Label key used in this file:

| Label | Meaning |
|---|---|
| **Implemented** | True in the current repository (Platform + PinoyBusinessPOS) |
| **Required** | Binding rule for every future ExItS product |
| **Unresolved** | Decision still open; do not invent a solution |
| **Example** | Illustration only — not product policy |

Do not invent Loan, Pawnshop, or BNPL business rules from this file.

---

## 1. Purpose

ExItS is one shared **Platform** (SaaS control plane) plus **independently subscribed products**.

| Layer | Role |
|---|---|
| Platform | Identity boundary, organizations, catalog, plans, subscriptions, entitlements, SaaS billing, Admin, Platform audit |
| Product | Operational domain, workflows, product-local authz, API/UI, reports, product DB, operational money |
| Shared | Technical primitives and contracts only — never authoritative product state |

Use this reference so new product work loads a small context pack instead of scanning unrelated products, historical reports, or the full Platform/POS trees.

---

## 2. Platform responsibilities

Platform owns (**Implemented** today unless noted):

- identity / users and the **future** production authentication boundary (**Unresolved:** R-091 — Dev/Testing identity is not production-secure)
- organizations and Platform memberships
- product catalog, plans, plan versions, trials
- **independent** product subscriptions and commercial state
- SaaS billing and payment records (`SaaSPayment*`)
- product entitlements, overrides, and commercial product access
- Platform Admin and Platform audit
- product discovery / launch metadata (catalog + Admin portfolio; unified multi-product launcher remains partial)

Platform does **not** own operational product workflows, product-local roles, or product operational money.

---

## 3. Product responsibilities

Each product owns (**Required**; POS is the **Implemented** reference):

- operational domain model and business rules
- workflows and lifecycle invariants
- product-local roles and grants
- product API and web/mobile UI
- product reports
- product database, schema, and migrations
- operational financial records and ledgers
- product-specific audit / immutable history where the domain requires it
- product-specific retention, privacy, and security rules

---

## 4. Isolation contract

| Rule | Binding |
|---|---|
| Separate Platform database | **Implemented** / **Required** |
| Separate database per product | **Implemented** (POS) / **Required** |
| No cross-product foreign keys | **Implemented** / **Required** |
| No direct product reads of Platform tables | **Implemented** / **Required** |
| No direct Platform reads of product operational tables | **Implemented** / **Required** |
| No shared authoritative operational database | **Implemented** / **Required** |
| Organization IDs may cross boundaries only as identifiers/contracts (Guid), never as cross-DB FKs | **Implemented** / **Required** |
| Platform product access ≠ product operational permission | **Implemented** / **Required** |
| Product-local authorization is authoritative inside the product | **Implemented** / **Required** |
| No PHI in a product unless that product explicitly authorizes and designs for it | **Required** (POS must not contain PHI) |

Naming convention (**Example** / **Required** pattern): `ExItS_Platform`, `ExItS_PinoyBusinessPOS`, `ExItS_<ProductName>`.

Integration across boundaries uses approved contracts or APIs only.

---

## 5. Subscription, entitlement, and authorization

### Independent subscription (**Implemented** / **Required**)

- Each product has its own subscription, plan, and commercial state.
- Changing one product’s subscription must not change another’s.
- One product’s entitlement must not authorize another product.
- Do not reuse one product subscription to unlock another.

### Access intersection (**Required**)

Effective operational access requires **all** applicable checks:

1. trusted actor
2. trusted organization context
3. Platform product access
4. allowed commercial state
5. required product entitlement
6. active product-local role
7. required product-local grant
8. resource ownership and workflow invariants

No check bypasses another.

### Commercial-state transport — D-P12-03

| Aspect | State |
|---|---|
| Platform SoR for subscriptions/entitlements | **Implemented** in Platform DB |
| POS consumption today | **Implemented** and **provisional**: Dev/Testing injects commercial facts via headers such as `X-Pos-Subscription-Status` and `X-Pos-Feature-Grants` (plus org/actor headers). Production paths fail closed for these commercial headers. |
| Live Platform→product entitlement projection / API as the sole product gate | **Documented intent** / **Unresolved** — do not invent a final transport in product work |
| Direct product EF/SQL against Platform tables | **Forbidden** (**Required**) |

Future products must preserve “no direct Platform table reads” while obtaining commercial facts through an approved contract. Until D-P12-03 is closed, treat header-style Dev gates as **provisional**, not the production design.

### Production authentication — R-091

**Unresolved.** Do not claim production-secure identity. Do not invent fake production login.

---

## 6. Financial boundary

| Money | Owner | Binding |
|---|---|---|
| SaaS subscription / billing payments | Platform | **Implemented** / **Required** |
| Product operational money | That product | **Required** |

**Example (POS — Implemented):** sales tenders, customer/utang payments, expenses, cashier shift cash movements, returns/refunds, Product-Based Utang ledgers — all stay in the POS database and must **never** become Platform SaaS billing records.

Future products define their own operational financial model. Do not copy POS money entities into another product.

---

## 7. Shared versus product-specific

### May be shared (technical)

Contracts/DTO primitives; design-system primitives; localization infrastructure; security abstractions; observability conventions; deployment conventions; test utilities; common infrastructure abstractions (e.g. DesignSystem, BackupRestore, Deployment helpers).

### Must not be shared as authoritative state

Product entities/tables; operational workflows; balances/ledgers; product role assignments; product-specific lifecycle state; mega shared-domain libraries.

Two verified consumers + product-neutral design remain the bar for new shared packages.

---

## 8. Deployment and versioning

Architectural model (**Implemented** for Platform + POS pilot images; **Required** for future products):

- one Platform deployable (API; Admin as Platform surface)
- one independently versioned image per product
- one persistent database per product
- deploy only licensed/subscribed products
- customer-specific **configuration**, never customer-specific source forks
- immutable versioned images
- independent upgrade/rollback per product where compatibility allows

Do not create Dockerfiles or Compose profiles from this reference alone. Production TLS and readiness remain open portfolio risks.

---

## 9. Product folder and documentation expectations

### Source tree (**Required** pattern)

```text
src/Products/<ProductName>/
├── … Domain, Application, Infrastructure, Api, clients, UI as authorized …
└── Docs/                    # intended product doc root — see D-P12-02
```

### Documentation categories (**Required** content; templates in P12-WP03)

product definition; architecture; security; authorization matrix; development plan; phase roadmap; testing strategy; FILE-MANIFEST; reports; deployment notes as needed.

### D-P12-02 — per-product docs root

| Decision | Resolution |
|---|---|
| Intended root | `src/Products/<ProductName>/Docs/` |
| Status | **Required** for **new** products; templates arrive in **P12-WP03** |
| Existing POS | Historical portfolio docs under `docs/` remain valid; do not mass-migrate POS docs in WP02 |
| Alternate `docs/products/...` | **Not authorized** as the primary root |

Do **not** create `_ProductTemplate` or new product folders from this WP.

### Shared foundation docs

```text
docs/Product-Foundation/
├── README.md
└── exits-product-foundation-reference.md   # this file (authoritative)
```

---

## 10. Context-loading rule

For a product work package, read **only**:

1. `.cursor/rules/exits-workflow.mdc`
2. this Product Foundation reference
3. the active product’s architecture / security / roadmap / definition docs
4. the current work-package report or prompt
5. files directly required for implementation

Do **not** routinely scan:

- unrelated products (including full POS when building a different product)
- old phase completion reports
- full Platform history or unrelated migrations
- removed HealthCare product content
- completed product implementation history unless directly required

Before expanding context, state the reason (contract reference, project dependency, compile/test failure, architecture invariant, or approved shared component).

Permanent Cursor rule packaging for this policy is **P12-WP04** — until then, this section is binding documentation.

---

## 11. Bootstrap readiness checklist

Guidance only — not scaffold implementation.

- [ ] Product name and Platform product code/identifier  
- [ ] Independent subscription / plans / entitlements defined with Platform  
- [ ] Database name and schema chosen (separate from Platform and other products)  
- [ ] Organization boundary (Platform Org Guid references only)  
- [ ] Product-local roles and grants defined  
- [ ] Operational-money model defined (not SaaS billing)  
- [ ] API and UI ownership clear  
- [ ] Deployable image boundary planned  
- [ ] Documentation root under `src/Products/<Name>/Docs/`  
- [ ] Security/privacy classification (including PHI: default none)  
- [ ] Explicit exclusions listed  
- [ ] Material missing decisions reported to product owner — do not invent  

---

## 12. Examples and anti-patterns

### Correct (**Example**)

- Platform + POS  
- Platform + Loan (separate DB, roles, money)  
- Platform + POS + Loan (independent subscriptions)  
- Separate databases and product-local roles  

### Incorrect (**Required** prohibitions)

- Product directly querying Platform tables  
- Shared operational database across products  
- Platform role automatically granting POS (or other product) operational permissions  
- One subscription unlocking every product  
- Customer-specific source forks  
- Copying POS domain entities into Loan/Pawnshop/BNPL  
- Storing operational money in Platform SaaS billing tables  
- Treating Dev headers as production authentication  

---

## 13. Open decisions (preserved)

| ID | Current state | Impact | Future decision point |
|---|---|---|---|
| **D-P12-01** | **Closed** — this path/name is authoritative | Context loading uses this file | — |
| **D-P12-02** | **Closed** for intent — `src/Products/<Name>/Docs/`; POS historical docs stay under `docs/` | New products use product Docs root | P12-WP03 templates |
| **D-P12-03** | **Open** — POS uses provisional Dev commercial headers; final Platform→product transport unresolved | Do not invent production transport | Dedicated authz/commercial WP or first new-product integration WP |
| **R-091** | **Open** — no production JWT/passwords/MFA/SSO | Honest Dev/Testing vs Production language | Production auth roadmap |
| **D-P12-04** | **Open** — stale engineering matrix hygiene | Prefer incremental updates | Maintainers / later Phase 12 WPs |

---

## 14. Scope gate

Unless behavior is explicitly defined by the active product’s authoritative docs or an established repository contract, **do not invent it**. Stop and report the missing product-owner decision.
