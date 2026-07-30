# Rollback

Phase: `P9-WP05-pilot-and-deployment`

| Field | Content |
|---|---|
| Trigger | Deploy/smoke/migration failure |
| Prerequisites | Rollback authorizer identified |
| Responsible role | Deployment Operator / Platform On-Call / POS On-Call / Security Lead / Pilot Sponsor (assign per environment) |
| Actions | Use RollbackAdvisor guidance; app redeploy vs backup restore |
| Expected result | Prior healthy version or restored DB validated |
| Failure escalation | Restore escalation |
| Evidence to retain | Decision + versions |

No real credentials. Do not describe StagingPilot as Production.
