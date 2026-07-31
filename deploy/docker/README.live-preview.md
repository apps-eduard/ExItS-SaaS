# ExItS live preview — overview (P14-WP02A)

Personal local preview only. **Not Production.** **Not** packaging (`compose.yaml`). Does **not** start P14-WP03.

## Preferred daily command

From repository root:

```powershell
.\tools\Start-LivePreviewLocal.ps1
```

Full operator guide: [`README.live-preview-local-development.md`](README.live-preview-local-development.md).

## Target shape

```text
Docker
├── Platform PostgreSQL  (host port 15533 → volume exits_live_preview_platform_db_data)
└── POS PostgreSQL       (host port 15534 → volume exits_live_preview_pos_db_data)

Local .NET (dotnet watch)
├── Platform API         http://localhost:8091
├── POS API              http://localhost:8092
└── Platform Admin Web   http://localhost:8090
```

App containers (`platform-api`, `pos-api`, `admin-web`) are optional (`--profile apps`). Daily work uses **Docker databases only**.

## One-time setup

```powershell
cd deploy\docker
Copy-Item .env.live-preview.example .env.live-preview
# Fill REPLACE_* values (DB passwords + LIVE_PREVIEW_SHARED_PASSWORD, min 12 chars).
# Never commit .env.live-preview.
```

## Databases only (without apps)

```powershell
cd deploy\docker
docker compose -f compose.live-preview.yaml --env-file .env.live-preview up -d
```

Default `up` starts **only** `platform-db` and `pos-db`. Do **not** run `down -v`.

## Stop

```powershell
.\tools\Stop-LivePreviewLocal.ps1                 # apps only
.\tools\Stop-LivePreviewLocal.ps1 -StopDatabases  # apps already stopped path + DB stop; volumes kept
```

## Optional: full containerized apps

```powershell
docker compose -f compose.live-preview.yaml --env-file .env.live-preview --profile apps up -d --build
```

Prefer host processes for day-to-day coding.

## Safety

| Do | Do not |
|---|---|
| Keep `exits_live_preview_*_db_data` volumes | `docker compose down -v` |
| Use ports 15533/15534 (DBs) and 8090–8092 (local apps) | Reuse packaging ports 8081/8082/15433/15434 |
| Treat as personal Staging preview | Claim Production-ready or start P14-WP03 |

## Related

- [`README.live-preview-local-development.md`](README.live-preview-local-development.md)
- `tools/Start-LivePreviewLocal.ps1` / `tools/Stop-LivePreviewLocal.ps1`
- [deploy/docker/README.md](README.md)
- [Production deployment architecture](../../docs/engineering/production-deployment-architecture.md)
- [P14-WP02A report](../../docs/reports/P14-WP02A-live-preview-test-users-and-quick-login.md)
