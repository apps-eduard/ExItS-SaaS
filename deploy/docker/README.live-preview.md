# ExItS live preview — local development workflow (P14-WP02A)

Personal local preview only. **Not Production.** **Not** packaging (`compose.yaml`). Does **not** start P14-WP03.

## Target shape

```text
Docker
├── Platform PostgreSQL  (host port 15533 → volume exits_live_preview_platform_db_data)
└── POS PostgreSQL       (host port 15534 → volume exits_live_preview_pos_db_data)

Local .NET processes
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

## Daily: databases only

```powershell
cd deploy\docker
docker compose -f compose.live-preview.yaml --env-file .env.live-preview up -d
docker compose -f compose.live-preview.yaml --env-file .env.live-preview ps
```

Default `up` starts **only** `platform-db` and `pos-db`. Named volumes are reused; do **not** run `down -v`.

Stop DBs without deleting volumes:

```powershell
docker compose -f compose.live-preview.yaml --env-file .env.live-preview stop
# or: down   (without -v)
```

## Daily: local APIs + Admin

With DBs healthy, either:

```powershell
cd deploy\docker
.\Start-LivePreviewLocal.ps1
```

or manually (three terminals), using the `LivePreview` launch profile after exporting connection settings from `.env.live-preview` (the script does this):

| Process | Profile | URL |
|---|---|---|
| Platform API | `LivePreview` | http://localhost:8091 |
| POS API | `LivePreview` | http://localhost:8092 |
| Platform Admin | `LivePreview` | http://localhost:8090 |

Open **http://localhost:8090/** → `/admin/login` → **Live Preview Test User**.

`ASPNETCORE_ENVIRONMENT=Staging` + `LivePreview:Enabled=true`. Seed/migrate runs only when LivePreview is enabled (non-Production).

## Optional: full containerized apps

```powershell
docker compose -f compose.live-preview.yaml --env-file .env.live-preview --profile apps up -d --build
```

When apps run in Docker, Admin/POS call Platform via the compose network (`LIVE_PREVIEW_PLATFORM_API_INTERNAL_URL`). Prefer host processes for day-to-day coding.

## Safety

| Do | Do not |
|---|---|
| Keep `exits_live_preview_*_db_data` volumes | `docker compose down -v` / volume prune for these DBs |
| Use ports 15533/15534 (DBs) and 8090–8092 (local apps) | Reuse packaging ports 8081/8082/15433/15434 |
| Treat stack as personal Staging preview | Claim Production-ready or start P14-WP03 here |

## Related

- `compose.live-preview.yaml` — Compose project `exits-live-preview`
- `Start-LivePreviewLocal.ps1` / `Stop-LivePreviewLocal.ps1`
- [deploy/docker/README.md](README.md)
- [Production deployment architecture](../../docs/engineering/production-deployment-architecture.md)
- [P14-WP02A report](../../docs/reports/P14-WP02A-live-preview-test-users-and-quick-login.md)
