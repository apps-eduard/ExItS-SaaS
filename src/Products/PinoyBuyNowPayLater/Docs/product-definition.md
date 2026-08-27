# Pinoy Buy Now Pay Later — Product Definition

> Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)  
> Unresolved items → [risks-and-decisions.md](risks-and-decisions.md). Do not invent commercial or legal policy.

| Field | Value |
|---|---|
| Product name | Pinoy Buy Now Pay Later (display — **Provisionally Approved for Implementation**, BNPL-D-00-01) |
| Platform product code | `pinoy-buy-now-pay-later` (**Provisionally Approved for Implementation**, BNPL-D-00-02) |
| Docs root | `src/Products/PinoyBuyNowPayLater/Docs/` |
| Status | BNPL-01 Product Scaffold Complete; Financing Domain Not Started |
| Last updated | 2026-08-27 |
| Implementation present | Scaffold only (no financing entities) |

## Purpose and users

- **Purpose:** Independently subscribed ExItS product that finances **commerce purchases** with structured agreements, installment schedules, repayments, and financing lifecycle — while leaving catalog, inventory, and authoritative sale ownership with Commerce/POS.
- **Target organizations:** Retail and commerce organizations that already (or will) use PinoyBusinessPOS (or an approved future commerce surface) and want structured buy-now-pay-later financing as a separate product entitlement.
- **Target users / jobs:** Organization staff via Owner / Manager / Approver / Sales / Collector / Reporting **presets** backed by explicit grants (identifiers open — BNPL-D-00-18). Do not hard-code authorization to role names. Do not copy POS, PLM, or PSP grant sets. Customer/Personal surfaces may later present plans and repayments if authorized (BNPL-D-00-13).

BNPL is a **separate first-class ExItS SaaS product**.

```text
ExItS Platform
├── PinoyBusinessPOS
├── PinoyLoanManager
├── PinoyServicePro
├── Pinoy Buy Now Pay Later
└── future products
```

BNPL is **not**:

- a renamed POS
- a duplicated POS application
- a POS inventory database
- simply another Utang button
- a PLM skin
- a shared-table extension of POS
- a reason for BNPL to directly query POS tables

From UX, BNPL may feel like an extended commerce/payment experience. Architecturally it remains its own bounded product/domain.

## Platform integration

| Concern | Owner | Notes |
|---|---|---|
| Identity / production auth | Platform | R-091 open — do not claim production-secure auth. Keep Dev/Testing vs Production language honest (D-P12-05). |
| Organizations / account context | Platform | BNPL stores organization id as a Guid reference / contract only. |
| Branches | Platform org model + POS branch facts | Financed physical sales require originating BranchId; branch inventory is Commerce-owned. |
| Catalog / plans / subscription | Platform | **Required:** independent subscription for this product only. Catalog registration not done (BNPL-D-00-02). |
| Entitlements / commercial access | Platform facts | D-P12-03 commercial-state transport — do not invent. Platform entitlement does not replace BNPL product-local authorization. |
| SaaS billing payments | Platform | Never store BNPL operational financing money in Platform SaaS billing. |
| Operational financing / roles / money | **This product** | Not implemented. |

## Domain ownership matrix

| Concern | Platform | POS / Commerce | BNPL |
|---|---|---|---|
| ExItS identity / Personal user | Owns | References | References via contract |
| Organization / membership | Owns | References | References |
| Product subscription / entitlement | Owns | — | Consumes for access gate |
| SaaS billing | Owns | — | — |
| Product catalog / SKU / price list | — | Owns | Reads via approved contract (display/validation) |
| Branch inventory / stock levels | — | Owns | Must **not** copy; reads availability via contract |
| Stock movements / deduction | — | Owns | Must **not** perform |
| Authoritative commercial sale | — | Owns | Records CommerceSaleId; does not replace sale |
| Sale lines / quantities | — | Owns | Immutable financed-item snapshot at activation |
| BNPL application / eligibility / approval | — | — | Owns |
| Financing agreement / principal / schedule | — | — | Owns |
| Repayments / overdue / collections (BNPL) | — | — | Owns |
| Merchant settlement (BNPL) | — | — | Owns state (funding model **Open**, BNPL-D-00-08) |
| Business Utang | — | Owns | Separate domain — do not merge |
| PLM loans | — | — | Separate product — do not merge |
| BNPL audit / reports / local authz | — | — | Owns |

