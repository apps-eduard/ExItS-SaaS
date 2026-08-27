# Customer Model

**Status:** Implemented foundation (BNPL-03)
**Implementation present:** Yes — `BnplCustomer`
**Related:** BNPL-D-00-13 (Personal UX still open), BNPL-D-00-04

## Principles

- BNPL must **not** create an incompatible duplicate ExItS identity system.
- Platform owns Personal user identity and public user identifiers (`EX-####-####`).
- BNPL maintains a **product-local customer profile** that may reference Platform and/or merchant-local Commerce customer identifiers via **stable identifiers**, not cross-product FKs.
- Organization isolation is mandatory. Customer ≠ Organization staff.

## Implemented identity

| Field | Notes |
|---|---|
| `CustomerId` | BNPL-owned Guid (client-stable for idempotent create) |
| `OrganizationId` | Immutable org ownership |
| `DisplayName` | Required |
| `Mobile` / `Email` | Optional contact only — **not** authorization identity |
| `Status` | `Active` / `Inactive` only |
| `LinkedPersonalPublicUserId` | Optional `EX-####-####` |
| `LinkedCommerceCustomerId` | Optional Commerce/POS customer Guid |

Customer scope = **Organization**. Branch is **not** part of customer identity (future financing/sale carries BranchId).

## Uniqueness (within Organization)

- `OrganizationId` + non-null Personal public id → unique
- `OrganizationId` + non-null Commerce customer id → unique
- Same Personal id in different Organizations → allowed

## Explicit non-goals

- Auto-creating Organization staff from BNPL customers
- Treating email/mobile as auth keys
- KYC / government ID / credit score in BNPL-03
- Cross-DB FK to PlatformUsers / PosCustomers
- Personal self-service (BNPL-D-00-13)
