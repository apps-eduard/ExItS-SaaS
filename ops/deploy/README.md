# ExItS Pilot and Deployment operations (P9-WP05)

Controlled **non-production** deployment packaging and pilot process for Platform + PinoyBusinessPOS.

## Important

- This is **not** a Production cutover kit. Authoritative Production direction: `docs/engineering/production-deployment-architecture.md` (P14-WP01). Production readiness: `docs/engineering/production-readiness-audit.md`.
- Do not expose Development/Testing identity headers on StagingPilot/Production.
- Do not commit secrets, dumps, or certificates.

## Layout

| Path | Purpose |
|---|---|
| `Invoke-ExItsDeploy.ps1` | Build/plan/validate/migrate/health/smoke/evidence orchestration |
| `Invoke-ExItsSmoke.ps1` | Health-only or Dev/Testing full smoke |
| `Invoke-ExItsPreDeployBackup.ps1` | Platform + POS backup before migrate |
| `templates/` | Env placeholders + secret provisioning checklist |
| `../../deploy/docker/` | Dockerfiles + `docker-compose.pilot.yml` + nginx |

## Quick start (plan / dry-run)

```powershell
pwsh ops/deploy/Invoke-ExItsDeploy.ps1 -Environment StagingPilot -Action Plan -ConfirmPhrase DEPLOY_PILOT_CONFIRMED
```

## Confirmation phrases

| Environment | Phrase |
|---|---|
| StagingPilot | `DEPLOY_PILOT_CONFIRMED` |
| Production | `DEPLOY_PRODUCTION_CONFIRMED` (still blocked by open release risks) |

## Related docs

- `docs/operations/pilot-and-deployment/`
- `docs/reports/P9-WP05-pilot-and-deployment.md`
- `ops/backup/README.md`
