# Installment Model

**Status:** BNPL-05 foundation implemented (explicit schedules only)
**Implementation present:** Yes — principal-only plan attached to financing offer
**Related:** BNPL-D-00-14, BNPL-D-00-15, BNPL-D-00-17 (remain **OPEN**)

## Concepts

| Concept | Notes |
|---|---|
| Financed amount / principal | Amount financed after down payment (`FinancedPrincipal`) |
| Down payment | On offer; **not** included in installment plan total |
| Explicit installment plan | Belongs to `BnplFinancingOffer`; part of accepted terms |
| Plan items | Sequence 1..N, PrincipalAmount > 0, explicit DueDate |
| Term / frequency | **Not implemented** — BNPL-D-00-14 OPEN |
| Interest / fees | **Not implemented** — BNPL-D-00-15 OPEN |
| Rounding | Exact total match required; no auto-adjust — policy OPEN |
| Installment collectibility | Only after ACTIVE (BNPL-07); pre-ACTIVE rows are planned schedule only |

## BNPL-05 behavior

- Staff/system supplies explicit rows (no `GenerateMonthlySchedule` / weekly / daily helpers)
- `sum(PrincipalAmount) == FinancedPrincipal` exactly
- Plan attach/replace only on current unaccepted `Offered` offer
- After customer acceptance: plan is immutable historical evidence
- AcceptOffer and Approve require a valid locked plan for new BNPL-05 flows
- No overdue / paid / partially-paid installment statuses

## Behaviors requiring Product Owner policy (still OPEN)

Do **not** guess in implementation:

- Allowed frequencies and max terms
- Interest / fees (BNPL-D-00-15)
- Rounding (last installment adjustment vs banker’s rounding)
- Partial payments across installments
- Early payoff calculation
- Overpayment handling
- Timezone and “due day” rules for PH operations
- Currency (expect PHP for PH market; multi-currency not assumed)

## Relation to ACTIVE

Schedule becomes **collectible** only when financing is ACTIVE (after commerce sale — BNPL-07). Pre-ACTIVE schedules must not accept repayments.
