# Pinoy Pawn Manager — Risks and Decisions

> Close items only with evidence. Do not invent legal conclusions or ₱ thresholds.

| Field | Value |
|---|---|
| Product | Pinoy Pawn Manager (PPM) |
| Last updated | 2026-08-27 |

## Portfolio items (preserve until closed upstream)

| ID | Type | Description | Current state | Impact |
|---|---|---|---|---|
| R-091 | Risk | Production authentication maturity | Portfolio-managed | Production identity honesty |
| D-P12-03 | Decision | Commercial-state transport to products | Open / provisional | How PPM learns entitlements without Platform table reads |
| D-P12-05 | Decision | Honest Dev/Testing vs Production language | Portfolio-managed | No false production-secure claims |
| D-P12-02 | Decision | Product docs root under `src/Products/<Name>/Docs/` | Closed (portfolio) | This Docs tree |

## Product decision register (`PPM-D-00-XX`)

| ID | Question | Current direction | Status | What it blocks | Safe default until decided |
|---|---|---|---|---|---|
| PPM-D-00-01 | Official product display name | **Pinoy Pawn Manager** | **Provisionally Approved for Implementation** (Product Owner, PPM-01) — **not** final marketing approval | Final marketing / brand lock | Use this name in scaffold, Local Validation catalog, and docs; marketing copy may still change |
| PPM-D-00-02 | Platform product code / slug | **`pinoy-pawn-manager`** | **Provisionally Approved for Implementation** (Product Owner, PPM-01) — **not** final marketing approval | Final marketing slug lock; production commercial catalog beyond Local Validation | Registered as `ProductCode.PinoyPawnManager` + Local Validation / Dev fixture only |
| PPM-D-00-03 | Product folder / project naming | **`PinoyPawnManager`** under `src/Products/` | **Provisionally Approved for Implementation** (Product Owner, PPM-01) — **not** final marketing approval | Rename cost if marketing forces change | Scaffold projects live under `src/Products/PinoyPawnManager/` |
| PPM-D-00-04 | Database name / schema | Propose DB `ExItS_PinoyPawnManager`; schema unset | OPEN | Persistence, migrations | Planning name only; create neither |
| PPM-D-00-05 | Accepted collateral categories | Configurable per org; jewelry/phones/etc. as candidates | OPEN | Intake forms, restrictions | Categories configurable; none assumed mandatory |
| PPM-D-00-06 | Appraisal methodology / configuration | Manual appraisal + notes/photos; optional supervisor | OPEN | Appraisal engine | Manual appraisal only; no AI valuation |
| PPM-D-00-07 | Loan-to-appraisal policy | Configurable; **no fixed %** | OPEN | Offer calculator | Record appraised value and principal separately; no auto % |
| PPM-D-00-08 | Interest / finance charge model | Contractual charges exist; method unset | OPEN | Tickets, renewals, redemption quotes | Do not invent rates; disclose snapshot when decided |
| PPM-D-00-09 | Maturity model | Agreement date + maturity date; TZ/business date unset | OPEN | Maturity jobs | Store explicit maturity datetime; computation Open |
| PPM-D-00-10 | Grace / default process | Operational states separate from legal authorization | OPEN | Unredeemed workflow | No auto ownership transfer at maturity |
| PPM-D-00-11 | Renewal rules | Renewals allowed subject to policy; not unlimited by assumption | OPEN | Renewal engine | Require explicit renewal acceptance + payment |
| PPM-D-00-12 | Partial-payment policy | Whether partial renewal/redemption allowed | OPEN | Payment posting | Full required amount until decided |
| PPM-D-00-13 | Authorized representative redemption | Whether third party may redeem | OPEN | Release security | Deny representative redemption until policy |
| PPM-D-00-14 | Disposition / auction model | Legal process TBD | OPEN | Disposition UI | Technical eligibility ≠ legal sale authority |
| PPM-D-00-15 | POS / Commerce inventory handoff | Explicit contract; no direct POS DB writes | OPEN | PPM-13 | Document boundary only; no handoff implementation |
| PPM-D-00-16 | Cross-branch collateral transfer | Controlled transfer only | OPEN | Multi-branch custody | No implicit cross-branch moves |
| PPM-D-00-17 | Cash-management integration | Integrate vs product-local cash controls | OPEN | Funds release UX | Record PPM payment facts; do not copy POS drawer without ADR |
| PPM-D-00-18 | Product-local grant identifiers | Capability catalog drafted in authorization-matrix | OPEN | PPM-02 | Use planning labels; no role-name hard-coding |
| PPM-D-00-19 | Retention / deletion / export | Evidence retention important | OPEN | Storage, privacy | Retain while subscribed; no silent purge |
| PPM-D-00-20 | Regulatory / licensing prerequisites | Must be verified for Philippines pawn operations | OPEN | Production claims | **LEGAL_AUTHORIZATION_CLAIMED=NO** |

## Product risks (`PPM-R-00-XX`)

| ID | Description | State | Impact | Mitigation / decision point |
|---|---|---|---|---|
| PPM-R-00-01 | Confusion with PLM / reuse of PLM loan entities | Mitigated in docs | Wrong domain model | Hard boundary docs; no PLM entity copy |
| PPM-R-00-02 | Treating pledged items as POS inventory while pledged | Mitigated in docs | Legal/custody failure | Ownership matrix + handoff ADR later |
| PPM-R-00-03 | Collapsing payment and physical release | Mitigated in docs | Wrong-item / premature release | Separate state machines |
| PPM-R-00-04 | Inventing Philippine legal maturity/grace/auction rules | Open vigilance | Legal/reputational | Compliance register; Open decisions |
| PPM-R-00-05 | Duplicate money release on retry | Mitigated in docs | Financial loss | Idempotency model |
| PPM-R-00-06 | Insider wrong-item release | Open | Customer/trust loss | Release checklist + audit |
| PPM-R-00-07 | Evidence over-collection / privacy | Open | DPA risk | Minimization + retention Open |
| PPM-R-00-08 | Cross-org or cross-branch leakage | Mitigated in docs | Isolation breach | Scoping + future architecture tests |
| PPM-R-00-09 | False compliance / licensing claims | Open vigilance | Legal | Explicit non-claims |
| PPM-R-00-10 | Overbuilding vault hierarchy before MVP | Open | Delay | Start with Branch → StorageArea → Bin/Bag |

## Accepted planning baselines (not final marketing / not legal closure)

- First-class independent product and subscription
- Separate operational persistence (DB name still Open — **PPM-D-00-04**)
- Display name / product code / directory provisionally approved for implementation in PPM-01 (**PPM-D-00-01/02/03**) — not final marketing Closed
- Appraisal value ≠ loan principal
- Agreement and appraisal snapshots are historical evidence
- Custody history is required (current location alone is insufficient)
- Payment completion ≠ physical release
- No auto transfer to retail inventory at maturity
- Web/PWA online-only for financial/custody mutations initially
- No AI valuation in foundation
- Technical capability ≠ legal authorization
- Local Validation / Dev catalog fixture ≠ full production commercial registration

## Instructions

- Prefer stable IDs (`PPM-D-00-XX`, `PPM-R-00-XX`, portfolio `R-…` / `D-…`).
- “Closed” requires repository or Product Owner evidence.
- Never disguise an assumption as an approved decision.
- Never mark ExItS/PPM as pawnshop-licensed without separate proof.
