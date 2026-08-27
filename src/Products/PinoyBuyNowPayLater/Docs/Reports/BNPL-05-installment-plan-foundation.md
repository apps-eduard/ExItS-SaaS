# BNPL-05 — Installment Plan / Schedule Foundation

| Field | Value |
|---|---|
| Package | BNPL-05 |
| Status | **COMPLETE** |
| Branch | `feat/bnpl` |
| Baseline | `b925499d0db59551ab1551d320478fe3510843da` |

## Delivered

- Explicit principal-only installment plan attached to `BnplFinancingOffer`
- Entities: `BnplInstallmentPlan`, `BnplInstallmentPlanItem`
- Schedule rows are **caller-supplied** (no automatic term/frequency generator)
- Invariant: `sum(item.PrincipalAmount) == FinancedPrincipal` exactly (no rounding correction)
- Due dates stored as explicit `DateOnly` business dates
- Attach/replace only while offer is current, unaccepted, and application is `Offered`
- Customer acceptance requires a valid plan; acceptance locks the plan
- Manual approval requires accepted locked plan
- Capability `bnpl.plan.manage` added; `bnpl.plan.read` used for GET
- Persistence: `bnpl.installment_plans`, `bnpl.installment_plan_items`
- Migration: `AddBnplInstallmentPlanFoundation`
- API: `PUT/GET .../applications/{id}/offers/{offerId}/installment-plan`

## Explicit non-goals (still out of scope)

- Automatic monthly/weekly/daily schedule generation
- Interest, fees, APR, late fees
- ACTIVE financing / CommerceSaleId
- Collectible debt / OutstandingBalance / repayments
- Overdue engine / grace / holiday shifting
- React / Personal customer UX
- Inventing BNPL-D-00-14 / 15 / 17 policy

## Open decisions (unchanged)

| Decision | Status |
|---|---|
| BNPL-D-00-14 term / frequency | **OPEN** |
| BNPL-D-00-15 interest / fees | **OPEN** |
| BNPL-D-00-17 early payoff / overdue / allocation | **OPEN** |
| BNPL-D-00-16 credit limits | **OPEN** |
| BNPL-D-00-08 merchant settlement | **OPEN** |
| BNPL-D-00-13 Personal UX | **OPEN** |

## Legacy BNPL-04 offers

No synthetic schedules are generated for historical principal-only offers.  
Offers accepted/approved without a plan cannot proceed toward BNPL-07 activation without an explicit controlled workflow.

## Next

**BNPL-06** — Commerce/POS product + availability integration
