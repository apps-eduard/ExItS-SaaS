# Security

**Purpose:** Access, privacy, security, authorization, consent, data classification, and audit rules.
**Canonical documents:** [../security.md](../security.md), [../authorization-matrix.md](../authorization-matrix.md)
**Status:** PLM Authorization Policy v1 accepted (PLM-DOC-05); **PLM-D-00-06 Closed for MVP**
**Implementation present:** No

Do not treat this folder as a second security specification.

| Doc | Subject |
|---|---|
| [role-and-grant-baseline.md](role-and-grant-baseline.md) | Index to PLM Authorization Policy v1 |
| [authorization-grant-catalog.md](authorization-grant-catalog.md) | Exact MVP grant identifiers |
| [default-role-preset-policy.md](default-role-preset-policy.md) | Role codes and default preset assignments |
| [resource-scope-and-data-minimization-policy.md](resource-scope-and-data-minimization-policy.md) | Scope types and data minimization |
| [privileged-access-and-owner-recovery-policy.md](privileged-access-and-owner-recovery-policy.md) | Owner bootstrap, last-Owner protection, recovery |
| [audit-and-history-baseline.md](audit-and-history-baseline.md) | High-risk history; not ordinary editable notes |

Grant catalog v1 and default presets are accepted (**PLM-D-00-06 Closed for MVP**). Custom roles deferred. Maker/checker and controlled Owner Override are accepted (**PLM-D-00-13 Closed**): [../Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md](../Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md). Workflow guards: [../Product/workflow-authorization-policy.md](../Product/workflow-authorization-policy.md). Personal linking / consent is planning intent only (PLM-D-00-05). Do not hard-code authorization to role names. Do not implement implicit role hierarchy. Do not copy PinoyBusinessPOS grant sets.
