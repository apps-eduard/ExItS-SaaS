# Release Plan

[Home](index.md) | [Dashboard](portfolio-progress.md)

## R0 — Assessment baseline

**Complete with documented risks (Phase 0).** HealthCare MVP inventoried; reuse, UI, runtime, and closeout recommendations recorded. Open risks documented.

## R0.5 — Platform/product capability boundary (Phase 1 docs)

**Complete (P1-WP01 accepted).** Ownership and prohibited coupling documented (ADR-011).

## R0.6 — Data ownership and contracts (Phase 1 docs)

**Complete (P1-WP02 + Cash/GCash accepted).**

## R0.7 — Extraction sequence and architecture approval (Phase 1 closeout)

**Complete (P1-WP04 / ADR-014).** Architecture approved for controlled implementation.

## R1 — ExITS Platform extraction

**Complete with documented risks (Phase 2 / P2-WP06).** Root Platform foundation, identity/org, commercial domain, HC contract boundaries, and migration dry-run validation delivered. HealthCare remains frozen. Auth, persistence, real HC integration/cutover, Admin UI, and POS were **not** delivered. See [phase-02-extraction-closeout.md](reports/phase-02-extraction-closeout.md).

## R2 — Platform portfolio administration

**Complete with documented risks (Phase 4 / P4-WP04).** HealthCare and PinoyBusinessPOS product plans, subscriptions, trials, manual payment activation, Platform Admin UI, audit, and system-role authorization delivered for development. Production authentication, gateways, invoices, and entitlement delivery remain open. See [P4-WP04 report](reports/P4-WP04-audit-authorization-and-closeout.md).

## R2.5 — PinoyBusinessPOS MAUI foundation (Phase 5)

**Complete with documented risks (P5-WP05).** MAUI shell, DesignSystem, themes/density, EN/Tagalog, reusable components, Dev/Testing authentication, onboarding, org selection, and commercial access gating delivered. Production auth and POS business workflows remain open. See [P5-WP05 report](reports/P5-WP05-authentication-onboarding-and-closeout.md).

## R3 — PinoyBusinessPOS Utang pilot

**Complete (P6-WP01–P6-WP06).** Organization-isolated customers, remarks-based credit, repayments/ledger, due dates/overdue monitoring, projection statements/receipts, and trial/continuity capability rules delivered and closed. Not production-ready. See [P6-WP06 closeout](reports/P6-WP06-utang-mvp-closeout.md).
MAUI app, bilingual UI, themes, customers, Utang, payments, overdue monitoring and basic cloud operation.

**Next:** **P7-WP02 — Offline Queue and Idempotency** when approved.

## R3.5 — Offline foundation (Phase 7 / P7-WP01–P7-WP05)

**Phase 7 complete with documented risks** — offline subsystem closed. See [P7-WP05 report](reports/P7-WP05-offline-closeout.md).

**P7-WP01 complete** — DeviceId, SQLite foundation, isolation, sync-status shell base, Dev diagnostics.

**P7-WP02 complete with documented risks** — encrypted generic outbox, idempotency, retry/access blocking, operational sync states. No offline business workflows. See [P7-WP02 report](reports/P7-WP02-offline-queue-and-idempotency.md).

**P7-WP03 complete with documented risks** — encrypted local customer/credit read models; offline customer create/update and credit create; row-level AES-GCM (SQLCipher deferred). See [P7-WP03 report](reports/P7-WP03-customer-and-credit-sync.md).

**P7-WP04 complete with documented risks** — encrypted local repayment projections; offline repayments, reversals, and due-date changes; projected balance with pending repayment; no offline statements/receipts. See [P7-WP04 report](reports/P7-WP04-payment-sync-and-recovery.md).

**Not production-ready** while R-109, R-022, full-database encryption, production auth/roles, and production background scheduling remain open.

## R3.6 — Basic Store started (Phase 8 / P8-WP01–P8-WP04)

**Phase 8 in progress** — P8-WP01 through P8-WP04 **complete** with documented risks. See [P8-WP04 report](reports/P8-WP04-basic-inventory.md).

Online-only catalog, Cash/ManualGCash/Utang sales, and basic inventory (immutable movements, sale deduction/void restoration). Features include `store-inventory-view` / `store-inventory-manage`. Migration `AddPosBasicInventory`. **No suppliers, warehouses, costing, offline inventory, or negative-stock override.** R-109 remains open.

**Next:** **P8-WP05 — Expenses** when approved.

## R4 — Commercial MVP

Offline synchronization, Basic Store and production hardening.

## R5 — Full POS

Purchasing, advanced inventory, shifts, returns/refunds and multiple registers.
