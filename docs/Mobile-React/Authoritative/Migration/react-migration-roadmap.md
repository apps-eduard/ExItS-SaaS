# React Migration Roadmap

**Status of packages:** PROPOSED / NOT STARTED (unless noted as already shipped on branch)
**Rule:** Do not sequence by old MAUI screen order or current React nav alone.
**Rule:** If backend contract missing → backend package before React UI.

Derived from [capability-parity-matrix.md](capability-parity-matrix.md) + [dependency-graph.md](dependency-graph.md).

Legacy WP03/WP04 numbering is **not** reused. New IDs below.

---

## Category A — FOUNDATION PARITY

### RMAP-01 — Account / session / staff-login parity
| Field | Content |
|-------|---------|
| Objective | Prove Personal + org-scoped staff login, profile/session, CSRF, sign-out, me |
| Why next | Everything depends on correct identity/session |
| Dependencies | None |
| Backend contracts | Platform auth (CURRENT) |
| MAUI reference | `/signin`, workspace/org select |
| React starting point | `SignInPage`, `SessionProvider`, antiforgery |
| Owner decisions | OD-ID-05, OD-ID-06 |
| Exclusions | Offline PIN; Start a Business UI |
| Tests | Unit + Playwright staff-alias + personal email |
| Acceptance | Staff `local@ORG######` and Personal email both authenticate; AccountClass isolation held |
| Next | RMAP-02 |

### RMAP-02 — Workspace / org / product-access / role guards
| Field | Content |
|-------|---------|
| Objective | Org context, product access, role homes, CreateSale guard correctness |
| Dependencies | RMAP-01 |
| Backend | ProductLocalRoleGrant, entitlements (CURRENT) |
| React start | `WorkspaceProvider`, `SessionGuards`, role pages |
| Exclusions | Org admin CRUD |
| Acceptance | Wrong class/role cannot open sell; staff cannot switch org |
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

### RMAP-04 — Catalog admin parity
| Field | Content |
|-------|---------|
| Objective | Categories/products CRUD, flags, images, SKU/barcode |
| Dependencies | RMAP-03 |
| Backend | CURRENT |
| MAUI | `/catalog*` |
| React start | extend beyond read-only catalog client |
| Owner | OD-UOM-01 |
| Exclusions | Global import advanced jobs can follow RMAP-04b |
| Next | RMAP-05 |

### RMAP-05 — Base UOM + SellingMode + product units
| Field | Content |
|-------|---------|
| Objective | Base UOM, ByWeight, Purchase/Sell units, MultiplierToBase, independent unit prices |
| Dependencies | RMAP-04 |
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
| Dependencies | RMAP-05 |
| Backend | CURRENT prices endpoint |
| Owner | OD-PRICE-01 |
| Exclusions | Cashier override (RMAP-B01) |
| Next | RMAP-07 |

### RMAP-07 — Inventory tracking + movements + opening stock
| Field | Content |
|-------|---------|
| Objective | Enable/disable tracking, opening movement, adjust, on-hand, oversell rules |
| Dependencies | RMAP-05 |
| Backend | CURRENT (default untracked aligned) |
| Owner | OD-INV-* |
| Next | RMAP-08 |

### RMAP-08 — Lots / expiry / FEFO (optional track)
| Field | Content |
|-------|---------|
| Objective | TracksExpiration + lots + FEFO sell allocation surfaces |
| Dependencies | RMAP-07 |
| Backend | CURRENT |
| Owner | OD-EXP-* |
| Next | RMAP-09 |

### RMAP-09 — Sell floor + cart parity (units/weight/stock)
| Field | Content |
|-------|---------|
| Objective | Search/categories/barcode, sell-unit selection, ByWeight, stock hints, cart edits |
| Dependencies | RMAP-05, RMAP-07 (for tracked checks) |
| React start | `SellFloorPage`, `SessionCartProvider` |
| Exclusions | Pay/checkout |
| Next | RMAP-10 |

### RMAP-10 — Registers + open shift gate
| Field | Content |
|-------|---------|
| Objective | Register awareness + open shift required for checkout |
| Dependencies | RMAP-03 |
| Backend | CURRENT |
| Next | RMAP-11 |

