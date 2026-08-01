# ADR-019 — Personal Utang versus Business Credit Ownership

[Decisions](README.md) | [Architecture](../architecture/saas-scopes-users-boundaries-navigation.md) | [Phase 16](../phases/phase-16-isolated-account-profiles-personal-utang-and-business-upgrade.md)

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-08-02 |
| Related | Architecture v1.5 §8.5–8.7 / §15, Phase 16, ADR-003, ADR-011, ADR-020 |

## Context

PinoyBusinessPOS already implements **organization-owned Business Utang / credit** in the POS product database. Phase 16 introduces **Personal Utang** as the free acquisition feature in Personal Scope, with a later optional upgrade into organization Business Credit. Reusing ledger ideas is desirable; merging mutable records or authz would violate Platform/Product ownership and session isolation (ADR-016).

## Decision

1. **Personal Utang** is Personal-owned, free acquisition capability. It works without an organization. Lender/Borrower are relationship roles, not permanent RBAC roles.
2. **Business Credit and Loan Management** (including existing POS Business Utang) are organization- and product-owned advanced features, gated by entitlement plus product-local roles (ADR-011).
3. Personal Utang and Business Credit **may share**:
   - calculation libraries
   - validation rules
   - UI components
   - ledger abstractions
   - reporting primitives
4. They **must not share**:
   - mutable records
   - tenant tables
   - authorization context
   - ownership
   - audit scope
   - active balances
5. Database ownership remains split: Platform for identity/profiles/orgs/entitlements; Personal operational Utang data in the Personal ownership boundary; POS Business Utang in the POS product database. No cross-database FKs (ADR-003).
6. Acquisition path is intentional:

```text
Free Personal Utang
→ Start a Business
→ Organization + POS entitlement + product role
→ Optional selective migration (ADR-020)
→ Business Credit
```

## Consequences

### Positive

- Clear product journey without collapsing personal and business ledgers.
- Safe library reuse without shared mutable state.
- Existing POS Utang remains the business path; Personal Utang is additive design/implementation work.

### Negative / Follow-on

- Two ledgers and migration UX (ADR-020) instead of one unified balance store.
- Naming discipline required: “Utang” in POS ≠ Personal Utang.

## Rejected alternatives

- Storing Personal Utang rows in POS tenant tables “for reuse.”
- Granting Business Credit access from a Personal session.
- Continuous sync of personal and business balances (forbidden; see ADR-020).
