# Restore escalation

Phase: `P9-WP05-pilot-and-deployment`

| Field | Content |
|---|---|
| Trigger | Migration/data integrity requires backup restore |
| Prerequisites | Verified backup-set; DESTROY_AND_RESTORE confirmation if non-empty |
| Responsible role | Deployment Operator / Platform On-Call / POS On-Call / Security Lead / Pilot Sponsor (assign per environment) |
| Actions | Follow ops/backup restore runbooks; Platform then POS as required |
| Expected result | Schema+smoke OK |
| Failure escalation | Keep pilot suspended |
| Evidence to retain | Backup-set IDs restored |

No real credentials. Do not describe StagingPilot as Production.