### RMAP-11 — Checkout / sale (online cash first)
| Field | Content |
|-------|---------|
| Objective | POST sales with snapshots; cash path; inventory effects; idempotency; Transaction Summary wording |
| Dependencies | RMAP-09, RMAP-10, RMAP-07 |
| Backend | CURRENT |
| Exclusions | Offline outbox (RMAP-21); price override (needs RMAP-B01); TaxDocument |
| Acceptance | Completes sale online; tracked stock cannot oversell; document = Transaction Summary |
| Next | RMAP-12 |

### RMAP-12 — Payments expansion + void
| Field | Content |
|-------|---------|
| Objective | ManualGCash/Utang online paths; void |
| Dependencies | RMAP-11 |
| Next | RMAP-13 |

### RMAP-12b — Cashier price override UI (only after RMAP-B01)
| Field | Content |
|-------|---------|
| Objective | Policy-gated override + reason + audit display |
| Dependencies | RMAP-B01, RMAP-11 |
| Owner | OD-PRICE-02..05 |
| Next | RMAP-13 |

### RMAP-13 — Customers + Business Utang
| Field | Content |
|-------|---------|
| Objective | Customers, credit, repayments, statements |
| Dependencies | RMAP-11 (for product utang sales) |
| Next | RMAP-14 |

### RMAP-14 — Returns / refunds
| Field | Content |
|-------|---------|
| Objective | Partial returns, restock, inventory restore |
| Dependencies | RMAP-11 |
| Next | RMAP-15 |

### RMAP-15 — Manual suppliers
| Field | Content |
|-------|---------|
| Objective | Supplier CRUD |
| Dependencies | RMAP-03 |
| Next | RMAP-16 |

### RMAP-16 — Connected suppliers
| Field | Content |
|-------|---------|
| Objective | Connect, expose≠share, buyer prices, links |
| Dependencies | RMAP-15, RMAP-04 |
| Invariants | EXPOSABLE≠SHARED; no inventory on share |
| Next | RMAP-17 |

### RMAP-17 — Purchasing + goods receipt
| Field | Content |
|-------|---------|
| Objective | PO lifecycle + receive-only inventory; connected PO receive |
| Dependencies | RMAP-15/16, RMAP-07 |
| Next | RMAP-18 |

---

## Category D — EXTENDED COMMERCE

### RMAP-18 — Branch fulfillment admin + readiness
| Field | Content |
|-------|---------|
| Objective | Address/coords/hours/pickup/delivery config in React |
| Dependencies | RMAP-03 |
| Owner | OD-DEL-01 |
| Next | RMAP-19 |

### RMAP-19 — Customer ordering / storefront / pickup / delivery
| Field | Content |
|-------|---------|
| Objective | Buyer shop + seller order ops |
| Dependencies | RMAP-18, RMAP-07, catalog |
| Next | RMAP-20 |

### RMAP-20 — Reports + dashboard
| Field | Content |
|-------|---------|
| Objective | Operational reports (no fake P&L) |
| Dependencies | RMAP-11+ |
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
| Dependencies | RMAP-01 |
| Can parallelize after foundation | Yes (after RMAP-02) |
| Next | RMAP-23 |

### RMAP-23 — Parity / security / UX hardening
| Field | Content |
|-------|---------|
| Objective | Authz matrix, cross-org denials, wording, a11y, performance |
| Dependencies | Core WPs |
| Next | RMAP-24 |

### RMAP-24 — E2E validation matrix execution
| Field | Content |
|-------|---------|
| Objective | Execute [validation-matrix.md](validation-matrix.md) owner + automated evidence |
| Dependencies | RMAP-23 |
| Next | STOP — owner review for production readiness claims |

---

## Already shipped on branch (not re-proposed)

Scaffold, PWA shell, browser session/workspace, sell-floor shell, session cart, preferences — treat as **starting capital** for RMAP-01..09, not as sales parity.

## Package count

| Category | Count |
|----------|-------|
| Foundation | 3 (RMAP-01..03) |
| Backend gaps | 2 (B01 required for override; B02 optional) |
| Core React parity | 14 (RMAP-04..17 incl 12b) |
| Extended commerce | 3 (18..20) |
| Hardening | 4 (21..24) |
| **Total proposed** | **26** (including optional B02 and 12b) |

## Backend-before-React list

1. **RMAP-B01** sale price policy — required before override UI
2. **RMAP-B02** Milligram — only if owner approves

All other listed React WPs reuse **PROVEN_CURRENT** backend contracts.
