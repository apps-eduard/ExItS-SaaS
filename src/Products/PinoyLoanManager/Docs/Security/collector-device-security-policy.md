# Pinoy Loan Manager — Collector Device Security Policy

**Status:** Accepted future-requirements policy (PLM-DOC-09); **not implemented**
**Implementation present:** No
**Last updated:** 2026-08-19

Future security requirements for MAUI collector devices. This document records **planning requirements only**. It does **not** claim any collector device security is implemented, certified, or production-ready.

Related: [../Architecture/mobile-and-offline-operating-model.md](../Architecture/mobile-and-offline-operating-model.md), [resource-scope-and-data-minimization-policy.md](resource-scope-and-data-minimization-policy.md), [../security.md](../security.md).

---

## Scope

Applies to organization-issued or organization-authorized devices running Pinoy Loan Manager MAUI for Collector (and other field roles if explicitly assigned).

Organization Web browser sessions and Personal apps are out of scope except where noted for comparison.

---

## MVP honesty statement

**No collector device security controls are implemented in Pinoy Loan Manager today.**

Documentation baseline acceptance does not imply:

- device enrollment
- MDM integration
- hardware-backed key storage
- encrypted offline database
- remote wipe capability
- biometric gate on app launch
- jailbreak / root detection

Any future implementation must be evidenced in code, tests, and deployment documentation before Production claims.

---

## Future requirements (when MAUI is authorized)

When collector MAUI implementation is authorized, the following requirements should be designed explicitly:

### Device and session binding

- Registered device or installation identity bound to organization staff identity
- Session tokens revocable server-side
- Forced re-authentication after policy-defined idle timeout on field devices
- Immediate loss of mutating capability when staff membership or grants are revoked

### Local data protection

- Minimize cached borrower PII on device; align with [resource-scope-and-data-minimization-policy.md](resource-scope-and-data-minimization-policy.md)
- Encrypt local read-only cache and drafts at rest when LocalStore is authorized
- No storage of full organization borrower exports on device by default
- Secure wipe of local PLM data on logout / decommission workflow (future)

### Authentication hardening (field)

- Organization policy may require device passcode or biometric gate before MAUI access (platform capability; not implemented)
- No shared Collector login across multiple simultaneous devices without audit visibility (future design)

### Integrity and abuse resistance

- Idempotency / correlation for financial commands ([../security.md](../security.md))
- Detect and surface duplicate submission attempts
- Optional jailbreak/root detection as **organization policy** — product may support reporting only; not a sole security control

### Loss and theft

- Remote session revocation from Organization Web (future)
- Documented operator procedure for reported device loss (Operations; not defined here)
- Pending Receipt floats and open collector accountability remain visible to Cashier/Manager after device loss

### Location and privacy

- Event-based GPS only when organization enables ([../Product/collector-route-and-location-policy.md](../Product/collector-route-and-location-policy.md))
- No covert tracking; staff disclosure required when enabled

---

## Explicit non-goals (this document)

- Claiming MVP meets bank-grade mobile security
- Selecting a specific MDM vendor
- Defining cryptographic algorithms or key lengths in this planning package
- Replacing organization physical cash controls or staff hiring vetting
- Legal sufficiency of privacy notices (PLM-D-00-11)

---

## Open until implementation

| Topic | Status |
|---|---|
| Exact crypto libraries and key storage | Open — implementation WP |
| Offline encrypted database | Deferred with offline posting package |
| MDM / EMM integration | Open — organization deployment choice |
| Device compliance attestation API | Open — future WP |
| Production security validation | Blocked on R-091 and implementation |
