# Eligibility and Approval

**Status:** Planning baseline (BNPL-00)  
**Implementation present:** No  
**Related:** BNPL-D-00-16, BNPL-D-00-26

## Separation of concerns

Keep these stages distinct:

1. **Eligibility evaluation** — may this customer/amount/term proceed?  
2. **Financing offer** — concrete terms presented  
3. **Customer acceptance** — customer agrees (when required)  
4. **Merchant/system approval** — if still required after offer  
5. **Commerce completion** — sale success → ACTIVE  

Do **not** collapse “eligible” into “ACTIVE.”

## Supported future models (architecture)

| Model | Notes |
|---|---|
| Merchant manual approval | Default safe path until automation authorized |
| Simple configured rules | Amount caps, term caps, product eligibility flags |
| Customer credit limit | Open (BNPL-D-00-16) |
| Financing amount / term restrictions | Open (BNPL-D-00-14) |
| Future automated risk engine | Allowed as future WP; **no AI credit model claimed or implemented in BNPL-00** |

## Declined path

If declined:

- No completed commerce sale  
- No inventory deduction  
- No ACTIVE financing  

## Outcomes

| Outcome | Financing | Commerce | Inventory |
|---|---|---|---|
| Declined | Terminal non-ACTIVE | None | Unchanged |
| Offered / accepted / approved pending sale | Non-ACTIVE | None yet | Unchanged |
| Activated | ACTIVE | Sale exists | Deducted by Commerce |

## Explicit non-claims

- No claim of credit bureau integration  
- No claim of fair-lending automation  
- No invented score thresholds
