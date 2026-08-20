# React Migration Roadmap

**Status of packages:** PROPOSED / NOT STARTED (unless noted as already shipped on branch)
**Rule:** Do not sequence by old MAUI screen order or current React nav alone.
**Rule:** If backend contract missing → backend package before React UI that depends on the desired contract.
**Execution:** After roadmap approval, batches follow [master-run-execution-protocol.md](master-run-execution-protocol.md).

Derived from [capability-parity-matrix.md](capability-parity-matrix.md) + [dependency-graph.md](dependency-graph.md).

Legacy WP03/WP04 numbering is **not** reused. New IDs below.

---

## Category 0 — UI FOUNDATION (prerequisite for visual WPs)

### RMAP-00 — React Shared UI/UX & Responsive Foundation
| Field | Content |
|-------|---------|
| Status | PROPOSED / NOT STARTED |
| Objective | Inventory, reuse/extend, and fill shared mobile-first / tablet-strong / desktop-capable UI primitives and interaction standards |
| Why next | Later visual WPs must not invent duplicate search/filter/list/form patterns |
| Dependencies | None (may run before or parallel to RMAP-01; required before visual list/form WPs) |
| Backend contracts | None |
| MAUI reference | Interaction patterns only (not Blazor ports) |
| React starting point | `components/exits/*`, `components/ui/*`, `globals.css`, sell-floor inlines |
| Owner decisions | OD-UI-01, OD-UI-02 |
| Must include | Shared UI inventory; design-token audit; breakpoints; SearchField; FilterButton; FilterChips; SortButton; ListToolbar; ResponsiveEntityList/EntityCard; status components; loading/empty/error/denied/offline; bottom-sheet/dialog; form sections/inputs; money/quantity display/input; sticky actions; phone/tablet/desktop proof; a11y baseline |
| Rule | REUSE/EXTEND existing good components; do not rename-only rewrites |
| Exclusions | Domain feature screens (catalog CRUD, checkout, etc.) |
| Tests | Component + viewport Playwright matrix (375 / 768 / 1024 / 1440) |
| Acceptance | Shared primitives documented; ListToolbar pattern demo; UI DoD checklist published; no horizontal overflow on demo screens |
| Docs | [06-react-ui-ux-and-responsive-foundation.md](../06-react-ui-ux-and-responsive-foundation.md) |
| Next | RMAP-01 (session) and/or visual WPs depending on RMAP-00 |

---

## Category A — FOUNDATION PARITY

### RMAP-01 — Account / session parity (Personal + CURRENT auth mechanics)
| Field | Content |
|-------|---------|
| Objective | Personal email login, cookie session, profiles/session guards, CSRF, sign-out, me; may verify CURRENT staff principal login **only as CURRENT contract**, without claiming desired person-link parity |
| Why next | Stable session foundation |
| Dependencies | None (UI shell polish may use RMAP-00) |
| Backend contracts | Platform auth **CURRENT** |
| MAUI reference | `/signin`, workspace/org select |
| React starting point | `SignInPage`, `SessionProvider`, antiforgery |
| Owner decisions | OD-ID-02..04, OD-ID-07 (not OD-ID-01/05 desired staff link) |
| Exclusions | Desired one-human staff identity; invite-accept-as-Personal; Offline PIN; Start a Business UI |
| Tests | Unit + Playwright Personal login; optional CURRENT `local@ORG######` login smoke |
| Acceptance | Personal session + AccountClass isolation; **must not** document desired staff person-link as done |
| Next | RMAP-02; staff **desired** parity waits on RMAP-B00 → RMAP-01b |

### RMAP-01b — React staff identity parity (desired person-link model)
| Field | Content |
|-------|---------|
| Objective | React UX for inviting/accepting/staff login under **owner-approved** post-RMAP-B00 contract |
| Dependencies | **RMAP-B00** (required), RMAP-01, RMAP-00 for UI |
| Backend | Post-RMAP-B00 contract only |
| Owner decisions | OD-ID-01, OD-ID-05, OD-ID-06, OD-ID-08 |
| Exclusions | Implementing CURRENT duplicate-human model as final desired parity |
| Acceptance | Matches approved person-link + alias contract; multi-org isolation; removal preserves Personal/other orgs |
| Readiness flag | `READY_FOR_REACT_STAFF_IDENTITY_PARITY` becomes YES only after RMAP-B00 |
| Next | RMAP-02 if not already done |

