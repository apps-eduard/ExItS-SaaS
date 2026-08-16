# BIR Registration Readiness and Activation (ExItS)

> Engineering design for **P26-WP06**. Not a BIR compliance certificate. Not TaxDocument issuance.

Canonical sources: [authoritative source register](../compliance/bir-authoritative-source-register.md) ·
[activation roadmap](../compliance/bir-compliance-activation-roadmap.md) ·
[organization compliance profile](organization-compliance-profile.md)

## Purpose

Capture organization and branch **registration readiness** so Platform can evaluate whether ExItS may eventually enable TaxDocument capability — **without** inventing regulatory facts or enabling issuance while runtime is unavailable.

## Scope (WP06)

| In scope | Out of scope |
|---|---|
| Registered taxpayer name + TIN (stored normalized; DTO exposes **MaskedTin** only) | Full TIN on any public DTO / QR / cashier UI |
| Branch compliance profiles (`BirBranchCode`, setup status, notes) | Assuming one org profile covers every branch |
| Compliance registration records + Platform Accept/Reject for readiness | Fake document verification, public evidence URLs |
| Readiness evaluation + submit-for-review | TaxDocument generation / numbering / fiscal memory |
| Org Web dense admin + MAUI Owner compact summary + Admin review UI | Cashier access |

## Domain model

### Organization compliance profile (extended)

- `RegisteredTaxpayerName`
- `TinNormalized` (9 digits; never returned on DTOs)
- `MaskedTin` derived (`***-***-123`)
- `SetupStatus` lifecycle (`NotConfigured` … `ActivationBlocked` / `Activated`)

### Branch compliance profile

- One row per organization branch (optional until readiness requires codes)
- `BirBranchCode`, `SetupStatus` (branch subset), `Notes`

### Compliance registration record

- Types: `PosPermitToUse`, `CasRegistration`, `EisCertification`, `EisPermitToTransmit`, `Other`
- Statuses include Owner-mutable (`Provided`, `UnderReview`, …) and Platform-only (`AcceptedForReadiness`, `RejectedForReadiness`)
- Accept/Reject is **ExItS readiness** acceptance — **not** BIR certification wording

### Readiness evaluator

`ComplianceActivationReadinessEvaluator` checks:

1. Registered taxpayer name
2. Valid TIN
3. BIR branch code on every active branch
4. At least one `PosPermitToUse` with `AcceptedForReadiness`
5. Compliance eligibility `Approved`
6. Current Owner sales-document education acknowledged
7. TaxDocument runtime available

While `TaxDocumentIssuanceRuntime.ImplementationAvailable == false`, overall status becomes `ActivationBlocked` when other items complete, and `IsReadyForTaxDocumentActivation` stays **false**.

Machine Identification Number (MIN) association is explicitly **FUTURE** (warning only).

## Authorization

| Actor | Read profile/readiness/regs | Mutate taxpayer / branch / regs / submit | Accept/Reject registration | Eligibility / issuance capability |
|---|---|---|---|---|
| Cashier | No (Org Web / MAUI gated) | No | No | No |
| Organization Manager | Org Web view | No (API Owner-only for membership mutations) | No | No |
| Organization Owner | Yes | Yes | No | Request review only |
| Platform ManageOrganizations | Yes | Yes | Yes | Yes |

## API surface (Platform)

- `GET .../compliance-profile`
- `PUT .../compliance-profile/registered-taxpayer`
- `GET .../compliance/readiness`
- `POST .../compliance/readiness/submit`
- `GET/PUT .../branches/{branchId}/compliance-profile`
- `GET .../compliance/branch-profiles`
- `GET/POST .../compliance/registration-records`
- `PUT .../compliance/registration-records/{id}`
- `POST .../compliance/registration-records/{id}/review`

## UI surfaces

| Surface | Role |
|---|---|
| Organization Web `/organization/tax-compliance` | Dense Owner/Manager view; Owner edits |
| Platform Admin Organizations → Compliance tab | Masked TIN, readiness, branches, Accept/Reject |
| MAUI `/organization/tax-compliance` | Owner-only compact summary; deep-link note to Org Web |

## Privacy

- TIN is **RESTRICTED COMPLIANCE** data.
- Persist normalized TIN only in Platform DB; expose **MaskedTin** on authorized DTOs.
- Never place TIN on Public Business QR or public identity contracts.
- See [post-phase21 privacy refresh](../compliance/post-phase21-privacy-impact-refresh.md).

## Non-claims

- No “BIR Compliant / Certified / Accredited” product copy.
- Eligibility Approved ≠ TaxDocument.
- Registration AcceptedForReadiness ≠ BIR certification.
- Tax configuration enable ≠ regulatory certification.
- Runtime remains unavailable until a future authorized WP.
