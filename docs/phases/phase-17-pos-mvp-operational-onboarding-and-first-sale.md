# Phase 17 — POS MVP Operational Onboarding and First Sale

[Architecture](../architecture/product-catalog-entitlement-and-role-model.md) | [Client experience boundaries](../architecture/client-experience-boundaries.md) | [Portfolio](../portfolio-progress.md) | [UI standards](../ui/ant-design-admin-ui-standards.md)

## Status

**Complete** (with documented residuals; closed 2026-07-29; post-implementation validation alignment 2026-07-29).

Phase 17 delivered POS operational onboarding and the first cash-sale journey on top of existing Phases 5–10 / P16 handoff capabilities. Phase 14 Production Deployment remains separate and unfinished.

The application remains **not production-ready**.

### Validation status (post-implementation)

| Layer | Status |
|---|---|
| Backend / domain / API workflow | **Validated** (unit + integration suites; provisioning aligned) |
| MAUI implementation | **Superseded by Phase 18 for Mobile Org essentials / Start Selling** — Phase 17 delivered setup/sales/shifts; Phase 18 delivered Personal/Org essentials and role homes (**Complete (implementation/scope)** — partial phone validation; Not Device Verified) per [Phase 18](phase-18-mobile-personal-organization-and-pos-experience.md); remaining ops UX continues in [Phase 19](phase-19-mobile-pos-operations-and-cashier-experience.md) |
| Device / Android SDK validation | **Not run** (Android SDK unavailable on validation host) |

### Client experience (authoritative)

Per [client-experience-boundaries](../architecture/client-experience-boundaries.md):

- Platform Administration = Web only
- Personal Account = Mobile
- Organization Owner essentials = Mobile (profile, subscription/entitlement status, staff invite, POS role assign/revoke, launch POS setup, Start Selling)
- Full Organization Administration = Web
- POS operations = Mobile

Organization Administration is **not** Web-only: Mobile owns the practical Owner essentials; Web owns full control.

Business creator provisioning:

```text
Start a Business
→ single Organization Owner
→ POS entitlement active
→ first POS Owner role granted
```

Organization Owner alone never grants POS access. Other members need an explicit POS role.

POS hierarchy: Owner ⊇ Manager ⊇ Cashier. Start Selling changes interface mode only; it does not change the POS role.

| Work Package | Status | Report |
|---|---|---|
| P17-WP01 | **Complete** | [report](../reports/P17-WP01-pos-access-handoff.md) |
| P17-WP02 | **Complete** | [report](../reports/P17-WP02-initial-pos-setup.md) |
| P17-WP03 | **Complete** | [report](../reports/P17-WP03-product-and-inventory-setup.md) |
| P17-WP04 | **Complete** | [report](../reports/P17-WP04-pos-staff-and-role-access.md) |
| P17-WP05 | **Complete** | [report](../reports/P17-WP05-register-and-shift-operations.md) |
| P17-WP06 | **Complete** | [report](../reports/P17-WP06-cash-sale-and-receipt.md) |
| P17-WP07 | **Complete** | [report](../reports/P17-WP07-void-refund-and-audit.md) |
| P17-WP08 | **Complete** | [report](../reports/P17-WP08-reports-hardening-and-closeout.md) |

---

## 1. Objective

Deliver a complete MVP first-sale journey:

```text
POS Owner launches POS
→ completes initial setup
→ creates products
→ assigns POS Cashier
→ Cashier signs in
→ starts shift
→ completes a cash sale
→ receipt is generated
→ inventory is reduced
→ Cashier closes shift
→ authorized Owner/Manager views the daily report
```

All access remains organization-isolated, entitlement-protected, and role-protected.

Authoritative access rule:

```text
Active organization membership
+ active POS entitlement
+ active product-local POS role
= POS access
```

Organization Owner alone must **not** grant POS access.

---

## 2. Current-state reconciliation

PinoyBusinessPOS already implements most operational MVP surfaces from Phases 5–10 and the Phase 16 launch handoff:

