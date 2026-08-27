# Pinoy Pawn Manager — Audit and History

> Security index: [README.md](README.md)  
> Parent: [../security.md](../security.md)

| Field | Value |
|---|---|
| Status | PPM-00 planning |
| Implementation | None |
| Last updated | 2026-08-27 |

## Intent

PPM distinguishes:

1. **Business history / snapshots** — agreement, appraisal, and identifying-item evidence that must remain historically accurate  
2. **Operational audit stream** — append-only (intent) record of who did what, when, under which org/branch context  

Neither replaces the other. Mutable “current ticket” views must not erase evidence.

## Required event classes (planning)

The following event classes are required for operational integrity when implemented. Names are planning labels; exact schemas deferred.

| Event class | Why it matters |
|---|---|
| **Customer selected** | Ties ticket/intake to customer reference / Platform link |
| **Photos / identifying evidence captured** | Collateral identity; tamper resistance |
| **Appraisal recorded / revised / approved** | Value evidence; supervisor path |
| **Offer proposed** | Terms disclosure before acceptance |
| **Pawn activated** | Binding operational start (with ticket snapshot) |
| **Money released / payment collected** | Financial audit; idempotency correlation |
| **Custody receive / move / locate check** | Physical control chain |
| **Renewal accepted / posted** | Obligation extension history |
| **Redemption payment posted** | Settlement money path |
| **Physical item released** | Separate from payment; recipient + item confirmation |
| **Disposition marked / handoff attempted** | Unredeemed path; Commerce bridge later |
| **Overrides / exceptions** | Rate, maturity, discrepancy, exception release, etc. |

## Minimum audit fields (planning)

Each audit event should capture, at minimum:

- OrganizationId, BranchId (when applicable)
- Actor PlatformUserId
- Event type / capability exercised
- Target object ids (ticket, item, payment, custody move)
- Timestamp (UTC storage; display TZ Open)
- Correlation / idempotency key when applicable
- Outcome (success / denied / failed)
- Optional reason / override justification

## History that must not silently mutate

- Appraisal snapshot at agreement time  
- Pawn agreement / ticket snapshot  
- Pledged-item identifying snapshot (including photo evidence references)  
- Custody movement chain  

Configuration or customer display-name changes after the fact must not rewrite these snapshots.

## Access

- `ppm.audit.view` / `ppm.reports.view` (planning labels)  
- Object-level and org/branch scope still apply  
- Audit views are not a substitute for legal discovery process design (**PPM-D-00-19/20** Open)

## Non-claims

- Audit design ≠ regulatory filing completeness  
- `LEGAL_AUTHORIZATION_CLAIMED=NO`  

## Related

- [custody-security.md](custody-security.md)
- [privacy-and-sensitive-data.md](privacy-and-sensitive-data.md)
- [../Architecture/idempotency-and-reconciliation.md](../Architecture/idempotency-and-reconciliation.md)
- [../Compliance/philippines-regulatory-review.md](../Compliance/philippines-regulatory-review.md)
