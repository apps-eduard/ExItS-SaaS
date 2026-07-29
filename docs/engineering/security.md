# Security

[Home](../index.md) | [Authorization](authorization-matrix.md)

## Invariants

1. PlatformOrganizationId and product tenant context are server-controlled.
2. One product cannot access another product’s operational database.
3. Product APIs enforce their own roles and permissions.
4. Subscription and feature checks are server-side.
5. Entitlement snapshots are signed/validated and time-bounded.
6. Posted financial records are append-only and corrected by reversal/adjustment.
7. HealthCare patient self-scope remains HealthCare-specific.
8. Secrets and tokens never appear in logs.
9. Localization cannot expose untranslated internal keys or sensitive debug details.
10. Theme selection cannot weaken focus visibility or contrast.
