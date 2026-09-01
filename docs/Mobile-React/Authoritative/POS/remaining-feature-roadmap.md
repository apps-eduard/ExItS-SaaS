# Pinoy Business POS — Remaining Feature Roadmap (Post MB2-05)

**Program:** ExItS-SaaS / PinoyBusinessPOS React PWA  
**Status:** AUTHORITATIVE (forward implementation plan)  
**Branch baseline:** `feat/organization` @ `e0747817a97c39ed97d5eae6179ccba91441a09e`  
**Supersedes for forward planning:** stale rows in [capability-parity-matrix.md](../Migration/capability-parity-matrix.md) where React status reads `MISSING` but code on `feat/organization` is now complete — verify source before trusting that matrix alone.

**Related:** [production-roadmap-policy.md](production-roadmap-policy.md) · [multi-branch-commerce-v2.md](multi-branch-commerce-v2.md) · [react-migration-roadmap.md](../Migration/react-migration-roadmap.md) (historical RMAP packages)

---

## 1. Current baseline

### Product mode (frozen)

| Policy | Value |
|--------|-------|
| Current generation | **ONLINE-ONLY PWA** (React + ASP.NET Core API + PostgreSQL) |
| Multi-Branch V2 | **COMPLETE through MB2-05** |
| MB2-06 offline/native | **DEFERRED** — separate future project |
| MB2-07 final multi-branch E2E | **DEFERRED** → FINAL-PRODUCTION-GATE-01 |
| Application-wide production hardening | **DO NOT START** until features + UI polish complete |

### Audit method (2026-09-01)

- Authoritative domain docs under `docs/Mobile-React/Authoritative/`
- React: `router.tsx`, `src/features/*`, API clients, RBAC guards, tests
- Backend: `ExItS.PinoyBusinessPOS.Api` endpoint maps + Application use cases
- MAUI: functional reference only (no visual port obligation)
- Explicit code search: TODO/FIXME/deferred/placeholder (test fixtures excluded)

### Completed domains (functional — not “production ready”)

Core POS React surfaces are **largely complete** on `feat/organization`:

| Domain | React status | Notes |
|--------|--------------|-------|
| Session / workspace / branch context | COMPLETE | RMAP-01…03 + MB2 branch binding |
| Catalog admin + Today's Prices + branch pricing | COMPLETE | MB2-01…03, MB2-05 |
| Sell floor + cart + checkout (Cash/GCash/Utang) | COMPLETE | Online; discounts + price override |
| Returns / void / transaction summary | COMPLETE | |
| Customers + repay + statement + business customers | COMPLETE | Branch privacy MB2-04-H1 |
| Inventory (adjust, lots/expiry, stock count, transfers, stock use, waste/loss, production) | COMPLETE | MB2-02 multi-branch authority |
| Purchasing (Direct Purchase, PO, GRN receive) | COMPLETE | Distinction preserved |
| Suppliers + connected suppliers + payables | COMPLETE | Buyer-side sharing |
| Expenses + categories | COMPLETE | |
| Shifts + cash handling settings | COMPLETE | |
| Customer ordering (seller + personal buyer storefront) | COMPLETE | |
| Branches + fulfillment + guided setup | COMPLETE | MB2-05 |
| Staff invite/assign + built-in POS roles | COMPLETE | Custom roles deferred |
| Devices (register + org device mgmt) | COMPLETE | Browser device model |
| Reports + management dashboard | COMPLETE | ~17 operational + 4 classic; client CSV export |
| Personal (Utang, Todo, stores, Start Business, linking) | COMPLETE | RMAP-22 Master Run 01 |

**Terminology:** “Implementation complete through MB2-05” describes **multi-branch feature scope**. The **application** still has remaining functional gaps listed below and has **not** entered final release gate.

---

## 2. Three-stage release model

```text
STAGE A — REMAINING FEATURE IMPLEMENTATION     ← THIS DOCUMENT (POS-NEXT-*)
        ↓
STAGE B — APPLICATION-WIDE UI/UX POLISH      (after Stage A)
        ↓
STAGE C — FINAL-PRODUCTION-GATE-01           (after Stage B; absorbs MB2-07)
```

Stage C includes: full application E2E, security/performance hardening, deployment readiness, full regression. **Do not schedule Stage C work inside POS-NEXT packages.**

Stage B includes: responsive consistency, navigation polish, loading/empty/error states, accessibility, native-speaker locale review, touch/keyboard workflows — **not** new business capabilities.

