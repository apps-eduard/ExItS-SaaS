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

### UD-06 — Staff alias credential / password semantics (RMAP-B00 blocker)

| Field | Value |
|-------|-------|
| ID | UD-06 |
| Marker | `RMAP_B00_CREDENTIAL_SEMANTICS_UNRESOLVED` |
| Question | When Personal accepts staff invite as same human, does org-scoped alias share Personal password or get a separate secret? Are lockouts/stamps shared or per-alias? Is formal person-link with separate staff principals acceptable? |
| Why it matters | Blocks safe RMAP-B00 schema/auth design; guessing couples Personal and employment auth incorrectly or invents multi-credential model without owner approval |
| Known evidence | Credential 1:1 `UserId`; login by `NormalizedEmail`; staff accept creates new user + new password; no LoginAlias table; OD-ID-01/05/06 do not specify password policy |
| Current behavior | Separate staff principal = separate password |
| Owner requirement | Same human + alias available; credential policy unspecified |
| Dependency impact | **Blocks Master Run 01** from RMAP-B00 onward |
| Recommended investigation | Product Owner answers A/B/C password policy + Option C acceptability; then resume RMAP-B00 |
| Blocking? | **YES** |

---

### UD-05 — Organization staff existing-person link

| Field | Value |
|-------|-------|
| ID | UD-05 |
| Marker | `ORGANIZATION_STAFF_EXISTING_PERSON_LINK_CONTRACT_MISSING` |
| Question | Exact target schema for one human with Personal + multi-org staff memberships + org-scoped aliases without duplicate humans? |
| Why it matters | Owner forbids duplicating a human merely for employment; CURRENT creates separate staff `PlatformUser` |
| Known evidence | `CreateOrganizationStaff`; `AcceptOrganizationInvitation` always adds new user; no `UserIdentity` / `LinkedPersonalUserId`; Personal cannot accept staff invite onto same identity; soft contact-email only |
| Current behavior | Separate credential principal per employment; alias = staff `NormalizedEmail` |
| Owner requirement | Personal may accept invite; same human; alias remains; multi-org isolated memberships; removal preserves Personal/other orgs |
| Dependency impact | Blocks RMAP-01 final validation, RMAP-01b, and RMAP-02 in Master Run 01 until PASS; architecture shape chosen inside RMAP-B00 audit (not pre-decided in docs); additionally blocked by UD-06 credential semantics |
| Recommended investigation | Resolve UD-06 first; then RMAP-B00 safest minimal design; MAUI regression; then RMAP-01/01b/02 |
| Blocking? | **YES** for Master Run 01 packages after RMAP-00 that depend on post-B00 identity; **YES** for `READY_FOR_REACT_STAFF_IDENTITY_PARITY` until B00 PASS |

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
