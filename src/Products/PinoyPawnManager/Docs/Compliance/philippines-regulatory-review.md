# Pinoy Pawn Manager — Philippines Regulatory Review (Open Questions)

> Compliance index: [README.md](README.md)  
> Decision **PPM-D-00-20** OPEN · Risk **PPM-R-00-04** / **PPM-R-00-09**

| Field | Value |
|---|---|
| Status | PPM-00 planning — **research caution only** |
| Implementation | None |
| Last updated | 2026-08-27 |
| Legal claim | `LEGAL_AUTHORIZATION_CLAIMED` = **NO** |

## Honesty preamble

This document lists **open questions** that must be answered with qualified legal/compliance review before production claims. It is **not** legal advice.

Do **not** treat the presence of a software field, workflow, or report as proof of Philippine regulatory compliance.

**Technical capability ≠ legal authorization.**

ExItS documentation must not invent current Philippine statutory conclusions (rates, license classes, grace periods, auction procedures, notice timelines, etc.) as closed facts. Where practice is commonly discussed in the industry, treat it as a **question to verify**, not as ExItS policy.

## Open questions register

| # | Topic | Open question (verify with counsel / regulators / Product Owner) | Doc / decision link |
|---|---|---|---|
| 1 | **Licensing** | What licenses, registrations, or permits are required to operate a pawnshop (or pawnshop-like collateral lending) in the Philippines? Who must hold them—the subscriber organization, not ExItS? | **PPM-D-00-20** |
| 2 | **Ticket contents** | What information must appear on a pawn ticket / pawn agreement for it to be valid or inspectable? Which fields are mandatory vs optional? | Ticket snapshot design; do not invent mandatory list |
| 3 | **KYC / AML** | What customer identification, recordkeeping, or AML/CTF obligations apply to pawn operators? When are ID copies required? | Privacy + Security; no invented KYC schema |
| 4 | **Retention** | How long must tickets, appraisals, photos, payment records, and custody logs be retained? How do retention rules interact with DPA deletion rights? | **PPM-D-00-19** |
| 5 | **Interest / charges** | Which interest, service charges, or fee structures are permitted, capped, or disclosure-regulated? | **PPM-D-00-08** — no invented rates |
| 6 | **Renewal** | What rules govern renewal/extension frequency, disclosures, and recomputation of maturity? | **PPM-D-00-11** |
| 7 | **Maturity / grace** | How are maturity date, any grace period, and default/unredeemed classification defined in applicable rules? | **PPM-D-00-09/10** — no invented calendars |
| 8 | **Notices** | What customer notices (maturity, default, disposition/auction) are required, in what form, and on what timeline? | Operations + Compliance later |
| 9 | **Unredeemed / auction / disposition** | When may unredeemed pledges be sold, auctioned, or otherwise disposed? What process, notice, surplus/deficit handling, and authority apply? | **PPM-D-00-14** — technical eligibility ≠ legal sale authority |
| 10 | **Data Privacy Act (DPA)** | What lawful bases, privacy notices, security measures, and cross-border rules apply to customer and collateral personal data (including photos)? | [../Security/privacy-and-sensitive-data.md](../Security/privacy-and-sensitive-data.md) |
| 11 | **Prohibited goods** | Which categories of goods may not be accepted as pledges (e.g. restricted items)? How should configurable category blocks align with law? | **PPM-D-00-05** |
| 12 | **Stolen / illicit property** | What duties apply if goods are suspected stolen (hold, report, refuse, cooperate with authorities)? How should software support without claiming investigative authority? | Security + Operations planning |

## Explicit non-conclusions

Until evidenced and Product Owner–accepted:

- ExItS / PPM is **not** claimed to be a licensed pawnshop operator  
- No fixed interest rate, LTV %, grace day count, or auction schedule is authoritative  
- No claim of AML/KYC program completeness  
- No claim that disposition workflow equals lawful auction authority  
- Subscriber organizations remain responsible for their own regulatory compliance  

## Safe engineering defaults while Open

| Default | Rationale |
|---|---|
| Keep regulatory items **OPEN** in [../risks-and-decisions.md](../risks-and-decisions.md) | Avoid false closure |
| Prefer configurable policy hooks over hard-coded “legal” constants | Law may differ by facts |
| Separate operational “unredeemed” from “legally eligible to dispose” | Prevent auto ownership transfer |
| Payment ≠ physical release | Custody and consumer protection caution |
| Minimize sensitive data collection | DPA risk (**PPM-R-00-07**) |
| Online-only money/custody mutations initially | Reduce ambiguous offline financial events |

## How to close a Compliance question

1. Obtain qualified legal/compliance input or official source evidence.  
2. Record evidence location and date in Decisions / Risks.  
3. Update Product Owner–accepted policy.  
4. Only then implement enforcing rules or production marketing claims.  

“Closed” requires repository or Product Owner evidence—not model inference.

## Related

- [README.md](README.md)
- [../risks-and-decisions.md](../risks-and-decisions.md)
- [../Security/privacy-and-sensitive-data.md](../Security/privacy-and-sensitive-data.md)
- [../Architecture/pos-commerce-boundary.md](../Architecture/pos-commerce-boundary.md)
