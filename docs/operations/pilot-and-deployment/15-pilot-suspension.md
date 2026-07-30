# Pilot suspension

Phase: `P9-WP05-pilot-and-deployment`

| Field | Content |
|---|---|
| Trigger | Critical defect / auth incident / backup failure mid-pilot |
| Prerequisites | Pilot Sponsor + On-Call |
| Responsible role | Deployment Operator / Platform On-Call / POS On-Call / Security Lead / Pilot Sponsor (assign per environment) |
| Actions | Disable ingress/access; notify users; preserve evidence; decide rollback/restore |
| Expected result | Pilot unreachable; evidence retained |
| Failure escalation | Executive escalation |
| Evidence to retain | Suspension notice + timeline |

No real credentials. Do not describe StagingPilot as Production.
