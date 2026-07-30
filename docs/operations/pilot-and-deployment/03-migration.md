# Migration

Phase: `P9-WP05-pilot-and-deployment`

| Field | Content |
|---|---|
| Trigger | Approved deploy window after backups verified |
| Prerequisites | BackupGate Allowed=true; correct DB targets |
| Responsible role | Deployment Operator / Platform On-Call / POS On-Call / Security Lead / Pilot Sponsor (assign per environment) |
| Actions | Platform ef database update; validate; POS ef database update; validate |
| Expected result | Schemas current; apps start |
| Failure escalation | Stop; treat as migration failure; consider restore (not ordinary app rollback) |
| Evidence to retain | Migration commands exit codes; schema checks |

No real credentials. Do not describe StagingPilot as Production.
