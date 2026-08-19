# Pinoy Loan Manager — Source and Project Layout

**Status:** PLM-01 scaffold; Gates B–D0 React Client + PWA + `/platform-api` transport
**Implementation present:** Product shell + React Client + PWA — no lending domain; no auth UI
**Last updated:** 2026-08-19

Physical layout after **PLM-01**, **PLM-CLIENT-GATE-B**, **PLM-CLIENT-GATE-C**, and **PLM-CLIENT-GATE-D0**. PLM-D-00-03 is **Closed**. PLM-D-00-09 is **Closed / Product Owner Approved**. `ExItS.PinoyLoanManager.Client` is the Browser + PWA host with same-origin `/platform-api` transport. LocalStore remains **intentionally deferred** (not authorized). MAUI is not the preferred future architecture. Capacitor is not started.

Related: [react-pwa-capacitor-client.md](react-pwa-capacitor-client.md), [api-and-contract-boundary.md](api-and-contract-boundary.md), [persistence-and-database-boundary.md](persistence-and-database-boundary.md), [mobile-offline-boundary.md](mobile-offline-boundary.md), [../architecture.md](../architecture.md), [../Reports/PLM-01-product-scaffold-and-isolation.md](../Reports/PLM-01-product-scaffold-and-isolation.md), [../Decisions/PLM-D-00-09-react-pwa-capacitor-client-strategy.md](../Decisions/PLM-D-00-09-react-pwa-capacitor-client-strategy.md).

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
├── ExItS.PinoyLoanManager.Web/               created (identity shell; future host/BFF)
├── ExItS.PinoyLoanManager.Client/            created (Gate B/C React + online-first PWA)
└── ExItS.PinoyLoanManager.LocalStore/        FUTURE ONLY IF AUTHORIZED
```

Do **not** physically create Capacitor Android/iOS projects in Gate C. PWA lives in this Client Vite application.

Tests:

- `tests/ExItS.PinoyLoanManager.UnitTests/` — assembly-load smoke tests
- `tests/ExItS.ArchitectureTests/PinoyLoanManagerArchitectureTests.cs` — isolation guards

All created projects are registered in `ExItS.slnx` under `/src/Products/PinoyLoanManager/`.

This matches Product Foundation section 9 for the scaffolded shell. **No Product Foundation document was modified in PLM-01 or PLM-01A.**

---

## Layering

| Project | Responsibility | References |
|---|---|---|
| **Domain** | Persistence-independent domain (marker only in PLM-01) | none |
| **Application** | Future use cases/contracts | Domain |
| **Infrastructure** | Future persistence/integrations | Application, Domain |
| **Api** | HTTP host; Loan operational authority | Application, Infrastructure |
| **ApiClient** | Future typed HTTP client | Application |
| **Web** | Future ASP.NET Core browser host / BFF / reverse-proxy / static hosting for the React Client. Current PLM-01 identity shell is scaffold evidence only. Must not own authoritative loan calculations, duplicated authorization, or a second Blazor lending UI. | ApiClient, Application, DesignSystem (current scaffold) |
| **Client** | React + TypeScript presentation (Browser now; PWA/Capacitor later). Gate B scaffold only. Must not own authoritative financial or grant rules. | none (no PLM .NET refs) |

UI projects must not reference Infrastructure, EF Core, or Npgsql. Domain remains persistence-independent.

**No project may reference PinoyBusinessPOS.** No PLM project may reference Platform Infrastructure.

Do not copy PinoyBusinessPOS React source into PinoyLoanManager.

---

## Explicit non-goals (still true)

- PWA, service worker, Capacitor, or Android projects in this package
- Database, DbContext, EF configuration, migrations, connection strings, secrets
- Platform catalog registration of `pinoy-loan-manager`
- Borrower/Loan/authorization/business screens
- Copying POS domain, use cases, roles, money, React client, or migrations
- Deleting or refactoring `ExItS.PinoyLoanManager.Web` in this package
