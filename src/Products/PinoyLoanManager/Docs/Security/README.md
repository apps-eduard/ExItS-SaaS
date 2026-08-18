# Security

**Purpose:** Access, privacy, security, authorization, consent, data classification, and audit rules.
**Status:** Foundation / planning only
**Implementation present:** No

This directory will hold Pinoy Loan Manager security and privacy documentation. No authorization matrix, roles, grants, or audit implementation exists yet.

---

## Recorded constraints (intent only)

- Product-local authorization will be authoritative inside Pinoy Loan Manager
- Platform product access is not operational permission
- Effective operational access must eventually satisfy the Product Foundation access intersection (trusted actor, organization context, Platform product access, commercial state, entitlements, product-local role/grant, resource/workflow invariants)
- PHI defaults to none unless a later package explicitly authorizes and designs for it
- Loan operational data remains inside Pinoy Loan Manager
- Personal linking, if implemented later, requires explicit consent
- EX ID / QR resolution must never auto-link a Personal identity to a borrower
- Do not claim production-secure authentication while **R-091** remains open
- Do not treat Dev/Testing commercial headers as the production design while **D-P12-03** remains open

---

## Future subjects (not defined in this package)

- product-local roles and grants
- authorization matrix
- consent model for Personal linking
- data classification beyond the PHI default
- retention and privacy rules
- product audit / immutable history
- secrets, credential, and token handling for Web and MAUI clients
- device biometrics and secure storage (client capability only; not designed here)
