# PinoyServicePro — Risks and Decisions

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)
> Close items only with evidence. Do not invent answers for portfolio-open items.

| Field | Value |
|---|---|
| Product | PinoyServicePro |
| Last updated | 2026-08-20 |

## Portfolio items (always preserve until closed upstream)

| ID | Type | Description | Current state | Impact | Decision point | Resolution criteria |
|---|---|---|---|---|---|---|
| R-091 | Risk | Production authentication missing | Open | No production-secure identity | Portfolio auth roadmap | Real Platform auth shipped + evidenced |
| D-P12-03 | Decision | Commercial-state transport to products | Open / provisional | How product learns subscription/entitlements without Platform table reads | Commercial/integration WP | Approved contract + implementation; no direct Platform EF/SQL |
| D-P12-05 | Decision | Honest Dev/Testing vs Production language | Open (tied to R-091) | Risk of claiming production-secure identity | With R-091 | Dev/Testing shortcuts labeled; Production fail-closed |

## Product decision register (`PSP-D-00-XX`)

For each decision: ID, Question, Current direction, Status, What it blocks, Safe default until decided.

| ID | Question | Current direction | Status | What it blocks | Safe default until decided |
|---|---|---|---|---|---|
| PSP-D-00-01 | Final Platform product code / slug | Propose `pinoy-service-pro` | Open / Product Owner Decision Required | Catalog, plans, independent subscription | Use proposed slug in docs only; do not register catalog |
| PSP-D-00-02 | Final DB name / schema | Propose DB `ExItS_PinoyServicePro`; schema unset | Open / Product Owner Decision Required | Persistence, migrations, ops | Planning name only; create neither |
| PSP-D-00-03 | Initial implementation surface / project layout | Prefer Org Web + product API; MAUI later | Open / Product Owner Decision Required | PSP-01 scaffold | No projects until authorized; document preferred direction only |
| PSP-D-00-04 | Offline scope | Do not inherit POS offline; classify per capability | Open / Product Owner Decision Required | PSP-11; device grants | Online-required for money and conflict-sensitive scheduling until decided |
| PSP-D-00-05 | Customer-facing booking MVP timing | Future capability; not assumed in early MVP | Open / Product Owner Decision Required | Personal/public booking WPs | Staff-created booking only |
| PSP-D-00-06 | Deposit policy | Likely needed for some templates | Open / Product Owner Decision Required | Payments, estimates | Deposits capability off until policy exists |
| PSP-D-00-07 | Split-payment policy | Common in service businesses | Open / Product Owner Decision Required | Payment posting | Single tender per completion until decided |
| PSP-D-00-08 | Commission policy | Optional for barber/salon | Open / Product Owner Decision Required | Payroll-adjacent reporting | Commission capability off |
| PSP-D-00-09 | Staff vs resource model details | People + optional resources (chair/bay/room) | Open / Product Owner Decision Required | Scheduling engine | Model both conceptually; do not finalize schema |
| PSP-D-00-10 | Asset extensibility | One CustomerAsset concept; template configures type | Open / Product Owner Decision Required | Repair verticals | Capability off for templates that do not need assets |
| PSP-D-00-11 | Material / inventory depth | Optional capability; not POS inventory copy | Open / Product Owner Decision Required | Parts/materials WPs | Track job-line consumption; full stock engine deferred |
| PSP-D-00-12 | Cross-branch scheduling / resource sharing | Explicit scope required | Open / Product Owner Decision Required | Multi-branch ops | No implicit cross-branch sharing |
| PSP-D-00-13 | Anonymous / public booking identity | High abuse/overbooking risk | Open / Product Owner Decision Required | Public booking | No anonymous public booking |
| PSP-D-00-14 | Notification channels / providers | Candidates listed; vendors not chosen | Open / Product Owner Decision Required | PSP-10 | In-app/email candidates only; no vendor integration |
| PSP-D-00-15 | Document / receipt requirements | Operational receipts likely | Open / Product Owner Decision Required | Payments / docs WPs | Simple operational receipt intent; not tax invoice |
| PSP-D-00-16 | Tax / compliance activation | No BIR/tax claims | Open / Product Owner Decision Required | Production finance claims | No tax-document issuance; use ExItS compliance architecture later |
| PSP-D-00-17 | Retention policy | History important; unlimited retention not assumed | Open / Product Owner Decision Required | Storage, privacy | Retain while org subscribed; no deletion-without-policy |
| PSP-D-00-18 | Product-local grant identifiers | Presets + grant areas recorded | Open / Product Owner Decision Required | PSP-02 and all ops | Use planning labels; no hard-coded role-name authz |
| PSP-D-00-19 | Refund / reversal policy | Likely needed | Open / Product Owner Decision Required | Payments | No silent deletes; refund capability off until policy |
| PSP-D-00-20 | Scheduling conflict policy details | Server-authoritative conflicts required | Open / Product Owner Decision Required | Booking engine | Deny overlapping exclusive staff/resource bookings until policy refined |
| PSP-D-00-21 | Documentation baseline owner approval | PSP-00 docs complete | Open / Product Owner Decision Required | Closing PSP-00 as approved | Treat as draft-complete; Implementation Not Started |

## Product risks

| ID | Type | Description | Current state | Impact | Owner / decision point | Evidence | Resolution criteria |
|---|---|---|---|---|---|---|---|
| PSP-R-00-01 | Risk | Domain becomes too retail/POS-specific | Mitigated in docs | Wrong product shape | Architecture review | Template model + no POS project refs | Vertical validation (PSP-12) |
| PSP-R-00-02 | Risk | Domain becomes too mechanic-specific | Mitigated in docs | Barber/salon unfit | Product design | Capability gating for assets/estimates/parts | Barber + Auto Repair validation |
| PSP-R-00-03 | Risk | Over-generic EAV weakens business rules | Mitigated in docs | Money/authz integrity | Architecture | Explicit domain concepts; EAV forbidden as primary | Persist rule in implementation WPs |
| PSP-R-00-04 | Risk | False compliance / BIR claims | Open vigilance | Legal/reputational | Product owner | Explicit non-claims in security docs | No claim without validation |
| PSP-R-00-05 | Risk | Cross-product data leakage | Mitigated in docs | Isolation breach | Architecture guards | Isolation statements + future guards | Architecture tests when code exists |

## Accepted planning baselines (not implementation)

- One product + stable core domain + capabilities + business templates + terminology
- Independent subscription, authorization, and operational persistence
- Booking ≠ completed service transaction
- Walk-in is capability-driven alongside booking
- CustomerAsset optional and capability-controlled
- Estimates optional
- Materials/parts optional; not automatic POS inventory reuse
- Operational money ≠ Platform SaaS billing
- Decimal monetary concepts
- PHI default none
- No customer-specific source forks
- Server-authoritative scheduling and money rules
- Platform Admin is not ServicePro operations UI

## Instructions

- Prefer stable IDs (`PSP-D-00-XX`, `PSP-R-00-XX`, portfolio `R-…` / `D-…`).
- “Closed” requires repository or operator evidence.
- A `TBD` may remain in docs only when linked to an explicit `PSP-D-00-XX` decision.
- Never disguise an assumption as an approved decision.
