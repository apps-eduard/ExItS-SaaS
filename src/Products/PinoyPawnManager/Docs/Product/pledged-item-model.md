# Pledged Item Model

> Index: [README.md](README.md)  
> Custody: [../Custody/README.md](../Custody/README.md)  
> Decisions: [../risks-and-decisions.md](../risks-and-decisions.md)

| Field | Value |
|---|---|
| Product | Pinoy Pawn Manager (PPM) |
| Status | PPM-00 planning |
| Implementation | **None** |
| Last updated | 2026-08-27 |
| LEGAL_AUTHORIZATION_CLAIMED | **NO** |

A **pledged item** (collateral) is a physical thing presented for pawn, inspected, appraised, and held in **PPM custody** while a pawn obligation is open. It is **not** PinoyBusinessPOS retail inventory while pledged ([PPM-R-00-02](../risks-and-decisions.md)).

---

## Core rule

| Statement | Value |
|---|---|
| `PAWN_ITEM_IS_NORMAL_POS_INVENTORY_WHILE_PLEDGED` | **NO** |
| Owner of pledged-item operational record | **PPM** |
| Owner of normal retail catalog / on-hand stock | **POS / Commerce** |
| Path into retail stock | Only via **authorized disposition handoff** ([unredeemed-and-disposition-model.md](unredeemed-and-disposition-model.md), [PPM-D-00-15](../risks-and-decisions.md)) |

Do not create POS SKUs, stock quantities, or barcode retail rows for items merely because they entered a vault.

---

## Planning concepts (not a schema)

| Concept | Intent |
|---|---|
| `PledgedItemId` | PPM-owned id |
| `OrganizationId` / `BranchId` | Scoping; holding branch for custody |
| Category | Configurable per org ([PPM-D-00-05](../risks-and-decisions.md)) |
| Description | Free-text operational description |
| Identifying attributes | Category-specific fields (see examples) |
| Serial / IMEI / hallmark / certificate refs | When applicable; uniqueness policy Open |
| Condition notes | Visible wear, defects, missing parts |
| Photo / media evidence refs | Required intent for meaningful items; retention [PPM-D-00-19](../risks-and-decisions.md) |
| Intake staff / timestamps | Audit |
| Linked appraisal(s) | [appraisal-model.md](appraisal-model.md) |
| Linked ticket / transaction | [pawn-transaction-model.md](pawn-transaction-model.md) |
| Custody state + location | Machine B ([../Custody/custody-state-model.md](../Custody/custody-state-model.md)) |
| Identifying snapshot | Frozen into agreement at binding time |

**Custody history is required.** Current bin/shelf alone is insufficient ([../Custody/custody-movement.md](../Custody/custody-movement.md)).

---

## Categories — configurable

Accepted collateral categories are **org-configurable** ([PPM-D-00-05](../risks-and-decisions.md)). None are assumed legally mandatory by ExItS.

Planning candidate categories (examples only):

| Category (example) | Typical identifying attributes (examples) |
|---|---|
| Jewelry (gold/silver) | Metal type, karat claim, weight claim, stones description, hallmark notes |
| Watches | Brand claim, model claim, serial if present, condition |
| Mobile phones | Brand, model, IMEI/serial, storage, condition, accessories present |
| Laptops / tablets | Brand, model, serial, specs summary, condition |
| Power tools / appliances | Brand, model, serial, condition |
| Documents / certificates as collateral | **High legal risk** — treat as restricted until compliance review; do not assume allowed |
| Vehicles | Generally **out of MVP** unless separately authorized (title/registration complexity) |

Orgs may disable categories. PPM software capability to record a category ≠ legal permission to accept that collateral in a jurisdiction ([PPM-D-00-20](../risks-and-decisions.md)).

---

## Intake workflow (conceptual)

```text
Item presented
→ Staff selects category
→ Capture description + identifying attributes
→ Capture photos / condition notes
→ Optional serial uniqueness check (policy Open)
→ Item enters custody RECEIVING
→ Appraisal (manual)
→ Continues on pawn transaction machine A
```

Initial Web/PWA: intake mutations that bind custody are **ONLINE-ONLY**.

---

## Evidence and immutability

| Rule | Intent |
|---|---|
| Intake photos are evidence | Do not silently replace after ticket activation |
| Corrections | Append correction events; never erase prior evidence silently ([../Custody/loss-damage-discrepancy.md](../Custody/loss-damage-discrepancy.md)) |
| Snapshot at agreement | Identifying description + evidence refs frozen on ticket |
| Category rename later | Does not rewrite historical snapshots |

---

## Relationship to appraisal and ticket

- One pledged item may have **multiple appraisal versions** over time; the agreement binds a specific appraisal snapshot.  
- One open pawn ticket typically references one primary pledged item in MVP planning (multi-item tickets = Open / future).  
- Appraised value lives on appraisal; principal on offer/ticket — not conflated.

---

## What pledged item is not

| Not | Why |
|---|---|
| POS `Product` / inventory row | Different ownership and sale semantics |
| PLM collateral attachment on unsecured loan | PLM does not own PPM custody domain |
| BNPL financed goods delivered to buyer | Opposite custody direction |
| Disposable draft photo album without audit | Custody + dispute readiness require history |

---

## Multi-branch

Items are held at a branch vault hierarchy ([../Custody/storage-location-model.md](../Custody/storage-location-model.md)). Moving an item to another branch is a **controlled transfer** ([PPM-D-00-16](../risks-and-decisions.md)), never an implicit side effect of staff login branch switch.

---

## Exclusions

- No inventory quantity / COGS model for pledged goods while pledged  
- No AI visual recognition / auto-category as foundation  
- No claim that accepting a category is PH-legal for every org  
