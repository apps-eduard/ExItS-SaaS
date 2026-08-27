# Role and Grant Baseline

**Status:** Planning baseline (BNPL-00)  
**Implementation present:** No  
**Related:** BNPL-D-00-18

## Presets (planning labels — not final codes)

Owner · Manager · BNPL Approver · Sales/Cashier · Collector/Support · Reporting/Read-only

## Grant areas (intent)

| Area | Examples |
|---|---|
| `bnpl.config` | Product settings |
| `bnpl.application.create` | Create financing request |
| `bnpl.application.approve` | Approve / decline |
| `bnpl.plan.read` | View plans |
| `bnpl.repayment.create` | Record repayment |
| `bnpl.collections.manage` | Collections queue |
| `bnpl.settlement.manage` | Settlement ops (when model exists) |
| `bnpl.audit.read` | Audit views |
| `bnpl.reports.read` | Reports |

Identifiers are **Open**. Do not hard-code authorization to role names. Do not implement implicit role hierarchy. Do not copy POS/PLM/PSP grant catalogs.
