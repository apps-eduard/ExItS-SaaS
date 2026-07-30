# Certificate issue

Phase: `P9-WP05-pilot-and-deployment`

| Field | Content |
|---|---|
| Trigger | TLS errors / expiry |
| Prerequisites | Cert ownership known |
| Responsible role | Deployment Operator / Platform On-Call / POS On-Call / Security Lead / Pilot Sponsor (assign per environment) |
| Actions | Validate proxy certs; renew from CA; reload nginx; retest HTTPS |
| Expected result | HTTPS healthy |
| Failure escalation | Suspend external pilot access until fixed |
| Evidence to retain | Cert fingerprint/expiry (no private keys) |

No real credentials. Do not describe StagingPilot as Production.
