# Pinoy Loan Manager — Source and Project Layout

**Status:** PLM-01 scaffold (physical layout proven)
**Implementation present:** Product shell only — no lending domain
**Last updated:** 2026-08-19

Physical layout after **PLM-01**. PLM-D-00-03 is **Closed**. MAUI and LocalStore remain planned and are **intentionally deferred** (not removed).

Related: [api-and-contract-boundary.md](api-and-contract-boundary.md), [persistence-and-database-boundary.md](persistence-and-database-boundary.md), [mobile-offline-boundary.md](mobile-offline-boundary.md), [../architecture.md](../architecture.md), [../Reports/PLM-01-product-scaffold-and-isolation.md](../Reports/PLM-01-product-scaffold-and-isolation.md).

---

## Tree

```text
src/Products/PinoyLoanManager/
├── Docs/
├── ExItS.PinoyLoanManager.Domain/            created
├── ExItS.PinoyLoanManager.Application/       created
├── ExItS.PinoyLoanManager.Infrastructure/    created (no EF/Npgsql)
├── ExItS.PinoyLoanManager.Api/               created (health only)
├── ExItS.PinoyLoanManager.ApiClient/         created (marker only)
├── ExItS.PinoyLoanManager.Web/               created (identity shell)
├── ExItS.PinoyLoanManager.Maui/              deferred (later field/mobile phase)
└── ExItS.PinoyLoanManager.LocalStore/        deferred until offline is authorized
```

Tests:

- `tests/ExItS.PinoyLoanManager.UnitTests/` — assembly-load smoke tests
- `tests/ExItS.ArchitectureTests/PinoyLoanManagerArchitectureTests.cs` — isolation guards

All created projects are registered in `ExItS.slnx` under `/src/Products/PinoyLoanManager/`.

This matches Product Foundation section 9. **No Product Foundation document was modified in PLM-01.**

---

## Layering

| Project | Responsibility | References |
|---|---|---|
| **Domain** | Persistence-independent domain (marker only in PLM-01) | none |
| **Application** | Future use cases/contracts | Domain |
| **Infrastructure** | Future persistence/integrations | Application, Domain |
| **Api** | HTTP host | Application, Infrastructure |
| **ApiClient** | Future typed HTTP client | Application |
| **Web** | Organization operational Blazor shell | ApiClient, Application, DesignSystem |

UI projects must not reference Infrastructure, EF Core, or Npgsql. Domain remains persistence-independent.

**No project may reference PinoyBusinessPOS.** No PLM project may reference Platform Infrastructure.

Web / MAUI component-sharing remains **OPEN** (PLM-D-00-09). MAUI is not created in PLM-01 so that Android SDK / MAUI workload is not required for this phase.

---

## Explicit non-goals (still true)

- Database, DbContext, EF configuration, migrations, connection strings, secrets
- Platform catalog registration of `pinoy-loan-manager`
- Borrower/Loan/authorization/business screens
- Copying POS domain, use cases, roles, money, or migrations
