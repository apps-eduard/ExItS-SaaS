# Service restart

Phase: `P9-WP05-pilot-and-deployment`

| Field | Content |
|---|---|
| Trigger | Process hang / config reload |
| Prerequisites | Identify service; confirm environment |
| Responsible role | Deployment Operator / Platform On-Call / POS On-Call / Security Lead / Pilot Sponsor (assign per environment) |
| Actions | Graceful stop (compose stop / SIGTERM); start; WaitHealth |
| Expected result | Liveness+readiness OK |
| Failure escalation | Escalate outage runbook |
| Evidence to retain | Restart timestamps |

No real credentials. Do not describe StagingPilot as Production.
