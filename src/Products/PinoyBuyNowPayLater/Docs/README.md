# Pinoy Buy Now Pay Later — Product Documentation

Authoritative product docs for **Pinoy Buy Now Pay Later** (short: **BNPL**; proposed product code `pinoy-buy-now-pay-later`).

Always load with:

1. `.cursor/rules/exits-workflow.mdc`
2. `.cursor/rules/exits-identity-model.mdc` (when identity/Personal linking is in scope)
3. `docs/Product-Foundation/exits-product-foundation-reference.md` (repo path)
4. Docs in this folder
5. The active work-package prompt/report
6. Files required for the task only

**Status:** BNPL-00 Documentation Foundation Complete; Implementation Not Started; Product Owner Approval Pending  
**Implementation present:** No  
**Documentation root:** `src/Products/PinoyBuyNowPayLater/Docs/` (D-P12-02)

BNPL is a **separate first-class ExItS SaaS product**, a sibling of PinoyBusinessPOS, PinoyLoanManager, and PinoyServicePro. It is not a POS module, not “Utang renamed,” not a PLM skin, not a shared-table extension of POS, and not a reason for BNPL to query POS tables directly.

```text
ExItS Platform
├── PinoyBusinessPOS
├── PinoyLoanManager
├── PinoyServicePro
├── Pinoy Buy Now Pay Later
└── future products
```

Permanent product principles:

```text
BNPL finances a commerce purchase.
Commerce (POS) owns inventory and the authoritative sale.
BNPL owns financing lifecycle, schedule, repayments, and BNPL audit.
Same Organization + Branch + Product = same authoritative stock.
Financing becomes ACTIVE only after successful commerce sale.
```

---

## Canonical documents

| Doc | Description |
|---|---|
| [product-definition.md](product-definition.md) | Purpose, ownership, boundaries, exclusions |
| [architecture.md](architecture.md) | System, data, surface, and isolation boundaries |
| [security.md](security.md) | Security, privacy, compliance posture |
| [authorization-matrix.md](authorization-matrix.md) | Access layers; role presets and grant intent |
| [development-plan.md](development-plan.md) | Delivery buckets and testing expectations |
| [roadmap.md](roadmap.md) | Phases and work packages |
| [risks-and-decisions.md](risks-and-decisions.md) | Open risks and `BNPL-D-00-XX` decisions |
| [FILE-MANIFEST.md](FILE-MANIFEST.md) | Path inventory |

Focused planning documents:

| Doc | Description |
|---|---|
| [Product/commerce-and-financed-purchase-model.md](Product/commerce-and-financed-purchase-model.md) | Commerce vs financing ownership; purchase flow |
| [Product/customer-model.md](Product/customer-model.md) | Customer / Personal identity references |
| [Product/financing-lifecycle.md](Product/financing-lifecycle.md) | State machine |
| [Product/eligibility-and-approval.md](Product/eligibility-and-approval.md) | Eligibility / offer / acceptance |
| [Product/installment-model.md](Product/installment-model.md) | Terms, schedule, rounding |
| [Product/repayment-model.md](Product/repayment-model.md) | Repayments and allocation |
| [Product/overdue-and-collections.md](Product/overdue-and-collections.md) | Overdue and collections baseline |
| [Product/merchant-settlement.md](Product/merchant-settlement.md) | Merchant settlement (open commercial model) |
| [Product/returns-cancellations-refunds.md](Product/returns-cancellations-refunds.md) | Cross-domain return coordination |
| [Product/reporting-baseline.md](Product/reporting-baseline.md) | Merchant / customer / audit reports |
| [Architecture/platform-integration.md](Architecture/platform-integration.md) | Platform identity and commercial contracts |
| [Architecture/commerce-pos-boundary.md](Architecture/commerce-pos-boundary.md) | POS entry paths and sale contracts |
| [Architecture/inventory-boundary.md](Architecture/inventory-boundary.md) | Shared authoritative stock rules |
| [Architecture/persistence-and-database-boundary.md](Architecture/persistence-and-database-boundary.md) | Separate DB isolation |
| [Architecture/api-and-contract-boundary.md](Architecture/api-and-contract-boundary.md) | APIs and contracts |
| [Architecture/failure-and-reconciliation.md](Architecture/failure-and-reconciliation.md) | Failure matrix |
| [Architecture/idempotency-model.md](Architecture/idempotency-model.md) | Idempotency and distributed safety |
| [Architecture/web-pwa-runtime-policy.md](Architecture/web-pwa-runtime-policy.md) | Online-only Web/PWA baseline |
| [Security/role-and-grant-baseline.md](Security/role-and-grant-baseline.md) | Presets and grant catalog intent |
| [Security/audit-and-history-baseline.md](Security/audit-and-history-baseline.md) | Operational audit |
| [Security/privacy-and-sensitive-data-baseline.md](Security/privacy-and-sensitive-data-baseline.md) | Privacy baseline |
| [Validation/BNPL-00-readiness-checklist.md](Validation/BNPL-00-readiness-checklist.md) | Docs-only readiness checklist |
| [Reports/BNPL-00-foundation-closeout.md](Reports/BNPL-00-foundation-closeout.md) | BNPL-00 closeout |

