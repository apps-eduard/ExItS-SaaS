# Backup failure

Phase: `P9-WP05-pilot-and-deployment`

| Field | Content |
|---|---|
| Trigger | Backup or verify fails |
| Prerequisites | None |
| Responsible role | Deployment Operator / Platform On-Call / POS On-Call / Security Lead / Pilot Sponsor (assign per environment) |
| Actions | Do not migrate/deploy; triage storage/credentials/pg_dump; retry verify |
| Expected result | Verified backup-set IDs recorded |
| Failure escalation | Escalate Platform On-Call |
| Evidence to retain | Failure message (redacted) |

No real credentials. Do not describe StagingPilot as Production.
