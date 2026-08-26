# Pinoy Loan Manager — Persistence and Database Boundary

**Status:** Planning / architecture baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Separate PLM database intent. Not a schema, migration, or naming close-out.

Related: [source-and-project-layout.md](source-and-project-layout.md), [../architecture.md](../architecture.md).

---

## Planning target

Separate product database.

**Logical name (Closed, PLM-D-00-02):** `ExItS_PinoyLoanManager`. Database is **not created**. Schema, connections, partitions, stamps, backups, and migrations remain **deferred**.

Schema name remains **OPEN**.

Infrastructure (when authorized) owns persistence. Domain remains persistence-independent. No generic repositories as a required pattern.

---

## Isolation (required)

- no cross-product FK
- no direct Platform reads
- no direct POS reads
- OrganizationId may be carried as Guid identity / contract, never a cross-product FK
- no shared Hangfire / operational DB with other products
- independent backup / restore from Platform and POS (procedure **OPEN**)

Do **not** automatically apply production migrations at API startup when an API exists later.

Do **not** create the database or migrations in this package.

---

## Explicit non-goals

- EF model / table design
- Migration files
- Using EF InMemory as PostgreSQL proof later
