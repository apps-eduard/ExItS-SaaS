# Audit and History Baseline

**Status:** Planning baseline (PSP-00)  
**Implementation present:** No  
**Related decision:** PSP-D-00-17

## Intent

Product-owned operational audit for sensitive actions (payments, estimate acceptance, cancellations, configuration changes, high-risk overrides).

Service history (customer-facing operational trail) is related but not identical — see [../Product/service-history-model.md](../Product/service-history-model.md).

## Rules

- Append-only / auditable effect for financial and high-risk events (planning)
- No silent deletes of financial history
- Tenant isolation and grant-scoped audit views
- Do not push operational payloads into Platform audit that violate boundaries
- Retention open (PSP-D-00-17)

## Non-claims

Audit trails do not by themselves constitute legal, tax, or BIR compliance.
