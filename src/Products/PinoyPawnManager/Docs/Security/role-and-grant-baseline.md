# Pinoy Pawn Manager — Role and Grant Baseline

> Security index: [README.md](README.md)  
> Matrix: [../authorization-matrix.md](../authorization-matrix.md)  
> Decision: **PPM-D-00-18** OPEN

| Field | Value |
|---|---|
| Status | PPM-00 planning |
| Implementation | None |
| Last updated | 2026-08-27 |

## Layers

1. **Platform authentication** — identity of the human  
2. **Organization + product entitlement** — org may use PPM  
3. **PPM product-local grants** — what the staff member may do  
4. **Resource scope** — Organization / Branch / vault / object  

Platform entitlement does **not** replace PPM grants. Authorization checks **grants**, not hard-coded role names.

## Operational presets (planning only)

Presets are packaging aids for grant bundles—not Platform role enums and not final identifiers.

| Preset | Intent |
|---|---|
| Owner | Org PPM administration, reports, grant management |
| Manager | Supervisory ops, approvals, exceptions |
| Appraiser | Appraisal create/revise within policy |
| Pawn Officer | Transactions, offers, renewals, redemptions (non-vault-specialist) |
| Cashier | Fund release / collection posting (**PPM-D-00-17** Open) |
| Vault / Custody Staff | Receive, move, locate, prepare release |
| Supervisor | High-value / exception approvals |
| Auditor / Read Only | Reports + audit view |

## Capability areas (planning labels)

Exact strings finalize under **PPM-D-00-18**. Examples from the matrix:

- Transactions: `ppm.pawn.create`, `ppm.pawn.offer`, `ppm.pawn.activate`
- Appraisal: `ppm.appraisal.create`, `ppm.appraisal.revise`, `ppm.appraisal.approve_high_value`
- Funds: `ppm.funds.release`, `ppm.payment.collect`
- Custody: `ppm.custody.receive`, `ppm.custody.move`, `ppm.custody.locate`
- Release: `ppm.item.release`, `ppm.item.release_exception`
- Renewal: `ppm.renewal.accept`
- Disposition: `ppm.disposition.mark_eligible`, `ppm.disposition.handoff`
- Overrides: `ppm.override.rate`, `ppm.override.maturity`, `ppm.discrepancy.resolve`
- Reports / audit / admin: `ppm.reports.view`, `ppm.audit.view`, `ppm.admin.config`, `ppm.admin.grants`

## Separation of duties (recommended)

| Action | Prefer separate actor for |
|---|---|
| High-value appraisal | Approval |
| Fund release | Appraisal create (when practical) |
| Physical item release | Payment posting (when practical) |
| Disposition handoff | Eligibility marking |

Small shops may not fully separate duties; org policy must be explicit when implemented.

## Supervisor thresholds

Configurable approvals may apply to principal, unusual appraisal, rate/maturity overrides, exceptional renewal/release, discrepancy close, and disposition approval. **Do not invent ₱ thresholds** in docs.

## Non-goals

- Copying POS or PLM grant sets verbatim  
- Implicit role hierarchy in code  
- Cross-org “super manager” without Platform authority  

## Related

- [custody-security.md](custody-security.md)
- [../risks-and-decisions.md](../risks-and-decisions.md)
