# BNPL-04 — Financing Application + Approval Lifecycle

| Field | Value |
|---|---|
| Package | BNPL-04 |
| Status | **COMPLETE** |
| Branch | `feat/bnpl` |
| Baseline | `f65ef026ac5ab9e315b1ffb0723ae637fc079df4` |

## Delivered

- `BnplFinancingApplication` aggregate with state machine through **APPROVED_PENDING_SALE**
- `BnplFinancingOffer` (versioned, accepted offers immutable)
- `BnplFinancingDecision` history (eligibility / approval / cancellation)
- Manual eligibility + manual approval (no AI, no credit score, no credit limit)
- Staff-recorded customer acceptance (BNPL-D-00-13 Personal UX still open)
- Capability `bnpl.application.read` added; create/approve remain separated
- Persistence: `bnpl.financing_applications`, `bnpl.financing_offers`, `bnpl.financing_decisions`
- Migration: `AddBnplFinancingApplicationLifecycle`
- API under `/api/v1/bnpl/applications/...`
- Idempotent create/submit/eligibility/offer/accept/approve
- Optimistic concurrency via `AggregateVersion`

## Implemented states

Draft → PendingEligibility → Offered → CustomerAccepted → ApprovedPendingSale
(+ Declined, Cancelled)

**ACTIVE is not implemented and cannot be reached.**

## APPROVED_PENDING_SALE invariants

- No outstanding debt
- No installments
- No repayments
- No inventory change
- No CommerceSaleId / ACTIVE transition

## Open decisions (unchanged)

BNPL-D-00-14, 15, 16, 08, 13, 20 remain OPEN.  
BNPL-D-00-26: manual approval implemented as **safe default**; Product Owner decision remains OPEN.

## Next

**BNPL-05** — Installment plan foundation (explicit schedules; see BNPL-05 report)
