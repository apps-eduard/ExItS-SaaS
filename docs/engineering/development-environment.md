# Development Environment Baseline

[Home](../index.md) | [Runtime baseline](../reuse/healthcare-runtime-baseline.md) | [Repository boundaries](repository-boundaries.md) | [P2-WP01 report](../reports/P2-WP01-extraction-baseline-and-safety.md)

Verified on the P0-WP02 assessment machine (2026-07-29) and extended for root Platform foundation in **P2-WP01**.

## ExItS root Platform (P2-WP01+)

| Component | Value |
|---|---|
| SDK pin | `global.json` → **10.0.302** (`rollForward`: `latestFeature`) |
| Solution | `ExItS.slnx` (SDK 10 solution format; preferred over `.sln`) |
| Target framework | `net10.0` via `Directory.Build.props` |
| Central packages | `Directory.Packages.props` (CPM) |
| API HTTP | `http://localhost:5288` (launch profile `http`) |

```powershell
# From ExItS-SaaS root
dotnet restore ExItS.slnx
dotnet build ExItS.slnx -c Release
dotnet test ExItS.slnx -c Release --no-build
dotnet run --project src/Platform/ExItS.Platform.Api/ExItS.Platform.Api.csproj -c Release --urls http://127.0.0.1:5288
```

Health checks: `GET /` and `GET /health`. No database required for foundation API.

## Required SDK and runtimes

| Component | Required for | Notes |
|---|---|---|
| .NET SDK **10.x** | API + Web | Verified `10.0.302` |
| `Microsoft.AspNetCore.App` 10.x | API + Web | Verified `10.0.10` |
| `Microsoft.NETCore.App` 10.x | All managed hosts | Verified `10.0.10` |
| Docker Desktop + Compose | Local PostgreSQL, Integration, E2E | Verified Docker `29.6.2`, Compose `v5.3.1` |
| Git 2.x | All work | Verified `2.55.0.windows.3` |

No HealthCare `global.json`. Target framework `net10.0` (Mobile host `net10.0-android`).

## PostgreSQL

- Preferred: `HealthCare/deploy/docker/dev` → database `healthcare_db_dev`, container `healthcare-db-dev`, Postgres **18**.
- Start: from `HealthCare/`, run `.\scripts\dev\start-local-dev.ps1`.
- Connection password lives in ignored Compose `.env` and API user secrets — **not** in documentation.

## MAUI / Android / Windows

| Item | Status on assessment machine | Classification |
|---|---|---|
| Workload `maui-android` | Installed | Required only for MAUI Android |
| Workload `maui-windows` | Missing | Required only for Windows MAUI (current project is Android TFM) |
| Android SDK folder | Present under `%LOCALAPPDATA%\Android\Sdk` | Required only for MAUI Android |
| `ANDROID_HOME` / `ANDROID_SDK_ROOT` | Unset | Missing wiring → `XA5300` on full solution build |
| JDK | Microsoft JDK 17 via `JAVA_HOME` | Required only for MAUI Android |

Do not install large workloads during portfolio WPs unless explicitly approved.

## Node / npm

Installed on the assessment machine but **not required** by HealthCare (no npm frontend toolchain).

## Restore / build / test (HealthCare cwd)

```powershell
dotnet restore HealthCare.sln
dotnet build src/HealthCare.Api/HealthCare.Api.csproj -c Release
dotnet build src/HealthCare.Web/HealthCare.Web.csproj -c Release
dotnet build src/HealthCare.PatientWeb/HealthCare.PatientWeb.csproj -c Release
dotnet test tests/HealthCare.UnitTests/HealthCare.UnitTests.csproj -c Release
dotnet test tests/HealthCare.ArchitectureTests/HealthCare.ArchitectureTests.csproj -c Release
dotnet test tests/HealthCare.Web.Tests/HealthCare.Web.Tests.csproj -c Release
dotnet test tests/HealthCare.PatientWeb.Tests/HealthCare.PatientWeb.Tests.csproj -c Release
dotnet test tests/HealthCare.Mobile.Tests/HealthCare.Mobile.Tests.csproj -c Release
```

## Known unsupported / deferred commands in this environment

| Command / suite | Why deferred |
|---|---|
| `dotnet build HealthCare.sln` (includes Mobile) | Fails `XA5300` without Android SDK env wiring |
| `HealthCare.IntegrationTests` | Needs Docker Testcontainers; HealthCare README: not the Windows baseline |
| `HealthCare.EndToEndTests` | Needs Playwright + `deploy/docker/e2e` Compose |
| First `git push -u origin main` for ExITS root | Remote repo exists but is empty; requires explicit user authorization to publish |

## Secrets

Never place real connection passwords, JWT signing keys, or Compose `.env` values in docs, commits, or chat logs. Use configuration **names** only (for example `ConnectionStrings__DefaultConnection`, `Jwt__SigningKey`).
