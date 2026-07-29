# HealthCare Runtime and Repository Baseline

[Dashboard](../portfolio-progress.md) | [Repository boundaries](../engineering/repository-boundaries.md) | [Dev environment](../engineering/development-environment.md) | [P0-WP02 report](../reports/P0-WP02-baseline-runtime-map.md)

**Work package:** P0-WP02  
**Date:** 2026-07-29  
**HealthCare freeze:** All paths under `HealthCare/` treated as read-only; no application files modified.

---

## 1. Repository topology

```text
ExItS-SaaS/                         # root Git (portfolio docs)
├── .git/
├── .gitignore                      # ignores HealthCare/ + secrets/build outputs
├── docs/                           # tracked
├── README.md / FILE-MANIFEST.md    # tracked
└── HealthCare/                     # nested independent Git; ignored by root
    ├── .git/                       # remote: https://github.com/apps-eduard/HealthCare.git
    ├── HealthCare.sln
    ├── src/ tests/ deploy/ Docs/ scripts/ tools/
    └── …
```

This is a **temporary Phase 0 model** until an approved repository-integration decision.

## 2. Git boundary

| Repo | Responsibility | Current state (2026-07-29) |
|---|---|---|
| Root `ExItS-SaaS` | Portfolio documentation and safety files | Branch `main`; remote `origin` → `https://github.com/apps-eduard/ExItS-SaaS.git`; remote is **empty** (`isEmpty: true`); local tracks `origin/main` which is **gone** |
| Nested `HealthCare/` | Product MVP source of truth | Branch `main`; ahead of its own `origin/main` by 16; local dirty PatientWeb files (pre-existing; not touched) |

**Safety verification:**

```powershell
git check-ignore -v HealthCare/
git ls-files HealthCare
git diff -- HealthCare/
```

Expected: ignore rule hits `HealthCare/`; no tracked HealthCare paths; empty diff from root against HealthCare.

## 3. Toolchain versions (this machine)

| Tool | Version |
|---|---|
| Git | 2.55.0.windows.3 |
| .NET SDK | 10.0.302 |
| ASP.NET Core runtime | 10.0.10 |
| .NET runtime | 10.0.10 |
| Docker | 29.6.2 |
| Docker Compose | v5.3.1 |
| Node / npm | v24.18.0 / 11.16.0 (not required by HealthCare) |
| MAUI workload | `maui-android` 10.0.0 installed |
| `maui-windows` | Not installed |
| JDK | `JAVA_HOME` → Microsoft JDK 17 |
| Android SDK | Present at `%LOCALAPPDATA%\Android\Sdk`; **`ANDROID_HOME` / `ANDROID_SDK_ROOT` unset** |

No `global.json` or Central Package Management (`Directory.Packages.props`) in HealthCare or root.

## 4. Required prerequisites

| Prerequisite | Classification |
|---|---|
| .NET SDK 10.x | Required for API and web development |
| Docker Desktop + Compose | Required for local PostgreSQL (dev) and for Integration/E2E |
| PostgreSQL 18 via `deploy/docker/dev` | Required for API and web development (runtime) |
| `maui-android` workload | Required only for MAUI Android host build |
| Android SDK + `ANDROID_HOME` | Required only for MAUI Android |
| JDK 17+ | Required only for MAUI Android |
| `maui-windows` | Required only for Windows MAUI (not used by current TFM `net10.0-android`) |
| Playwright + E2E Compose | Required only for EndToEndTests |
| Testcontainers / Docker | Required only for IntegrationTests |
| Node/npm | Optional / unused by HealthCare |

## 5. Application hosts

