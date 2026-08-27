# Role and Grant Baseline

**Status:** BNPL-02..04 capability identifiers implemented
**Implementation present:** Capability catalog + presets + access guard + financing application authz
**Related:** BNPL-D-00-18 (extended in BNPL-03/04)

## Capability identifiers (authoritative)

| Capability | Purpose |
|---|---|
| `bnpl.config` | Product settings |
| `bnpl.customer.read` | View / search BNPL customers |
| `bnpl.customer.manage` | Create / update / link BNPL customers |
| `bnpl.application.read` | View / search financing applications |
| `bnpl.application.create` | Create/edit/submit/offer/accept/cancel applications |
| `bnpl.application.approve` | Eligibility + final approve/decline |
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
