# Storage Location Model

> Index: [README.md](README.md)  
> Movements: [custody-movement.md](custody-movement.md)  
> Decisions: [../risks-and-decisions.md](../risks-and-decisions.md)

| Field | Value |
|---|---|
| Product | Pinoy Pawn Manager (PPM) |
| Status | PPM-00 planning |
| Implementation | **None** |
| Last updated | 2026-08-27 |
| LEGAL_AUTHORIZATION_CLAIMED | **NO** |

Storage locations answer: **where is the bag/item right now?** Hierarchy exists for clarity, but MVP must not over-engineer empty levels ([PPM-R-00-10](../risks-and-decisions.md)).

---

## Full conceptual hierarchy

```text
Organization
 └── Branch
      └── Vault (or StorageArea)
           └── Cabinet (or Rack)
                └── Shelf / Drawer
                     └── Bin
                          └── Bag (tag / sealed pouch)
                               └── Pledged item instance
```

| Level | Intent |
|---|---|
| Organization | Tenant scope |
| Branch | Physical site; ticket `BranchId` |
| Vault / StorageArea | Secured room or cage |
| Cabinet / Rack | Furniture unit |
| Shelf / Drawer | Horizontal subdivision |
| Bin | Smallest fixed slot |
| Bag | Movable sealed unit with barcode/tag id |

Not every org needs every level populated.

---

## MVP pragmatism

Safe MVP shape (planning):

```text
Organization → Branch → StorageArea → BinOrBag
```

| MVP choice | Rationale |
|---|---|
| Collapse Cabinet/Shelf if unused | Fewer empty mandatory dropdowns |
| Require Branch + at least one StorageArea | Minimum meaningful hold |
| Bag id strongly recommended | Wrong-item defense |
| Deepen hierarchy later | Config/additive, not rewrite history |

Do not block first custody go-live on seven nested mandatory entities.

---

## Planning concepts (not schema)

| Concept | Intent |
|---|---|
| Location node id | PPM-owned |
| Parent node | Tree |
| Node type | Vault / Cabinet / Shelf / Bin / Bag / custom |
| Code / label | Human + scan code |
| Branch id | Required on nodes |
| Active flag | Soft-retire locations |
| Capacity hint | Optional; not inventory quantity for sale |

Bags are first-class for pawn: one bag ↔ one pledged item is a common pattern; multi-item bags are Open / discouraged for MVP.

---

## Rules

| Rule | Intent |
|---|---|
| Item current location is a node (usually Bag or Bin) | Queryable |
| Moving between nodes creates movement history | [custody-movement.md](custody-movement.md) |
| Deleting a location with history | Soft-retire only |
| Cross-branch location assign | Forbidden without transfer workflow ([PPM-D-00-16](../risks-and-decisions.md)) |
| POS aisle/shelf reuse | Forbidden — different product |

---

## Labeling and scanning (future)

Barcode/QR on bags is desirable for release safeguards ([item-release.md](item-release.md)). Printer hardware integration is out of PPM-00. Manual code entry remains valid.

---

## Multi-branch

Each branch maintains its own vault tree. “Transfer to Branch B” is a custody transfer event, not editing `BranchId` on a bin silently.

---

## Online-only

Creating locations and assigning items to locations are **ONLINE-ONLY** mutations initially.

---

## Exclusions

- No 3D warehouse map  
- No claim of insurance-grade vault certification  
- No shared storage tables with POS stockrooms  
