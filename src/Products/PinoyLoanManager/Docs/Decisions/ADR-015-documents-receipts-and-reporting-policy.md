# ADR-015 — Documents, receipts, and reporting policy

**Status:** Accepted product policy (PLM-DOC-08); not implemented
**Date:** 2026-08-19

---

## Context

PLM needed Product Owner rules for authoritative document types, durable receipt identity, account statements, template versioning, operational KPI formulas, PAR definitions, aging buckets, and the MVP report catalog. Prior WP08 baselines listed documents and reporting areas but left receipt numbering, PAR formulas, and exact KPI definitions open.

---

## Decision

1. **Document catalog** — Plan authoritative document types for origination, financial transactions, lifecycle/collections, and account servicing. Templates are versioned; rendered copies preserve template version and Loan/transaction snapshot.
2. **Document identity** — Every authoritative document/receipt has immutable machine identity, Organization scope, human-readable reference (server-generated, unique within Organization/document-type scope, immutable after issuance), source resource link, generation metadata, and status.
3. **Receipts** — Every posted Payment, Disbursement, Cash Refund, Settlement, Principal Prepayment, and Recovery Payment receives durable receipt identity. Print/render/delivery failure does not roll back the financial transaction. Reprints are copies, not new transactions.
4. **Statements** — Loan statements explain component balances and activity; no single unexplained balance.
5. **KPI formulas** — Approve GROSS OUTSTANDING PRINCIPAL, PAST-DUE SCHEDULED AMOUNT, COLLECTION RATE FOR PERIOD, and PAR-X with standard PAR 1/7/30/60/90 views. Written-Off Loans excluded from active PAR denominator.
6. **Aging buckets** — Approve Current (0), 1–7, 8–30, 31–60, 61–90, 91+; Matured Past Due and Written-Off separately identifiable.
7. **Report catalog** — Plan scope-filtered operational reports for origination, collections, cash, write-off/recovery, audit, and related areas.
8. **Legal boundary** — Do not claim statutory accounting ratios or legally mandated document/receipt formats. PLM-D-00-11 remains Open for legal sufficiency and exact mandatory numbering format.

Canonical: [../Product/document-and-receipt-policy.md](../Product/document-and-receipt-policy.md), [../Product/reporting-kpi-and-aging-policy.md](../Product/reporting-kpi-and-aging-policy.md).

---

## Consequences

Document, receipt, and reporting formulas are approved for MVP planning. Implementation, legally mandated formats, and GL/statutory reporting remain future work.

**Still open:** legal document sufficiency (PLM-D-00-11), exact legally required receipt numbering format, notification provider, implementation.
