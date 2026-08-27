# Loss, Damage, and Discrepancy

> Index: [README.md](README.md)  
> Evidence: [../Product/pledged-item-model.md](../Product/pledged-item-model.md), [../Product/appraisal-model.md](../Product/appraisal-model.md)  
> Decisions: [../risks-and-decisions.md](../risks-and-decisions.md)

| Field | Value |
|---|---|
| Product | Pinoy Pawn Manager (PPM) |
| Status | PPM-00 planning |
| Implementation | **None** |
| Last updated | 2026-08-27 |
| LEGAL_AUTHORIZATION_CLAIMED | **NO** |

Incidents record **loss, damage, mismatch, or unexplained variance** in custody. They protect customers, staff, and the org. They must **never** be “fixed” by silently editing intake photos, appraisal notes, or movement history.

---

## Incident types (planning)

| Type | Examples |
|---|---|
| **Missing** | Bag not in expected bin; empty sleeve |
| **Mismatch** | Tag/ticket points to different item than bag contents |
| **Damage** | Broken, wet, stone missing vs intake condition |
| **Count / seal break** | Seal number changed; bag opened without movement |
| **Wrong release attempt** | Checklist caught near-miss |
| **Intake error discovered later** | Description wrong; correction needed **without erasing** originals |
| **Transfer loss** | Cross-branch transfer variance |

Insurance, police, and regulatory filings are **org process / compliance Open** — PPM tracks operational incidents only unless later scoped.

---

## Hard evidence rule

| Forbidden | Required instead |
|---|---|
| Replace intake photo in place | Add new photo set linked as **correction** / incident evidence |
| Edit appraisal amount quietly | Superseding appraisal + incident link if custody-related |
| Delete movement rows | Append correcting movement or annotate via incident |
| Backdate fabricated history | Explicit correction event with real timestamps |
| Mark `RELEASED` to clear a missing item | Keep open incident; escalate |

Retention of original evidence: [PPM-D-00-19](../risks-and-decisions.md) Open — default **retain while relevant to ticket/dispute**; no silent purge.

---

## Incident record concepts

| Concept | Intent |
|---|---|
| Incident id | Unique |
| Organization / branch | Scope |
| Linked item / ticket / bag | Subject |
| Type + severity | Ops classification |
| Discovered by / timestamp | Who found it |
| Expected vs observed | Structured notes |
| Evidence refs | New photos, seal photos |
| Linked movements | Prior and corrective |
| Status | Open / investigating / resolved / written-off (ops labels) |
| Resolution notes | What was done |
| Capability-gated closers | Not any cashier alone for severe cases |

---

## Interaction with ticket and custody

| Situation | Direction |
|---|---|
| Missing during `ACTIVE` | Block redeem release; keep ticket open; incident |
| Damage vs intake | Document; customer communication is ops policy; do not alter intake snapshot |
| Mismatch at `RELEASE_PENDING` | Abort release; incident; possibly quarantine both bags |
| During disposition | Block handoff until resolved or explicitly accepted risk by authorized role |

Renewals should also block if the vault cannot confirm the item ([../Product/renewal-model.md](../Product/renewal-model.md)).

---

## Privacy and minimization

Incident photos may include customer property details. Apply minimization ([PPM-R-00-07](../risks-and-decisions.md)): collect what investigation needs; avoid unrelated bystanders; no PHI by default.

---

## Reporting

Incidents appear on the discrepancy register ([../Product/reporting-baseline.md](../Product/reporting-baseline.md)). Near-miss wrong releases are valuable even when no loss occurred.

---

## Online-only

Opening and resolving incidents that affect custody eligibility are **ONLINE-ONLY** on initial Web/PWA.

---

## What this is not

| Not | Why |
|---|---|
| Automatic legal liability determination | Ops record only |
| Inventory shrinkage module for POS retail | Different product |
| License to destroy evidence after “resolve” | Evidence integrity |

---

## Exclusions

- No claims workflow engine in PPM-00  
- No invented statutory compensation tables  
- No silent admin tools that rewrite intake evidence  
