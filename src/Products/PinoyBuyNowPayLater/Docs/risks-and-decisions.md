# Pinoy Buy Now Pay Later — Risks and Decisions

> Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)  
> Close items only with evidence. Do not invent answers for portfolio-open or legal/commercial items.

| Field | Value |
|---|---|
| Product | Pinoy Buy Now Pay Later |
| Last updated | 2026-08-27 |

## Portfolio items (always preserve until closed upstream)

| ID | Type | Description | Current state | Impact | Decision point | Resolution criteria |
|---|---|---|---|---|---|---|
| R-091 | Risk | Production authentication gaps for portfolio claims | Open | No false production-secure claims | Portfolio auth roadmap | Real Platform auth shipped + evidenced for claimed scope |
| D-P12-03 | Decision | Commercial-state transport to products | Open / provisional | How BNPL learns subscription/entitlements without Platform table reads | Commercial/integration WP | Approved contract + implementation |
| D-P12-05 | Decision | Honest Dev/Testing vs Production language | Open (tied to R-091) | Risk of claiming production-secure identity | With R-091 | Dev/Testing shortcuts labeled; Production fail-closed |

## Accepted / decided baselines (BNPL-00)

| ID | Decision | Status |
|---|---|---|
| BNPL-D-00-05 | BNPL does not own duplicate inventory; no independent stock ledger | **Decided** |
| BNPL-D-00-06 | Same Organization + Branch + Product = same authoritative stock via Commerce | **Decided** |
| BNPL-D-00-07 | Financing becomes ACTIVE only after successful commerce sale | **Decided** |
| BNPL-D-00-09 | Financed purchase details require immutable snapshot | **Decided** |
| BNPL-D-00-10 | No duplicated POS sale engine; Path A and Path B converge | **Decided** |
| BNPL-D-00-11 | Web/PWA baseline is online-only for financial mutations | **Decided** |
| BNPL-D-00-12 | No direct cross-product database reads; approved APIs/contracts only | **Decided** |
| BNPL-D-00-21 | BNPL is a first-class separate ExItS product (not POS module / Utang / PLM skin) | **Decided** |
| BNPL-D-00-22 | Existing ACTIVE financing remains operable without POS for financing-independent ops | **Decided** |
| BNPL-D-00-23 | Server-side idempotency and reconciliation required for financial operations | **Decided** |
| BNPL-D-00-24 | Product availability display is not a stock reservation | **Decided** |

## Product decision register (`BNPL-D-00-XX`)

