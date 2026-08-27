# Pinoy Pawn Manager — Privacy and Sensitive Data

> Security index: [README.md](README.md)  
> Compliance: [../Compliance/philippines-regulatory-review.md](../Compliance/philippines-regulatory-review.md)

| Field | Value |
|---|---|
| Status | PPM-00 planning |
| Implementation | None |
| Last updated | 2026-08-27 |
| Related decision | **PPM-D-00-19** retention / deletion / export OPEN |

## Intent

PPM handles sensitive operational data: customer identity references, collateral photos, serials/IMEI, jewelry marks, appraisal notes, and payment facts. Privacy design must minimize collection, limit purpose, control access, and keep retention Open until decided.

## Classification (planning)

| Class | Examples | Handling intent |
|---|---|---|
| Identity references | Name, contact, Platform Personal link | Least privilege; not a second auth DB |
| Verification fields | ID images / KYC-like fields | **Legal/product decision** — do not invent mandatory KYC schema |
| Collateral evidence | Photos, serials, IMEI, marks | Access-controlled; not marketing assets |
| Financial | Principal, payments, charges | Org-scoped; audit correlated |
| Custody | Locations, movements | Staff with custody grants |
| PHI | Default **none** | No PHI unless separately authorized |

## Minimization principles

- Collect only what intake, appraisal, custody, redemption, and compliance workflows require  
- Prefer references and hashes/ids over re-storing Platform identity payloads when possible  
- Do not publish collateral photos outside authorized staff surfaces  
- Do not use evidence for unrelated analytics without separate authorization  

## Access control

- Product-local grants for evidence view/upload  
- Org and branch isolation  
- Exceptional export subject to admin grant + retention policy (**PPM-D-00-19**)

## Retention (Open)

Safe default until decided: retain while organization remains subscribed; **no silent purge** of ticket/evidence needed for operational disputes. Legal retention periods are Compliance Open items—do not invent statutory durations here.

## Data Privacy Act (caution only)

Philippine Data Privacy Act considerations are tracked as open questions in Compliance. Software capability to store photos or ID data is **not** authorization to process personal data without a lawful basis and org compliance program.

`LEGAL_AUTHORIZATION_CLAIMED=NO`

## Risks

- **PPM-R-00-07** evidence over-collection / privacy  

## Related

- [audit-and-history.md](audit-and-history.md)
- [../Compliance/README.md](../Compliance/README.md)
- [../risks-and-decisions.md](../risks-and-decisions.md)