| Area | Existing state | Phase 17 action |
|---|---|---|
| Platform → POS launch | P16-WP09 product discovery, launch, product-local roles, bearer introspect, commercial + role middleware | **Reconcile / harden** unauthorized, no-entitlement, no-role UX; preserve API fail-closed |
| Catalog / inventory | Categories, products, SKU, barcode, price, stock, low-stock, search | **Reuse**; fill gaps only |
| POS roles | Owner, Admin, StoreManager, Cashier, InventoryStaff, ReportingUser + assign/revoke | **Reuse**; map MVP labels Owner/Manager/Cashier; keep org/POS role separation |
| Registers / shifts | Default-capable registers, open/close shift, opening/closing cash, variance | **Reuse**; ensure one default register from setup |
| Cash sale / receipt | Checkout, tender, change, sale number, stock deduction, history | **Reuse**; enrich receipt fields from store setup + tax mode |
| Void / returns | Void with reason; sale returns with stock restore; Manager/Owner capabilities | **Reuse**; document MVP controls |
| Reports | Sales summary, shifts, inventory/low-stock, operational reports | **Reuse**; sales-by-cashier where missing |
| **Operational store setup** | **Missing** (client onboarding ≠ store setup) | **Implement** (WP02) |

POS operational UI remains **MAUI**. Platform Admin remains Blazor Ant Design. Do not redesign unrelated Admin pages.

---

## 3. Scope

### In scope

1. Confirm platform-to-product access handoff (membership + entitlement + POS role).
2. First-time POS operational setup for POS Owner (store name, PHP, tax mode, receipt info, default register, completion/resume).
3. Minimum catalog and inventory for first sale (reuse).
4. POS staff role assignment/revocation (reuse + membership guard documentation/hardening).
5. Register and cashier shift MVP flow (reuse).
6. Cash sale, receipt, stock reduction, sales history (reuse + receipt enrichment).
7. Minimum void/refund/return controls with audit (reuse).
8. MVP daily/shift/stock reports, hardening, localization where supported, documentation closeout.

### Architecture boundaries (non-negotiable)

- Platform owns identity, organizations, subscriptions, SaaS payments, entitlements, Platform Administration.
- POS owns operational POS data, roles, registers, shifts, products, inventory, sales, receipts, refunds, POS reporting.
- POS money is separate from SaaS subscription/payment records.
- POS uses its own database and `pos` schema.
- No cross-product database access or cross-database foreign keys.
- POS API must not trust UI-only authorization.

---

## 4. Work packages

### P17-WP01 — POS Access Handoff

Validate active membership, entitlement, and product-local POS role; load organization context; deny when any requirement is missing; prevent cross-organization access; clear unauthorized / no-entitlement / no-role states; API authorization preserved when UI is hidden.

### P17-WP02 — Initial POS Setup

POS Owner completes: business/store display name; currency PHP; tax-inclusive or tax-exclusive behavior; receipt information; one default store context; one default register; setup completion + resume; Cashier cannot change setup; Owner may manage after onboarding.

### P17-WP03 — Product and Inventory Setup

Categories; product CRUD; name, SKU, optional barcode, selling price, stock, low-stock, active/inactive; search; stock validation on sale. Defer suppliers/PO/warehouses/variants unless already required by existing architecture (already present modules remain available but are not Phase 17 MVP gates).

### P17-WP04 — POS Staff and Role Access

Assign/revoke POS Owner, Manager (StoreManager), Cashier only for active org members; role-based nav + API auth; immediate denial after suspension / entitlement removal / role revoke; audit; no custom roles; Organization Staff creation does not auto-grant POS access.

### P17-WP05 — Register and Shift Operations

One default register; start shift with opening cash; one active shift rules per existing domain; view own shift; expected/closing cash; variance; end shift; Manager/Owner visibility; sales require valid open shift per existing design.

### P17-WP06 — Cash Sale and Receipt

Search/select products; cart qty; stock validation; subtotal/tax/total; cash + change; transactional complete; inventory reduce; unique receipt/sale number; receipt display; history/detail; duplicate submission protection. MVP payment: Cash (existing ManualGCash/Utang remain but are not Phase 17 gates).

