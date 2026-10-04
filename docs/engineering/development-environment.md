# Development Environment Baseline

[Home](../index.md) | [Repository boundaries](repository-boundaries.md) | [Local Validation workflow](../../deploy/docker/README.local-validation-workflow.md) | [P2-WP01 report](../reports/P2-WP01-extraction-baseline-and-safety.md)

ExItS-SaaS is an independent multi-product portfolio. Use ExItS Platform + PinoyBusinessPOS Local Validation only.

## ExItS root Platform

| Component | Value |
|---|---|
| SDK pin | `global.json` → **10.0.302** (`rollForward`: `latestFeature`) |
| Solution | `ExItS.slnx` |
| Target framework | `net10.0` via `Directory.Build.props` |
| Central packages | `Directory.Packages.props` (CPM) |

```powershell
# From ExItS-SaaS root
dotnet restore ExItS.slnx
dotnet build ExItS.slnx -c Release
dotnet test ExItS.slnx -c Release --no-build
```

## Local Validation (preferred for POS + Platform together)

Use the Local Validation stack (not a nested product tree):

| Port | Service |
|---|---|
| **8095** | Platform Admin (React; canonical browser sign-in) |
| **8091** | Platform API |
| **8092** | POS API |
| **5177** | React Personal, Organization, and POS |
| **15533** | Platform PostgreSQL (Docker; do not expose) |
| **15534** | POS PostgreSQL (Docker; do not expose) |
| **8025** | Mailpit UI |

Local ports are **internal**. Production public entry is HTTPS **:443** via reverse proxy (see [ADR-022](../decisions/ADR-022-separated-antdesign-web-hosts-and-unified-auth.md)).

```powershell
# From ExItS-SaaS root (see deploy/docker/README.local-validation-workflow.md)
.\tools\Start-LocalValidation.ps1
```

Health checks: `GET http://127.0.0.1:8091/health` and `GET http://127.0.0.1:8092/health`.

## Platform / POS PostgreSQL (Local Validation)

| Item | Value |
|---|---|
| Platform DB container | `exits-local-validation-platform-db` (host **15533**) |
| POS DB container | `exits-local-validation-pos-db` (host **15534**) |
| Image | `postgres:16` (Local Validation compose) |
| Auto-migrate at API startup | Follow Local Validation / product docs — do not invent Production migrate-at-start |

Prefer `dotnet user-secrets` for non-local credentials. Integration tests use Testcontainers (Docker required).

## Required SDK and runtimes

| Component | Required for | Notes |
|---|---|---|
| .NET SDK **10.x** | Platform + POS | Verified `10.0.302` |
| Docker Desktop + Compose | Local Validation DBs / packaging | Required for Local Validation |
| Git 2.x | All work | |
| Node.js | React clients | Platform Admin Web and PinoyBusinessPOS React |

Target framework: `net10.0`. .NET MAUI and the Android workload are retired. Mobile delivery is the React PWA, with Capacitor when native packaging is added. See [ADR-024](../decisions/ADR-024-react-only-client-standard-and-legacy-ui-retirement.md).

## Secrets

Never place real connection passwords, JWT signing keys, or Compose `.env` values in docs, commits, or chat logs. Use configuration **names** only.
