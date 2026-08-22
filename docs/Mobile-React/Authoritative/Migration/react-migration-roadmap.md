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

| Field                | Content                                                                                                                                                                                                                                                                                                                                           |
| -------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Status               | COMPLETE (Master Run 01)                                                                                                                                                                                                                                                                                                                          |
| Objective            | Inventory, reuse/extend, and fill shared mobile-first / tablet-strong / desktop-capable UI primitives and interaction standards                                                                                                                                                                                                                   |
| Why next             | Later visual WPs must not invent duplicate search/filter/list/form patterns                                                                                                                                                                                                                                                                       |
| Dependencies         | None (required before visual list/form WPs; first package in Master Run 01)                                                                                                                                                                                                                                                                       |
| Backend contracts    | None                                                                                                                                                                                                                                                                                                                                              |
| MAUI reference       | Interaction patterns only (not Blazor ports)                                                                                                                                                                                                                                                                                                      |
| React starting point | `components/exits/*`, `components/ui/*`, `globals.css`, sell-floor inlines                                                                                                                                                                                                                                                                        |
| Owner decisions      | OD-UI-01, OD-UI-02                                                                                                                                                                                                                                                                                                                                |
| Must include         | Shared UI inventory; design-token audit; breakpoints; SearchField; FilterButton; FilterChips; SortButton; ListToolbar; ResponsiveEntityList/EntityCard; status components; loading/empty/error/denied/offline; bottom-sheet/dialog; form sections/inputs; money/quantity display/input; sticky actions; phone/tablet/desktop proof; a11y baseline |
| Rule                 | REUSE/EXTEND existing good components; do not rename-only rewrites                                                                                                                                                                                                                                                                                |
| Exclusions           | Domain feature screens (catalog CRUD, checkout, etc.)                                                                                                                                                                                                                                                                                             |
| Tests                | Component + viewport Playwright matrix (375 / 768 / 1024 / 1440)                                                                                                                                                                                                                                                                                  |
| Acceptance           | Shared primitives documented; ListToolbar pattern demo; UI DoD checklist published; no horizontal overflow on demo screens                                                                                                                                                                                                                        |
| Docs                 | [06-react-ui-ux-and-responsive-foundation.md](../06-react-ui-ux-and-responsive-foundation.md)                                                                                                                                                                                                                                                     |
| Next                 | RMAP-B00 (Master Run 01 execution order)                                                                                                                                                                                                                                                                                                          |

---

## Category A — FOUNDATION PARITY

### RMAP-01 — Account / session parity (post-B00 validation in Master Run 01)

| Field                | Content                                                                                                                                                          |
| -------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Status               | **COMPLETE** — [POS-REACT-RMAP-01-account-session-parity.md](../../Reports/POS-REACT-RMAP-01-account-session-parity.md)                                          |
| Objective            | Personal email login, cookie session, profiles/session guards, CSRF, sign-out, me; final validation includes reconciled identity/session behavior after RMAP-B00 |
| Why next             | Stable session foundation on the intended identity architecture (avoids rework around duplicate-staff-principal UX)                                              |
| Dependencies         | **RMAP-B00** (Master Run 01 order); RMAP-00 for any UI polish                                                                                                    |
| Backend contracts    | Platform auth after RMAP-B00 PASS; `/me` now includes `homeOrganizationId` + `organizationContextLocked`                                                         |
| MAUI reference       | `/signin`, workspace/org select                                                                                                                                  |
| React starting point | `SignInPage`, `SessionProvider`, antiforgery                                                                                                                     |
| Owner decisions      | OD-ID-02..04, OD-ID-07; session behavior must not contradict OD-ID-01/05 after B00                                                                               |
| Exclusions           | Full staff invite/accept UX (RMAP-01b); Offline PIN; Start a Business UI                                                                                         |
| Tests                | Unit + Playwright Personal/staff login; AccountClass denial; reload; logout/CSRF                                                                                 |
| Acceptance           | Personal session + AccountClass isolation; validates against post-B00 identity/session rules                                                                     |
| Next                 | RMAP-01b                                                                                                                                                         |

### RMAP-01b — React staff identity parity (desired person-link model)

| Field           | Content                                                                                                                                          |
| --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| Status          | **COMPLETE** — reconciled by RMAP-02R — [POS-REACT-RMAP-01b-staff-identity-parity.md](../../Reports/POS-REACT-RMAP-01b-staff-identity-parity.md) |
| Objective       | React UX for inviting/accepting/staff login under **owner-approved** post-RMAP-B00 contract                                                      |
| Dependencies    | **RMAP-B00** (required), RMAP-01, RMAP-00 for UI                                                                                                 |
| Backend         | Post-RMAP-B00 contract only                                                                                                                      |
| Owner decisions | OD-ID-01, OD-ID-05, OD-ID-06, OD-ID-08                                                                                                           |
| Exclusions      | Late Personal link OPEN; implementing CURRENT duplicate-human model as final desired parity                                                      |
| Acceptance      | Matches approved person-link + alias contract; multi-org isolation; removal preserves Personal/other orgs                                        |
| Readiness flag  | `READY_FOR_REACT_STAFF_IDENTITY_PARITY` = YES                                                                                                    |
| Reconciliation  | Invite authority corrected in **RMAP-02R** (Owner membership; Manager/Cashier denied)                                                            |
| Next            | RMAP-02                                                                                                                                          |