### RMAP-02 — Workspace / org / product-access / role guards
| Field | Content |
|-------|---------|
| Objective | Org context, product access, role homes, CreateSale guard correctness |
| Dependencies | RMAP-01; RMAP-00 if visual polish |
| Backend | ProductLocalRoleGrant, entitlements (CURRENT) |
| React start | `WorkspaceProvider`, `SessionGuards`, role pages |
| Exclusions | Org admin CRUD; desired staff invite UX (RMAP-01b) |
| Acceptance | Wrong class/role cannot open sell; CURRENT staff lock behavior until RMAP-B00 changes it |
| Next | RMAP-03 |

### RMAP-03 — Branch / device operational context
| Field | Content |
|-------|---------|
| Objective | Bound branch (and device where required) for POS ops |
| Dependencies | RMAP-02 |
| Backend | Platform branches/devices; POS operational-branch (CURRENT) |
| React start | workspace branch binding, `NoAccessibleBranchPage` |
| Exclusions | Full branch fulfillment admin (RMAP-18) |
| Acceptance | No accessible branch → blocked; bound context on POS calls |
| Next | RMAP-04 |

---

## Category B — DOMAIN CONTRACT GAPS (backend-first when needed)

### RMAP-B00 — Staff identity / existing-person link reconciliation
| Field | Content |
|-------|---------|
| Status | PROPOSED / NOT STARTED |
| Objective | Backend/domain/auth/test design implementing owner one-human staff model while preserving org-scoped login alias availability and membership isolation |
| Why | OWNER_CONFIRMED_CHANGE; marker `ORGANIZATION_STAFF_EXISTING_PERSON_LINK_CONTRACT_MISSING` |
| Dependencies | None (Platform identity) |
| Backend | **NEW / CHANGING** vs P19 separate-staff-`PlatformUser` employment |
| MAUI | Compatibility/regression required after API change |
| React | **RMAP-01b only after this passes** |
| Owner decisions | OD-ID-01, OD-ID-05, OD-ID-06, OD-ID-08 |
| Exclusions | Shipping React staff invite UX that hard-codes duplicate humans as final |
| Tests | Identity unit + integration; multi-org; removal isolation; alias uniqueness |
| Acceptance | Personal can become staff without unrelated duplicate human; alias works; Org A removal preserves Personal/Org B |
| Next | MAUI regression (optional RMAP-B00-M) → RMAP-01b |

### RMAP-B01 — Sale price policy backend (BLOCKING for override UI only)
| Field | Content |
|-------|---------|
| Objective | Domain+API+tests for Fixed/CashierAdjustable, limits, reason, audit |
| Why | OWNER_CONFIRMED_CHANGE; PROVEN_MISSING |
| Dependencies | Catalog product model |
| Backend | **NEW** — UD-02 |
| MAUI | Regression after API exists |
| React | **Not started until backend complete** |
| Exclusions | Shipping override UI early |
| Next | Optional RMAP-B01-M MAUI; then RMAP-12b React override |

### RMAP-B02 — Milligram UOM (OPTIONAL)
| Field | Content |
|-------|---------|
| Objective | Resolve UD-01; add enum only if owner confirms |
| Blocking? | NO for core parity |
| Next | Catalog UOM picker update if approved |

---

## Category C — REACT PARITY (core POS)

Visual packages below depend on **RMAP-00** unless noted non-UI.

### RMAP-04 — Catalog admin parity
| Field | Content |
|-------|---------|
| Objective | Categories/products CRUD, flags, images, SKU/barcode |
| Dependencies | RMAP-03, **RMAP-00** |
| Backend | CURRENT |
| MAUI | `/catalog*` |
| React start | extend beyond read-only catalog client |
| Owner | OD-UOM-01 |
| UI DoD | Phone/tablet/desktop list+form patterns |
| Exclusions | Global import advanced jobs can follow RMAP-04b |
| Next | RMAP-05 |

