# Pinoy Pawn Manager — Product Definition

> Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)  
> Unresolved items → [risks-and-decisions.md](risks-and-decisions.md). Do not invent policy or law.

| Field | Value |
|---|---|
| Product display name | Pinoy Pawn Manager (**PPM-D-00-01** Provisionally Approved for Implementation — not final marketing) |
| Short code | PPM |
| Platform product code | `pinoy-pawn-manager` (**PPM-D-00-02** Provisionally Approved for Implementation — not final marketing) |
| Product directory | `src/Products/PinoyPawnManager/` (**PPM-D-00-03** Provisionally Approved for Implementation — not final marketing) |
| Docs root | `src/Products/PinoyPawnManager/Docs/` |
| Status | **PPM-01 complete** — implementation scaffold only; no operational domain |
| Last updated | 2026-08-27 |
| Implementation present | Scaffold only (Domain / Application / Infrastructure / Api / UnitTests) — no DbContext, migrations, or pawn entities |

## Purpose and users

- **Purpose:** Independently subscribed ExItS product for **pawnshop / collateral-backed lending operations**: identify customer → inspect & capture pledged item → appraise → offer terms → create pawn agreement/ticket → take item into custody → release funds → support maturity, renewal, redemption, and (when authorized) unredeemed disposition workflows.
- **Target organizations:** Independently subscribed pawn/collateral-lending organizations. Multi-branch is intended; single-branch orgs may use one default branch.
- **Target users:** Organization staff via product-local grants/presets (**PPM-D-00-18** Open). Customers may later use ExItS Personal as a **presentation** surface only; PPM remains operational authority for pawn data.

Pinoy Pawn Manager is a **separate first-class ExItS SaaS product**.

```text
ExItS Platform
├── PinoyBusinessPOS
├── PinoyLoanManager
├── PinoyBuyNowPayLater (future / separate)
├── PinoyServicePro
├── PinoyPawnManager
└── future products
```

## What PPM is not

| Not this | Why |
|---|---|
| A PLM feature | PLM is unsecured/general lending without mandatory physical pawn custody |
| A POS feature | POS owns retail inventory/sales; pledged items are not ordinary stock while pledged |
| BNPL with collateral | BNPL finances a purchase; goods go **to** the customer. PPM takes goods **into** custody |
| Generic inventory only | Custody + appraisal + pawn obligation + redemption dominate |
| A duplicate Platform identity system | Platform owns ExItS identity/auth |
| Automatic legal authorization | Operating a pawnshop requires separate licensing/regulatory compliance |

## Platform integration

| Concern | Owner | Notes |
|---|---|---|
| Identity / production auth | Platform | Portfolio **R-091** / honesty **D-P12-05** apply |
| Organizations / memberships | Platform | PPM stores `OrganizationId` as Guid/contract only |
| Branches | Platform facts + product usage | PPM records `BranchId` on operational records; branch vaults are PPM-owned |
| Catalog / plans / subscription | Platform | Independent subscription required; **Local Validation / Dev** fixture via `EnsurePpmLocalValidationCatalog` (PPM-01) — not full production commercial registration |
| Entitlements | Platform facts | **D-P12-03** Open — no Platform table reads |
| SaaS billing | Platform | Never store pawn operational money as Platform SaaS payments |
| Pawn operations / custody / money | **PPM** | Not implemented (scaffold only) |

## Domain ownership matrix

### Platform owns

- ExItS identity (Personal / Organization staff model)
- Organization identity and memberships
- SaaS product catalog, plans, subscriptions
- Entitlement / product access facts (transport TBD D-P12-03)
- SaaS billing
- Platform administration
- Platform-level audit for Platform actions

### PPM owns

