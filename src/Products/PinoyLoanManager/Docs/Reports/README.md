# Reports

**Purpose:** Evidence of completed Pinoy Loan Manager work packages.
**Status:** PLM-DOC-11 complete; PLM-01 scaffold; Gates B–D3 complete; PLM-02A complete
**Implementation present:** Product shell + React Client + online-first PWA + cookie Sign In + Personal account lifecycle + org/product-access gate + fail-closed server access boundary; Platform `pinoy-loan-manager` catalog + current-session access API

Reports in this directory will eventually contain:

- delivered scope
- exclusions
- migrations (when relevant)
- tests
- validation
- risks / open decisions
- Git hashes
- exact next work package

Do not rewrite historical signed-off reports merely to erase history.

---

## Reports in this package

| Report | Purpose |
|---|---|
| [PLM-00-foundation-closeout.md](PLM-00-foundation-closeout.md) | PLM-00 documentation closeout, implementation gates, recommended PLM-01 |
| [PLM-DOC-01-product-identity-and-personal-linking.md](PLM-DOC-01-product-identity-and-personal-linking.md) | PLM-DOC-01 product identity, Borrower identity, and Personal linking finalization |
| [PLM-DOC-02-financial-calculation-and-allocation.md](PLM-DOC-02-financial-calculation-and-allocation.md) | PLM-DOC-02 calculation, fees, rounding, and payment allocation |
| [PLM-DOC-03-schedule-delinquency-penalty-and-maturity.md](PLM-DOC-03-schedule-delinquency-penalty-and-maturity.md) | PLM-DOC-03 calendar, delinquency, penalties, and maturity |
| [PLM-DOC-04-settlement-reversals-variance-and-accounting.md](PLM-DOC-04-settlement-reversals-variance-and-accounting.md) | PLM-DOC-04 settlement, reversals, variance, and accounting |
| [PLM-DOC-05-authorization-and-operational-security.md](PLM-DOC-05-authorization-and-operational-security.md) | PLM-DOC-05 roles, grants, workflow security |
| [PLM-DOC-06-restructuring-write-off-recovery-and-collections.md](PLM-DOC-06-restructuring-write-off-recovery-and-collections.md) | PLM-DOC-06 restructuring, Write-Off, Recovery, collections |
| [PLM-DOC-07-onboarding-application-and-approval.md](PLM-DOC-07-onboarding-application-and-approval.md) | PLM-DOC-07 onboarding, application, approval |
| [PLM-DOC-08-documents-reporting-privacy-and-notifications.md](PLM-DOC-08-documents-reporting-privacy-and-notifications.md) | PLM-DOC-08 documents, reporting, privacy, notifications |
| [PLM-DOC-09-mobile-field-treasury-and-ui-boundaries.md](PLM-DOC-09-mobile-field-treasury-and-ui-boundaries.md) | PLM-DOC-09 mobile, treasury, UI boundaries; **PLM-D-00-09 Closed** |
| [PLM-DOC-10-platform-personal-and-commercial-contracts.md](PLM-DOC-10-platform-personal-and-commercial-contracts.md) | PLM-DOC-10 Platform, Personal, commercial contracts; **PLM-D-00-05 Closed** |

## Implementation evidence (PLM-01 / Client Gates / PWA / PLM-02A)

| Report | Purpose |
|---|---|
| [PLM-01-product-scaffold-and-isolation.md](PLM-01-product-scaffold-and-isolation.md) | PLM-01 product shell, isolation, deferred LocalStore |
| [PLM-01A-react-pwa-capacitor-architecture-decision.md](PLM-01A-react-pwa-capacitor-architecture-decision.md) | PLM-01A React + PWA + Capacitor architecture; PLM-D-00-09 closed |
| [PLM-CLIENT-GATE-B-react-client-scaffold.md](PLM-CLIENT-GATE-B-react-client-scaffold.md) | React Client scaffold |
| [PLM-CLIENT-GATE-C-browser-pwa-foundation.md](PLM-CLIENT-GATE-C-browser-pwa-foundation.md) | Browser + online-first PWA |
| [PLM-CLIENT-GATE-D0-browser-auth-transport.md](PLM-CLIENT-GATE-D0-browser-auth-transport.md) | Same-origin `/platform-api` + cookie policy |
| [PLM-CLIENT-GATE-D1-mobile-sign-in-session.md](PLM-CLIENT-GATE-D1-mobile-sign-in-session.md) | Mobile-first Sign In + session UI |
| [PLM-CLIENT-GATE-D2-account-lifecycle-mailpit.md](PLM-CLIENT-GATE-D2-account-lifecycle-mailpit.md) | Register / Activate / Forgot / Reset |
| [PLM-D3-PRE-product-registration-self-access.md](PLM-D3-PRE-product-registration-self-access.md) | Product code + Local Validation fixture + current-session access API |
| [PLM-CLIENT-GATE-D3-organization-product-access.md](PLM-CLIENT-GATE-D3-organization-product-access.md) | Organization discovery + product-access gate |
| [PLM-PWA-H1-cache-storage-security.md](PLM-PWA-H1-cache-storage-security.md) | PWA cache/storage security |
| [PLM-PWA-H2-install-update-lifecycle.md](PLM-PWA-H2-install-update-lifecycle.md) | PWA install/update lifecycle |
| [PLM-PWA-H3-connectivity-fail-closed.md](PLM-PWA-H3-connectivity-fail-closed.md) | Fail-closed connectivity UX |
| [PLM-PWA-H4-production-preview-reliability.md](PLM-PWA-H4-production-preview-reliability.md) | Production-preview PWA reliability |
| [PLM-PWA-H5-csrf-compatibility.md](PLM-PWA-H5-csrf-compatibility.md) | CSRF compatibility with Platform auth |
| [PLM-02A-server-access-boundary-foundation.md](PLM-02A-server-access-boundary-foundation.md) | Fail-closed server access boundary |

PLM-00-WP01 through PLM-00-WP09 do not add a separate report file.