### RMAP-05 — Base UOM + SellingMode + product units
| Field | Content |
|-------|---------|
| Objective | Base UOM, ByWeight, Purchase/Sell units, MultiplierToBase, independent unit prices |
| Dependencies | RMAP-04, RMAP-00 |
| Backend | CURRENT multi-UOM |
| MAUI | ProductUnitDraft, sell-as dialogs |
| Owner | OD-UOM-02..08 |
| Exclusions | Milligram unless RMAP-B02; Open Sack workflow |
| Acceptance | Rice-style shared pool configurable end-to-end in React admin |
| Next | RMAP-06 |

### RMAP-06 — Today’s Prices
| Field | Content |
|-------|---------|
| Objective | Bulk current selling price updates with concurrency |
| Dependencies | RMAP-05, RMAP-00 |
| Backend | CURRENT prices endpoint |
| Owner | OD-PRICE-01 |
| Exclusions | Cashier override (RMAP-B01) |
| Next | RMAP-07 |

### RMAP-07 — Inventory tracking + movements + opening stock
| Field | Content |
|-------|---------|
| Objective | Enable/disable tracking, opening movement, adjust, on-hand, oversell rules |
| Dependencies | RMAP-05, RMAP-00 |
| Backend | CURRENT (default untracked aligned) |
| Owner | OD-INV-* |
| Next | RMAP-08 |

### RMAP-08 — Lots / expiry / FEFO (optional track)
| Field | Content |
|-------|---------|
| Objective | TracksExpiration + lots + FEFO sell allocation surfaces |
| Dependencies | RMAP-07, RMAP-00 |
| Backend | CURRENT |
| Owner | OD-EXP-* |
| Next | RMAP-09 |

### RMAP-09 — Sell floor + cart parity (units/weight/stock)
| Field | Content |
|-------|---------|
| Objective | Search/categories/barcode, sell-unit selection, ByWeight, stock hints, cart edits |
| Dependencies | RMAP-05, RMAP-07, RMAP-00 |
| React start | `SellFloorPage`, `SessionCartProvider` |
| Exclusions | Pay/checkout |
| Next | RMAP-10 |

### RMAP-10 — Registers + open shift gate
| Field | Content |
|-------|---------|
| Objective | Register awareness + open shift required for checkout |
| Dependencies | RMAP-03, RMAP-00 |
| Backend | CURRENT |
| Next | RMAP-11 |

### RMAP-11 — Checkout / sale (online cash first)
| Field | Content |
|-------|---------|
| Objective | POST sales with snapshots; cash path; inventory effects; idempotency; Transaction Summary wording |
| Dependencies | RMAP-09, RMAP-10, RMAP-07, RMAP-00 |
| Backend | CURRENT |
| Exclusions | Offline outbox (RMAP-21); price override (needs RMAP-B01); TaxDocument |
| Acceptance | Completes sale online; tracked stock cannot oversell; document = Transaction Summary |
| Next | RMAP-12 |

### RMAP-12 — Payments expansion + void
| Field | Content |
|-------|---------|
| Objective | ManualGCash/Utang online paths; void |
| Dependencies | RMAP-11, RMAP-00 |
| Next | RMAP-13 |

### RMAP-12b — Cashier price override UI (only after RMAP-B01)
| Field | Content |
|-------|---------|
| Objective | Policy-gated override + reason + audit display |
| Dependencies | RMAP-B01, RMAP-11, RMAP-00 |
| Owner | OD-PRICE-02..05 |
| Next | RMAP-13 |

### RMAP-13 — Customers + Business Utang
| Field | Content |
|-------|---------|
| Objective | Customers, credit, repayments, statements |
| Dependencies | RMAP-11, RMAP-00 |
| Next | RMAP-14 |

### RMAP-14 — Returns / refunds
| Field | Content |
|-------|---------|
| Objective | Partial returns, restock, inventory restore |
| Dependencies | RMAP-11, RMAP-00 |
| Next | RMAP-15 |

### RMAP-15 — Manual suppliers
| Field | Content |
|-------|---------|
| Objective | Supplier CRUD |
| Dependencies | RMAP-03, RMAP-00 |
| Next | RMAP-16 |

