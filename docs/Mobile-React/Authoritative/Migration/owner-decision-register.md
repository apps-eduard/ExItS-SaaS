# Owner Decision Register

Owner-confirmed requirements from the authoritative audit package prompt.
**These are requirements, not automatic CURRENT implementation claims.**

Each row: Owner decision → CURRENT alignment → Action implication.

## PRODUCT / UOM

| ID | Owner decision | CURRENT | Action |
|----|----------------|---------|--------|
| OD-UOM-01 | Merchants include sari-sari, meat, fish, produce, rice/feed, bulk powders | Supported by catalog + ByWeight + units | Migrate CURRENT |
| OD-UOM-02 | Weighted/fractional selling required | PROVEN_CURRENT ByWeight | React parity |
| OD-UOM-03 | Package/break-bulk via one physical inventory pool | PROVEN_CURRENT `CatalogProductUnit` + base pool | React parity; do not fork SKUs |
| OD-UOM-04 | Rice base kg + loose + 5/10/25/50 packages | PROVEN_CURRENT pattern | Use product units |
| OD-UOM-05 | Selling-unit price independent of factor arithmetic | PROVEN_CURRENT | Preserve |
| OD-UOM-06 | Sale/receipt preserve customer-facing unit+qty; inventory consumes canonical | PROVEN_CURRENT snapshots | Preserve |
| OD-UOM-07 | Movement history preserves entered unit/qty/factor/canonical effect | PROVEN_CURRENT snapshots | Preserve |
| OD-UOM-08 | Explicit Open Sack not automatically required | Aligns with shared pool | No BreakPack WP unless owner revisits |
| OD-UOM-09 | Milligram support | PROVEN_MISSING in enum | Unresolved decision `POS_MILLIGRAM_UOM_UNRESOLVED` |

## PRICING

| ID | Owner decision | CURRENT | Action |
|----|----------------|---------|--------|
| OD-PRICE-01 | Market prices may change daily; fast update | Today’s Prices PROVEN_CURRENT | React parity |
| OD-PRICE-02 | Current-price change ≠ one-sale override | Override backend PROVEN_CURRENT (RMAP-B01) | React UI = RMAP-12b |
| OD-PRICE-03 | Cashier overrides controlled by permissions | **SUPERSEDED**: Cashier **DENY**; Manager ≤100%; Owner unlimited | Role capabilities, not product policy |
| OD-PRICE-04 | Fixed default; optional CashierAdjustable | **SUPERSEDED** by locked PO role matrix | Do not implement CashierAdjustable |
| OD-PRICE-05 | Override reason/audit; manager ceiling | PROVEN_CURRENT (reason + audit rows; Manager ≤100% inclusive) | Preserve |
| OD-PRICE-06 | No UI-only price authority | CURRENT server prices + capability gates | Preserve |

## INVENTORY

| ID | Owner decision | CURRENT | Action |
|----|----------------|---------|--------|
| OD-INV-01 | Default UNTRACKED for new products | Aligned | Preserve |
| OD-INV-02 | Untracked ≠ zero | Aligned | Preserve |
| OD-INV-03 | Tracked authoritative; no opening → zero | Aligned | Preserve |
| OD-INV-04 | Oversell tracked prohibited | Aligned | Preserve |
| OD-INV-05 | Opening auditable movement + actor/time | Aligned | Preserve |

## EXPIRY

| ID | Owner decision | CURRENT | Action |
|----|----------------|---------|--------|
| OD-EXP-01 | Default OFF; requires tracked | Aligned lots model | React parity later |
| OD-EXP-02 | Expiry on batch/layer; lot optional | Aligned InventoryLot | Preserve |
| OD-EXP-03 | FEFO; multiple batches; canonical qty | Aligned | Preserve |

## ROLES

| ID | Owner decision | CURRENT | Action |
|----|----------------|---------|--------|
| OD-ROLE-01 | Org membership ≠ POS roles | Aligned | Preserve |
| OD-ROLE-02 | Owner/Manager/Cashier are POS concepts | Aligned (mapped codes) | Preserve |
| OD-ROLE-03 | Cashier cannot manage catalog/inventory by default | Aligned MAUI | Preserve in React guards |
| OD-ROLE-04 | Price override only when permitted | Future policy | Block until backend |

## PERSONAL / ORGANIZATION

| ID | Owner decision | CURRENT | Action |
|----|----------------|---------|--------|
| OD-ID-01 | One human must not be duplicated merely for employment; Personal + org memberships under same person | **CURRENT after RMAP-B00** — separate staff principals + formal `LinkedPersonalUserId` (Option C) | React staff UI = RMAP-01b |
| OD-ID-02 | Personal initiates business journey | Start a Business CURRENT | React later |
| OD-ID-03 | Org owns operational state | Aligned | Preserve |
| OD-ID-04 | Same human Personal + multi-org ownership | Aligned for Owner memberships | Preserve |
| OD-ID-05 | Existing Personal may accept staff invite and become Org Staff on same human | **PROVEN_CURRENT** (new staff principal + person-link; membership stays on staff) | Marker resolved |
| OD-ID-06 | Org-scoped login alias must remain available | Alias format PROVEN_CURRENT as **real** staff login | Preserve |
| OD-ID-07 | Personal QR ≠ Business QR; ledgers distinct | Aligned | Preserve |
| OD-ID-08 | Removing Org A must not delete Personal / Org B / unrelated profiles | PROVEN_CURRENT isolation for revoke/suspend paths | Preserve under any redesign |
| OD-ID-09 | Org role ≠ POS role; Customer link ≠ Staff | Aligned | Preserve |

## UI / RESPONSIVE

| ID | Owner decision | CURRENT | Action |
|----|----------------|---------|--------|
| OD-UI-01 | Mobile-first, tablet-strong, desktop-capable | Partial sell-shell responsive | RMAP-00 + every UI WP DoD |
| OD-UI-02 | UI DoD = functional + mobile + tablet + desktop + a11y + responsive + docs | Not yet enforced as Authoritative DoD | Enforce via RMAP-00 + master-run protocol |

## DELIVERY

| ID | Owner decision | CURRENT | Action |
|----|----------------|---------|--------|
| OD-DEL-01 | Delivery requires branch location/hours/fulfillment config | PROVEN_CURRENT Platform+MAUI | React config + ordering after branch parity |

## Recording rule

When implementing React WPs, cite Owner Decision IDs above plus CURRENT evidence paths. If CURRENT and owner diverge, schedule backend work first.
