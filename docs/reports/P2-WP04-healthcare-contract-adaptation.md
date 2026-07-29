# P2-WP04 — HealthCare Contract Adaptation

## 1. Assignment

| Field | Value |
|---|---|
| Phase | Phase 2 — Platform Extraction and HealthCare Reconnection |
| Work package | P2-WP04 — HealthCare Contract Adaptation |
| Status | Ready for Review |
| Branch | `main` |
| Date | 2026-07-29 |

## 2. Summary

Created Platform-side, transport-independent contract and adapter **foundation** for future HealthCare reconnection: versioned envelopes, identity/membership/organization-mapping/product-access/subscription/entitlement projections, projection apply policy (idempotency/ordering/conflict/gap), checkpoint + reconciliation abstractions, and HealthCare delivery interfaces. **No** HealthCare modifications, transport, persistence, messaging, HTTP delivery, or business API routes.

## 3. Contracts created

| Contract | Purpose |
|---|---|
| `ContractEnvelope<T>` | Versioned immutable envelope (MessageId, CorrelationId, UTC times, source version) |
| `ContractVersion` | Positive major (+ optional minor); unsupported majors fail closed |
| `PlatformUserProjection` | User id, display name, email, account status, source version |
| `OrganizationMembershipProjection` | Org/user, membership status, **Platform** org role only |
| `OrganizationMappingProjection` | Explicit reversible Platform org ↔ opaque external org/clinic id (1→many) |
| `ProductAccessProjection` | Product access ≠ entitlement ≠ clinical permission |
| `SubscriptionProjection` | Commercial fields only (no payment secrets) |
| `EntitlementSnapshotProjection` + `FeatureGrantProjection` | Snapshot grants for local projection |

## 4. Intentionally excluded fields

Passwords, refresh tokens, MFA, cookies, Patient/MedicalNote/Diagnosis/Prescription, clinical roles, payment instruments, POS/Utang transaction details, broker/HTTP headers.

## 5. Projection application rules

| Case | Outcome |
|---|---|
| Same Message ID | `DuplicateIgnored` |
| Lower source version | `OlderVersionIgnored` |
| Same version, different Message ID | `Conflict` → ReconciliationRequired state |
| Next sequential version | `Applied` (+ checkpoint save) |
| Version gap | `VersionGapDetected` (unless reconciliation snapshot) |
| Unsupported major | `UnsupportedVersion` (no grant) |
| Reconciliation snapshot | May bridge gap / replace commercial checkpoint only |

## 6. Adapter / reconciliation boundary

**Interfaces only (no implementation):**  
`IHealthCare*ProjectionDelivery`, `IPlatformProjectionReconciliationService`, `IProjectionCheckpointStore`

**Use cases:** `ProjectionContractBuilders`, `EvaluateProjectionApplicability`, `RequestProjectionReconciliation` (returns `SourceUnavailable` until transport WP).

## 7. Packages / API

- No new NuGet packages.
- Routes: `GET /`, `GET /health` only; phase `P2-WP04-healthcare-contract-adaptation`; port **5288**.

## 8. Tests

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| ExItS.Platform.UnitTests | 85 | 0 | 0 |
| ExItS.ArchitectureTests | 19 | 0 | 0 |
| **Total** | **104** | **0** | **0** |

| Command | Exit |
|---|---:|
| `dotnet restore ExItS.slnx` | 0 |
| `dotnet build ExItS.slnx -c Release` | 0 (0 warnings/errors) |
| `dotnet test ExItS.slnx -c Release` | 0 |

## 9. HealthCare freeze

`git ls-files HealthCare` empty; ignored; not in solution; not moved; unchanged. Contracts ≠ completed integration.

## 10. Risks

- R-016 remote empty; R-020 Integration/E2E before cutover; R-026 premature import still mitigated.
- **R-036** contract major skew / unsupported version at runtime.
- **R-037** projection version gaps / duplicate-conflict handling until transport exists.
- **R-038** organization mapping errors (1 Platform org → many clinics).
- **R-039** accidental role escalation if consumers misread Platform org roles as clinical roles.
- Contracts do **not** equal HealthCare integration complete.

## 11. Next work package

**P2-WP05 — Regression and Migration Validation** (do not begin until authorized).

## 12. Commit

| Field | Value |
|---|---|
| Hash | _(recorded after commit)_ |
| Message | `feat(platform): add healthcare contract adaptation boundary` |
