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

Sign in through normal Admin credential login using the approved Local Validation identities (Olivia Mendoza, Rafael Torres, Maria Santos, Carlo Reyes, Ana Cruz, Daniel Garcia, Luis Navarro, Sofia Ramos) via normal Platform credential login; password from `LocalValidation:SharedPassword` / `LOCAL_VALIDATION_SHARED_PASSWORD` env (never commit the secret). No quick-login UI.

## Migration from Live Preview

- Rename `.env.live-preview` → `.env.local-validation` and replace `LIVE_PREVIEW_` with `LOCAL_VALIDATION_`.
- Docker project/volumes are now `exits-local-validation*` (prior `exits-live-preview*` volumes are not attached automatically).