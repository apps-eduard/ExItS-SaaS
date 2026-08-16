# P26-WP06 — BIR Registration Profile and Activation Readiness

## Status

**Code Complete / Owner + Regulatory Validation Pending.** Phase 26 remains **OPEN**. Phase 25 remains **OPEN**. This is **not** phase closeout. No Device / Browser / Production Ready claim. This work package makes **no** BIR-compliance claim and does **not** produce `TaxDocument` records.

| Field | Value |
|---|---|
| Starting SHA | `1f81cb637dda648fbe56ba78dfaf69e5115b821f` |
| Feature SHA | **TBD** (do not commit in this pass unless owner requests) |
| Device Verified | **No** |
| Browser Verified | **No** |
| Regulatory Validated | **No** |
| Production Ready | **No** |
| BIR Certified / Compliant claim | **No** |
| TaxDocument runtime available | **No** (`TaxDocumentIssuanceRuntime.ImplementationAvailable = false`) |

Canonical living playbook:
[docs/compliance/bir-compliance-activation-roadmap.md](../compliance/bir-compliance-activation-roadmap.md).

Authoritative sources:
[docs/compliance/bir-authoritative-source-register.md](../compliance/bir-authoritative-source-register.md).

Design:
[docs/engineering/bir-registration-readiness-and-activation.md](../engineering/bir-registration-readiness-and-activation.md).

## Delivered capability

### Domain / Platform API (pre-existing for this UI pass; included in WP06 scope)

- Registered taxpayer name + normalized TIN; DTOs expose **MaskedTin** only
- Branch compliance profiles
- Compliance registration records + Platform review (Accept/Reject for readiness)
- GET readiness / submit readiness
- Readiness evaluator keeps activation blocked while TaxDocument runtime is unavailable

### Organization Web

- Dense `/organization/tax-compliance` page (Owner/Manager view; Owner mutate)
- Sections: Business registration, Branches, Registrations, ExItS status
- Nav + Sales Documents link; cashier gated out

### Platform Admin

- Organizations Compliance tab: MaskedTin, registered name, readiness checklist, branch profiles, registration Accept/Reject
- Issuance enable control remains present but disabled while runtime unavailable; wording avoids BIR certification claims

### MAUI

- Owner-only compact `/organization/tax-compliance` summary
- MoreHub **Business** section link
- EN + fil-PH `TaxCompliance_*` keys
- Deep-link note that detailed admin is on Organization Web

### Guards / docs

- `BirComplianceUiGuardTests` + extended Phase26 wording guards
- Source register, readiness design, roadmap CONFIRMED/IMPLEMENTED/… sections, profile doc, phase page, indexes

## Persistence and migration

Migration: `20260816110906_AddBirRegistrationReadinessProfiles` (Platform). LocalStore / POS schema unchanged. No auto-enable of issuance.

## Explicit exclusions

- No TaxDocument generation, numbering, invoice layout, or fiscal memory
- No “BIR Compliant / Certified / Accredited” UI copy
- No phase closeout
- No Device / Browser / Production Ready claim
- No NPC compliance claim

## Validation evidence

| Check | Result |
|---|---|
| `dotnet build` Platform.Admin Release | **Succeeded** (13 pre-existing CS0618 Checkbox warnings) |
| `dotnet build` PinoyBusinessPOS.Web Release | **Succeeded** (0 warnings / 0 errors) |
| Maui.Tests filter `Phase26\|BirCompliance\|TaxCompliance` | **5 passed** / 0 failed / 0 skipped |
| Platform.UnitTests filter `BirRegistration` | **12 passed** / 0 failed / 0 skipped |

## Risks and open decisions

- Feature commit hash **TBD** until owner authorizes commit/push
- Owner + regulatory validation outstanding
- Migration apply/rollback/re-apply on authorized non-production PostgreSQL pending
- MIN association remains FUTURE

## Documentation changed

- Created: source register, readiness design, this report
- Updated: roadmap, organization-compliance-profile, phase-26, index/reports/phases/portfolio/FILE-MANIFEST, privacy refresh TIN note

## Next

**Owner + regulatory validation.** Do not enable TaxDocument runtime. Phase 26 remains **OPEN**.

## Privacy Impact

| Field | Value |
|---|---|
| Personal data changed? | **Yes** — registered taxpayer name; TIN (normalized at rest; masked on DTOs) |
| Data subjects | Organization Owners; Platform reviewers; limited Org Manager view |
| Purpose | BIR registration readiness for future ExItS TaxDocument activation |
| Data categories | **RESTRICTED COMPLIANCE** (TIN, registration references) |
| New exposure/access | Org Web / Admin / MAUI Owner summary; **not** Public QR; cashiers excluded |
| Retention impact | Organization lifetime — **RETENTION PERIOD REQUIRES LEGAL/DPO CONFIRMATION** |
| Offline/local impact | No LocalStore replication of TIN/evidence |
| Third-party/vendor impact | None new (future evidence storage still deferred) |
| Security controls | MaskedTin only on DTOs; Owner mutate; Platform review; no BIR certification wording |
| PIA/ROPA update required? | **Yes** (extend P21-WP11 inventory) |
| Legal/DPO review required? | **Yes** |

See [post-phase21-privacy-impact-refresh.md](../compliance/post-phase21-privacy-impact-refresh.md). **NPC compliance NOT CLAIMED.**
