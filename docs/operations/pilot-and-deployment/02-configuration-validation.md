# Configuration validation

Phase: `P9-WP05-pilot-and-deployment`

| Field | Content |
|---|---|
| Trigger | Before migrate/deploy |
| Prerequisites | Env vars from secret store; no echoed secrets |
| Responsible role | Deployment Operator / Platform On-Call / POS On-Call / Security Lead / Pilot Sponsor (assign per environment) |
| Actions | ExItS.Deployment.Cli validate-config with explicit --env |
| Expected result | Valid=true |
| Failure escalation | Fix config; never bypass Production/Pilot guards |
| Evidence to retain | CLI findings output (redacted) |

No real credentials. Do not describe StagingPilot as Production.
