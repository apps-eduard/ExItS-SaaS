# Decisions

**Purpose:** Architecture Decision Records (ADRs) explaining important architectural or business choices and **WHY**.
**Status:** Foundation / planning only
**Implementation present:** No

Major irreversible or cross-product choices must eventually receive an ADR in this directory. No ADR is approved in this work package.

---

## Open decisions (do not invent answers)

| ID | Subject | Notes |
|---|---|---|
| PLM-D-00-01 | Platform catalog registration | Proposed slug `pinoy-loan-manager` is not registered |
| PLM-D-00-02 | Product database | Proposed name `ExItS_PinoyLoanManager`; schema and migrations not authorized |
| PLM-D-00-03 | Physical source/test/deploy layout | Code projects deferred; later architecture package decides the tree beside `Docs/` |
| PLM-D-00-04 | Generic Platform cross-product relationship schema | Personal may later link independently to POS Customer, Loan Borrower, and future BNPL customer; do not design the Platform schema in PLM-00-WP01 |
| PLM-D-00-05 | Personal ↔ Borrower linking implementation | Optional, consent-required, never auto-link from EX ID / QR; mechanism undecided |
| PLM-D-00-06 | Product-local roles and grants | Not defined |
| PLM-D-00-07 | Loan operational-money model | Must not be SaaS billing; ledger/entities undecided |
| PLM-D-00-08 | Loan business policy | Interest, amortization, penalties, allocation, delinquency, restructuring, write-off, scoring, limits, approval, collateral, regulatory rules |
| PLM-D-00-09 | Client project authorization | Web Blazor and MAUI Blazor Hybrid are proposed only |
| PLM-D-00-10 | Product Foundation template fill | Mandatory templates (`product-definition.md`, `architecture.md`, and related files) are not filled in this structure-only package |
| D-P12-03 | Platform→product commercial-state transport | Portfolio-open; do not invent |
| R-091 | Production authentication | Portfolio-open; do not invent |
| D-P12-05 | Honest Dev/Testing vs Production language | Tied to R-091 |

---

## Recorded intentions (not ADRs)

The following are planning constraints already agreed for Pinoy Loan Manager. They should be restated in ADRs only when a later package makes an irreversible implementation choice:

- first-class sibling product under ExItS Platform
- independent subscription
- separate database
- no cross-product foreign keys
- no direct POS or Platform table reads
- Platform integration via approved contracts / APIs only
- SaaS billing money vs loan operational money
- optional Personal linking with explicit consent
- borrower may exist without an ExItS Personal account