### RMAP-02 — Workspace / org / product-access / role guards

| Field          | Content                                                                                                                                            |
| -------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| Status         | **COMPLETE** — reconciled by RMAP-02R — [POS-REACT-RMAP-02-workspace-authorization.md](../../Reports/POS-REACT-RMAP-02-workspace-authorization.md) |
| Objective      | Org context, product access, role homes, CreateSale guard correctness against **post-B00** identity model                                          |
| Dependencies   | RMAP-01, **RMAP-01b**, RMAP-00 if visual polish                                                                                                    |
| Backend        | ProductLocalRoleGrant, entitlements; session/org rules from post-B00 contract                                                                      |
| React start    | `WorkspaceProvider`, `SessionGuards`, role pages                                                                                                   |
| Exclusions     | Org admin CRUD                                                                                                                                     |
| Acceptance     | Wrong class/role cannot open sell; workspace/role guards validated using post-B00 staff/person model                                               |
| Reconciliation | Experience vs role model locked in **RMAP-02R**                                                                                                    |
| Next           | RMAP-02R                                                                                                                                           |

### RMAP-02R — Role / experience authority reconciliation

| Field        | Content                                                                                                                                   |
| ------------ | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Status       | **COMPLETE** — [POS-REACT-RMAP-02R-role-experience-reconciliation.md](../../Reports/POS-REACT-RMAP-02R-role-experience-reconciliation.md) |
| Objective    | Lock Owner/Manager/Cashier product model; separate Organization admin from POS operations; experience switch without role mutation        |
| Dependencies | RMAP-02                                                                                                                                   |
| Acceptance   | StoreManager alone denied Org Web; invite Owner-only; Owner Admin/Ops/Sell experiences; Manager Ops+Sell; Cashier Sell only               |
| Next         | RMAP-03                                                                                                                                   |

### RMAP-03 — Branch / device operational context

| Field        | Content                                                                                                               |
| ------------ | --------------------------------------------------------------------------------------------------------------------- |
| Status       | **COMPLETE** — [POS-REACT-RMAP-03-branch-device-context.md](../../Reports/POS-REACT-RMAP-03-branch-device-context.md) |
| Objective    | Bound branch, and device where genuinely required, for POS operations                                                 |
| Dependencies | RMAP-02R                                                                                                              |
| Backend      | Platform branches/devices; POS operational-branch (CURRENT)                                                           |
| React start  | workspace branch binding, `NoAccessibleBranchPage`, operational-branch after bind                                     |
| Exclusions   | Full branch fulfillment admin (RMAP-18); inventing browser PosDevice                                                  |
| Acceptance   | No accessible branch → blocked; bound context on POS calls; device deferred honestly                                  |
| Status       | **COMPLETE**                                                                                                          |
| Report       | [POS-REACT-RMAP-03-branch-device-context.md](../../Reports/POS-REACT-RMAP-03-branch-device-context.md)                |
| Next         | RMAP-04 (COMPLETE) → RMAP-05                                                                                          |

---

## Category B — DOMAIN CONTRACT GAPS (backend-first when needed)

### RMAP-B00 — Staff identity / existing-person link reconciliation

| Field     | Content                                                                                                                                                                     |
| --------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Status    | **COMPLETE** (Repair 02 + Review Repair 03). Historical hard stop retained.                                                                                                 |
| Objective | Backend/domain/auth/test reconciliation: Option C formal person-link + separate staff passwords; Personal may accept; alias remains real login; multi-org/removal isolation |
| Report    | [POS-REACT-RMAP-B00-identity-reconciliation.md](../../Reports/POS-REACT-RMAP-B00-identity-reconciliation.md)                                                                |
| Next      | Product Owner + ChatGPT review. Do **not** start RMAP-01 in this repair.                                                                                                    |

### RMAP-B01 — Sale price override backend (BLOCKING for override UI only)

| Field        | Content                                                             |
| ------------ | ------------------------------------------------------------------- |
| Status       | **BACKEND IMPLEMENTED** — React RMAP-12b **COMPLETE** (see RMAP-12b report)                |
| Objective    | Domain+API+tests for role-gated override, ≤100% manager ceiling, reason, audit |
| Why          | Locked PO policy (Cashier DENY / Manager ≤100% / Owner unlimited); CashierAdjustable **SUPERSEDED** |
| Dependencies | Catalog product model                                               |
| Backend      | **DONE** — UD-02 resolved for backend; report [POS-REACT-RMAP-B01-sale-price-override-backend.md](../../Reports/POS-REACT-RMAP-B01-sale-price-override-backend.md) |
| MAUI         | Optional regression                                                 |
| React        | **RMAP-12b only after this backend**                                |
| Exclusions   | React override UI; per-product Fixed/CashierAdjustable              |
| Next         | RMAP-12b React override UX                                          |

### RMAP-B02 — Milligram UOM (OPTIONAL)

| Field     | Content                                        |
| --------- | ---------------------------------------------- |
| Objective | Resolve UD-01; add enum only if owner confirms |
| Blocking? | NO for core parity                             |
| Next      | Catalog UOM picker update if approved          |