| Service | Project | Purpose | Protocol | Default Port | Dependencies |
|---|---|---|---|---:|---|
| HealthCare API | `HealthCare.Api` | JWT API, Identity, Hangfire, health | HTTP / HTTPS | **5080** / **7080** | PostgreSQL |
| Staff Web | `HealthCare.Web` | Blazor Server staff BFF + AntDesign | HTTP / HTTPS | **5018** / **7021** | API |
| Patient Web | `HealthCare.PatientWeb` | Blazor Server patient BFF | HTTP | **5020** | API |
| MAUI client | `HealthCare.Mobile` | Patient/Doctor hybrid app | HTTPS/HTTP to API | n/a (device) | API (`Mobile:ApiBaseUrl`, emulator default `http://10.0.2.2:5080`) |
| PostgreSQL (dev) | `deploy/docker/dev` | Persistent local DB | TCP | **5432** (host) | Docker |
| PostgreSQL (legacy root compose) | `docker-compose.yml` | Optional legacy helper | TCP | 5432 | Docker |
| PostgreSQL (E2E) | `deploy/docker/e2e` | Disposable E2E DB | internal | not published by default | Docker |
| Hangfire | hosted in API | Background jobs (reminders/summaries) | HTTP dashboard | same as API `/hangfire` | PostgreSQL |
| Email | `IAccountEmailSender` | Account mail | n/a | n/a | Dev capture only (no production SMTP in MVP) |
| File storage | — | Not a separate service in MVP | — | — | — |
| Reverse proxy | optional k8s e2e | Lab only | — | — | `deploy/k8s/e2e` |

Sources: `Properties/launchSettings.json` for each host; Compose files under `deploy/docker/`.

## 6. Ports and URLs

| URL | Role |
|---|---|
| `http://localhost:5080` | API (http profile) |
| `https://localhost:7080` | API HTTPS |
| `http://localhost:5080/swagger` | Swagger UI (Development) |
| `http://localhost:5080/openapi/v1.json` | OpenAPI document (Development) |
| `http://localhost:5080/health` | Liveness |
| `http://localhost:5080/health/ready` | Readiness (DB) |
| `http://localhost:5080/hangfire` | Hangfire dashboard (Development when enabled) |
| `http://localhost:5018` | Staff Web |
| `http://localhost:5020` | Patient Web |

**Auth structure (names only):**

- JWT `Issuer` / `Audience`: configured as `HealthCare` / `HealthCare`
- Config keys: `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey`, `Jwt__AccessTokenDurationMinutes`, `Jwt__RefreshTokenDurationDays`
- Staff BFF cookie (Development): `HealthCare.Staff.Auth` (`Bff:CookieName`); production-oriented `__Host-HealthCare.Staff` when HTTPS required
- Patient BFF cookie: `HealthCare.Patient.Auth`
- API tokens stored server-side (distributed cache); browser never receives API tokens
- **CORS:** not configured on API (no `AddCors`); browsers talk to BFF hosts, which call the API server-side

## 7. Startup order (local Development)

1. Start PostgreSQL: `.\scripts\dev\start-local-dev.ps1` (creates ignored `.env`, user secrets, migrations)
2. Run API: `dotnet run --project src/HealthCare.Api --launch-profile http`
3. Run Staff Web and/or Patient Web
4. Optional: MAUI Android against emulator API base URL

## 8. Database topology

