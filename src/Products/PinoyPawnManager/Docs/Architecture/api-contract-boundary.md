# Pinoy Pawn Manager — API Contract Boundary

> Architecture index: [README.md](README.md)  
> Persistence: [persistence-boundary.md](persistence-boundary.md)

| Field | Value |
|---|---|
| Status | PPM-00 planning |
| Implementation | None |
| Last updated | 2026-08-27 |

## Intent

When implemented, PPM exposes its own API surface for organization staff Web/PWA clients. Integration with Platform and (later) Commerce uses **approved contracts** and stable identifiers—not shared assemblies of EF entities or DTO leakage of persistence models.

## Contract rules

| Rule | Intent |
|---|---|
| Product-owned API | PPM API project(s) own pawn/custody/payment endpoints |
| DTOs ≠ EF entities | API/UI must not expose persistence entities |
| Guids / value objects for externals | Org, branch, user, future Commerce refs |
| Server-authoritative authz | Grants + org/branch/object scope checked server-side |
| Idempotency headers/keys | Required for money and high-risk custody mutations |
| No cross-product internal APIs by default | Prefer explicit published contracts if any |

## Surfaces (planning)

| Surface | Notes |
|---|---|
| PPM Organization Web / PWA | Primary consumer; online-only mutations initially |
| Platform Admin | Catalog/subscription only—not pawn ops API consumer |
| ExItS Personal | Optional future read/presentation (**PPM-15**) |
| Commerce handoff | Future outbound/inbound contract (**PPM-D-00-15**) |

## Versioning and honesty

- Do not claim production-secure endpoints while portfolio auth maturity (**R-091**) is incomplete  
- Development-stage unauthenticated shortcuts must be labeled honestly (**D-P12-05**)  
- Do not invent regulatory ticket field mandates in API schemas without Compliance closure  

## Non-goals (PPM-00)

- OpenAPI specs, controllers, or client SDKs  
- Shared “portfolio loan DTO” reused from PLM  
- Direct calls into POS inventory write APIs without handoff ADR  

## Related

- [platform-integration.md](platform-integration.md)
- [idempotency-and-reconciliation.md](idempotency-and-reconciliation.md)
- [web-pwa-runtime-policy.md](web-pwa-runtime-policy.md)
- [../authorization-matrix.md](../authorization-matrix.md)
