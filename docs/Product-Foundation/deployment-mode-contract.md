# Deployment Mode Contract

**Status:** Authoritative **planning** guidance (EXITS-ARCH-01). Not implemented.
**Decisions:** **D-HOST-04**, **D-HOST-05**, **D-HOST-08**
**Index:** [hosting-and-deployment-operating-model.md](hosting-and-deployment-operating-model.md)

---

## 1. Three architectural modes

| Mode | Name | Role |
|---|---|---|
| **A** | Hosted multi-tenant SaaS | **Primary / default** (**D-HOST-01**) |
| **B** | Dedicated single-tenant hosting | Optional, justified customers (**D-HOST-02**) |
| **C** | Customer on-prem | Supported **special** mode (**D-HOST-03**); topology **D-P14-01** |

Do not claim any hosted mode is implemented today.

---

## 2. Same product, different placement

Do **not** create separate editions such as:

- POS-Cloud codebase
- POS-OnPrem codebase
- POS-Enterprise codebase

Instead:

```text
PinoyBusinessPOS
        |
same product architecture
same core code
same contracts
same migration model
        |
configuration / deployment differences
        |
 +------+----------+
 |                 |
Hosted          On-prem
 |                 |
Shared stamp    Customer host
```

The same rule applies to Pinoy Loan Manager, future Pawnshop, and future products.

Customer-specific behavior must be **configuration / entitlement** driven where appropriate.

**Never** customer-specific source forks (**D-HOST-04**).

Do **not** create `CustomerA` / `CustomerB` / `CustomerC` product branches as architecture.

---

## 3. Data ownership is stable

Deployment mode does **not** change data ownership (**D-HOST-05**).

| Placement | POS operational data owner | PLM operational data owner | Platform data owner |
|---|---|---|---|
| Hosted shared stamp | PinoyBusinessPOS | Pinoy Loan Manager | Platform |
| Dedicated environment | PinoyBusinessPOS | Pinoy Loan Manager | Platform |
| Customer on-prem | PinoyBusinessPOS | Pinoy Loan Manager | Platform |

No cross-product foreign keys. No direct product reads of Platform tables. No direct Platform reads of product operational tables.

---

## 4. Unified Platform Admin

Preserve **D-SCALE-01**.

Do **not** create POS Platform Admin, PLM Platform Admin, or Pawnshop Platform Admin as separate applications.

Product-specific Platform administration appears as **modules** inside one unified admin experience, for example:

```text
Products
  ├── POS
  ├── Pinoy Loan Manager
  └── Pawnshop
Subscriptions
Entitlements
Billing
Usage
Organizations
Tenant Placement
Audit
Support
Operations
```

Physical backend modules may later scale independently. Ordinary product operations stay in product organization UIs, not Platform Admin.

---

## 5. Independent Product Application Planes

PinoyBusinessPOS, Pinoy Loan Manager, future Pawnshop, and future products remain independent application planes.

Each owns: operational domain, workflows, operational authorization, API, organization-facing UI, operational financial state, operational audit, product database authority, scaling profile.

---

## 6. Release management

All deployment modes should use:

- versioned releases
- explicit compatibility contracts
- controlled migrations
- rollback planning
- release evidence

**Forbidden:** automatic `Database.Migrate()` on Production application startup paths (unchanged **D-P14-03**).

---

## 7. Version compatibility (**D-HOST-08**)

Independent deployment must not become uncontrolled version drift.

Future releases may need supported compatibility windows such as:

- Platform version
- Product version
- API contract version
- DB schema version
- client version

Do **not** choose numeric compatibility windows now. Record the **requirement** only.
