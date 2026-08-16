# ExItS SaaS

Multi-product SaaS platform for shared identity, subscriptions, and entitlements, with PinoyBusinessPOS as the first retail product.

## Overview

ExItS is an independent multi-product portfolio. Shared commercial and identity capabilities live on the **Platform**. Product-specific operations and data stay inside each product.

| Surface | Responsibility |
|---|---|
| **ExItS Platform** | Identity, organizations, plans, subscriptions, entitlements, Platform Admin, audit |
| **PinoyBusinessPOS** | Retail POS operations (catalog, sales, inventory, credit/utang, offline, mobile/web) |
| **Personal Web** | Personal account experience (utang, business upgrade, linked-customer views) |

Platform and product databases are separate authorities. There are no cross-database foreign keys or joins. Authorization and entitlements are explicit; product operational data never becomes Platform data.

## Current Status

| Area | Status |
|---|---|
| Development | Active |
| Current delivery focus | [Phase 29](docs/phases/phase-29-data-integrity-query-performance-and-database-hardening.md) — Open / Partial Closeout |
| Local Validation (host apps) | Available |
| Full Docker Local Validation | Available |
| Development PostgreSQL backup/restore | Proven |
| Production payment provider | Not enabled |
| Production Backup/Restore Proven | No |
| Production Ready | No |

Detailed status: [docs/portfolio-progress.md](docs/portfolio-progress.md)

## Architecture at a Glance

- **Boundaries:** Platform owns commercial/identity contracts; each product owns its operational domain and database.
- **Data:** Separate Platform and POS PostgreSQL authorities; tenant isolation enforced in application services.
- **Layering:** Domain → Application → Infrastructure → API/UI (no Infrastructure references from UI).
- **Clients:** Platform Admin (Ant Design Blazor), Organization Web, Personal Web, POS API, Android-first .NET MAUI.

Details: [Approved architecture](docs/engineering/approved-architecture-summary.md) · [Architecture](docs/engineering/architecture.md) · [Repository boundaries](docs/engineering/repository-boundaries.md)

## Technology

Verified from repository configuration:

| Area | Stack |
|---|---|
| Runtime | .NET SDK **10.0.302** (`global.json`) |
| Backend | ASP.NET Core, Entity Framework Core, Npgsql / PostgreSQL |
| Web UI | Blazor; Platform Admin uses **Ant Design** Blazor |
| Mobile | .NET MAUI (Android-first) |
| Containers | Docker / Docker Compose |
| Tests | xUnit, Testcontainers (PostgreSQL) |

Solution entry point: [`ExItS.slnx`](ExItS.slnx)

## Repository Structure

```text
ExItS-SaaS/
├── ExItS.slnx
├── src/
│   ├── Platform/          # Platform domain, API, Admin, Personal Web
│   ├── Products/          # PinoyBusinessPOS (API, Web, MAUI, …)
│   └── Shared/            # DesignSystem, BackupRestore, Deployment helpers
├── tests/
├── tools/                 # Local Validation launchers
├── deploy/docker/         # Compose, Dockerfiles, Local Validation docs
├── ops/                   # Backup and deploy operator scripts
└── docs/                  # Architecture, phases, reports, runbooks
```

## Local Development

**Preferred daily workflow** — Docker for PostgreSQL + Mailpit; application hosts under `dotnet watch`:

```powershell
.\tools\Start-LocalValidation.ps1 -PublicHost 100.120.79.81
```

Replace the host with your Tailscale or LAN address as needed. Omit `-PublicHost` for localhost-only.

Operator cheat sheets (Local Validation only, not Production):

- [Start / stop](Start-LocalValidation.md)
- [Reset users](Reset-LocalValidation.md)
- [Reset products / templates](Reset-Products-And-Business-Templates.md)
- [Workflow details](deploy/docker/README.local-validation-workflow.md)

## Full Docker Validation

Deployment-like validation with application containers (slower than host `dotnet watch`). Database volumes are preserved.

```powershell
# Reuse existing images
.\tools\Start-DockerLocalValidation.ps1 -PublicHost 100.120.79.81

# Rebuild application images
.\tools\Start-DockerLocalValidation.ps1 -PublicHost 100.120.79.81 -Build

# Rebuild without cache, then start
.\tools\Start-DockerLocalValidation.ps1 -PublicHost 100.120.79.81 -CleanBuild
```

Stop apps (keep infrastructure):

```powershell
.\tools\Stop-DockerLocalValidation.ps1
```

Do **not** use `docker compose down -v` for normal operation — that destroys database volumes.

## Build and Test

From the repository root:

```powershell
dotnet restore ExItS.slnx
dotnet build ExItS.slnx -c Release
dotnet test ExItS.slnx -c Release
```

## Documentation

| Topic | Link |
|---|---|
| Documentation home | [docs/index.md](docs/index.md) |
| Portfolio progress | [docs/portfolio-progress.md](docs/portfolio-progress.md) |
| Phase roadmap | [docs/phases/README.md](docs/phases/README.md) |
| Work-package reports | [docs/reports/README.md](docs/reports/README.md) |
| Architecture | [docs/engineering/architecture.md](docs/engineering/architecture.md) |
| Security | [docs/engineering/security.md](docs/engineering/security.md) |
| Testing strategy | [docs/engineering/testing-strategy.md](docs/engineering/testing-strategy.md) |
| Local Validation | [deploy/docker/README.local-validation-workflow.md](deploy/docker/README.local-validation-workflow.md) |
| Production readiness | [docs/engineering/production-readiness-audit.md](docs/engineering/production-readiness-audit.md) |
| Contributing | [CONTRIBUTING.md](CONTRIBUTING.md) |
| Security policy | [SECURITY.md](SECURITY.md) |

## Production Readiness

ExItS is **not** claimed as Production-ready. Production payment providers are not enabled, and Production backup/restore is not proven.

Authoritative assessment: [Production readiness audit](docs/engineering/production-readiness-audit.md)
