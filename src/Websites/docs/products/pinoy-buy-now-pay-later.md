# Product Truth: Pinoy Buy Now Pay Later

> Source: `src/Products/PinoyBuyNowPayLater/Docs/product-definition.md` and related docs.
> Implementation status as of 2026-09-03 code inspection.

---

## Product Identity

| Field | Value |
|---|---|
| Product display name | Pinoy Buy Now Pay Later (provisionally approved — BNPL-D-00-01) |
| Platform product code | `pinoy-buy-now-pay-later` (provisionally approved — BNPL-D-00-02) |
| Status | **IN DEVELOPMENT (scaffold only)** — BNPL-01 scaffold complete; financing domain not started |
| Implementation | Api, Application, Domain, Infrastructure projects exist (scaffold); no financing entities, no DbContext, no migrations |

---

## What the Product Is Intended to Be

From product documentation:

- Independently subscribed ExItS product that finances **commerce purchases** with structured agreements, installment schedules, and repayments
- Catalog, inventory, and authoritative sale ownership remain with PinoyBusinessPOS (or approved future commerce surface)
- Role presets planned (identifiers open — BNPL-D-00-18)
- Customer/Personal surfaces may later present plans and repayments (BNPL-D-00-13 — authorized intent only)

**Scaffold exists. No financing logic is implemented.**

---

## Confirmed Capabilities

**None.** Only project scaffold exists.

---

## Marketing Classification

**IN DEVELOPMENT** — Scaffold only. No operational capability.

- Safe messaging: "Buy now, pay later for commerce businesses. In development."
- Do not describe financing features as available.
- Do not imply SEC or BSP licensing.
- Display name and product code are provisionally approved but not final.

---

## Prohibited Claims

- Do not describe installment plans, APR, or financing terms.
- Do not imply regulatory approval.
- Do not promise a release date.
- Display name requires final Product Owner confirmation before use in marketing.
