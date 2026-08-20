# RMAP-07 — Inventory tracking + movements + opening stock

## Status

**COMPLETE**

## Baseline

starting SHA: `ae614cab6cc7ca43d3eff1d829d3840e7ba3606a` (post RMAP-05; RMAP-06/07 shipped together after RMAP-05)

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
