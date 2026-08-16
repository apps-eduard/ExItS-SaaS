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
| **8090** | Platform Admin (canonical browser sign-in) |
| **8091** | Platform API |
| **8092** | POS API |
| **8093** | Organization Web |
| **8094** | Personal Web |
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
| Android SDK + workloads | POS MAUI Android | Required only for MAUI Android builds |

Target frameworks: managed `net10.0`; POS MAUI host `net10.0-android`.

## PinoyBusinessPOS MAUI — PhysicalDevice (preferred)

Physical-device Local Validation is **preferred** over the Android emulator (emulator is slow/unreliable in this environment). Preserve the existing PhysicalDevice / Tailscale Debug profile.

| Item | Value |
|---|---|
| Package id | `com.exits.pinoybusinesspos` |
| Profile | `-p:PosLocalValidationTarget=PhysicalDevice` |
| Default Tailscale host | `100.120.79.81` (override with `-p:PosLocalValidationPublicHost=...`) |
| Embedded settings | `appsettings.LocalValidation.PhysicalDevice.json` |

```powershell
$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
$env:PATH = "$env:ANDROID_HOME\platform-tools;$env:PATH"

dotnet build "src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/ExItS.PinoyBusinessPOS.Maui.csproj" `
  -c Debug -f net10.0-android `
  -p:PosLocalValidationTarget=PhysicalDevice `
  -p:AndroidSdkDirectory="$env:ANDROID_HOME" `
  -t:Install
```

Phone must reach Platform/POS on the Tailscale/LAN host ports **8091** / **8092**. Keep `AllowedHosts` / cleartext domains aligned with that public host.

## PinoyBusinessPOS MAUI — Emulator

Emulator uses the **same** Tailscale PublicHost as physical devices (`100.120.79.81`). Start Local Validation with `-PublicHost 100.120.79.81`.

| Item | Value |
|---|---|
| Package id | `com.exits.pinoybusinesspos` |
| Profile | `-p:PosLocalValidationTarget=PhysicalDevice` (**default** Debug) |
| API base URLs | `http://100.120.79.81:8091` / `http://100.120.79.81:8092` |
| AVD | Create/use an ExItS-named AVD (for example `ExItS_Pixel_API34`) |

```powershell
$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
$env:PATH = "$env:ANDROID_HOME\platform-tools;$env:ANDROID_HOME\emulator;$env:PATH"

emulator -avd ExItS_Pixel_API34

dotnet build "src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/ExItS.PinoyBusinessPOS.Maui.csproj" `
  -c Debug -f net10.0-android `
  -p:PosLocalValidationTarget=PhysicalDevice `
  -p:AndroidSdkDirectory="$env:ANDROID_HOME" `
  -t:Install
```

Legacy `10.0.2.2` loopback is no longer the default; only use `-p:PosLocalValidationTarget=Emulator` for experiments.

## Secrets

Never place real connection passwords, JWT signing keys, or Compose `.env` values in docs, commits, or chat logs. Use configuration **names** only.
