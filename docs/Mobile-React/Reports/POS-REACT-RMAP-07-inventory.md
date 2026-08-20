# RMAP-07 — Inventory tracking + movements + opening stock

## Status

**COMPLETE**

## Baseline

starting SHA: (post RMAP-06 docs)

## Contract review

| Area | Finding |
|------|---------|
| Default | Tracking **OFF**; untracked ≠ zero / out-of-stock |
| Enable | Optional opening quantity → OpeningStock movement when > 0 |
| Adjust | In/Out with reason; base quantity |
| Disable | Requires zero on-hand (and reserved) |
| Oversell | Server rejects insufficient stock when tracked |
| Lots/expiry | Excluded (RMAP-08) |
| Owner decision | NO |

## Implementation

- `/inventory` list shows **Not tracked** vs on-hand
- Detail: enable/opening, adjust, disable, movement history
- ManageInventory / ViewInventory UI gates (Owner/StoreManager; Cashier denied)

## Exclusions

- Lots, FEFO, expiry alerts (RMAP-08)
- Stock counts / transfers advanced UX

## Next

**HARD STOP.** Do not start RMAP-08.
