# Pinoy Loan Manager — Application Surface Model

**Status:** Agreed product direction; client strategy **PLM-D-00-09 Closed / Product Owner Approved**; Gate D2 account lifecycle present
**Implementation present:** React Client + online-first PWA + cookie Sign In; Capacitor not started
**Last updated:** 2026-08-19

Agreed application surfaces for Pinoy Loan Manager. Capacitor is **not** created in Gate D1.

Root architecture: [../architecture.md](../architecture.md). Client architecture: [react-pwa-capacitor-client.md](react-pwa-capacitor-client.md). ADR: [../Decisions/PLM-D-00-09-react-pwa-capacitor-client-strategy.md](../Decisions/PLM-D-00-09-react-pwa-capacitor-client-strategy.md). Operating model: [../Product/lending-operating-model.md](../Product/lending-operating-model.md). Daily ops: [../Product/daily-operational-workflow.md](../Product/daily-operational-workflow.md). Authorization: [../Security/role-and-grant-baseline.md](../Security/role-and-grant-baseline.md).

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

## B. Pinoy Loan Manager Organization Web / PWA

This is the **full operational application** for the lending organization.

It is the shared React Client (`ExItS.PinoyLoanManager.Client`, **not created yet**) running in the browser and as an installable PWA. `ExItS.PinoyLoanManager.Web` is the future ASP.NET Core host/BFF, not a second Blazor lending UI.

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

This list records **surface ownership only**. Do not implement them here. Do not invent data models, calculations, or API contracts.

This surface is **not** Platform Admin.

---

## C. Pinoy Loan Manager Capacitor Android

**Same** React application as Organization Web/PWA, hosted in a thin Capacitor Android container.

Mobile may present a role/capability-optimized subset such as:

- assigned work
- borrower lookup
- collection workflow
- approved field disbursement
- cash accountability
- remittance

**only** when those business packages are authorized.

It is **not** a separate Android business implementation and is **not** intended to duplicate the complete Organization Admin Web.

Authorization remains server enforced. Hiding a route on Android is **not** authorization.

The previous MAUI Blazor Hybrid preferred path is **superseded** (PLM-D-00-09). Capacitor must not become a loan calculation engine, authorization engine, financial ledger, or second API.

Possible later native capabilities (secure storage, camera/document capture, biometrics, connectivity, notifications) remain listed in [react-pwa-capacitor-client.md](react-pwa-capacitor-client.md) and are **not authorized** here. Collector offline behavior is open. **Server remains authoritative** for final financial authorization / posting. See [../Product/daily-operational-workflow.md](../Product/daily-operational-workflow.md) and [mobile-offline-boundary.md](mobile-offline-boundary.md).

iOS is later only, by separate Product Owner authorization.

---

## D. ExItS Personal

This is the **customer / borrower** experience.

Do **not** create a separate borrower application. Do **not** merge Personal borrower UX into the organization React Client.

Loan area intent: [../Product/personal-loan-experience.md](../Product/personal-loan-experience.md). Keep Personal peer-to-peer “I Lent / I Borrowed” separate from organizational PLM Loans.

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
| Organization Web / PWA | Full lending operations (shared React Client) |
| Capacitor Android | Same React Client; field / collector subset later |
| ExItS Personal | Borrower presentation |

Server-authoritative business rules remain in Pinoy Loan Manager. UI/API must not become a second source of truth.

---

## Explicit non-goals

- Creating the React Client, PWA code, or Capacitor/Android workspace in this package
- Using Platform Admin as the loan operations console
- Duplicating full Admin Web on Android
- A standalone borrower app
- Treating Personal as the loan ledger
- Copying PinoyBusinessPOS React
- Restoring MAUI as the preferred field client
