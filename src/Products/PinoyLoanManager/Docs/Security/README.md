# Security

**Purpose:** Access, privacy, security, authorization, consent, data classification, and audit rules.
**Canonical documents:** [../security.md](../security.md), [../authorization-matrix.md](../authorization-matrix.md)
**Status:** Foundation / planning only
**Implementation present:** No

Do not treat this folder as a second security specification.

| Doc | Subject |
|---|---|
| [role-and-grant-baseline.md](role-and-grant-baseline.md) | Owner / Manager / Cashier / Collector presets; grant catalog **intent**; scope; SoD |
| [audit-and-history-baseline.md](audit-and-history-baseline.md) | High-risk history; not ordinary editable notes |

Grant **identifiers** remain **Open / Product Owner Decision Required** (PLM-D-00-06). Maker/checker and controlled Owner Override are accepted (**PLM-D-00-13 Closed**): [../Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md](../Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md). Personal linking / consent is planning intent only (PLM-D-00-05). Do not hard-code authorization to role names. Do not implement implicit role hierarchy. Do not copy PinoyBusinessPOS grant sets.
