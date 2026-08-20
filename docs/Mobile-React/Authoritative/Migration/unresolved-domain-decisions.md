# Unresolved Domain Decisions

Only **true** unresolved or missing-contract items supported by this audit.
Markers appear only when evidence supports them.

---

### UD-01 — Milligram UOM

| Field | Value |
|-------|-------|
| ID | UD-01 |
| Marker | `POS_MILLIGRAM_UOM_UNRESOLVED` |
| Question | Should `Milligram` be added to `UnitOfMeasure`, or is Gram sufficient with decimal precision? |
| Why it matters | Bulk powders / pharmacy-like precision expectations |
| Known evidence | Enum has Gram/Kilogram only; no Milligram matches in POS source |
| Current behavior | Gram + 3 decimal qty precision |
| Owner requirement | Milligram remains an audit/decision item |
| Dependency impact | Catalog UOM lists, conversions, UI pickers |
| Recommended investigation | Owner confirm whether Gram decimals cover powders; if not, backend enum+migration package |
| Blocking? | **NO** for base React catalog/sell parity using existing UOMs |

---

### UD-02 — Sale price policy / cashier override

| Field | Value |
|-------|-------|
| ID | UD-02 |
| Markers | `POS_SALE_PRICE_POLICY_CONTRACT_MISSING`, `POS_CASHIER_PRICE_OVERRIDE_CONTRACT_MISSING` |
| Question | Exact product policy fields, permission codes, min/max rules, reason schema, manager approval threshold? |
| Why it matters | Owner requires Fixed vs CashierAdjustable; no UI-only authority |
| Known evidence | No `SalePricePolicy` / `CashierAdjustable` types; checkout uses catalog/unit price |
| Current behavior | Catalog/Today’s Prices change future prices; no per-sale override model |
| Owner requirement | Controlled override with audit |
| Dependency impact | Blocks React override UI; does **not** block Today’s Prices or fixed-price checkout |
| Recommended investigation | Backend domain design package + tests before any React override controls |
| Blocking? | **YES** for override UI; **NO** for standard checkout at catalog price |

---

### UD-03 — Dedicated price history audit table

| Field | Value |
|-------|-------|
| ID | UD-03 |
| Marker | `POS_PRICE_HISTORY_AUDIT_UNRESOLVED` |
| Question | Is sale-line snapshot + current overwrite enough, or is a catalog price-history table required? |
| Why it matters | Owner wants audit of original vs applied price for overrides; catalog change audit may also be desired |
| Known evidence | No price_history entity; sales snapshot unit prices; Today’s Prices overwrite |
| Current behavior | Historical sales immutable via snapshots; catalog current price has no dedicated history stream |
| Owner requirement | Audit original vs applied for overrides; daily price changes |
| Dependency impact | Couples to UD-02 for override audit; catalog history optional |
| Recommended investigation | Decide whether Platform/POS audit log or dedicated table |
| Blocking? | **NO** for React Today’s Prices migration; **YES** for full override-audit acceptance |

---

### UD-04 — BreakPack / Open Sack explicit workflow

| Field | Value |
|-------|-------|
| ID | UD-04 |
| Marker | *(none emitted — owner says Open Sack not automatically required)* |
| Question | Any merchant UX still needs an explicit break-pack action beyond shared-pool conversion? |
| Why it matters | Avoid building unused workflow |
| Known evidence | No BreakPack/BreakBulk/Repack domain; shared pool CURRENT |
| Current behavior | Sell/purchase units convert to base automatically |
| Owner requirement | Open Sack not automatically required |
| Dependency impact | None unless owner revisits |
| Recommended investigation | None now |
| Blocking? | **NO** |

---

### UD-06 — Staff alias credential / password semantics

| Field | Value |
|-------|-------|
| ID | UD-06 |
| Marker | `RMAP_B00_CREDENTIAL_SEMANTICS_UNRESOLVED` |
| Status | **RESOLVED** (Product Owner Repair 02) |
| Resolution | Separate staff principals + separate passwords + independent lockout; Option C formal person-link |
| Blocking? | **NO** |

---

### UD-05 — Organization staff existing-person link

| Field | Value |
|-------|-------|
| ID | UD-05 |
| Marker | `ORGANIZATION_STAFF_EXISTING_PERSON_LINK_CONTRACT_MISSING` |
| Status | **RESOLVED** (RMAP-B00) |
| Resolution | `LinkedPersonalUserId` on org-scoped staff; authenticated Personal accept; email is not the link |
| Blocking? | **NO** for backend; React UI is RMAP-01b |

---

### UD-07 — Late Personal link for standalone staff

| Field | Value |
|-------|-------|
| ID | UD-07 |
| Marker | `ORGANIZATION_STAFF_LATE_PERSONAL_LINK_FLOW_DEFERRED` |
| Question | How does a standalone staff principal later formally link after the human creates Personal? |
| Why it matters | Avoid email auto-merge of legacy/unlinked staff |
| Current behavior | No automatic merge; unlinked staff remain valid |
| Blocking? | **NO** for RMAP-B00 |

---

## Markers explicitly not emitted

| Marker | Why not emitted |
|--------|-----------------|
| `POS_MULTI_UOM_CONVERSION_CONTRACT_MISSING` | Conversion via `CatalogProductUnit.MultiplierToBase` is PROVEN_CURRENT |
| `POS_BREAK_BULK_CONTRACT_MISSING` | Shared-pool break-bulk selling PROVEN_CURRENT; named BreakBulk workflow absent by design |
| `POS_SELL_UNIT_PRICING_CONTRACT_MISSING` | Independent sell-unit prices PROVEN_CURRENT |
| `POS_EXPIRY_BATCH_CONTRACT_MISSING` | InventoryLot + TracksExpiration + FEFO PROVEN_CURRENT |
| `POS_FEFO_POLICY_UNRESOLVED` | FEFO allocator PROVEN_CURRENT |
| `POS_CANONICAL_INVENTORY_UOM_UNRESOLVED` | Base UOM = `CatalogProduct.UnitOfMeasure` PROVEN_CURRENT |