## Boundaries (checklist)

Recorded as **required intent**. Nothing below is implemented.

- [x] Independent product subscription — required intent
- [ ] Separate database `ExItS_PinoyBuyNowPayLater` — **Open** (BNPL-D-00-04); proposed name only; not created
- [x] No direct Platform / POS / PLM table reads; no cross-product FKs — required intent
- [x] No duplicate inventory ledger — required intent (BNPL-D-00-05)
- [x] Same Org + Branch + Product = same authoritative stock — required intent (BNPL-D-00-06)
- [x] Financing ACTIVE only after successful commerce sale — required intent (BNPL-D-00-07)
- [x] Immutable financed-purchase snapshot — required intent (BNPL-D-00-09)
- [x] No duplicated POS sale engine — required intent (BNPL-D-00-10)
- [ ] Product-local roles and grants defined — presets/intent recorded; identifiers **Open** (BNPL-D-00-18)
- [x] Trusted org + product context enforced server-side — required intent; not implemented
- [x] PHI / sensitive data: default **none** unless explicitly authorized
- [x] No customer-specific source forks

## BNPL vs Utang vs PLM (summary)

| | Business Utang (POS) | BNPL | Pinoy Loan Manager |
|---|---|---|---|
| Origin | Merchant store credit on sale | Financed commerce purchase | Loan / financing release |
| Structure | Informal / simple debt | Agreement + schedule + lifecycle | Loan domain + collections |
| Inventory | POS sale deducts stock | Commerce sale deducts stock; BNPL does not | Typically not retail inventory |
| Product | Part of POS / Personal experience | First-class product | First-class product |
| Merge domains? | **No** | **No** | **No** |

Shared technical primitives (schedules, decimal money math, idempotency patterns) may be evaluated later. Bounded domain ownership stays independent. Detail: [Product/commerce-and-financed-purchase-model.md](Product/commerce-and-financed-purchase-model.md).

## Surfaces (proposed)

| Surface | Ownership | Notes |
|---|---|---|
| BNPL API | Product | Product-owned API intended. No API project in BNPL-00 (BNPL-D-00-03). |
| Organization BNPL Web / PWA | Product | Primary merchant operations surface. Online-only mutations (BNPL-D-00-11). |
| POS checkout hand-off | POS UI + BNPL contracts | Payment method = BNPL → financing request → commerce completion |
| BNPL-first product browse | Product UI + Commerce contracts | Must invoke authoritative commerce sale — not a second sale engine |
| Customer / ExItS Personal | Platform presentation | Future plans/repayments if authorized (BNPL-D-00-13) |
| Platform Admin | Platform | SaaS administration only — not normal BNPL operations UI |

## Operational money

**Open** for fees, interest, settlement funding, early payoff, overdue fees (BNPL-D-00-08, BNPL-D-00-14–17).

Required / agreed direction:

- SaaS subscription money remains Platform-owned.
- Customer financing balances and repayments remain BNPL-owned operational money.
- Commerce sale tender/receipt remains Commerce-owned for the purchase event.
- Merchant settlement is a **separate** financial concern from customer outstanding balance.
- Authoritative money math: decimal, not binary floating-point.
- Do not invent regulated interest or licensing claims.

## Explicit exclusions (BNPL-00)

- No implementation code, projects, migrations, databases, catalog registration
- No real payment-provider integration
- No AI/credit-scoring claims
- No offline financing mutation queue
- No duplicate inventory or direct POS DB access
- No merging Utang or PLM domains into BNPL
- No production legal/compliance certification claims
