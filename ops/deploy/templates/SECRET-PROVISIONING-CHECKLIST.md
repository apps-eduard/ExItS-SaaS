# Operator checklist — secret provisioning (no real credentials in this file)
# Phase: P9-WP05-pilot-and-deployment

1. Identify target environment (Development | Testing | StagingPilot | Production). Refuse ambiguous targets.
2. Confirm Git commit SHA and clean working tree for StagingPilot/Production.
3. Provision Platform DB credentials in environment-owned secret storage (not repo, not shared chat).
4. Provision POS DB credentials separately (least privilege; no cross-DB rights; never legacy product).
5. Provision TLS private key + certificate chain for reverse proxy (pilot or Production CA as applicable).
6. Provision CORS allowed origins list (explicit; Production deny-by-default if empty).
7. Provision AllowedHosts (never `*`).
8. Confirm ASPNETCORE_ENVIRONMENT will NOT be Development/Testing for StagingPilot/Production.
9. Confirm Development identity / commercial / actor headers are unavailable in StagingPilot/Production.
10. Record backup encryption key location (EXITS_BACKUP_KEY_FILE) if off-host encryption is used — never beside dumps.
11. Export only required variables into the deployment shell session; do not echo values.
12. Run configuration validation (ExItS.Deployment.Cli validate-config) before migrate/deploy.
13. Rotate credentials after pilot ends or after suspected exposure.
14. Do not copy Production secrets into Development, or Development secrets into Production.
