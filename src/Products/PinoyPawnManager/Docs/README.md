# Pinoy Pawn Manager — Product Documentation

Authoritative product docs for **Pinoy Pawn Manager** (short code **PPM**).

Always load with:

1. `.cursor/rules/exits-workflow.mdc`
2. `.cursor/rules/exits-product-context.mdc`
3. `docs/Product-Foundation/exits-product-foundation-reference.md`
4. Docs in this folder
5. The active work-package prompt/report
6. Files required for the task only

| Field | Value |
|---|---|
| Display name | Pinoy Pawn Manager (**PPM-D-00-01** Provisionally Approved for Implementation — not final marketing) |
| Short code | PPM |
| Platform product code | `pinoy-pawn-manager` (**PPM-D-00-02** Provisionally Approved for Implementation — not final marketing) |
| Product directory | `src/Products/PinoyPawnManager/` (**PPM-D-00-03** Provisionally Approved for Implementation — not final marketing) |
| Documentation root | `src/Products/PinoyPawnManager/Docs/` (D-P12-02) |
| Status | **PPM-01 complete** — implementation scaffold present; **no** operational domain |
| Implementation present | **Scaffold only** (Domain / Application / Infrastructure / Api / UnitTests) — no DbContext, migrations, or pawn entities |
| Last updated | 2026-08-27 |

Pinoy Pawn Manager is a **separate first-class ExItS SaaS product**. It is **not** a PinoyLoanManager module, **not** a PinoyBusinessPOS feature, **not** BNPL with collateral, and **not** ordinary retail inventory management.

```text
ExItS Platform
├── PinoyBusinessPOS
├── PinoyLoanManager
├── PinoyBuyNowPayLater (future / separate)
├── PinoyServicePro
├── PinoyPawnManager
└── future products
```

**Legal honesty:** Software capability does **not** equal pawnshop licensing or regulatory authorization. See [Compliance/philippines-regulatory-review.md](Compliance/philippines-regulatory-review.md). **LEGAL_AUTHORIZATION_CLAIMED=NO**.

---

## Permanent product principles

| Principle | Value |
|---|---|
| `PPM_FIRST_CLASS_PRODUCT` | YES |
| `PPM_IS_PLM_MODULE` | NO |
| `PPM_IS_POS_MODULE` | NO |
| `PPM_IS_BNPL_MODULE` | NO |
| `PPM_OWNS_PAWN_COLLATERAL` | YES |
| `PPM_OWNS_CUSTODY` | YES |
| `POS_OWNS_NORMAL_RETAIL_INVENTORY` | YES |
| `DIRECT_POS_DB_ACCESS` | NO |
| `DIRECT_PLM_DB_ACCESS` | NO |
| `DIRECT_BNPL_DB_ACCESS` | NO |
| `PAWN_ITEM_IS_NORMAL_POS_INVENTORY_WHILE_PLEDGED` | NO |
| `PHYSICAL_RELEASE_SEPARATE_FROM_PAYMENT` | YES |
| `CUSTODY_HISTORY_REQUIRED` | YES |
| `LEGAL_AUTHORIZATION_CLAIMED` | NO |
| Implementation | Scaffold only — no operational pawn domain |
| Web/PWA runtime (initial) | **ONLINE-ONLY** for financial and custody mutations |

---

## Canonical documents

| Doc | Description |
|---|---|
| [product-definition.md](product-definition.md) | Purpose, ownership matrix, boundaries, exclusions |
| [architecture.md](architecture.md) | System shape, persistence, surfaces, state machines overview |
| [security.md](security.md) | Threats, privacy, evidence, least privilege |
| [authorization-matrix.md](authorization-matrix.md) | Capability/grant planning matrix |
| [development-plan.md](development-plan.md) | Delivery buckets and test gates |
| [roadmap.md](roadmap.md) | PPM-00 … PPM-16 packages |
| [risks-and-decisions.md](risks-and-decisions.md) | `PPM-D-00-*` / `PPM-R-00-*` register |
| [FILE-MANIFEST.md](FILE-MANIFEST.md) | Path inventory |
| [Reports/PPM-01-product-scaffold-platform-registration.md](Reports/PPM-01-product-scaffold-platform-registration.md) | PPM-01 closeout |

### Domain folders

| Folder | Focus |
|---|---|
| [Product/](Product/README.md) | Pawn transaction, appraisal, ticket, maturity, renewal, redemption, disposition, reporting |
| [Custody/](Custody/README.md) | Custody states, storage, movement, release, discrepancies |
| [Architecture/](Architecture/README.md) | Platform / PLM / BNPL / POS boundaries, idempotency, PWA policy |
| [Security/](Security/README.md) | Grants, custody security, audit, privacy |
| [Compliance/](Compliance/README.md) | Philippine regulatory review (open questions) |
| [Decisions/](Decisions/README.md) | ADRs (when closed) |
| [Phases/](Phases/README.md) | Phase index (roadmap is canonical) |
| [Operations/](Operations/README.md) | Operating notes (planning) |
| [Reports/](Reports/README.md) | Work-package reports |
| [Validation/](Validation/README.md) | Readiness checklists |

---

## Core business idea

A **pawn transaction** is collateral-secured lending where the pawnshop takes a **physical pledged item into custody**, records appraisal evidence, discloses terms, releases money, and later supports **redemption**, **renewal/extension**, or **unredeemed disposition** according to configured policy and applicable law (law timing = open decisions).

Payment completion and **physical item release** are related but **separate** transitions.

---

## Agent instructions

- **PPM-01 is complete** (scaffold + Local Validation / Dev Platform registration). Do not invent operational pawn domain without PPM-02+ authorization.
- Do not invent closed legal rates, grace periods, auction rules, or licensing claims.
- Prefer stable IDs (`PPM-D-00-XX`, `PPM-R-00-XX`, portfolio `R-…` / `D-…`).
- Do not copy PLM loan entities or POS inventory entities as pawn collateral.
- Treat this Docs tree as authoritative product intent.
- Next package: **PPM-02** (explicit authorization required).