### RMAP-B03 — Sale discount / adjustment backend contract

| Field                        | Content                                                                                                    |
| ---------------------------- | ---------------------------------------------------------------------------------------------------------- |
| Status                       | **FINAL CLOSED** (backend + payment-boundary closeout)                                                     |
| Objective                    | Backend-first commercial sale discount / adjustment contract                                               |
| Blocking?                    | YES for discount UI                                                                                        |
| Report                       | [POS-REACT-RMAP-B03-sale-discount-contract.md](../../Reports/POS-REACT-RMAP-B03-sale-discount-contract.md) |
| Closeout                     | [POS-REACT-RMAP-B03-final-closeout.md](../../Reports/POS-REACT-RMAP-B03-final-closeout.md)                 |
| Distinct from                | Today's Price · Cashier Price Override · Promotion · Regulatory Discount                                   |
| Current payments             | Cash · GCash (`ManualGCash`) · Utang                                                                       |
| Future payments (infra only) | Card · provider/API GCash                                                                                  |
| Exclusions                   | React discount UX (**RMAP-11b**); promotions; regulatory; RMAP-TAX; provider payment UX                    |
| Next                         | RMAP-08 COMPLETE — next commercial UX packages remain gated                                                |

### RMAP-B04 — Linked ExItS buyer purchase projection

| Field     | Content                                                                                                               |
| --------- | --------------------------------------------------------------------------------------------------------------------- |
| Status    | **COMPLETE**                                                                                                          |
| Report    | [POS-REACT-RMAP-B04-linked-buyer-purchase-history.md](../../Reports/POS-REACT-RMAP-B04-linked-buyer-purchase-history.md) |
| Objective | Read-only projection of seller-owned Completed sales into authenticated Personal/Organization buyer purchase history  |
| Blocking? | YES for buyer purchase-history UI                                                                                     |
| Rules     | Seller Sale remains authoritative; no ownership transfer; no cross-org DB shortcut; privacy/retention review required |
| Delivered | Personal linked-merchant statement + lazy receipt (Phase-24 APIs); Organization buyer **unsupported** (no API contract) |
| Next      | RMAP-23 hardening                                                                                                     |

---

## Category C — REACT PARITY (core POS)

Visual packages below depend on **RMAP-00** unless noted non-UI.

### RMAP-04 — Catalog admin parity

| Field        | Content                                                                                              |
| ------------ | ---------------------------------------------------------------------------------------------------- |
| Status       | **COMPLETE**                                                                                         |
| Objective    | Categories/products CRUD, flags, images, SKU/barcode                                                 |
| Dependencies | RMAP-03, **RMAP-00**                                                                                 |
| Backend      | CURRENT                                                                                              |
| MAUI         | `/catalog*`                                                                                          |
| React        | `/catalog*` admin pages + ManageCatalog gate                                                         |
| Report       | [POS-REACT-RMAP-04-catalog-admin-parity.md](../../Reports/POS-REACT-RMAP-04-catalog-admin-parity.md) |
| Exclusions   | Global import advanced jobs can follow RMAP-04b; UOM/prices/inventory deferred                       |
| Next         | RMAP-05                                                                                              |

### RMAP-05 — Base UOM + SellingMode + product units

| Field        | Content                                                                                |
| ------------ | -------------------------------------------------------------------------------------- |
| Status       | **COMPLETE**                                                                           |
| Objective    | Base UOM, ByWeight, Purchase/Sell units, MultiplierToBase, independent unit prices     |
| Dependencies | RMAP-04, RMAP-00                                                                       |
| Backend      | CURRENT multi-UOM                                                                      |
| React        | Product form UOM/mode/packages                                                         |
| Report       | [POS-REACT-RMAP-05-product-units.md](../../Reports/POS-REACT-RMAP-05-product-units.md) |
| Exclusions   | Milligram unless RMAP-B02; Open Sack workflow                                          |
| Next         | RMAP-06                                                                                |

### RMAP-06 — Today’s Prices

| Field             | Content                                                                                                      |
| ----------------- | ------------------------------------------------------------------------------------------------------------ |
| Status            | **COMPLETE — validation closeout complete**                                                                  |
| Objective         | Bulk current selling price updates with concurrency                                                          |
| Report            | [POS-REACT-RMAP-06-todays-prices.md](../../Reports/POS-REACT-RMAP-06-todays-prices.md)                       |
| Shared impl SHA   | `d3e4e3da` (with RMAP-07; history not rewritten)                                                             |
| Validation repair | `cb91145b`                                                                                                   |
| Exclusions        | Cashier override (RMAP-B01); commercial discount UX deferred to **RMAP-11b** (backend RMAP-B03 FINAL CLOSED) |
| Next              | RMAP-07 COMPLETE — ChatGPT review / HARD STOP                                                                |

### RMAP-07 — Inventory tracking + movements + opening stock

| Field             | Content                                                                        |
| ----------------- | ------------------------------------------------------------------------------ |
| Status            | **COMPLETE — validation closeout complete**                                    |
| Objective         | Enable/disable tracking, opening, adjustments, on-hand, movements, oversell    |
| Report            | [POS-REACT-RMAP-07-inventory.md](../../Reports/POS-REACT-RMAP-07-inventory.md) |
| Shared impl SHA   | `d3e4e3da` (with RMAP-06; history not rewritten)                               |
| Validation repair | `cb91145b`                                                                     |
| Exclusions        | Lots/expiry (RMAP-08 COMPLETE)                                                 |
| Next              | RMAP-08 COMPLETE                                                               |