Receipt fields: organization/store, register, cashier, date/time, receipt number, items, qty, unit price, subtotal, tax, total, cash received, change.

### P17-WP07 — Void, Refund, and Audit

Pre-completion line removal ≠ refund; completed sales historically traceable; Manager/Owner for void/return; mandatory reason; audit actor/org/sale/time/reason/action; stock restoration; Cashier unauthorized denial; no destructive delete of completed sales.

### P17-WP08 — Reports, Hardening, and Closeout

Today’s sales; transaction count; cash collected; sales by cashier; shift summary; current stock; low-stock. Harden isolation, entitlement, roles, validation, duplicate-sale protection, transactions, audit, responsive UI, loading/empty/error/unauthorized states, EN/FIL where supported. Closeout report with SHAs, tests, push status.

---

## 5. Authorization model

```text
Platform session ≠ POS access
Organization membership ≠ POS access
Organization Owner ≠ POS access
POS entitlement alone ≠ POS access
POS role alone ≠ POS access

membership (active) ∧ entitlement (active) ∧ POS role (active) ⇒ POS access
```

POS product-local roles (MVP labels):

| MVP label | Existing POS role code |
|---|---|
| POS Owner | `Owner` (and `Admin` as elevated owner-equivalent where already present) |
| POS Manager | `StoreManager` |
| POS Cashier | `Cashier` |

Organization roles remain Platform-owned and must not be mixed into POS capability checks.

---

## 6. Functional requirements

- End-to-end first-sale journey (Definition of Done).
- Setup resume for incomplete onboarding.
- Stock cannot go silently negative on sale when inventory tracking is enabled (existing validation).
- Duplicate checkout protection via existing idempotency.
- Reports scoped strictly by organization.

---

## 7. Non-functional requirements

- Organization isolation on every POS query/mutation.
- Fail-closed commercial and role middleware.
- Transactional sale + stock + receipt number allocation.
- Responsive MAUI operational UI; Admin Ant Design standards only where Admin is touched.
- English and Filipino strings where PosResources already support localization.

---

## 8. Testing requirements

Cover at least:

- membership + entitlement + POS role grants access;
- missing membership / entitlement / POS role denies;
- Organization Owner without POS role denied;
- suspended member denied;
- cross-organization denied;
- onboarding complete + resume; Cashier cannot change setup;
- product validation; shift start/close; duplicate active-shift prevention;
- sale completion; stock reduction; receipt generation; duplicate submission protection;
- unauthorized void/refund denial; authorized void/refund with reason; stock restoration;
- report organization isolation.

Do not mark a WP complete while relevant tests fail.

---

## 9. Exclusions

Do not add unless already implemented and required for compatibility:

- multiple branches / warehouses;
- supplier management / purchase orders as Phase 17 gates;
- advanced variants; custom POS roles; offline sync as a Phase 17 gate;
- loyalty; accounting integration;
- GCash/Maya/card gateway integration; split payment;
- advanced tax engine; advanced analytics;
- complex refund approval workflows.

---

## 10. Definition of Done

Phase 17 is complete only when the end-to-end journey in §1 works with organization isolation, entitlement protection, and role protection, all eight WP reports exist, portfolio/phase indexes are updated, targeted tests pass, and a focused Phase 17 commit is pushed to `origin/main`.

---

## 11. Closeout requirements

WP08 closeout must record:

- all eight WP statuses;
- final end-to-end journey evidence;
- test totals;
- known limitations;
- deferred post-MVP scope;
- final commit SHA;
- push status;
- clean/dirty working-tree status.

---

## 12. UI notes

Follow `docs/ui/ant-design-admin-ui-standards.md` for any Admin surfaces. POS MAUI keeps existing shell patterns. Concise primary actions (e.g. `+ Product`). Filter-on-change without unnecessary Apply. Do not redesign unrelated Platform or full Organization Administration Web pages. Mobile Organization Owner essentials remain in scope for the product journey per [client-experience-boundaries](../architecture/client-experience-boundaries.md); Phase 17 does not claim device validation of those Mobile Org screens.
