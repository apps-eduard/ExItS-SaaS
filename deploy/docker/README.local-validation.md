# ExItS Local Validation — overview (P16-WP11)

Production-equivalent **local deployment** for validation. Same application code as Production; only local configuration differs (ports, secrets, seed flag, TLS off for host-run apps).

**Not** packaging (`compose.yaml`). Does **not** close Phase 16 or start Phase 17. Production topology template remains `compose.production.yaml`.

## Preferred daily command

From repository root:

``powershell
.\tools\Start-LocalValidation.ps1
``

Full operator guide: [`README.local-validation-workflow.md`](README.local-validation-workflow.md).

## Target shape

``text
Docker
├── Platform PostgreSQL  (host port 15533)
└── POS PostgreSQL       (host port 15534)

Local .NET (dotnet watch)
├── Platform API         http://localhost:8091
├── POS API              http://localhost:8092
└── Platform Admin Web   http://localhost:8090
``

## One-time setup

``powershell
cd deploy\docker
Copy-Item .env.local-validation.example .env.local-validation
# Fill REPLACE_* values. Never commit .env.local-validation.
``

Sign in on Admin via the Local Validation identity dropdown (server-side normal `POST /auth/login`) or manual credentials.

Organizations (2 users each):
- **Sampaguita Neighborhood Store** (`sampaguita-store`) — Rafael Torres (Owner), Maria Santos (Cashier)
- **Mabuhay Mini Mart** (`mabuhay-mini-mart`) — Carlo Reyes (Owner), Ana Cruz (Member, no POS)

Also: Platform (Olivia Mendoza, Daniel Garcia), Personal (Luis Navarro, Sofia Ramos). Password from `LOCAL_VALIDATION_SHARED_PASSWORD` env only (never commit; never exposed in the browser).

Local Validation does **not** seed `phase16-seed-org` / Phase16 test users. If that org still appears, it is leftover from an older seed on a preserved volume — recreate Platform DB volumes (or wipe the Platform database) and restart `.\tools\Start-LocalValidation.ps1`.

## Migration from Live Preview

- Rename `.env.live-preview` → `.env.local-validation` and replace `LIVE_PREVIEW_` with `LOCAL_VALIDATION_`.
- Docker project/volumes are now `exits-local-validation*` (prior `exits-live-preview*` volumes are not attached automatically).