### RMAP-08 — Lots / expiry / FEFO (optional track)

| Field              | Content                                                                                      |
| ------------------ | -------------------------------------------------------------------------------------------- |
| Status             | **COMPLETE**                                                                                 |
| Objective          | TracksExpiration + lots + expiry inventory surfaces (not checkout FEFO)                      |
| Dependencies       | RMAP-07, RMAP-00                                                                             |
| Backend            | CURRENT                                                                                      |
| Report             | [POS-REACT-RMAP-08-lots-expiry-fefo.md](../../Reports/POS-REACT-RMAP-08-lots-expiry-fefo.md) |
| Implementation SHA | `4c38bb0e`                                                                                   |
| Exclusions         | Checkout FEFO allocation (**RMAP-11**); Card/provider payments                               |
| Owner              | OD-EXP-*                                                                                     |
| Next               | RMAP-09                                                                                      |

### RMAP-09 — Sell floor + cart parity (units/weight/stock)

| Field              | Content                                                                                    |
| ------------------ | ------------------------------------------------------------------------------------------ |
| Status             | **COMPLETE**                                                                               |
| Objective          | Search/categories/barcode, sell-unit selection, ByWeight, stock hints, cart edits          |
| Dependencies       | RMAP-05, RMAP-07, RMAP-00                                                                  |
| React start        | `SellFloorPage`, `SessionCartProvider`                                                     |
| Report             | [POS-REACT-RMAP-09-sell-floor-cart.md](../../Reports/POS-REACT-RMAP-09-sell-floor-cart.md) |
| Implementation SHA | `ae433fd2`                                                                                 |
| Exclusions         | Pay/checkout; FEFO allocation; camera barcode (deferred)                                   |
| Next               | RMAP-10 COMPLETE                                                                           |

### RMAP-10 — Registers + open shift gate

| Field        | Content                                                                                        |
| ------------ | ---------------------------------------------------------------------------------------------- |
| Status       | **COMPLETE**                                                                                   |
| Objective    | Register awareness + open shift required for checkout readiness                                |
| Dependencies | RMAP-03, RMAP-00, RMAP-09                                                                      |
| Backend      | CURRENT                                                                                        |
| React        | `/registers`, `/shifts*`, `ShiftContextProvider`, checkout readiness gate (Pay still disabled) |
| Report       | [POS-REACT-RMAP-10-register-shift.md](../../Reports/POS-REACT-RMAP-10-register-shift.md)       |
| Exclusions   | Sale POST (RMAP-11); inventing PosDevice; register CRUD admin UX                               |
| Next         | RMAP-10b                                                                                       |

### RMAP-10b — Browser POS device authorization

| Field        | Content                                                                                                                                     |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------- |
| Status       | **COMPLETE**                                                                                                                                |
| Objective    | Durable browser installation identity + Platform PosDevice register/redeem/authorize so money POS can attach `X-Pos-Installation-Device-Id` |
| Dependencies | RMAP-03, RMAP-10, RMAP-00                                                                                                                   |
| Backend      | CURRENT (+ branch-conflict / staff redeem ACL hardening)                                                                                    |
| React        | `browser-installation-identity`, `pos-devices-client`, `/org/devices`, `/devices/register`, hydrate authorize, POS header                   |
| Report       | [POS-REACT-RMAP-10b-browser-pos-device.md](../../Reports/POS-REACT-RMAP-10b-browser-pos-device.md)                                          |
| Exclusions   | Sale POST (RMAP-11); Dev money bypass; Capacitor; inventing fake authorized terminals                                                       |
| Next         | RMAP-11                                                                                                                                     |

### RMAP-11 — Checkout / sale (online cash first)

| Field                  | Content                                                                                                                               |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| Status                 | **COMPLETE**                                                                                                                          |
| Objective              | POST sales with snapshots; cash path; inventory effects; idempotency; Transaction Summary wording                                     |
| Dependencies           | RMAP-09, RMAP-10, RMAP-10b, RMAP-07, RMAP-00                                                                                          |
| Backend                | CURRENT                                                                                                                               |
| Current payment labels | Cash · GCash · Utang (GCash maps to internal `ManualGCash`)                                                                           |
| Exclusions             | Offline outbox (RMAP-21); price override (needs RMAP-B01); TaxDocument; Card/provider GCash UX; commercial discount UX (**RMAP-11b**) |
| Acceptance             | Completes sale online; tracked stock cannot oversell; document = Transaction Summary                                                  |
| Report                 | [POS-REACT-RMAP-11-checkout-sale.md](../../Reports/POS-REACT-RMAP-11-checkout-sale.md)                                                |
| Next                   | RMAP-11b                                                                                                                              |

### RMAP-11b — Commercial Discount UX

