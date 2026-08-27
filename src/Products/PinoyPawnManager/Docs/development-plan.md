# Pinoy Pawn Manager — Development Plan

| Field | Value |
|---|---|
| Status | PPM-00 planning |
| Last updated | 2026-08-27 |

## Delivery principles

1. Documentation and decisions before domain code.
2. First-class product isolation (own DB, own grants, own APIs).
3. Never weaken tests to pass a package.
4. Financial and custody mutations are server-authoritative and idempotent.
5. Legal/regulatory Open items remain Open until evidenced.
6. Do not implement adjacent products (POS/PLM/BNPL) inside PPM packages.

## Buckets

| Bucket | Packages | Outcome |
|---|---|---|
| Foundation docs | PPM-00 | Authoritative Docs tree |
| Scaffold & access | PPM-01 … PPM-02 | Projects + authz shell |
| Domain core | PPM-03 … PPM-07 | Customer, item, appraisal, ticket, funds |
| Custody & lifecycle | PPM-08 … PPM-12 | Storage, maturity, renewal, redeem, unredeemed |
| Commerce bridge | PPM-13 | Disposition handoff contract |
| Hardening | PPM-14 … PPM-16 | Reports, Personal (optional), E2E/security |

## Testing expectations (future implementation)

| Area | Expectation |
|---|---|
| Unit | State machines, quote calculations (when policy closed), grant checks |
| Integration | PostgreSQL/Testcontainers for persistence; no EF InMemory as PG proof |
| Isolation | Architecture tests: no forbidden project references / nested foreign products |
| Idempotency | Retry of release/renewal/redemption does not double-post |
| Custody | Movement history; release requires readiness + confirmation |
| E2E | Online Web happy paths for intake → active → redeem/renew |

## Explicit non-goals for early packages

- Offline mutation outbox for Web
- AI appraisal
- Direct POS inventory writes
- Claiming BIR/pawnshop license compliance
- Copying PLM loan schedule engines as pawn tickets

## Gate to implementation

- PPM-00 complete (this Docs tree)
- Product Owner awareness of Open `PPM-D-00-*` items that block policy-sensitive packages
- Explicit authorization to start PPM-01