Category folders below are indexes only. They must not become a second source of truth.

---

## Category indexes

| Directory | Purpose |
|---|---|
| [Product/](Product/README.md) | **WHAT** — financing and commerce-coordination domain |
| [Architecture/](Architecture/README.md) | **HOW** — surfaces, persistence, contracts, failure |
| [Security/](Security/README.md) | Access, privacy, audit baselines |
| [Decisions/](Decisions/README.md) | Future ADRs — register is [risks-and-decisions.md](risks-and-decisions.md) |
| [Phases/](Phases/README.md) | Sequencing — points to [roadmap.md](roadmap.md) |
| [Reports/](Reports/README.md) | Work-package evidence |
| [Validation/](Validation/README.md) | Readiness / validation evidence |
| [Operations/](Operations/README.md) | Deployment and production operations (planning) |

Do not scatter BNPL product docs into the repository-root `docs/` tree unless the content is genuinely portfolio-wide.

---

## Identity (proposed)

| Item | Value | Status |
|---|---|---|
| Display name | Pinoy Buy Now Pay Later | Open (BNPL-D-00-01) |
| Short identifier | BNPL | Recorded for docs |
| Repository directory | `PinoyBuyNowPayLater` | Recorded (BNPL-D-00-03 — folder provisional until owner closes naming) |
| Product code / slug | `pinoy-buy-now-pay-later` | Open (BNPL-D-00-02) |
| Future database | `ExItS_PinoyBuyNowPayLater` | Open (BNPL-D-00-04) — planning name only; not created |

Phase-12 historical sketches used `BuyNowPayLater` / `ExItS_BuyNowPayLater`. Those names are **superseded as planning aliases** unless the Product Owner deliberately reverts. Prefer the **Pinoy\*** convention used by PLM and PSP.

---

## Ownership (summary)

| Layer | Owns |
|---|---|
| **Platform** | Identity, organizations, memberships, session context, product catalog/subscription/entitlement, SaaS billing, Platform Admin, Platform audit |
| **POS / Commerce** | Catalog, branch inventory, stock movements, authoritative commercial sale, sale lines, stock deduction, commerce receipts |
| **BNPL** | Financing application, eligibility/approval, agreement, financed-purchase snapshot, schedule, repayments, overdue/collections (product-local), merchant settlement state (when model decided), BNPL audit/reports/authorization |

Isolation: independent subscription; separate logical database; no cross-product FKs; no direct POS/PLM/Platform operational table reads; OrganizationId/BranchId/ProductId/SaleId as identifiers/contracts only; approved APIs only.

Authoritative text: [product-definition.md](product-definition.md) and [architecture.md](architecture.md).

---

## Explicit exclusions (BNPL-00)

No implementation exists. No solution/project creation, migrations, database creation, Platform catalog registration, real payment-provider integration, interest/fee policy invention, regulated lending claims, POS or PLM domain reuse by project reference, offline financing mutation queues, duplicate inventory, or production deployment.
