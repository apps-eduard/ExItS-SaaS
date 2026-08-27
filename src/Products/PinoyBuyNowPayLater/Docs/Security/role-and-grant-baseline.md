# Role and Grant Baseline

**Status:** BNPL-02 capability identifiers implemented; BNPL-03 customer capabilities extended
**Implementation present:** Capability catalog + presets (bundles) + operational access guard + customer CRUD authz
**Related:** BNPL-D-00-18 (Implemented in BNPL-02; extended in BNPL-03)

## Capability identifiers (authoritative)

| Capability | Purpose |
|---|---|
| `bnpl.config` | Product settings |
| `bnpl.customer.read` | View / search BNPL customers |
| `bnpl.customer.manage` | Create / update / link BNPL customers |
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
| Manager | customer read/manage + create/approve/read/repayment/collections/audit/reports (not config/settlement by default) |
| BnplApprover | customer.read + approve + plan.read |
| Sales | customer read/manage + create + plan.read |
| Collector | customer.read + plan.read + repayment + collections |
| Reporting | customer.read + plan.read + reports.read |

Source: `BnplCapabilityPresets`. Authorization checks **capabilities**, not preset labels.

## Rules

- Deny by default
- No role-name authorization
- No implicit hierarchy
- Unknown capability → deny
- create ≠ approve; repayment ≠ settlement
- customer.manage ≠ application.approve