| ID | Question | Current direction | Status | What it blocks | Safe default until decided |
|---|---|---|---|---|---|
| BNPL-D-00-01 | Official product display name | **Pinoy Buy Now Pay Later** | Provisionally Approved for Implementation by Product Owner in BNPL-01 | Final marketing/public naming | Use approved display in scaffold/UI chrome; final marketing may still refine |
| BNPL-D-00-02 | Official Platform product code / slug | `pinoy-buy-now-pay-later` | Provisionally Approved for Implementation by Product Owner in BNPL-01 | Catalog, plans, subscription | Registered in ProductCode + Local Validation catalog (Dev/Testing) |
| BNPL-D-00-03 | Repository directory / project naming | `PinoyBuyNowPayLater` under `src/Products/` | Provisionally Approved for Implementation by Product Owner in BNPL-01 | Scaffold BNPL-01 | Directory and projects created |
| BNPL-D-00-04 | Final DB name / schema | `ExItS_PinoyBuyNowPayLater` + schema `bnpl` | Provisionally Approved / Implemented in BNPL-03 | Financing persistence later | Customer foundation only; no production auto-migrate |
| BNPL-D-00-08 | Merchant-funded vs platform-funded BNPL / settlement model | Must separate customer balance from merchant settlement | Open / Legal & Commercial Decision Required | BNPL-10 | Document both; implement neither |
| BNPL-D-00-13 | Personal / customer self-service MVP timing | Future capability | Open / Product Owner Decision Required | BNPL-13 | Staff-operated MVP |
| BNPL-D-00-14 | Term / frequency choices | Daily/weekly/monthly candidates possible | Open / Product Owner Decision Required | Future schedule generators | BNPL-05 stores explicit rows only; do not invent default terms |
| BNPL-D-00-15 | Fees / interest model | May be zero-interest merchant promo or fee-bearing | Open / Legal & Commercial Decision Required | Pricing, disclosures | No interest engine until policy |
| BNPL-D-00-16 | Credit-limit model | Per-customer / per-org limits possible | Open / Product Owner Decision Required | Eligibility | Manual approval path only |
| BNPL-D-00-17 | Early payoff / overdue fee / refund allocation rules | Required before production money | Open / Product Owner Decision Required | BNPL-08–11 | Record repayments principal-only until policy |
| BNPL-D-00-18 | Product-local grant / capability identifiers | includes `bnpl.plan.manage` | Implemented in BNPL-02; extended in BNPL-03/04/05 | Future grant persistence transport | Authorize by capability only |
| BNPL-D-00-19 | Retention / deletion / export policy | Financial history important | Open / Product Owner Decision Required | Privacy ops | Retain while org subscribed; no silent purge |
| BNPL-D-00-20 | Regulatory / licensing prerequisites | Technical capability ≠ legal authorization | Open / Legal Decision Required | Production claims | No license/compliance claims |
| BNPL-D-00-25 | Production payment channels for repayments | Cash / GCash / other | Open / Product Owner Decision Required | BNPL-08 providers | Manual recorded repayment first |
| BNPL-D-00-26 | Approval model (manual vs rules vs future risk engine) | Manual path implemented as safe default in BNPL-04 | Open / Product Owner Decision Required | Future automation | Manual approve/decline only until owner decides |
| BNPL-D-00-27 | Documentation baseline owner approval | BNPL-00 docs complete | Open / Product Owner Decision Required | Closing BNPL-00 as approved | Treat as draft-complete; Implementation Not Started |
| BNPL-D-00-28 | Phase-12 names `BuyNowPayLater` / `ExItS_BuyNowPayLater` | Prefer Pinoy\* alignment | Deferred / Owner may supersede | Naming consistency | Use PinoyBuyNowPayLater in this foundation |

## Product risks

| ID | Type | Description | Current state | Impact | Owner / decision point | Resolution criteria |
|---|---|---|---|---|---|---|
| BNPL-R-00-01 | Risk | Duplicate inventory drift | Mitigated in docs | Wrong stock / oversell | Architecture | Guards + Commerce-only stock mutations |
| BNPL-R-00-02 | Risk | ACTIVE financing without commerce sale | Mitigated in docs | Phantom debt / no goods | Lifecycle | State machine + orchestration tests |
| BNPL-R-00-03 | Risk | Domain merged with Utang or PLM | Mitigated in docs | Wrong product shape | Product design | Explicit boundaries; no project refs |
| BNPL-R-00-04 | Risk | False regulatory / licensing claims | Open vigilance | Legal/reputational | Product owner + counsel | No claim without verified authorization |
| BNPL-R-00-05 | Risk | Settlement model chosen silently | Open | Wrong commercial/legal path | BNPL-D-00-08 | Explicit owner/legal decision |
| BNPL-R-00-06 | Risk | Cross-product data leakage | Mitigated in docs | Isolation breach | Architecture guards | Architecture tests when code exists |
| BNPL-R-00-07 | Risk | Ambiguous network outcomes create duplicates | Mitigated in docs | Double sale / double finance | Idempotency model | Idempotency + reconcile tests |
| BNPL-R-00-08 | Risk | POS outage blocks all BNPL ops | Mitigated in docs | Operational fragility | Dependency matrix | Financing-independent ops documented |

## Instructions

- Prefer stable IDs (`BNPL-D-00-XX`, `BNPL-R-00-XX`, portfolio `R-…` / `D-…`).
- “Closed” / “Decided” requires repository or operator evidence; decided architecture baselines above are binding for implementation agents.
- A `TBD` may remain in docs only when linked to an explicit `BNPL-D-00-XX` decision.
- Never disguise an assumption as an approved commercial or legal decision.
