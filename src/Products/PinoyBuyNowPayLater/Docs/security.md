# Pinoy Buy Now Pay Later — Security

> Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)  
> Detail: [Security/](Security/README.md), [authorization-matrix.md](authorization-matrix.md)

| Field | Value |
|---|---|
| Product | Pinoy Buy Now Pay Later |
| Status | Planning baseline (BNPL-00); Implementation Not Started |
| Last updated | 2026-08-27 |
| Implementation present | No |

## Posture

BNPL handles financing agreements, balances, repayments, and potentially sensitive customer financial history. Security must be **fail-closed**, server-authoritative, and least-privilege. Do not claim production-secure Platform authentication while R-091 remains open for broader portfolio production claims — keep Dev/Testing vs Production language honest (D-P12-05).

## Access intersection (required intent)

```text
trusted actor
+ trusted organization
+ valid Platform product access (BNPL entitlement)
+ allowed commercial state
+ required entitlement
+ active BNPL product-local role/assignment
+ required product-local grant
+ resource / workflow authorization
(+ branch scope where required)
= operation allowed
```

Platform entitlement is an **entry gate**, not a substitute for BNPL operational grants.

## Threat themes (planning)

| Theme | Direction |
|---|---|
| Cross-tenant leakage | Org isolation; no cross-org queries; Guid org context from trusted session |
| Inventory race / oversell | Final stock check in Commerce; BNPL must not “reserve” by local decrement |
| Duplicate financing / sale | Idempotency keys + reconciliation ([Architecture/idempotency-model.md](Architecture/idempotency-model.md)) |
| Privilege escalation | Explicit grants; no role-name hard-coding; no implicit hierarchy |
| Customer data exposure | Least privilege; Personal identity via contracts; consent where linking |
| Settlement fraud / double pay | Separate settlement ledger; idempotent settlement ops (when model exists) |
| Offline mutation abuse | Online-only financial mutations (BNPL-D-00-11) |

## Secrets and credentials

- No secrets in docs or source committed as plaintext production credentials
- Payment-provider credentials (future) stay out of BNPL operational domain docs until authorized
- Data protection / key ring follows Platform patterns when Platform services are used

## Privacy

Default: **no PHI**. BNPL may process financial and contact data — treat as sensitive commercial data. Retention, export, and deletion policies are **Open** (BNPL-D-00-19). Detail: [Security/privacy-and-sensitive-data-baseline.md](Security/privacy-and-sensitive-data-baseline.md).

## Regulatory / compliance risk (technical vs legal)

BNPL **can become regulated financing activity**. This documentation records questions for Philippine legal/regulatory review. It does **not** make legal conclusions and does **not** claim ExItS is licensed or compliant.

Distinguish:

| Concept | Meaning |
|---|---|
| **Technical capability** | Software can record agreements, schedules, repayments |
| **Legal authorization to operate** | Separate licensing/registration/disclosure requirements |

Open review themes (non-exhaustive): lending/financing classification; licensing/registration; disclosures; interest/fees; consumer protection; collections practices; privacy/data processing; credit/risk data; payment handling; merchant funding/settlement model. See [risks-and-decisions.md](risks-and-decisions.md) (BNPL-D-00-20, BNPL-R-00-04).

## Audit

Application, approval, acceptance, activation, repayments, reversals, settlement, and status transitions must be auditable. Audit history is **not** editable business state. Detail: [Security/audit-and-history-baseline.md](Security/audit-and-history-baseline.md).

## Explicit non-claims

- No claim of BIR/tax compliance
- No claim of lending license
- No claim of production-secure auth beyond current Platform evidence
- No claim that Dev/Local Validation equals Production hardening
