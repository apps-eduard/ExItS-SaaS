# Final Portfolio Boundaries

[Home](../index.md) | [Phase 0 final assessment](../reports/phase-00-final-assessment-and-recommendation.md) | [Phase 1 architecture approval](../reports/phase-01-architecture-approval.md) | [Approved summary](approved-architecture-summary.md) | [Capability boundary (P1-WP01)](platform-product-capability-boundary.md) | [Contracts (P1-WP02)](platform-product-contracts.md) | [Extraction sequence (P1-WP03)](../reuse/extraction-sequence.md) | [Data ownership](data-ownership.md) | [ADR-010](../decisions/ADR-010-separate-ui-implementations-platform-and-pos.md) | [ADR-011](../decisions/ADR-011-platform-authority-and-product-local-projections.md) | [ADR-012](../decisions/ADR-012-versioned-platform-contracts-and-local-projections.md) | [ADR-014](../decisions/ADR-014-approve-exits-portfolio-architecture-for-controlled-implementation.md)

Approved at Phase 0 closeout (P0-WP04). Capability ownership: P1-WP01. Contracts: P1-WP02. Extraction sequence: P1-WP03. **Phase 1 architecture approved (P1-WP04 / ADR-014)** for controlled implementation beginning at **P2-WP01** when authorized.

| Capability | Platform | HealthCare | PinoyBusinessPOS | Shared Contract | Notes |
|---|---|---|---|---|---|
| Global identity / users | Own (target) | Uses / projects | Uses / projects | UserId, auth claims | Extract carefully |
| Authentication / refresh tokens | Own (target) | Adapt client | Adapt client | Token/issuer contracts | HC JWT today |
| Organizations | Own | Product org linkage | Product org linkage | PlatformOrganizationId | |
| Memberships | Own (multi-product) | HC staff membership | Store/product roles | Membership DTO | HC today: one staff membership / user |
| Tenant context | Rules + APIs | Enforce in HC | Enforce in POS | Server-derived scope | Never trust client org IDs |
| Roles / permissions | Platform roles | Clinical roles | Retail roles | Permission catalog shape | Separate catalogs |
| Platform administration | Own (**native UI**) | HC platform-admin *role* in Staff Web only | — | Admin APIs | No Ant in new admin |
| Audit / security events | Platform audit | Clinical + HC org audit | POS audit | Event schemas | PHI stays in HC |
| Validation / ProblemDetails / pagination | Patterns → shared later | Existing | New | DTO shapes | |
| BFF / session patterns | May use for Admin | Staff/Patient BFF | MAUI session | Pattern only | |
| Background jobs | Platform jobs later | Hangfire HC jobs | POS jobs later | — | Don’t share HC reminder jobs |
| Notifications (email) | Platform mail later | Dev capture today | POS notifications | Interfaces | |
| Products / plans / trials | Own | Consumes | Consumes | Product/Plan IDs | Missing today |
| Subscriptions / payments | Own | Consumes | Consumes | Subscription status | Missing today |
| Entitlements / overrides | Own + publish | Local snapshot | Local snapshot | EntitlementSnapshot | Fail-safe required |
| Billing | Own | — | — | Invoice later | Phase 3 |
| Localization / themes / density | Admin native UI | HC keep current | POS native UI | Token names, `en`/`fil` | ADR-010 |
| Tables / date / motion components | Native library | Ant (Staff) / native (Patient/Mobile) | Native library | Models only | No Ant sharing |
| Clinics / doctors / patients / appointments / notes | — | Own | — | — | Never rename to POS |
| Patient self-scope | — | Own | — | — | Not a POS rule |
| Stores / branches / registers | — | — | Own | — | |
| Customers / CustomerCredit / ledgers | — | — | Own | — | Not Patient |
| Products / barcodes / sales / inventory | — | — | Own | — | Generic retail |
| Expenses / suppliers / purchasing | — | — | Own | Basic Store / Full POS | |
| Shifts / returns / multi-register | — | — | Own | Full POS (Phase 10 complete) | |
| Offline DB / sync state | — | — | Own | Sync contracts later | Phase 7 |
| Offline synchronization | — | — | Own | Idempotency rules | |
| Ant Design Blazor | — | Staff Web only | Forbidden | — | Forbidden in Platform Admin |
| Tailwind | Forbidden | — | Forbidden | — | |

## Entitlement behavior (approved direction)

Product APIs use a **local entitlement projection/snapshot** with version, effective timestamp, refresh/expiry policy, fail-safe behavior, grace period, and audit trail. Daily operations must not fail solely because Platform is temporarily unavailable.

## Identity mapping

Cross-product correlation uses Platform user and organization identifiers. Products may store projections; they must not own the global identity source of truth after extraction.