- Pawn customer references / profile extensions (not a second login identity)
- Pledged item / collateral records
- Appraisal and appraisal history
- Item photos and identifying evidence (authorized retention)
- Valuation / offer records
- Pawn agreement / pawn ticket (immutable historical snapshots)
- Principal and contractual charges (policy Open)
- Maturity, renewal, redemption workflows
- Payments against pawn obligations
- Physical custody, storage locations, movements, release
- Maturity/default/unredeemed operational status
- Unredeemed disposition workflow **inside PPM** (legal eligibility Open)
- PPM operational audit and reports
- PPM-specific authorization grants

### Explicitly not PPM-owned by default

| Concern | Owner |
|---|---|
| Normal retail product catalog / on-hand inventory | PinoyBusinessPOS / Commerce |
| Ordinary POS sales / checkout | POS |
| Unsecured installment loan engines | PLM |
| BNPL purchase financing | BNPL |
| Platform session cookies / Personal auth | Platform |

## Boundaries (required intent — not implemented)

- [x] Independent product subscription intent
- [x] Separate logical database proposed `ExItS_PinoyPawnManager` (**PPM-D-00-04** Open)
- [x] No direct Platform / POS / PLM / BNPL database reads; no cross-product FKs
- [x] Trusted org + product + branch context enforced server-side (when implemented)
- [x] PHI default **none** unless separately authorized
- [x] No customer-specific source forks
- [x] Pledged item ≠ POS inventory while pledged
- [x] Payment ≠ physical release
- [x] Custody movement history required
- [x] Legal authorization **not** claimed

## Surfaces (planning)

| Surface | Ownership | Notes |
|---|---|---|
| PPM API | Product | Scaffold host + health only (PPM-01); no operational endpoints |
| Organization Web / PWA | Product | Primary ops UI; **online-only** mutations initially — not created in PPM-01 |
| Platform Admin | Platform | Catalog/subscription only — not pawn ops UI |
| ExItS Personal | Platform presentation | Optional future ticket/status view (**PPM-15**) |
| MAUI / native offline | Deferred | Separate architecture decision if ever needed |

## Canonical operational flow

```text
Customer arrives
→ Customer identified (Platform Person and/or PPM customer reference)
→ Item presented for pledge
→ Item inspected; details/photos captured
→ Item appraised; appraised value recorded
→ Loan offer calculated/proposed (policy Open)
→ Terms disclosed; customer accepts
→ Pawn agreement/ticket created (snapshot)
→ Item moves into pawnshop custody
→ Cash/authorized funds released
→ Pawn loan ACTIVE
→ Later: REDEEM | RENEW/EXTEND | FAIL TO REDEEM (then matured/unredeemed/disposition per policy+law)
```

Detail: [Product/pawn-transaction-model.md](Product/pawn-transaction-model.md).

## Comparison summary

| Product | Essence |
|---|---|
| **PLM** | Financing / repayment; physical pawn custody **not** the core domain |
| **BNPL** | Buy now → goods to customer → finance the purchase |
| **POS** | Retail inventory and sales |
| **PPM** | Pledge → custody → appraisal → pawn obligation → redeem/renew/dispose |

## Exclusions (still true after PPM-01)

- No DbContext, migrations, or operational database
- No pawn operational domain entities
- No Organization Web / PWA / MAUI projects
- No fixed interest rates, LTV %, grace days, or auction schedules
- No AI valuation engine
- No claim of Philippine pawnshop regulatory compliance
- No direct POS inventory writes
- No PLM entity reuse as pawn tickets
- No full production commercial catalog registration beyond Local Validation / Dev fixture

## Related docs

- [architecture.md](architecture.md)
- [Architecture/plm-boundary.md](Architecture/plm-boundary.md)
- [Architecture/bnpl-boundary.md](Architecture/bnpl-boundary.md)
- [Architecture/pos-commerce-boundary.md](Architecture/pos-commerce-boundary.md)
- [Compliance/philippines-regulatory-review.md](Compliance/philippines-regulatory-review.md)
- [Reports/PPM-01-product-scaffold-platform-registration.md](Reports/PPM-01-product-scaffold-platform-registration.md)
