# Deployment

Phase: `P9-WP05-pilot-and-deployment`

| Field | Content |
|---|---|
| Trigger | Operator initiates controlled deploy/rehearsal |
| Prerequisites | Clean Git; confirmed environment; secrets provisioned |
| Responsible role | Deployment Operator / Platform On-Call / POS On-Call / Security Lead / Pilot Sponsor (assign per environment) |
| Actions | Run Invoke-ExItsDeploy.ps1 Plan/ValidateConfig; pre-deploy backup; BackupGate; Migrate; start services; WaitHealth; Smoke; Evidence |
| Expected result | Services healthy; evidence recorded |
| Failure escalation | Stop; do not continue; escalate to Platform/POS On-Call |
| Evidence to retain | Commit SHA, package version, backup-set IDs, durations (redacted logs) |

No real credentials. Do not describe StagingPilot as Production.
