# P21 — Privacy readiness visibility / product status UI

| Field | Value |
|---|---|
| Status | **Code Complete / Validation Pending** |
| Starting SHA | `57e155ec` |
| Feature SHA | `da26870d` |
| Phase 21 | **OPEN** — Legal/DPO/Owner Validation Pending |
| Legal / NPC Compliant | **No — not claimed** |
| Browser Verified | **No** |
| Device Verified | **No** |
| Production Ready | **No** |

## Delivered

### Platform Admin (`/admin/privacy-compliance`)

- Operational readiness banner and derived overall status (`PrivacyReadinessOverallStatus`)
- Summary cards: Requirements, Ready, Action needed, External/legal review, Evidence coverage
- Category readiness table with links into existing detail routes
- Privacy-impact follow-ups (P25/P26 catalog items)
- Status badges use “Verified internally” instead of green “Compliant”
- Explicit disclaimer: readiness tooling only — not legal/NPC certification

### Organization Web (`/settings/privacy`)

- Owner/Manager view of technical safeguards, business actions, responsibility split, legal/NPC “Not verified”
- Safe POS projection `OrganizationPrivacyReadinessDto` via `GET /api/v1/pos/privacy-readiness`
- Does **not** call `/api/v1/platform/privacy-compliance/*`

### MAUI (`/org/privacy`)

- More → Privacy & Data Protection
- Owner dashboard compact “Privacy setup needs attention” when actionable items exist
- Same DTO / legal semantics as Organization Web

## Explicit non-claims

UI and resources do not say “Privacy compliant”, “NPC compliant”, “Fully compliant”, or “Certified by NPC”.

## Reused Phase 21 model

- `ComplianceRequirement` / `ComplianceItemStatus` / `EnsurePrivacyComplianceCatalog`
- Extended overview via `PrivacyReadinessDerivation` + enriched `PrivacyComplianceOverviewDto`