---

## 3. Feature matrix (remaining / partial)

Legend: **BE** = backend, **RE** = React, **E2E** = end-to-end usable by merchant today.

| Domain | Feature | BE | RE | E2E | Pri | Recommended action |
|--------|---------|----|----|-----|-----|-------------------|
| Sales | Sales history browse (list/search/detail beyond returns hub) | COMPLETE | PARTIAL | NO | P1 | POS-NEXT-03 — dedicated `/sales` list using `listSales` |
| Credit | Org overdue dashboard | COMPLETE | NOT_STARTED | NO | P1 | POS-NEXT-01 |
| Credit | Customer overdue detail + filters | COMPLETE | NOT_STARTED | NO | P1 | POS-NEXT-01 |
| Credit | Customer ledger view | COMPLETE | PARTIAL | NO | P1 | POS-NEXT-01 — client exists, no page |
| Credit | Manual credit entry | COMPLETE | NOT_STARTED | NO | P1 | POS-NEXT-01 |
| Credit | Due date set/clear/history | COMPLETE | PARTIAL | NO | P1 | POS-NEXT-01 — checkout sets due date only |
| Credit | Write-off create/reverse | COMPLETE | NOT_STARTED | NO | P1 | POS-NEXT-01 |
| Parties | Explicit customer/supplier branch assign/revoke UI | COMPLETE | NOT_STARTED | NO | P1 | POS-NEXT-02 — API from MB2-05; no detail-page UI |
| Connected suppliers | Incoming orders (seller queue) | COMPLETE | NOT_STARTED | NO | P1 | POS-NEXT-04 |
| Connected suppliers | Auto-link-exact / draft revalidate / link sync delta | COMPLETE | NOT_STARTED | NO | P2 | POS-NEXT-04 (subset) |
| Inventory | Low-stock list | COMPLETE | PARTIAL | NO | P2 | POS-NEXT-05 — KPI only on dashboard |
| Inventory | Reorder settings + suggestions | COMPLETE | NOT_STARTED | NO | P2 | POS-NEXT-05 |
| Inventory | Physical audit / reconciliation | COMPLETE | NOT_STARTED | NO | P2 | POS-NEXT-05 |
| Inventory | Standalone opening-stock workflow | COMPLETE | PARTIAL | NO | P2 | POS-NEXT-05 — embedded in product create only |
| Registers | Register create/edit/deactivate + activity | COMPLETE | PARTIAL | NO | P2 | POS-NEXT-06 — list-only; PWA auto-provision |
| Reports | Sales-by-cashier | COMPLETE | NOT_STARTED | NO | P2 | POS-NEXT-07 |
| Pricing | Promotion custom-default + origin override | PARTIAL | NOT_STARTED | NO | P2 | POS-NEXT-08 — deferred from MB2-03 |
| Roles | Custom POS role authoring | PARTIAL | DEFERRED | NO | P3 | Optional; built-in roles sufficient for v1 |
| Personal | Rewards placeholder | N/A | SCAFFOLD | NO | P3 | POS-NEXT-10 or retire |
| Sell | Camera barcode scanning | N/A | NOT_STARTED | NO | P2 | POS-NEXT-11 — RMAP-09 deferred |
| Storefront | Friendly public URL slugs | N/A | NOT_STARTED | NO | P3 | POS-NEXT-11 |
| Payments | Live electronic payment provider (non-fake gateway) | PARTIAL | PARTIAL | NO | P2 | Stage C / separate payment package — `FakePaymentGateway` today |
| Tax | RMAP-TAX controlled activation | PARTIAL | NOT_STARTED | NO | — | **OUT OF CURRENT SCOPE** — Platform compliance gate |
| Storefront | RMAP-B05 public org landing | N/A | NOT_STARTED | NO | — | **NOT AUTHORIZED** |
| Offline | Transactional offline / outbox / sync | LEGACY MAUI | POLICY DEFERRED | — | — | **FUTURE NATIVE/OFFLINE PROJECT** — not POS-NEXT |

### Obsolete / consolidate (do not implement)

| Item | Classification | Notes |
|------|----------------|-------|
| MAUI offline LocalStore parity in React PWA | RETIRE_LATER for current generation | Policy: online-only; RMAP-21 offline code may exist on branch but is not active roadmap |
| Legacy “Received Stock” as separate from Direct Purchase | CONSOLIDATE | React `ReceiveStockPage` = Direct Purchase fast receive |
| Per-package “production hardening” | RETIRE | Replaced by FINAL-PRODUCTION-GATE-01 |
| Capability matrix “React MISSING” for checkout/catalog/etc. | OBSOLETE DOC | Historical; source-verify on `feat/organization` |