### RMAP-16 — Connected suppliers
| Field | Content |
|-------|---------|
| Objective | Connect, expose≠share, buyer prices, links |
| Dependencies | RMAP-15, RMAP-04, RMAP-00 |
| Invariants | EXPOSABLE≠SHARED; no inventory on share |
| Next | RMAP-17 |

### RMAP-17 — Purchasing + goods receipt
| Field | Content |
|-------|---------|
| Objective | PO lifecycle + receive-only inventory; connected PO receive |
| Dependencies | RMAP-15/16, RMAP-07, RMAP-00 |
| Next | RMAP-18 |

---

## Category D — EXTENDED COMMERCE

### RMAP-18 — Branch fulfillment admin + readiness
| Field | Content |
|-------|---------|
| Objective | Address/coords/hours/pickup/delivery config in React |
| Dependencies | RMAP-03, RMAP-00 |
| Owner | OD-DEL-01 |
| Next | RMAP-19 |

### RMAP-19 — Customer ordering / storefront / pickup / delivery
| Field | Content |
|-------|---------|
| Objective | Buyer shop + seller order ops |
| Dependencies | RMAP-18, RMAP-07, catalog, RMAP-00 |
| Next | RMAP-20 |

### RMAP-20 — Reports + dashboard
| Field | Content |
|-------|---------|
| Objective | Operational reports (no fake P&L) |
| Dependencies | RMAP-11+, RMAP-00 |
| Next | RMAP-21 |

---

## Category E — HARDENING / VALIDATION

### RMAP-21 — Offline / LocalStore / outbox (cash + customers)
| Field | Content |
|-------|---------|
| Objective | Selective offline parity with capability matrix |
| Dependencies | RMAP-11, RMAP-13 |
| Exclusions | Offline inventory/purchasing/reports |
| Next | RMAP-22 |

### RMAP-22 — Personal surface parity (Utang, Explore, Start Business)
| Field | Content |
|-------|---------|
| Objective | Replace Personal shell with real Personal journeys |
| Dependencies | RMAP-01, RMAP-00 |
| Can parallelize after foundation | Yes (after RMAP-02) |
| Next | RMAP-23 |

### RMAP-23 — Parity / security / UX hardening
| Field | Content |
|-------|---------|
| Objective | Authz matrix, cross-org denials, wording, a11y, performance, responsive debt |
| Dependencies | Core WPs, RMAP-00 |
| Next | RMAP-24 |

### RMAP-24 — E2E validation matrix execution
| Field | Content |
|-------|---------|
| Objective | Execute [validation-matrix.md](validation-matrix.md) owner + automated evidence |
| Dependencies | RMAP-23 |
| Next | STOP — owner review for production readiness claims |

---

## Already shipped on branch (not re-proposed)

Scaffold, PWA shell, browser session/workspace, sell-floor shell, session cart, preferences — treat as **starting capital**, not sales parity and not RMAP-00 complete.

## Package count (after DOCS-RECONCILIATION-01)

| Category | Count |
|----------|-------|
| UI foundation | 1 (RMAP-00) |
| Foundation | 4 (RMAP-01, 01b, 02, 03) |
| Backend gaps | 3 (B00 staff identity; B01 price policy; B02 optional Milligram) |
| Core React parity | 14 (RMAP-04..17 incl 12b) |
| Extended commerce | 3 (18..20) |
| Hardening | 4 (21..24) |
| **Total proposed** | **29** (including optional B02 and 12b) |

## Backend-before-React list

1. **RMAP-B00** staff existing-person link — required before desired React staff identity parity (RMAP-01b)
2. **RMAP-B01** sale price policy — required before override UI
3. **RMAP-B02** Milligram — only if owner approves

## Suggested first implementation master run (NOT STARTED)

After Product Owner + ChatGPT approve docs:

```text
MASTER RUN A (example ≤10):
  RMAP-00 → RMAP-01 → RMAP-02 → RMAP-03 → RMAP-04 → RMAP-05 → RMAP-06 → RMAP-07 → RMAP-08 → RMAP-09
```

**RMAP-B00** may be scheduled in an earlier or parallel Platform backend master run; **RMAP-01b must not** enter a React batch before RMAP-B00 PASS.

Do **not** start any package in this documentation package.
