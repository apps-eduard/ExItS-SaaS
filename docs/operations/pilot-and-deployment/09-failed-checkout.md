# Failed checkout investigation

Phase: `P9-WP05-pilot-and-deployment`

| Field | Content |
|---|---|
| Trigger | Pilot reports sale/checkout failure |
| Prerequisites | Org id; time window; correlation ids |
| Responsible role | Deployment Operator / Platform On-Call / POS On-Call / Security Lead / Pilot Sponsor (assign per environment) |
| Actions | Inspect safe logs (no payloads); check idempotency/conflict outcomes; reproduce on disposable data if needed |
| Electronic payments | If Card/GCash: confirm sale stuck in `AwaitingPayment` vs completed; check `payment_attempts` status and webhook/simulation path — processing is **simulated** (`FakePaymentGateway`); see [P19-card-gcash-payment-ui-and-simulation](../../reports/P19-card-gcash-payment-ui-and-simulation.md) |
| Expected result | Root cause classified |
| Failure escalation | If data integrity risk — suspend pilot |
| Evidence to retain | Incident notes |

No real credentials. Do not describe StagingPilot as Production.
