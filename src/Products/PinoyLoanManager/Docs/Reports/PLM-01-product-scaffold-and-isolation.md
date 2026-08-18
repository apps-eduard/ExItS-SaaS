# Pinoy Loan Manager — PLM-01 Product Scaffold and Isolation

**Status:** Scaffold complete (no lending domain)
**Last updated:** 2026-08-19
**Branch:** `feat/plm-01-scaffold`

## Delivered

Isolated product **shell** for Pinoy Loan Manager. No borrower, loan, authorization, persistence, or Platform catalog work.

### Projects created

| Project | Role |
|---|---|
| `ExItS.PinoyLoanManager.Domain` | Persistence-independent domain assembly (marker only) |
| `ExItS.PinoyLoanManager.Application` | Use-case assembly; references Domain |
| `ExItS.PinoyLoanManager.Infrastructure` | Future persistence/integrations; **no** EF/Npgsql/DbContext |
| `ExItS.PinoyLoanManager.Api` | Minimal ASP.NET Core host; `/health` only |
| `ExItS.PinoyLoanManager.ApiClient` | Typed client boundary (marker only) |
| `ExItS.PinoyLoanManager.Web` | Blazor Web organization shell (identity page only) |

Registered in `ExItS.slnx` under `/src/Products/PinoyLoanManager/`.

### Test projects

| Project | Role |
|---|---|
| `tests/ExItS.PinoyLoanManager.UnitTests` | Assembly-load smoke tests |
| `tests/ExItS.ArchitectureTests` (extended) | `PinoyLoanManagerArchitectureTests` — isolation/reference guards |

### Intentionally deferred

- `ExItS.PinoyLoanManager.Maui` — field/mobile belongs to a later phase; avoid Android SDK / MAUI workload in PLM-01
- `ExItS.PinoyLoanManager.LocalStore` — not justified until offline is authorized

MAUI remains in the product **plan**. Deferral is not removal.

## Layering

```text
Domain
  ↑
Application
  ↑
Infrastructure

Api composes Application + Infrastructure
ApiClient references Application only
Web references ApiClient + Application + DesignSystem
Web does not reference Infrastructure
```

No PLM project references PinoyBusinessPOS or Platform Infrastructure.

## Persistence

Not created: database, DbContext, EF configuration, migrations, connection strings, secrets.

Proposed name `ExItS_PinoyLoanManager` remains **open** (PLM-D-00-02).

## Platform

No catalog registration of `pinoy-loan-manager`. No subscriptions, entitlements, Personal linking, or usage billing.

## Validation

| Check | Result |
|---|---|
| `dotnet restore ExItS.slnx` | Pass |
| `dotnet build ExItS.slnx -c Release --no-restore` | **Blocked by environment** — POS MAUI `XA5300` Android SDK directory not found (`ExItS.PinoyBusinessPOS.Maui`, `net10.0-android`) |
| `dotnet test ExItS.slnx -c Release --no-build` | **Blocked by environment** — full solution Release build did not succeed |
| PLM Domain/Application/Infrastructure/Api/ApiClient/Web Release build | Pass |
| `ExItS.PinoyLoanManager.UnitTests` Release | Pass (1 passed) |
| `PinoyLoanManagerArchitectureTests` Release | Pass (4 passed) |

Git hashes for this work package are the `feat/plm-01-scaffold` commit (`chore(plm): scaffold isolated product projects`).

Known environment: Android SDK / XA5300 on existing POS MAUI. Not Device Verified. Not Production Ready.

## Explicit non-goals (this WP)

- Lending/borrower/authorization implementation
- Fake authentication
- WeatherForecast / Counter / demo pages
- Starting PLM-02