---

## 4. Core flow audit summary

### Selling (COMPLETE with noted gaps)

| Area | Status |
|------|--------|
| Sell floor search / categories / barcode (keyboard) | COMPLETE |
| Weighted products + multi-UOM | COMPLETE |
| Branch effective pricing | COMPLETE (MB2-03) |
| Today's Prices | COMPLETE |
| Commercial discount + price override | COMPLETE |
| Customer selection + Utang checkout | COMPLETE |
| Cash / manual GCash reference / receipt summary | COMPLETE |
| Returns + void from summary | COMPLETE |
| **Sales list / history browse** | **GAP** → POS-NEXT-03 |
| Camera barcode | DEFERRED → POS-NEXT-11 |

### Inventory (authority COMPLETE; operational UX gaps)

Multi-branch inventory **authority** is frozen (MB2-02D). Remaining work is **merchant-facing surfaces** for replenishment/audit (POS-NEXT-05).

### Purchasing (COMPLETE)

Direct Purchase (`ReceiveStockPage`) supports search, qty, cost, supplier, expiry/lot, payment mode. PO + GRN flows complete. Incoming **connected-supplier orders** remain seller-side gap (POS-NEXT-04).

### Customers / Utang (PARTIAL)

Directory, detail, repay, statement, branch privacy: COMPLETE. **Collections operations** (overdue, ledger, write-off, manual credit): MAUI-complete, React missing → POS-NEXT-01.

### Reporting (MOSTLY COMPLETE)

Operational + classic reports and client-side CSV export: COMPLETE. Missing: **sales-by-cashier** report surface. Profitability reports exist but depend on cost inputs from purchasing — document as verify-on-use, not fake P&L.

---

## 5. Recommended implementation order (Stage A)

Packages are **coherent capabilities**, online-PWA only, ordered to minimize rework.

### POS-NEXT-01 — Credit collections & Utang operations

**Priority:** P1  
**Objective:** Close MAUI parity gap for credit lifecycle management.

**Includes:**
- Organization overdue summary + customer overdue pages
- Customer ledger page
- Manual credit entry (authorized roles)
- Due date management (set/clear/history)
- Write-off create/list/reverse (authorized roles)

**Dependencies:** Existing credit/repayment/write-off APIs; MB2-04-H1 branch privacy scoping.

**Excludes:** Offline credit sync; production hardening.

---

### POS-NEXT-02 — Party branch access management UI

**Priority:** P1  
**Objective:** Surface MB2-05 ExplicitAssign APIs on customer and supplier detail screens.

**Includes:**
- Grant/revoke branch visibility for customers and suppliers
- Respect multi-source provenance (transaction grants survive explicit revoke)
- RBAC: org governance only

**Dependencies:** MB2-05 party access APIs (complete).

---

### POS-NEXT-03 — Sales history & operational browse

**Priority:** P1  
**Objective:** Merchant-facing sales list/search/detail (MAUI `/sales` parity).

**Includes:**
- `/sales` list with filters (date, payment, branch context)
- Drill-down to existing transaction summary
- Navigation from role home / More hub

**Dependencies:** Existing `listSales` / `getSale` APIs.

---

### POS-NEXT-04 — Connected supplier incoming orders

**Priority:** P1  
**Objective:** Seller workflow for connected-supplier order intake.

**Includes:**
- Incoming orders queue + detail + accept/reject actions
- Optional: auto-link-exact, draft revalidation hooks (if needed for E2E)

**Dependencies:** Connected supplier APIs (complete); purchasing PO flow.

---

### POS-NEXT-05 — Inventory replenishment & audit surfaces

**Priority:** P2  
**Objective:** Operational inventory management beyond core MB2 authority.

**Includes:**
- Low-stock list (dedicated page)
- Reorder thresholds + suggestions
- Physical audit / reconciliation views
- Optional: standalone opening-stock entry for existing tracked products

**Dependencies:** MB2-02D inventory authority (frozen).

---

### POS-NEXT-06 — Register administration

**Priority:** P2  
**Objective:** Full register lifecycle without relying on PWA auto-provision only.

**Includes:**
- Create / edit / activate / deactivate registers
- Register activity/history panel

