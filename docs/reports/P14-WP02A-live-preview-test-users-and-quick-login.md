# P14-WP02A — Live Preview Test Users and Quick Login

> **Historical / superseded.** This report documents the former Live Preview stack and quick-login path.
> Current operator runtime is **Local Validation** (`deploy/docker/compose.local-validation.yaml`, `tools/Start-LocalValidation.ps1`).
> See [P16-WP11 Local Validation replaces Live Preview](P16-WP11-local-validation-replaces-live-preview.md).
> Filenames are retained for history; do not treat this package as active guidance.

Phase marker: `P14-WP02A-live-preview-test-users-and-quick-login`

Package: **P14-WP02A — Reliable local Live Preview launcher (one command)**
Prior tip: `d2513bf4b778e6366c636210961b1f988fc768fc`
Feature tip: `ffe12b1ffe73f8e202079c3ed76b7c1f39bd6e9d`

## Status

**Complete.** One-command local Live Preview: Docker PostgreSQL only + ordered local `dotnet watch` for Platform API, POS API, and Admin. Persistent Admin DataProtection keys under `%LOCALAPPDATA%\ExItS\LivePreview\DataProtectionKeys`. Volumes preserved. **Not Production.** **P14-WP03 not started.**

## Operator command

```powershell
.\tools\Start-LivePreviewLocal.ps1
```

```powershell
.\tools\Stop-LivePreviewLocal.ps1
.\tools\Stop-LivePreviewLocal.ps1 -StopDatabases   # volumes kept
```

Docs: `deploy/docker/README.live-preview-local-development.md`

## Delivered

| Artifact | Role |
|---|---|
| `tools/Start-LivePreviewLocal.ps1` | Docker DBs → wait healthy → API → POS → Admin; port/health checks |
| `tools/Stop-LivePreviewLocal.ps1` | Stop repo-scoped apps; optional DB stop without `-v` |
| Persistent DP keys | `%LOCALAPPDATA%\ExItS\LivePreview\DataProtectionKeys` (live-preview only) |
| Compose profile `apps` | Optional containerized apps; default `up` remains DBs only |

## Explicit exclusions

- P14-WP03 TLS/proxy
- Volume reset / `down -v`
- Weakening antiforgery
- Phase 15

## Validation

| Check | Result |
|---|---|
| Architecture tests (scripts/docs) | Pass |
| Full Release tests | **1268 passed / 0 failed / 0 skipped** |

## Exact next

**P14-WP03** when authorized.
