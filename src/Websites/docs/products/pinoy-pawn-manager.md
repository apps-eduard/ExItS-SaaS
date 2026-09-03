# Product Truth: Pinoy Pawn Manager

> Source: `src/Products/PinoyPawnManager/Docs/product-definition.md` and related docs.
> Implementation status as of 2026-09-03 code inspection.

---

## Product Identity

| Field | Value |
|---|---|
| Product display name | Pinoy Pawn Manager (provisionally approved — PPM-D-00-01; not final marketing name) |
| Short code | PPM |
| Platform product code | `pinoy-pawn-manager` (provisionally approved — PPM-D-00-02; not final) |
| Status | **IN DEVELOPMENT (scaffold only)** — PPM-01 scaffold complete; no operational domain |
| Implementation | Api, Application, Domain, Infrastructure, UnitTests scaffold; no DbContext, no migrations, no pawn entities |

---

## What the Product Is Intended to Be

From product documentation:

- Independently subscribed ExItS product for **pawnshop / collateral-backed lending operations**
- Intended workflow: identify customer → inspect pledged item → appraise → offer terms → create pawn agreement/ticket → take item into custody → release funds → support maturity, renewal, redemption, and unredeemed disposition
- Customers may later use ExItS Personal as presentation surface only; PPM remains operational authority for pawn data
- Multi-branch support intended
- Staff grants/presets open (PPM-D-00-18)

**Scaffold exists. No pawn operations are implemented.**

---

## Confirmed Capabilities

**None.** Only project scaffold exists.

---

## Marketing Classification

**IN DEVELOPMENT** — Scaffold only. No operational capability.

- Safe messaging: "Pawnshop management for Filipino pawn operators. In development."
- Do not describe pawn agreements, appraisal workflows, or custody features as available.

---

## Prohibited Claims

- Operating a pawnshop requires separate Philippine licensing and regulatory compliance. Do not imply ExItS handles licensing.
- Do not describe appraisal, collateral management, or pawn ticket workflows as available.
- Display name and product code require final Product Owner confirmation.
- Do not promise a release date.