| Field        | Content                                                                                                                                                                    |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Status       | **COMPLETE**                                                                                                                                                               |
| Objective    | Expose the authoritative RMAP-B03 commercial discount contract in React checkout                                                                                           |
| Dependencies | RMAP-11, RMAP-B03 FINAL CLOSED, RMAP-00                                                                                                                                    |
| Scope        | Line + sale discount; percent + fixed; required reason; server quote/preview; Owner/Manager auth; Cashier denied by default; friendly Gross/Discount/Amount to Pay wording |
| Exclusions   | Price override (RMAP-12b / B01); promotions/coupons; regulatory Senior/PWD discounts; RMAP-TAX; Card/provider-payment implementation                                       |
| Acceptance   | Quote + checkout with intents; cashier denied; zero-total Cash with “No payment required”                                                                                  |
| Report       | [POS-REACT-RMAP-11b-commercial-discount-ux.md](../../Reports/POS-REACT-RMAP-11b-commercial-discount-ux.md)                                                                 |
| Next         | RMAP-12                                                                                                                                                                    |

### RMAP-12 — Payments expansion + void

| Field        | Content                                                                                                                            |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------------- |
| Objective    | Current GCash (`ManualGCash`) / Utang online paths; void; preserve future Card/provider GCash infra without making them current UX |
| Dependencies | RMAP-11, RMAP-00                                                                                                                   |
| Status       | **COMPLETE** — report [POS-REACT-RMAP-12-payments-void.md](../../Reports/POS-REACT-RMAP-12-payments-void.md)                       |
| Next         | RMAP-13                                                                                                                            |

### RMAP-12b — Cashier price override UI (only after RMAP-B01)

| Field        | Content                                                                                                                      |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------- |
| Objective    | Policy-gated override + reason + audit display                                                                               |
| Dependencies | RMAP-B01, RMAP-11, RMAP-00                                                                                                   |
| Owner        | OD-PRICE-02..05                                                                                                              |
| Status       | **COMPLETE** — report [POS-REACT-RMAP-12b-price-override.md](../../Reports/POS-REACT-RMAP-12b-price-override.md)             |
| Next         | Do **not** start B04/B05/RMAP-21/TAX until authorized                                                                        |

### RMAP-13 — Customers + Business Utang

| Field        | Content                                                                                                          |
| ------------ | ---------------------------------------------------------------------------------------------------------------- |
| Objective    | Customers, credit, repayments, statements                                                                        |
| Dependencies | RMAP-11, RMAP-00                                                                                                 |
| Status       | **COMPLETE** — report [POS-REACT-RMAP-13-customers-utang.md](../../Reports/POS-REACT-RMAP-13-customers-utang.md) |
| Next         | RMAP-14                                                                                                          |

### RMAP-14 — Returns / refunds

| Field        | Content                                                                                                                                            |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| Objective    | Partial returns, restock, inventory restore                                                                                                        |
| Dependencies | RMAP-11, RMAP-00                                                                                                                                   |
| Status       | **COMPLETE** — `RMAP_14_FINAL=APPROVED`; React UI started; concurrency gaps CLOSED ([report](../../Reports/POS-REACT-RMAP-14-returns-refunds.md)). |
| Next         | RMAP-15/16/17 complete when authorized; do **not** start RMAP-18 until authorized.                                                                 |

### RMAP-15 — Manual suppliers

| Field        | Content                                                                                                                              |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------ |
| Objective    | Supplier CRUD                                                                                                                        |
| Dependencies | RMAP-03, RMAP-00                                                                                                                     |
| Status       | **COMPLETE** — React manual suppliers UI ([report](../../Reports/POS-REACT-RMAP-15-suppliers.md)); `RMAP_15_NATIVE_SPEAKER=PENDING`. |
| Next         | RMAP-16 **COMPLETE** (authorized); see RMAP-16                                                                                       |

### RMAP-16 — Connected suppliers

| Field        | Content                                                                                                                                           |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| Objective    | Connect, expose≠share, buyer prices, links                                                                                                        |
| Dependencies | RMAP-15, RMAP-04, RMAP-00                                                                                                                         |
| Invariants   | EXPOSABLE≠SHARED; no inventory on share                                                                                                           |
| Status       | **COMPLETE** — React connected suppliers UI ([report](../../Reports/POS-REACT-RMAP-16-connected-suppliers.md)); `RMAP_16_NATIVE_SPEAKER=PENDING`. |
| Next         | RMAP-17 authorized and completed after this package.                                                                                              |

### RMAP-17 — Purchasing + goods receipt

| Field        | Content                                                                                                                                                                                                     |
| ------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Objective    | PO lifecycle + receive-only inventory; connected PO receive                                                                                                                                                 |
| Dependencies | RMAP-15/16, RMAP-07, RMAP-00                                                                                                                                                                                |
| Invariants   | PO create/submit never increases inventory; stock only on goods receipt / direct purchase                                                                                                                   |
| Status       | **COMPLETE** — React purchasing + GRN + direct purchase ([report](../../Reports/POS-REACT-RMAP-17-purchasing-receiving.md)); `RMAP_17_NATIVE_SPEAKER=PENDING`; Direct purchase **PASS** (not CONTRACT_GAP). |
| Next         | RMAP-18 authorized and completed after this package.                                                                                                                                                        |

---

## Category D — EXTENDED COMMERCE

### RMAP-18 — Branch fulfillment admin + readiness

