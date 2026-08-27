# Eligibility and Approval

**Status:** Manual path implemented (BNPL-04/05)
**Implementation present:** Yes — manual eligibility + manual approval + schedule acceptance
**Related:** BNPL-D-00-16, BNPL-D-00-26

## Separation of concerns (binding)

1. **Eligibility evaluation** — may this request proceed? (`ApproveEligibility` / `DeclineEligibility`)
2. **Financing offer** — concrete principal-only terms (`CreateOffer`)
3. **Explicit installment plan** — caller-supplied principal schedule on the offer (`AttachOrReplaceInstallmentPlan`)
4. **Customer acceptance** — staff-recorded acceptance of offer **and** schedule (`AcceptOffer`)
5. **Merchant approval** — final approve to APPROVED_PENDING_SALE (`Approve`; requires locked plan)
6. **Commerce completion** — sale success → ACTIVE (**BNPL-07 only**)

Do **not** collapse eligible into ACTIVE. Do not collapse create capability into approve.
Do not collapse `application.approve` into `plan.manage`.

## BNPL-04/05 safe default

Manual eligibility and manual approval are implemented as the **safe default** (BNPL-D-00-26 Product Owner decision remains OPEN for future automation models).

## Explicit non-claims

- No credit bureau / credit score
- No credit limit engine (BNPL-D-00-16 OPEN)
- No AI risk engine
- No interest/fee/term engine (BNPL-D-00-14/15 OPEN)
- No automatic schedule generator
