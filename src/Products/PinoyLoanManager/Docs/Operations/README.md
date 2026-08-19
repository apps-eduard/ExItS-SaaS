# Operations

**Purpose:** Deployment, migrations, backup/restore, recovery, observability, and production-readiness documentation.
**Status:** Foundation / planning only
**Implementation present:** No

This directory will hold Pinoy Loan Manager operations notes when a deployable packaging or persistence package is authorized.

---

## Recorded intent (not implemented)

- independently versioned product image (future)
- one persistent database per product (logical name `ExItS_PinoyLoanManager` — **Closed for name**, PLM-D-00-02; not created)
- customer-specific **configuration**, never customer-specific source forks
- no Dockerfiles, Compose profiles, or production migration apply from this package
- do not automatically apply production migrations at API startup when an API exists later

---

## Future subjects (not created here)

- deployment notes
- migration apply / rollback / re-apply procedures
- backup and restore
- recovery
- observability
- production-readiness evidence