| Field        | Content                                                                                                                                                                                                                               |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Objective    | Address/coords/hours/pickup/delivery config in React                                                                                                                                                                                  |
| Dependencies | RMAP-03, RMAP-00                                                                                                                                                                                                                      |
| Owner        | OD-DEL-01                                                                                                                                                                                                                             |
| Status       | **COMPLETE** — React branch fulfillment admin + readiness ([report](../../Reports/POS-REACT-RMAP-18-branch-fulfillment.md)); `RMAP_18_NATIVE_SPEAKER=PENDING`; `RMAP_B05_AUTHORIZED=NO` not started; `RMAP18_SCHEMA_CONTRACT_GAP=NO`. |
| Next         | RMAP-19 authorized and **COMPLETE** (see RMAP-19). Do **not** start RMAP-B05.                                                                                                                                                         |

### RMAP-19 — Customer ordering / storefront / pickup / delivery

| Field        | Content                                                                                                                                                                                                                                                   |
| ------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Objective    | Buyer shop + seller order ops                                                                                                                                                                                                                             |
| Dependencies | RMAP-18, RMAP-07, catalog, RMAP-00                                                                                                                                                                                                                        |
| Status       | **COMPLETE** — React customer storefront (linked merchants) + seller order queue ([report](../../Reports/POS-REACT-RMAP-19-customer-ordering.md)); `RMAP_19_NATIVE_SPEAKER=PENDING`; `RMAP_B05_AUTHORIZED=NO` not started / not accidentally implemented. |
| Next         | RMAP-20 authorized and **COMPLETE** (see RMAP-20). Do **not** start RMAP-B05.                                                                                                                                                                              |

### RMAP-20 — Reports + dashboard

| Field         | Content                                                                                                                                                                                                                                                                                                                                   |
| ------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Objective     | Management overview + operational reports (no fake P&L)                                                                                                                                                                                                                                                                                   |
| Dependencies  | Prior sales/inventory/expense packages, RMAP-11+, RMAP-00                                                                                                                                                                                                                                                                                 |
| Status        | **COMPLETE** — React management dashboard + operational/classic reports ([report](../../Reports/POS-REACT-RMAP-20-reports-dashboard.md)); Tax UI exposed **NO**; Fake P&L **NO**; Buyer purchase projection **NO**; `RMAP_20_NATIVE_SPEAKER=PENDING`.                                                                                    |
| Future gating | Default org: **no tax-specific report navigation**. When TAX_ACTIVE (RMAP-TAX): tax report sections may appear. Commercial discount reporting (Gross / Commercial Discounts / Net) is independent and is **not** a statutory tax report unless RMAP-TAX defines it. Buyer purchase-history projection is not seller reporting (RMAP-B04). |
| Next          | **HARD STOP.** Do **not** start RMAP-21 until authorized. Do **not** start RMAP-TAX or RMAP-B04.                                                                                                                                                                                                                                            |

---

## Category E — HARDENING / VALIDATION

### RMAP-22 — Personal surface parity (Utang, To-do, Stores, Start Business)

| Field                            | Content                                                                                                                                                                                                                                                                 |
| -------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Objective                        | Replace thin React Personal shell with real Personal journeys (Utang-first Home, Utang core, invitations/reminders, To-do domain+UI, stores/ordering, Start Business, integrated Personal↔Business E2E)                                                              |
| Dependencies                     | RMAP-01, RMAP-00; RMAP-19 for ordering reuse                                                                                                                                                                                                                            |
| Execution order (PO decision)    | **Pulled forward before RMAP-21 Offline** so the SaaS ecosystem can be validated end-to-end while Personal was only a thin shell. Historic IDs are **not** renumbered. See [Personal implementation roadmap](../Personal/personal-implementation-roadmap.md). |
| Status                           | **COMPLETE / APPROVED** — Personal Master Run 01 + Review Repair 01. Owner quick-fix polish accepted as RMAP-21 start baseline (`86ded438`). |
| Exclusions                       | RMAP-B04 buyer purchase projection; RMAP-B05; RMAP-TAX; production cutover; Loan SaaS                                                                                                                                                                   |
| Next                             | RMAP-21 Offline Master Run 01 **COMPLETE** (awaiting PO review)                                                                                                                                                                                                 |

### RMAP-21 — Offline / LocalStore / outbox (POS + Personal)

| Field        | Content                                                                                                                                                          |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Objective    | Warm-session offline: IndexedDB LocalStore + encrypted outbox; real Connection & Sync; selective Cash Sell; business customers/credit; Personal Utang; Personal To-do; reconnect/E2E |
| Dependencies | RMAP-11, RMAP-13, RMAP-22 Personal online; [offline capability matrix](../Offline/react-pwa-offline-capability-matrix.md) |
| Status       | **COMPLETE / AWAITING PO REVIEW** — Master Run 01 (`RMAP_21_AUTHORIZED=YES`). Start `86ded438` → end `5ef9109a`. Packages 21A.0–21H delivered. |
| Scope        | POS selective offline + Business customer offline + Personal Utang offline + Personal To-do offline |
| Exclusions   | Offline inventory/purchasing/suppliers/reports/branch admin/staff admin/billing; GCash; Business Utang checkout; discount/override; lot/expiry (fail closed); cold-start unlock = `DEFERRED_SECURITY_GAP` |
| Next         | **HARD STOP** for Product Owner + ChatGPT review. Do not start RMAP-23 until authorized. Report: [POS-REACT-RMAP-21-OFFLINE-MASTER-RUN-01.md](../../Reports/POS-REACT-RMAP-21-OFFLINE-MASTER-RUN-01.md) |

