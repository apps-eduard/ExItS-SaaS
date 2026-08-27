# Pinoy Buy Now Pay Later — Authorization Matrix

> Capability identifiers: **Implemented in BNPL-02; extended in BNPL-03** (BNPL-D-00-18). Do not hard-code authorization to role/preset names.

| Field | Value |
|---|---|
| Product | Pinoy Buy Now Pay Later |
| Status | BNPL-03 customer foundation + access |
| Last updated | 2026-08-27 |
| Implementation present | Access guard + customer capabilities + customer API |

## Access layers

| Layer | Owner | Purpose |
|---|---|---|
| Platform account / session | Platform | Authenticated actor |
| Organization membership | Platform | Org membership context |
| Product entitlement | Platform | BNPL subscription/commercial gate |
| Product assignment | Platform | Actor assigned BNPL product access |
| Branch access | Trusted transport → BNPL context | Opaque Platform org-branch Guids |
| BNPL capability | Trusted transport → BNPL context | Operational permission |

## Effective access formula

```text
trusted actor
+ trusted organization membership
+ organization BNPL entitlement
+ actor BNPL product assignment
+ branch scope (when required)
+ required BNPL capability (when required)
= operation allowed
```

Any missing/invalid layer → **DENY**. Default runtime provider is unavailable (fail closed) until D-P12-03 transport is wired.

## Capability matrix (intent via presets)

| Capability | Owner | Manager | Approver | Sales | Collector | Reporting |
|---|---|---|---|---|---|---|
| bnpl.config | Y | N | N | N | N | N |
| bnpl.customer.read | Y | Y | Y | Y | Y | Y |
| bnpl.customer.manage | Y | Y | N | Y | N | N |
| bnpl.application.create | Y | Y | N | Y | N | N |
| bnpl.application.approve | Y | Y | Y | N | N | N |
| bnpl.plan.read | Y | Y | Y | Y | Y | Y |
| bnpl.repayment.create | Y | Y | N | N | Y | N |
| bnpl.collections.manage | Y | Y | N | N | Y | N |
| bnpl.settlement.manage | Y | N | N | N | N | N |
| bnpl.audit.read | Y | Y | N | N | N | N |
| bnpl.reports.read | Y | Y | N | N | N | Y |

## Customer staff operations

Require `X-Bnpl-Branch-Id` + `bnpl.customer.read` / `bnpl.customer.manage`.
Customer aggregate is organization-scoped (not branch-bound).

## Least privilege

- Deny by default
- Separate create vs approve
- Separate repayment vs settlement
- Branch-scoped staff must not see other branches unless org-wide scope is explicitly asserted
- Customer/Personal actors never receive organization staff capabilities

## Diagnostic / customer surfaces

- `GET /api/v1/bnpl/access/me`
- `POST|GET|PATCH /api/v1/bnpl/customers` (+ personal-link / commerce-link)
