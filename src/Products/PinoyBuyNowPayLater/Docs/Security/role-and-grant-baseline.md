# Role and Grant Baseline

**Status:** BNPL-02 capability identifiers implemented  
**Implementation present:** Capability catalog + presets (bundles) + operational access guard  
**Related:** BNPL-D-00-18 (Provisionally Approved / Implemented in BNPL-02)

## Capability identifiers (authoritative)

| Capability | Purpose |
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

Source: `ExItS.PinoyBuyNowPayLater.Domain.Access.BnplCapabilityCodes`.

## Presets (bundles only — never authorize by name)

| Preset label | Bundle intent |
|---|---|
| Owner | All capabilities |
| Manager | create/approve/read/repayment/collections/audit/reports (not config/settlement by default) |
| BnplApprover | approve + plan.read |
| Sales | create + plan.read |
| Collector | plan.read + repayment + collections |
| Reporting | plan.read + reports.read |

Source: `BnplCapabilityPresets`. Authorization checks **capabilities**, not preset labels.

## Rules

- Deny by default  
- No role-name authorization  
- No implicit hierarchy  
- Unknown capability → deny  
- create ≠ approve; repayment ≠ settlement  