### RMAP-23 — Parity / security / UX hardening

| Field        | Content                                                                      |
| ------------ | ---------------------------------------------------------------------------- |
| Objective    | Authz matrix, cross-org denials, wording, a11y, performance, responsive debt |
| Dependencies | Core WPs, RMAP-00                                                            |
| Next         | RMAP-TAX                                                                     |

### RMAP-TAX — Final controlled tax activation

| Field                   | Content                                                                                                                                                                                                                                                             |
| ----------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Status                  | **NOT STARTED**                                                                                                                                                                                                                                                     |
| Position                | After RMAP-23; before RMAP-24                                                                                                                                                                                                                                       |
| Objective               | Platform compliance approval → TAX_SETUP_REQUIRED → TAX_ACTIVE UX; capability-gated menus/checkout/reports; discount/tax interaction validation; Transaction Summary vs future TaxDocument; suspension/revocation; offline fail-closed; BIR/legal/accounting review |
| Default org UX (future) | TAX_NOT_AVAILABLE — no tax menu/widgets/reports; checkout does not apply ExItS tax; document = Transaction Summary                                                                                                                                                  |
| After Platform approval | TAX_SETUP_REQUIRED then TAX_ACTIVE after valid setup                                                                                                                                                                                                                |
| Explicit non-claims     | Not BIR certification; not government approval                                                                                                                                                                                                                      |
| Next                    | RMAP-24                                                                                                                                                                                                                                                             |

### RMAP-24 — E2E validation matrix execution

| Field        | Content                                                                         |
| ------------ | ------------------------------------------------------------------------------- |
| Objective    | Execute [validation-matrix.md](validation-matrix.md) owner + automated evidence |
| Dependencies | RMAP-23; RMAP-TAX when tax paths are claimed                                    |
| Next         | STOP — owner review for production readiness claims                             |

---

## Already shipped on branch (not re-proposed)

Scaffold, PWA shell, browser session/workspace, sell-floor shell, session cart, preferences — treat as **starting capital**, not sales parity. **RMAP-00** shared UI foundation is COMPLETE (reuse in later visual WPs).

## Package count (after DOCS-RECONCILIATION-01)

| Category           | Count                                                                                      |
| ------------------ | ------------------------------------------------------------------------------------------ |
| UI foundation      | 1 (RMAP-00)                                                                                |
| Foundation         | 4 (RMAP-01, 01b, 02, 03)                                                                   |
| Backend gaps       | 5 (B00; B01; B02 optional Milligram; B03 COMPLETE; B04 NOT STARTED) + RMAP-TAX NOT STARTED |
| Core React parity  | 14 (RMAP-04..17 incl 12b)                                                                  |
| Extended commerce  | 3 (18..20)                                                                                 |
| Hardening          | 5 (21..24 + RMAP-TAX)                                                                      |
| **Total proposed** | **31+** (including optional B02, 12b, B04, RMAP-TAX)                                       |

## Backend-before-React list

1. **RMAP-B00** staff existing-person link — required before RMAP-01 final validation, RMAP-01b, and RMAP-02 in Master Run 01
2. **RMAP-B01** sale price policy — required before override UI
3. **RMAP-B02** Milligram — only if owner approves
4. **RMAP-B03** Sale discount / adjustment backend contract — **FINAL CLOSED**. Discount React UX = **RMAP-11b** (**COMPLETE**).
5. **RMAP-B04** Linked ExItS buyer purchase projection — **NOT STARTED**
6. **RMAP-TAX** Final controlled tax activation — **NOT STARTED** (after RMAP-23, before RMAP-24)

## APPROVED PROPOSED MASTER RUN 01

