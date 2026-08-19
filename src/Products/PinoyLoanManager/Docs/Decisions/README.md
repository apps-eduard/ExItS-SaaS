# Decisions

**Purpose:** Architecture Decision Records (ADRs) explaining important architectural or business choices and **WHY**.
**Canonical register:** [../risks-and-decisions.md](../risks-and-decisions.md)
**Status:** Foundation / planning only
**Implementation present:** No

PLM-00-WP03 through WP10 record agreed **product direction** in Product / Architecture / Security docs; ADRs close irreversible identity and linking choices where explicitly approved.

## Approved ADRs (PLM-DOC-01)

| ADR | Subject | Decisions closed |
|---|---|---|
| [ADR-001-product-identity-and-database-name.md](ADR-001-product-identity-and-database-name.md) | Product code and logical database name | PLM-D-00-01; PLM-D-00-02 (logical name only) |
| [ADR-002-borrower-personal-cardinality-and-consent.md](ADR-002-borrower-personal-cardinality-and-consent.md) | Borrower/Personal cardinality, MVP linking, consent | Product behavior accepted; PLM-D-00-04, PLM-D-00-05, PLM-D-00-11, PLM-D-00-13 remain open |

## Approved ADRs (PLM-DOC-02)

| ADR | Subject | Decisions closed |
|---|---|---|
| [ADR-003-supported-interest-and-schedule-methods.md](ADR-003-supported-interest-and-schedule-methods.md) | MVP interest/schedule methods | Product methods accepted; PLM-D-00-08 remains Open / Partially Resolved |
| [ADR-004-rounding-fees-and-payment-allocation.md](ADR-004-rounding-fees-and-payment-allocation.md) | Rounding, fees, allocation | **PLM-D-00-12 Closed**; PLM-D-00-07 / PLM-D-00-08 remain Open / Partially Resolved |

Major future irreversible or cross-product choices must receive an ADR here when explicitly approved.

Do not close PLM-D-00-03, PLM-D-00-04 through PLM-D-00-09, PLM-D-00-11, PLM-D-00-13, D-P12-03, R-091, or D-P12-05 without explicit approval. Do not mark PLM-D-00-07 or PLM-D-00-08 Closed.