**Dependencies:** Register APIs (complete); shift open flow.

---

### POS-NEXT-07 — Reporting completeness

**Priority:** P2  
**Objective:** Close remaining report surface gaps.

**Includes:**
- Sales-by-cashier operational report (backend exists)
- Hub link + access matrix entry

**Excludes:** Fake P&L; tax reports (RMAP-TAX).

---

### POS-NEXT-08 — Promotion pricing enhancement

**Priority:** P2  
**Objective:** Implement deferred MB2-03 promotion behavior.

**Includes:**
- Local → Standard promotion may set organization default ≠ origin price
- Retain origin via branch price override

**Dependencies:** MB2-03 pricing model (frozen).

---

### POS-NEXT-09 — Organization experience polish (functional)

**Priority:** P2/P3  
**Objective:** Remaining org-side functional gaps (not visual polish).

**Includes (candidate — confirm before start):**
- Unified org settings hub (preferences + cash handling + links)
- Custom POS roles (only if product owner elevates from P3)

**Excludes:** Platform Admin features.

---

### POS-NEXT-10 — Personal product completeness

**Priority:** P3  
**Objective:** Close or retire thin Personal placeholders.

**Includes:**
- Personal Rewards: implement or remove “coming soon”
- Any remaining Personal notification depth (org parity is lower priority)

---

### POS-NEXT-11 — Selling & storefront enhancements

**Priority:** P2/P3  
**Objective:** Non-blocking enhancements.

**Includes:**
- Camera barcode scanning (where device API available)
- Friendly public store URL slugs (optional)

---

## 6. Stage B — Application-wide UI/UX polish

**Entry criteria:** Stage A packages complete (or explicitly deferred with owner acceptance).

**Scope (examples):**
- Consistent spacing, typography, card/list patterns across all domains
- Phone / tablet / desktop responsive QA on every major screen
- Navigation coherence (More hub, role homes, back-stack)
- Loading / empty / error / success feedback standardization
- Accessibility baseline (focus, labels, touch targets)
- Five-locale native-speaker review (`NATIVE_SPEAKER=PENDING` flags)
- Barcode-first cashier keyboard workflows

**Not in scope:** New business rules, offline architecture, production security audit.

---

## 7. Stage C — FINAL-PRODUCTION-GATE-01

See [production-roadmap-policy.md §9](production-roadmap-policy.md#9-final-production-gate--scope-once).

Absorbs **MB2-07** multi-branch final E2E. Runs **once** after Stages A + B.

---

## 8. Deferred — future native/offline project (NOT in POS-NEXT)

Do **not** schedule under current online PWA roadmap:

- MB2-06 cross-surface offline hardening
- Capacitor native host + SQLite
- Offline sales/payments/inventory/purchasing sync
- Outbox/inbox / conflict resolution
- Offline customer/supplier branch-aware sync
- Offline branch-price cache invalidation
- Cold-start offline unlock
- RMAP-21 offline master run (historical; policy-superseded for current generation)

---

## 9. Deferred — compliance / authorization-gated (NOT in POS-NEXT)

| Item | Notes |
|------|-------|
| RMAP-TAX | Platform compliance approval required |
| RMAP-B05 | Public org storefront landing — not authorized |
| Live payment gateway | Replace `FakePaymentGateway` — likely Stage C or dedicated payment integration |
| BIR / TaxDocument | Compliance program — not current POS feature package |
| Milligram UOM (UD-01) | Optional; Gram decimals may suffice |
| Price history audit table (UD-03) | Enhancement; not blocking core ops |

---

## 10. Known P2 (documented — not silent closure)

| Item | Category |
|------|----------|
| Utang summary outstanding totals may remain org-wide for branch staff | MB2-04-H1 conservative behavior |
| Readiness staff/device counts use POS-side heuristics | MB2-05; Platform join deferred |
| Credit aging uses UTC calendar dates | Backend limitation until org TZ modeled |
| Electronic payments simulated (`FakePaymentGateway`) | Online PWA dev/test only |

---

## 11. Next action

**FIRST_RECOMMENDED_FEATURE_PACKAGE = POS-NEXT-01** (Credit collections & Utang operations)

Then: POS-NEXT-02 → POS-NEXT-03 → POS-NEXT-04 → (P2 packages as prioritized).

**Do not start:** MB2-06, MB2-07, FINAL-PRODUCTION-GATE-01, or application-wide production hardening until Stages A and B are complete.