**Name:** Foundation + Catalog/Inventory Baseline
**Status:** RMAP-00…RMAP-20 PASS for delivered packages. Master Run 02 Review Repair 01/02 closed expiry-return, refund fidelity, and return/void concurrency. RMAP-14 React returns/refunds **COMPLETE**. RMAP-15 React manual suppliers **COMPLETE** (`RMAP_15_NATIVE_SPEAKER=PENDING`). RMAP-16 React connected suppliers **COMPLETE** (`RMAP_16_NATIVE_SPEAKER=PENDING`). RMAP-17 React purchasing + GRN + direct purchase **COMPLETE** (`RMAP_17_NATIVE_SPEAKER=PENDING`). RMAP-18 React branch fulfillment admin **COMPLETE** (`RMAP_18_NATIVE_SPEAKER=PENDING`; `RMAP_B05_AUTHORIZED=NO` not started). RMAP-19 React customer ordering / storefront / pickup / delivery **COMPLETE** (`RMAP_19_NATIVE_SPEAKER=PENDING`; `RMAP_B05 accidentally implemented=NO`). RMAP-20 React reports + management dashboard **COMPLETE** (`RMAP_20_NATIVE_SPEAKER=PENDING`; Tax UI exposed **NO**; Fake P&L **NO**; Buyer purchase projection **NO**). RMAP-B01 backend + RMAP-12b React price override **COMPLETE**. **Product Owner decision:** execute **RMAP-22 Personal Master Run 01 before RMAP-21 Offline** (order change only; RMAP-21 not cancelled). `RMAP_21_AUTHORIZED=NO` until separately authorized after Personal online validation.
**Stop rule:** After Master Run packages + authorized B03 closeout → HARD STOP for Product Owner + ChatGPT review (historical). RMAP-08/09 completed after that stop when authorized. After RMAP-14 → HARD STOP pending RMAP-15 authorization (historical). After RMAP-15 → HARD STOP pending RMAP-16 authorization (historical). After RMAP-16 → HARD STOP pending RMAP-17 authorization (historical). After RMAP-17 → HARD STOP pending RMAP-18 authorization (historical). After RMAP-18 → HARD STOP pending RMAP-19 authorization (historical). After RMAP-19 → HARD STOP pending RMAP-20 authorization (historical). After RMAP-20 / 12b → Product Owner authorized **RMAP-22 Personal** ahead of RMAP-21.
**Do not include (still gated):** RMAP-23; RMAP-B04; RMAP-B05 (`RMAP_B05_AUTHORIZED=NO`); RMAP-TAX implementation; production cutover
**Active authorized track:** RMAP-21 Offline Master Run 01 **COMPLETE** (awaiting PO review) — [master report](../../Reports/POS-REACT-RMAP-21-OFFLINE-MASTER-RUN-01.md). Prior: RMAP-22 Personal Master Run 01 **APPROVED**; Owner quick-fix polish accepted at `86ded438`.
**Completed beyond Master Run 01 table:** RMAP-08 lots/expiry inventory surfaces; RMAP-09 sell floor + session cart; RMAP-10 registers + open shift gate; RMAP-10b browser POS device authorization; RMAP-11 online cash checkout; RMAP-11b commercial discount UX; RMAP-12 current payments (Cash/GCash/Utang) + void; RMAP-13 customers + Business Utang; RMAP-14 returns / refunds; RMAP-15 manual suppliers; RMAP-16 connected suppliers; RMAP-17 purchasing + goods receipt + direct purchase; RMAP-18 branch fulfillment admin + readiness; RMAP-19 customer ordering / storefront / pickup / delivery (linked merchants — no B05 public landing); RMAP-20 reports + management dashboard (no tax UI; no fake P&L; no B04 buyer purchase projection)
**Distinction preserved:** Today's Price ≠ Cashier Price Override ≠ Commercial Discount ≠ Promotion ≠ Regulatory Discount
**Completion report (B00):** [POS-REACT-RMAP-B00-identity-reconciliation.md](../../Reports/POS-REACT-RMAP-B00-identity-reconciliation.md)
**Completion report (RMAP-01):** [POS-REACT-RMAP-01-account-session-parity.md](../../Reports/POS-REACT-RMAP-01-account-session-parity.md)

**Historical stop:** [POS-REACT-RMAP-B00-identity-hard-stop.md](../../Reports/POS-REACT-RMAP-B00-identity-hard-stop.md)

| #   | ID           | Title                                                              |
| --- | ------------ | ------------------------------------------------------------------ |
| 01  | **RMAP-00**  | React Shared UI/UX & Responsive Foundation                         |
| 02  | **RMAP-B00** | Staff identity / existing-person link reconciliation               |
| 03  | **RMAP-01**  | Account/session parity (validate post-B00)                         |
| 04  | **RMAP-01b** | React staff identity parity under post-B00 contract                |
| 05  | **RMAP-02**  | Workspace / organization / product-access / role guards (post-B00) |
| 06  | **RMAP-03**  | Branch / device operational context                                |
| 07  | **RMAP-04**  | Catalog admin parity                                               |
| 08  | **RMAP-05**  | Base UOM + SellingMode + product units / multi-UOM                 |
| 09  | **RMAP-06**  | Today’s Prices                                                     |
| 10  | **RMAP-07**  | Inventory tracking + opening stock + movements                     |

### Master Run 01 dependency intent

| Package  | Intent                                                                                                            |
| -------- | ----------------------------------------------------------------------------------------------------------------- |
| RMAP-00  | Independent UI prerequisite; first so later visual work reuses shared components                                  |
| RMAP-B00 | Backend identity reconciliation early enough that session/workspace/role work validates the intended architecture |
| RMAP-01  | Account/session parity after B00 so final validation includes reconciled identity/session behavior                |
| RMAP-01b | Explicitly depends on B00 + RMAP-01 + RMAP-00                                                                     |
| RMAP-02  | Validates role/workspace against **post-B00** identity model — not pre-B00 duplicate-staff-principal as final     |
| RMAP-03+ | Build on reconciled identity/workspace foundation through catalog/inventory baseline                              |

Execution order (not an inherent UI↔backend domain coupling):

```text
RMAP-00 → RMAP-B00 → RMAP-01 → RMAP-01b → RMAP-02 → RMAP-02R → RMAP-03 → RMAP-04 → RMAP-05 → RMAP-06 → RMAP-07
→ HARD STOP
```

Do **not** start any package until Product Owner + ChatGPT issue the Master Run 01 implementation command.
