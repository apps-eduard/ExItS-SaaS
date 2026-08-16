# Pilot entry and exit criteria

## Pilot type (authorized)

**Internal technical / staging pilot** (non-production). Not a public Production deployment. Not a restricted external customer pilot while R-091 remains open.

## Entry criteria (all mandatory)

- [ ] Automated tests pass (solution Release)
- [ ] Android Release build succeeds
- [ ] Deployment artifacts versioned (`package-version`)
- [ ] Environment configuration validation passes
- [ ] Development/Testing bypasses unavailable on pilot hosts
- [ ] HTTPS requirements for chosen pilot environment satisfied
- [ ] Platform + POS backups created and verified (manifest + SHA-256)
- [ ] Migration rehearsal passes (Platform then POS)
- [ ] Health/readiness endpoints pass
- [ ] Smoke tests pass (HealthOnly on StagingPilot; Full on disposable Testing)
- [ ] Known risks disclosed to pilot users
- [ ] Support and rollback ownership assigned
- [ ] Pilot users understand authentication and data limitations

Unmet mandatory criteria ⇒ **blocked pilot**.

## Exit criteria

- [ ] No unresolved critical data-integrity defect
- [ ] No confirmed cross-organization leakage
- [ ] No duplicate financial execution
- [ ] No negative stock caused by concurrency
- [ ] No unrecoverable migration failure
- [ ] Backup/recovery procedure remains usable
- [ ] Acceptable error rate under pilot workload
- [ ] Critical MVP workflows complete successfully
- [ ] Support incidents classified
- [ ] Pilot feedback recorded
- [ ] Release blockers updated honestly

Pilot completion ≠ Production approval.

## Monitored workflows

Organization/product access, customers, Utang, catalog/barcode, sales, Product-Based Utang, inventory, expenses, dashboard/reports, offline/reconnect where supported.

## Prohibited

Public Production use with Dev/Testing headers; tax/VAT/refunds/accounting/purchasing/payroll; payment gateway claims; treating Manual GCash as verified; legacy product; PITR claims.
