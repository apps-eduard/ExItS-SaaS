# Reporting Baseline

> Index: [README.md](README.md)  
> Decisions: [../risks-and-decisions.md](../risks-and-decisions.md)

| Field | Value |
|---|---|
| Product | Pinoy Pawn Manager (PPM) |
| Status | PPM-00 planning |
| Implementation | **None** |
| Last updated | 2026-08-27 |
| LEGAL_AUTHORIZATION_CLAIMED | **NO** |

This baseline lists **report families** PPM should eventually support for pawn operations. It is not a BI implementation, not a statutory filing pack, and not proof of accounting-GL completeness.

**LEGAL_AUTHORIZATION_CLAIMED=NO.** Regulatory report formats for Philippine pawnshops remain Open ([PPM-D-00-20](../risks-and-decisions.md)).

---

## Principles

| Principle | Intent |
|---|---|
| Org + branch scoped | No cross-org leakage |
| Prefer snapshots and event history | Live config must not rewrite historical figures |
| Separate operational vs financial vs custody views | Different audiences |
| Online generation initially | Heavy exports may be async later; mutations remain online-only |
| Not Platform SaaS billing reports | Pawn money ≠ subscription invoices |
| Not POS sales reports for pledged goods | Until after authorized handoff |

---

## 1. Operational reports

Day-to-day pawnshop workload.

| Report (planning name) | Purpose | Typical filters |
|---|---|---|
| Intake / draft pipeline | Incomplete tickets | Branch, staff, date |
| Offers awaiting acceptance | Conversion follow-up | Branch, age |
| Active pawn listing | Open obligations | Branch, maturity window |
| Maturing soon | Worklist before maturity | Branch, N days (config, not law) |
| Matured open | Post-maturity still open | Branch |
| Renewal activity | Renewals completed / pending | Date range |
| Redemptions completed | Settled + released | Date range |
| Cancellations | Pre-activation aborts | Reason codes |
| Unredeemed queue | Ops classification list | Branch |
| Disposition in progress | Machine D worklist | Stage |

These support [pawn-transaction-model.md](pawn-transaction-model.md) states without implying legal foreclosure.

---

## 2. Custody reports

Physical control and vault integrity — companion to [../Custody/](../Custody/README.md).

| Report (planning name) | Purpose |
|---|---|
| Inventory by location | Counts per vault/cabinet/shelf/bin/bag |
| Movement log | Who moved what, when, from→to |
| Release log | Payment-linked releases; staff; ticket |
| Release-pending aging | Paid but not yet handed over |
| Discrepancy / incident register | Loss, damage, mismatch ([../Custody/loss-damage-discrepancy.md](../Custody/loss-damage-discrepancy.md)) |
| Cross-branch transfer log | Controlled transfers only ([PPM-D-00-16](../risks-and-decisions.md)) |
| Disposition custody transfers | Items leaving pawn custody path |

Custody history must remain reconstructible even after `CLOSED`.

---

## 3. Financial reports (product-local)

Pawn money facts inside PPM — **not** a full general ledger claim.

| Report (planning name) | Purpose | Caution |
|---|---|---|
| Principal released | Cash/channel out by period | Idempotent ops only |
| Renewal collections | Fees/principal components as configured | Method Open ([PPM-D-00-08](../risks-and-decisions.md)) |
| Redemption collections | Amounts taken to settle | Partials Open |
| Outstanding principal book | Open tickets | Not mark-to-market appraisal |
| Channel breakdown | Cash vs other | Cash drawer integration Open ([PPM-D-00-17](../risks-and-decisions.md)) |
| Void / reversal log | Corrections | Must be explicit events |
| Disposition proceeds (future) | If recorded in PPM | Legal/accounting Open |

Do not present these as BIR-ready or BSP-ready packs without compliance closure.

---

## 4. Management reports

Leadership / owner summaries.

| Report (planning name) | Purpose |
|---|---|
| Portfolio summary | Counts and principal by state |
| Branch comparison | Volume, release, redeem, unredeemed rates |
| Collateral mix | By category (configurable) |
| Appraisal vs principal spread | Risk posture (not LTV mandate) |
| Staff productivity | Intakes, appraisals, releases (non-punitive design care) |
| Aging buckets | Open tickets by age / maturity proximity |
| Exception dashboard | Discrepancies, release-pending, dual-control failures |

Management KPIs must not encode invented statutory ratios.

---

## 5. Audit / evidence exports (related)

Not “pretty charts,” but exportable trails:

- Ticket snapshot + renewals  
- Appraisal history  
- Payment operations  
- Custody movements and release checklist results  
- Disposition authorization trail  

Retention: [PPM-D-00-19](../risks-and-decisions.md) Open — no silent purge.

---

## Access control

Reports inherit product-local grants ([../authorization-matrix.md](../authorization-matrix.md), [PPM-D-00-18](../risks-and-decisions.md)). Financial and PII-heavy exports are higher privilege than floor operational lists.

---

## Non-goals (PPM-00 / foundation)

- Embedded Power BI / third-party warehouse  
- Cross-product consolidated GL  
- Statutory PH pawnshop regulatory filings claimed complete  
- Real-time offline report mutation  

---

## Exclusions

- No report SQL or UI in PPM-00  
- No fixed interest income recognition standard claimed  
- No POS sales inclusion for pledged items pre-handoff  
