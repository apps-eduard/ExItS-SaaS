# Pinoy Loan Manager — Source and Project Layout

**Status:** Planning / architecture baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Future **implementation target** for Pinoy Loan Manager source layout. **Do not create these projects in this package.**

Related: [api-and-contract-boundary.md](api-and-contract-boundary.md), [persistence-and-database-boundary.md](persistence-and-database-boundary.md), [mobile-offline-boundary.md](mobile-offline-boundary.md), [../architecture.md](../architecture.md).

---

## Target tree

Subject to repository Product Foundation conventions (`src/Products/<ProductName>/` plus `Docs/`):

```text
src/Products/PinoyLoanManager/
├── Docs/
├── ExItS.PinoyLoanManager.Domain/
├── ExItS.PinoyLoanManager.Application/
├── ExItS.PinoyLoanManager.Infrastructure/
├── ExItS.PinoyLoanManager.Api/
├── ExItS.PinoyLoanManager.ApiClient/
├── ExItS.PinoyLoanManager.Web/
├── ExItS.PinoyLoanManager.Maui/
└── ExItS.PinoyLoanManager.LocalStore/   only if/when justified
```

Tests should follow repository Product Foundation / existing solution conventions (typically `tests/ExItS.PinoyLoanManager.*.Tests` when authorized). Exact test-project names remain for the scaffold WP.

This matches Product Foundation section 9 (product folder with Domain, Application, Infrastructure, Api, clients, UI as authorized). **No Product Foundation conflict.** PLM-D-00-03 remains open until an authorized scaffold creates the projects.

---

## Layering

| Project | Responsibility |
|---|---|
| **Domain** | Persistence-independent domain model and invariants |
| **Application** | Use cases and contracts; **no** Infrastructure reference |
| **Infrastructure** | Persistence and integrations |
| **Api** | HTTP / API host |
| **ApiClient** | Typed client contracts / consumer utilities as appropriate |
| **Web** | PLM Organization Web (full operations) |
| **Maui** | Limited field operational client |
| **LocalStore** | Future mobile / offline persistence **if needed** |

UI projects must not reference Infrastructure, EF Core, or Npgsql. Domain remains persistence-independent.

**No project may reference PinoyBusinessPOS.**

Do **not** introduce a new application framework merely because this is a new product. Follow existing ExItS solution technology direction (.NET, Blazor Web, MAUI Blazor Hybrid). No NuGet / package additions in this WP.

Web / MAUI component-sharing is **Closed** (PLM-D-00-09). See [web-maui-component-sharing-policy.md](web-maui-component-sharing-policy.md).

---

## Explicit non-goals

- Creating directories, `.csproj`, or `ExItS.slnx` entries
- Copying POS domain projects
- Authorizing LocalStore by default
