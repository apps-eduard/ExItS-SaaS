# P2-WP02 — Shared Identity and Organization Boundary

## 1. Assignment

| Field | Value |
|---|---|
| Phase | Phase 2 — Platform Extraction and legacy product Reconnection |
| Work package | P2-WP02 — Shared Identity and Organization Boundary |
| Status | Ready for Review |
| Branch | `main` |
| Date | 2026-07-29 |

## 2. Summary

Implemented the first Platform **Domain** and **Application** boundary for global users, platform organizations, organization memberships, platform organization roles, product-access concepts (`ProductCode` + minimal `ProductAccess`), account/membership/organization statuses, strongly typed identifiers, domain invariants, explicit repository contracts, and use cases. Behavior is proven with unit and architecture tests. No authentication, persistence, EF/Npgsql, business API routes, catalog, plans, subscriptions, entitlements, legacy product integration, or POS.

## 3. Acceptance criteria and evidence

| Criterion | Status | Evidence |
|---|---|---|
| P2-WP01 recorded Complete | Met | portfolio-progress / phase-02 |
| Strong IDs (`PlatformUserId`, `PlatformOrganizationId`, `OrganizationMembershipId`) | Met | Domain Identity/Organizations |
| Platform User / Organization / Membership models | Met | Aggregates + controlled statuses |
| Platform org roles only | Met | `OrganizationRole` enum (Owner/Administrator/Member) |
| `ProductCode` modeled; product-local roles absent | Met | Products + architecture tests |
| Patient / POS Customer not Platform entities | Met | Architecture type-name tests |
| Controlled status transitions | Met | Domain + unit tests |
| Explicit application contracts; no generic repository | Met | `IPlatform*Repository` only |
| No persistence / EF / auth / business API | Met | No packages; API still `/` + `/health` |
| API starts without DB; health works | Met | Runtime on `http://127.0.0.1:5288` |
| Architecture + unit tests pass | Met | **61** passed / 0 failed / 0 skipped |
| portfolio independence verification | Met | `git ls-files` empty; ignored; not in solution |
| Docs + focused commit | Met | This report / hash section |

## 4. Types created

### Identifiers
- `PlatformUserId`, `PlatformOrganizationId`, `OrganizationMembershipId`

### Status / role / product
- `AccountStatus` (Active, Suspended, Deactivated)
- `OrganizationStatus` (Active, Suspended, Closed)
- `MembershipStatus` (Active, Suspended, Removed)
- `OrganizationRole` (OrganizationOwner, OrganizationAdministrator, OrganizationMember)
- `ProductCode`, `ProductAccessStatus`, `ProductAccess`

### Aggregates
- `PlatformUser`, `PlatformOrganization`, `OrganizationMembership`

### Domain support
- `DomainException`, `DomainErrorCodes`, `IClock`

### Application
- `ApplicationResult` / `ApplicationResult<T>`, `ApplicationErrorCodes`
- `IPlatformUserRepository`, `IPlatformOrganizationRepository`, `IOrganizationMembershipRepository`
- Use cases: `CreatePlatformUser`, `SuspendPlatformUser`, `CreatePlatformOrganization`, `AddOrganizationMembership`, `SuspendOrganizationMembership`, `ChangeOrganizationRole`

## 5. Domain invariants (summary)

- IDs reject `Guid.Empty`; equality by value; no implicit cross-type conversion.
- Display names and emails/slugs/product codes validated and normalized; UTC `DateTimeOffset` required.
- User: Active↔Suspended; Active/Suspended→Deactivated; Deactivated terminal (no reactivate/update).
- Organization: Active↔Suspended; Active/Suspended→Closed; Closed terminal.
- Membership: Active↔Suspended; Active/Suspended→Removed; Removed not silently reactivated.
- One active membership per user+organization pair enforced at application boundary (DB uniqueness later).
- Membership roles are organization-level only; product access ≠ entitlement.

## 6. Domain error strategy

`DomainException` with stable `ErrorCode` strings (`DomainErrorCodes`). Application use cases catch domain failures and return `ApplicationResult` with the same or application-level codes (not found / conflict). Not coupled to ASP.NET Core `ProblemDetails`. No large Result framework.

## 7. Dependency and packages

- Project refs unchanged in direction: Domain ← Application ← Infrastructure ← Api.
- UnitTests now also references Application (for use-case tests).
- **No new NuGet packages** (no EF, Npgsql, Identity, JWT, MediatR, FluentValidation, etc.).

## 8. API changes

None for business routes. Existing:

- `GET /` → 200 JSON (`phase`: `P2-WP02-identity-organization`)
- `GET /health` → 200 `Healthy`

Port **5288**. No database configuration. Clean shutdown verified.

## 9. Tests

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| ExItS.Platform.UnitTests | 48 | 0 | 0 |
| ExItS.ArchitectureTests | 13 | 0 | 0 |
| **Total** | **61** | **0** | **0** |

| Command | Exit |
|---|---:|
| `dotnet restore ExItS.slnx` | 0 |
| `dotnet build ExItS.slnx -c Release` | 0 (0 warnings, 0 errors) |
| `dotnet test ExItS.slnx -c Release --no-build` | 0 |

## 10. portfolio independence verification

| Check | Result |
|---|---|
| `git ls-files legacy product` | empty |
| `git check-ignore -v legacy product/` | `.gitignore:6:legacy product/` |
| `dotnet sln ExItS.slnx list` | Platform + test projects only |
| legacy product files modified | No |

## 11. Risks and open decisions

- Authentication (login/JWT/Identity) still absent by design — P2-WP03+ / later security WPs.
- Persistence uniqueness for memberships deferred to infrastructure WP.
- R-016 remote still empty; do not push without authorization.
- Product catalog / plans / entitlements remain out of scope (P2-WP03).

## 12. Next work package

**P2-WP03 — Products, Plans and Entitlement Foundation** (do not begin until authorized).

## 13. Commit

| Field | Value |
|---|---|
| Hash | `49f8ae81f9c5b8a60307ac1d9eb67eab8d2f45ba` |
| Message | `feat(platform): add identity organization domain boundary` |
