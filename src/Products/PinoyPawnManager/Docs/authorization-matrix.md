# Pinoy Pawn Manager — Authorization Matrix (Planning)

> Grants are planning labels until **PPM-D-00-18** closes. Do not hard-code authorization to role names.

| Field | Value |
|---|---|
| Status | PPM-00 planning baseline |
| Last updated | 2026-08-27 |

## Layers

1. **Platform authentication** — who the human is  
2. **Organization + product entitlement** — org may use PPM (Platform commercial facts)  
3. **PPM product-local grants** — what staff may do inside PPM  
4. **Resource scope** — Organization / Branch / assigned vault / object-level  

Platform entitlement does **not** replace PPM grants.

## Suggested operational presets (not final Platform roles)

| Preset (planning) | Intent |
|---|---|
| Owner | Org PPM administration, reports, grant management |
| Manager | Supervisory ops, approvals, exceptions |
| Appraiser | Appraisal create/revise within policy |
| Pawn Officer | Create transactions, offers, renewals, redemptions (non-vault-specialist) |
| Cashier | Fund release / collection posting (cash controls Open **PPM-D-00-17**) |
| Vault / Custody Staff | Receive, move, locate, prepare release |
| Supervisor | High-value / exception approvals |
| Auditor / Read Only | Reports + audit view |

Presets map to grants; authorization checks grants.

## Capability catalog (planning identifiers)

| Capability area | Example grant labels | Notes |
|---|---|---|
| Transactions | `ppm.pawn.create`, `ppm.pawn.offer`, `ppm.pawn.activate` | Activation after acceptance/funds |
| Appraisal | `ppm.appraisal.create`, `ppm.appraisal.revise`, `ppm.appraisal.approve_high_value` | Threshold Open |
| Funds | `ppm.funds.release`, `ppm.payment.collect` | Idempotent |
| Custody | `ppm.custody.receive`, `ppm.custody.move`, `ppm.custody.locate` | Movement audit required |
| Release | `ppm.item.release`, `ppm.item.release_exception` | After payment readiness |
| Renewal | `ppm.renewal.accept` | Policy Open |
| Disposition | `ppm.disposition.mark_eligible`, `ppm.disposition.handoff` | Legal eligibility Open |
| Overrides | `ppm.override.rate`, `ppm.override.maturity`, `ppm.discrepancy.resolve` | Dual control preferred |
| Reports / audit | `ppm.reports.view`, `ppm.audit.view` | |
| Admin | `ppm.admin.config`, `ppm.admin.grants` | Owner-scoped |

Exact string identifiers finalize under **PPM-D-00-18**.

## Separation of duties (recommended)

| Action | Prefer separate actor for |
|---|---|
| High-value appraisal | Approval |
| Fund release | Appraisal create (when practical) |
| Physical item release | Payment posting (when practical) |
| Disposition handoff | Eligibility marking |

Configurable; not all small shops can fully separate — document org policy.

## Supervisor / threshold controls

Configurable approvals may apply to (**amounts Open — do not invent ₱**):

- Principal above threshold
- Unusual appraisal vs category norms
- Manual rate override
- Maturity override
- Exceptional renewal
- Exceptional release
- Custody discrepancy close
- Disposition approval

## Non-goals

- Copying POS or PLM grant sets verbatim
- Implicit role hierarchy in code
- Branch-global “manager sees all orgs”
