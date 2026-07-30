# Database outage

Phase: `P9-WP05-pilot-and-deployment`

| Field | Content |
|---|---|
| Trigger | Readiness fails / DB unreachable |
| Prerequisites | Which DB (Platform vs POS) |
| Responsible role | Deployment Operator / Platform On-Call / POS On-Call / Security Lead / Pilot Sponsor (assign per environment) |
| Actions | Check network/creds/disk; restore service; WaitHealth |
| Expected result | DB healthy; APIs ready |
| Failure escalation | Pilot suspension; restore escalation if corruption |
| Evidence to retain | Outage timeline |

No real credentials. Do not describe StagingPilot as Production.
