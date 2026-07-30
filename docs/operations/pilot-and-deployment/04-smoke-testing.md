# Smoke testing

Phase: `P9-WP05-pilot-and-deployment`

| Field | Content |
|---|---|
| Trigger | After readiness green |
| Prerequisites | Disposable or designated pilot data |
| Responsible role | Deployment Operator / Platform On-Call / POS On-Call / Security Lead / Pilot Sponsor (assign per environment) |
| Actions | SmokeHealth on StagingPilot; SmokeFull only Dev/Testing |
| Expected result | Contracts pass |
| Failure escalation | Rollback app package if smoke fails post-deploy |
| Evidence to retain | Smoke transcript |

No real credentials. Do not describe StagingPilot as Production.