| Item | Value |
|---|---|
| Provider | PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL` |
| DbContext | `HealthCareDbContext` (`IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`) |
| Migrations | `HealthCare/src/HealthCare.Infrastructure/Persistence/Migrations/` |
| Dev DB name | `healthcare_db_dev` (compose default) |
| E2E DB name | `health_care_e2e` |
| Legacy compose DB | `healthcare` |
| Connection config | `ConnectionStrings:DefaultConnection` / `ConnectionStrings__DefaultConnection` (prefer user secrets; password not in tracked appsettings) |
| Seed | Development seeders on API startup; E2E `--seed` via DbMigrate |
| Hangfire | PostgreSQL storage (same connection); schema managed by Hangfire |

| Area | Current Owner | Future Candidate Owner | Migration Risk |
|---|---|---|---|
| AspNet* Identity, RefreshTokens | HealthCare DB | ExItS Platform | High — FK and issuer/session impact |
| Organizations (+ limits/profile) | HealthCare DB | ExItS Platform | High |
| SecurityEvents / OrganizationAuditEvents | HealthCare DB | Platform (+ HC subset) | Medium |
| Clinics, StaffMembers, Patients, Appointments, MedicalNotes | HealthCare DB | ExItS_HealthCare | Keep in product |
| Hangfire tables | HealthCare DB | HealthCare (or shared infra later) | Medium |

**Do not** apply/edit migrations in Phase 0 assessment WPs.

## 9. Configuration names (no values)

| Kind | Names / files |
|---|---|
| Tracked templates | `appsettings.json`, `appsettings.Development.json`, `appsettings.E2E.json`, `appsettings.Production.json`, `.env.example`, `deploy/docker/*/ .env.example` |
| Local-only secrets | `deploy/docker/dev/.env`, `deploy/docker/e2e/.env`, API user secrets |
| Env overrides | `ConnectionStrings__DefaultConnection`, `Jwt__*`, `DevelopmentSeed__*`, `Hangfire__*`, `Api__BaseUrl`, `Bff__*`, `Portal__*`, `Mobile__*` |
| Should be ignored | `**/.env`, `bin/`, `obj/` (root `.gitignore` + HC `.gitignore`) |

## 10. Secret-handling approach

- Prefer `scripts/dev/start-local-dev.ps1` → Compose `.env` (gitignored) + `dotnet user-secrets` for API connection string.
- Development JWT/signing and seed credentials appear as lab placeholders in Development configuration; treat as **non-production**.
- Never commit real passwords, signing keys, or `.env` files to the root repository.
- Root `.gitignore` excludes `HealthCare/` entirely during Phase 0.

## 11. Build commands

```powershell
cd HealthCare
dotnet restore HealthCare.sln
# Non-MAUI baseline (authoritative for Windows without Android env wiring):
dotnet build src/HealthCare.Api/HealthCare.Api.csproj -c Release
dotnet build src/HealthCare.Web/HealthCare.Web.csproj -c Release
dotnet build src/HealthCare.PatientWeb/HealthCare.PatientWeb.csproj -c Release
# Full solution (expected XA5300 without ANDROID_HOME):
dotnet build HealthCare.sln -c Release
```

**P0-WP02 results:** restore exit 0; non-MAUI builds OK; full solution exit 1 (`XA5300`); Mobile with explicit `AndroidSdkDirectory` still failed (`XA0035` RID `win-x64`) — environmental, not application regression.

## 12. Test commands

```powershell
dotnet test tests/HealthCare.UnitTests/HealthCare.UnitTests.csproj --no-build -c Release
dotnet test tests/HealthCare.ArchitectureTests/HealthCare.ArchitectureTests.csproj --no-build -c Release
dotnet test tests/HealthCare.Web.Tests/HealthCare.Web.Tests.csproj --no-build -c Release
dotnet test tests/HealthCare.PatientWeb.Tests/HealthCare.PatientWeb.Tests.csproj --no-build -c Release
dotnet test tests/HealthCare.Mobile.Tests/HealthCare.Mobile.Tests.csproj --no-build -c Release
```

**P0-WP02 totals:** Passed **1102** / Failed **0** / Skipped **0**.

Not run: IntegrationTests (Testcontainers), EndToEndTests (Playwright + E2E Compose) — per HealthCare README Windows guidance.

## 13. Known environment limitations

- Root remote empty; first push required to create `origin/main` (user action; not performed in this WP).
- `ANDROID_HOME` unset despite SDK folder existing → full solution Mobile build fails XA5300.
- `maui-windows` not installed; Mobile TFM is Android-only.
- Nested HealthCare working tree has pre-existing dirty PatientWeb files.
- Node present but unused.

## 14. Future Platform extraction implications

- Keep ignoring or deliberately importing HealthCare only after an approved strategy (see [repository-boundaries.md](../engineering/repository-boundaries.md)).
- Split DB ownership later (`ExItS_Platform` / `ExItS_HealthCare`); do not move Identity without session/issuer plan.
- New Platform Admin and POS use a **shared native** UI foundation (not AntDesign). HealthCare Staff Web may keep its BFF + Ant patterns in place; new apps must not inherit AntDesign or HC ports.
- Establish CI that runs non-MAUI build + Windows-safe tests before extraction.
