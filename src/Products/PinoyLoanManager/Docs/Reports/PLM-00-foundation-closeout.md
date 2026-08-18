# Pinoy Loan Manager — PLM-00 Foundation Closeout

**Status:** Documentation closeout (planning only)
**Implementation present:** No
**Last updated:** 2026-08-19

This report closes **PLM-00 Foundation & Product Decisions** as a **documentation** phase. It does **not** authorize PLM-01 or any application implementation.

Companion: [../Validation/PLM-00-readiness-checklist.md](../Validation/PLM-00-readiness-checklist.md), [../roadmap.md](../roadmap.md), [../risks-and-decisions.md](../risks-and-decisions.md).

---

## Product vision (confirmed)

**Pinoy Loan Manager** is an independently subscribed ExItS SaaS product for lending operations. It is a sibling of PinoyBusinessPOS, not a POS module.

### Applications / surfaces

1. **Unified ExItS Platform Admin Web** — Platform Owner / Platform Admin; SaaS subscriptions, entitlements, billing
2. **PLM Organization Web** — full operational lending administration
3. **PLM MAUI Blazor Hybrid** — limited field / collector operation
4. **ExItS Personal** — borrower / customer-facing Loan experience (presentation only)

Platform Admin is **not** the normal borrower-loan operations UI.

### Default organization role presets

- Owner
- Manager
- Cashier
- Collector

Presets are backed by **explicit grants**. No implicit role hierarchy. Identifiers remain open (PLM-D-00-06).

### Origination

- Traditional Loan
- Quick Loan

Both converge into **one** financial core after disbursement.

---

## PLM-00 work packages

| WP | Commit intent | Status |
|---|---|---|
| WP01 | Documentation workspace | Completed |
| WP02 | Product definition & architecture | Completed |
| WP03 | Lending operating model & Quick Loan | Completed |
| WP04 | Financial calculation & loan lifecycle | Completed |
| WP05 | Authorization, cash control, operational workflow | Completed |
| WP06 | Borrower, Personal linking, Quick Loan publishing | Completed |
| WP07 | Traditional loan & origination | Completed |
| WP08 | Reporting, documents, notifications, customer visibility | Completed |
| WP09 | Technical layout & integration boundary | Completed |
| WP10 | Foundation closeout & implementation readiness | This package |

---

## Coherence review

Reviewed for contradictions across ownership, Personal/Borrower, Quick vs Traditional, roles, collector cash vs loan ledger, penalties, allocation, schedule/maturity, reporting, and source boundaries.

**No silent rewrite** of prior accepted planning baselines.

### Explicit tensions (open, not hidden)

These are **open decisions**, not resolved by this closeout:

- **SoD vs small org:** multiple presets may attach to one person; high-risk self-approval for all org sizes remains **PLM-D-00-13**.
- **Cashier close-with-variance:** unresolved variance must remain visible; exact close policy remains **OPEN**.
- **Layout vs scaffold:** `ExItS.PinoyLoanManager.*` is a planning target; projects do not exist (PLM-D-00-03).
- **Database name:** proposed `ExItS_PinoyLoanManager`; final approval **PLM-D-00-02**.
- **Commercial transport:** D-P12-03 still open; no shared DB invented.
- **Personal linking schema:** lifecycle intent recorded; generic Platform relationship model **PLM-D-00-04**; mechanism **PLM-D-00-05**.
- **Financial engine:** modes and ledger principles recorded; formulas/rates/rounding/component order still **OPEN**.

No Product Foundation conflict: future source tree matches `src/Products/<Name>/` plus `Docs/` (D-P12-02 / foundation §9).

---

## Implementation gates

### A. Resolved enough for scaffold (PLM-01)

- independent product and independent subscription
- separate database (name proposed; not created)
- project boundaries recorded
- Web + MAUI direction
- roles are grant presets (identifiers later)
- Personal / Borrower separation
- no cross-product DB access
- no POS project dependency

### B. Resolved enough for early domain work (after scaffold + access)

Foundational modeling **without rates** is supported for:

- Borrower
- organization / branch boundaries
- applications / request concepts
- disbursement separation from approval
- financial event / audit principles
- collector cash vs loan ledger as separate facts

### C. Must be resolved before financial-engine implementation

- supported MVP interest formula(s)
- exact rounding (PLM-D-00-12)
- payment component allocation order
- fee rules
- penalty legal/business limits
- schedule exception treatment
- early settlement treatment

### D. Must be resolved before Production

- legal / compliance validation (PLM-D-00-11)
- production authentication (R-091)
- commercial-state transport (D-P12-03)
- production security / device validation
- backup / recovery
- deployment
- observability
- privacy / retention
- accounting decisions where required

---

## Recommended next phase

**PLM-01 Product Scaffold & Isolation**

Then (refine later as needed):

- PLM-02 Identity / Organization / Product Access
- PLM-03 Product-local Authorization
- PLM-04 Borrower Foundation
- PLM-05 Loan Product / Quick Loan Template Foundation
- PLM-06 Application / Quick Loan Request
- PLM-07 Approval / Disbursement
- PLM-08 Schedule / Financial Engine
- PLM-09 Payments
- PLM-10 Collections / Penalties
- PLM-11 Reporting / Documents
- PLM-12 Security / Audit / Privacy
- PLM-13 MAUI / Offline capabilities
- PLM-14 Production validation / closeout

**Do not start PLM-01 in this package.**

---

## Exclusions (still true)

- no application code
- no `.csproj`
- no PLM database / migrations
- no API / UI implementation
- no `ExItS.slnx` change
- no POS or Platform implementation change
- no shared Product Foundation change
- no legal compliance claim

---

## Legal / compliance

No lending, cash, collection, penalty, waiver, document, or workflow in PLM-00 is claimed legally compliant. External qualified review remains required before Production. This closeout does not invent Philippine regulations.
