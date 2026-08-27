# Installment Model

**Status:** Planning baseline (BNPL-00)  
**Implementation present:** No  
**Related:** BNPL-D-00-14, BNPL-D-00-15, BNPL-D-00-17

## Concepts

| Concept | Notes |
|---|---|
| Financed amount / principal | Amount financed after down payment |
| Down payment | May be zero; policy Open |
| Term | Number of periods — choices Open |
| Frequency | Daily / weekly / monthly / other — Open |
| Number of installments | Derived from term + frequency |
| Installment amount | Decimal money; rounding policy Open |
| Due dates | Timezone / business-date handling Open |
| Installment status | Unpaid / partially paid / paid (planning) |

## Behaviors requiring Product Owner policy

Do **not** guess in implementation:

- Allowed frequencies and max terms  
- Interest / fees (BNPL-D-00-15)  
- Rounding (last installment adjustment vs banker’s rounding)  
- Partial payments across installments  
- Early payoff calculation  
- Overpayment handling  
- Timezone and “due day” rules for PH operations  
- Currency (expect PHP for PH market; multi-currency not assumed)

## Safe default until decided

- Document schedules as planned decimal amounts  
- Do not generate interest in BNPL-00 or early WPs without policy  
- Prefer explicit schedule rows over “compute forever” floating formulas once ACTIVE (snapshot schedule)

## Relation to ACTIVE

Schedule becomes **collectible** only when financing is ACTIVE (after commerce sale). Pre-ACTIVE schedules, if drafted for offer display, must not accept repayments.
