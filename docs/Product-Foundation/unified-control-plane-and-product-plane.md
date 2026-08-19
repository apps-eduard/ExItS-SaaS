# Unified Control Plane and Product Plane

**Status:** Authoritative **planning** guidance (EXITS-SCALE-00). Not implemented.
**Decisions:** **D-SCALE-01**, **D-SCALE-02**, **D-SCALE-10**
**Index:** [exits-scale-and-growth-architecture.md](exits-scale-and-growth-architecture.md)
**Foundation:** [exits-product-foundation-reference.md](exits-product-foundation-reference.md)

Do not treat this file as a claim that Platform, POS, or any future product currently supports millions of users.

---

## 1. One logical Platform Control Plane

ExItS has **one logical Platform Control Plane** (**D-SCALE-01**).

ExItS has **one unified Platform Admin experience**.

That remains the intended architecture even with:

- many ExItS products
- many organizations
- millions of registered users
- large transaction volumes

**One logical Platform Admin does not mean:**

- one process forever
- one server forever
- one database for everything
- one deployment unit forever
- one service forever

Platform Admin is a unified **administrative experience** over modular Platform capabilities. Backend modules may later scale independently. Do **not** create a separate Platform Admin application per product without a future documented reason.

---

## 2. Control-plane capabilities

Conceptual Platform Control Plane responsibilities (control-plane only):

- Identity / Users
- Organizations
- Product Catalog
- Plans
- Subscriptions
- Entitlements
- SaaS Billing
- Usage / Metering
- Provisioning
- Platform Audit
- Support / Operations
- Deployment / Tenant Placement metadata (future)
- future Platform capabilities

Platform responsibilities remain **control-plane** responsibilities. Platform does not own product operational workflows, product-local roles, or product operational money.

---

## 3. Product / application plane

Each ExItS product remains an independently owned **application plane** (**D-SCALE-02**).

Examples: PinoyBusinessPOS, Pinoy Loan Manager, future Pawnshop, future products.

Each product owns:

- operational domain
- product authorization
- product workflows
- product APIs
- product UI
- product operational money
- product database / data partition
- product-specific audit
- product-specific performance and scaling

One product must not require another product’s runtime to operate.

Product outages should not automatically become cross-product outages (**D-SCALE-10**). No architecture can promise zero cascading failures; this is a resilience **goal**.

---

## 4. ExItS Personal

ExItS Personal remains a customer-facing **Platform-owned** surface.

It may aggregate **authorized** experiences across products.

It must **not** become:

- a shared product operational database
- a shared loan ledger
- a shared POS ledger
- a shared product authorization authority

Product data displayed by Personal comes through **approved product contracts/APIs** only. Personal must not read product operational tables.

---

## 5. Platform Admin vs product organization admin

| Surface | Meaning | Examples |
|---|---|---|
| **Platform Admin** | ExItS SaaS / control-plane administration | subscriptions, entitlements, organizations, plans, billing, usage, platform audit, tenant placement |
| **Product Organization Admin** | the customer organization’s operational administration | POS Organization Web (merchant POS operations); PLM Organization Web (lending operations); future Pawnshop Organization Web (pawn operations) |

Do **not** combine normal product operations into Platform Admin.

Product-specific **Platform** administration (catalog, plans, commercial configuration for that product) may appear as **modules inside the same unified Platform Admin experience**.

Conceptual future admin modules (still one UI):

- Organizations, Users, Products, Plans, Subscriptions, Entitlements
- Billing, Usage, Tenant Placement
- Support, Audit, Security Operations
- Deployment / Fleet Operations

---

## 6. Control-plane failure isolation

Desired resilience principle (**D-SCALE-10**):

A temporary Platform control-plane outage should **not** unnecessarily stop already-authorized product operations **where safe architecture permits continuity**.

**D-P12-03 remains OPEN.** This pack does **not** invent final entitlement caching, lease, or token behavior.

Future commercial-state transport must consider, without selecting a mechanism here:

- availability
- revocation
- stale authorization
- fail-closed security
- continuity
- expiry
- auditability

Exact mechanism remains a future Platform decision. Until then, do not treat Dev/Testing commercial headers as production design.

---

## 7. Platform billing and usage at scale

| Money | Owner |
|---|---|
| SaaS subscription / billing | Platform |
| Product operational money (POS tenders, loan ledgers, collector cash, future pawn tickets, …) | That product |

Product usage signals must eventually cross through **approved contracts/events**.

No product writes Platform billing tables.

No Platform writes product operational money tables.

Usage processing should be:

- retriable
- idempotent
- auditable
- reconcilable

Do **not** block authoritative product transaction completion on synchronous Platform billing unless an explicitly approved future invariant requires it.

Example (planning only):

```text
PLM Disbursement
        |
authoritative PLM transaction completes
        |
durable event / publication mechanism
        |
Platform usage processing
```

Platform billing must not require a cross-database transaction with a product.

**D-P12-03 remains open.**

---

## 8. Cost awareness

Scale-out architecture must consider cost per tenant and per product.

Future metrics may include:

- compute per product
- storage per product
- event volume
- large-tenant cost
- background jobs
- bandwidth
- observability volume

Do not let one product’s infrastructure cost silently subsidize another without Platform pricing awareness. This is a **planning** requirement, not a billing formula.

---

## 9. Security at scale (planning; not readiness)

Future needs (none claimed complete by this pack):

- production authentication
- MFA / admin hardening
- least privilege
- secret management and key rotation
- tenant isolation
- privileged support controls
- break-glass procedures
- immutable / high-integrity audit
- rate limiting and abuse detection
- session / token revocation

**R-091 remains open** until production authentication is accepted as Production-ready. Do not claim security readiness from this documentation pack.

### Privileged support

Platform support must **not** receive silent unrestricted product data access.

Future support access must be designed intentionally with concepts such as:

- explicit privileged grants
- reason
- time-bound access
- customer / organization context
- audit
- sensitive-data minimization

Do not implement that model in this package.
