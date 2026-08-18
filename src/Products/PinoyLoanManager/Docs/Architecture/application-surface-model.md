# Pinoy Loan Manager — Application Surface Model

**Status:** Agreed product direction (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Agreed application surfaces for Pinoy Loan Manager. No client, API, or UI project is authorized in this package.

Root architecture: [../architecture.md](../architecture.md). Operating model: [../Product/lending-operating-model.md](../Product/lending-operating-model.md). Daily ops: [../Product/daily-operational-workflow.md](../Product/daily-operational-workflow.md). Authorization: [../Security/role-and-grant-baseline.md](../Security/role-and-grant-baseline.md).

---

## A. ExItS Platform Admin Web

**One** unified Platform Admin application for all ExItS SaaS products.

Platform roles currently:

- Platform Owner
- Platform Admin

Platform Admin owns SaaS **control-plane** functions such as:

- organizations
- product catalog
- Pinoy Loan Manager subscription
- plans
- entitlements
- Platform SaaS billing
- Platform usage charges
- Platform invoices
- Platform audit
- product activation / suspension

Platform Admin must **not** become the normal UI for managing borrower loans.

Platform Administrator does not automatically receive Loan operational access. See [../authorization-matrix.md](../authorization-matrix.md).

---

## B. Pinoy Loan Manager Organization Web

This is the **full operational application** for the lending organization.

Expected functional areas **eventually** include:

- Dashboard
- Borrowers
- Traditional Loans
- Quick Loans
- Applications / Requests
- Approvals
- Active Loans
- Payments
- Collections
- Collectors
- Cashier / Cash Management
- Penalties / Exceptions / Waivers
- Disbursements
- Reconciliation
- Reports
- Configuration
- Staff / roles / grants
- Audit

Proposed client direction remains Blazor Web. No web project is authorized (PLM-D-00-09).

This surface is **not** Platform Admin.

---

## C. Pinoy Loan Manager MAUI Blazor Hybrid

**Limited** operational / field application.

Primarily for:

- Collector
- field operations
- assigned borrowers
- collection routes / work
- collecting payment
- recording missed collection reasons
- approved loan disbursement
- cash accountability
- end-of-day remittance

It is **not** intended to duplicate the complete Organization Admin Web.

Possible later native capabilities (secure storage, camera/document capture, biometrics, connectivity, notifications, SQLite/offline) remain listed in [../architecture.md](../architecture.md) and are **not authorized**. Collector offline behavior is open. **Server remains authoritative** for final financial authorization / posting. See [../Product/daily-operational-workflow.md](../Product/daily-operational-workflow.md).

---

## D. ExItS Personal

This is the **customer / borrower** experience.

Do **not** create a separate borrower application.

Personal may eventually show:

- My Lenders
- available Quick Loan offers
- Quick Loan request / application
- approval status
- active loans
- balances
- schedules
- payment history
- notifications

Personal is only a **presentation / customer** surface.

Pinoy Loan Manager remains **authoritative** for Loan operational data. Personal may consume authorized Loan information through Loan APIs / contracts only.

POS Customer ≠ Loan Borrower. Linking is optional, consent-required, and never auto-activated from EX ID / QR resolution. See [../architecture.md](../architecture.md), [../Product/personal-borrower-linking.md](../Product/personal-borrower-linking.md), and [personal-integration-boundary.md](personal-integration-boundary.md).

---

## Surface vs authority

| Surface | Role |
|---|---|
| Platform Admin | SaaS control plane |
| Organization Web | Full lending operations |
| MAUI Hybrid | Field / collector operations (subset) |
| ExItS Personal | Borrower presentation |

Server-authoritative business rules remain in Pinoy Loan Manager. UI/API must not become a second source of truth.

---

## Explicit non-goals

- Scaffolding any of these clients in this package
- Using Platform Admin as the loan operations console
- Duplicating full Admin Web on MAUI
- A standalone borrower app
- Treating Personal as the loan ledger
