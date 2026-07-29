# Phase 0 — Existing HealthCare Assessment

[Dashboard](../portfolio-progress.md) | [All Phases](README.md) | [Next](phase-01-platform-boundary.md)

## Objective

Understand the completed HealthCare MVP and classify safe reuse before any structural change.

## Work packages

### P0-WP01 — Repository and Reuse Inventory

Status: **Ready for Review** (2026-07-29)

#### Required outcomes

- Discover the existing repository structure without assumptions.
- Inventory reusable and product-specific capabilities.
- Record Ant Design usage and coupling.
- Record exact build/test baseline if safe and available.
- Update the reuse matrix and completion report.

#### Definition of Done

- [x] Approved outcomes complete.
- [x] Applicable tests pass with exact evidence. *(Windows-safe suite 1102 passed; Integration/E2E deferred per HC README.)*
- [x] Dashboard and phase page updated.
- [x] Completion report created.
- [x] Focused commit created and hash recorded. *(see below)*
- [ ] Working tree clean of *unintended* changes. *(`HealthCare/` intentionally untracked.)*

#### Evidence

- Assessment: [docs/reuse/healthcare-reuse-assessment.md](../reuse/healthcare-reuse-assessment.md)
- Matrix: [docs/reuse/reuse-classification-matrix.md](../reuse/reuse-classification-matrix.md)
- Report: [docs/reports/P0-WP01-completion.md](../reports/P0-WP01-completion.md)
- Alias: [docs/reports/P0-WP01-healthcare-reuse-assessment.md](../reports/P0-WP01-healthcare-reuse-assessment.md)

#### Commands run

```powershell
git status
git branch --show-current
dotnet --version
dotnet restore HealthCare.sln
dotnet build HealthCare.sln -c Release
# non-MAUI builds OK; Mobile fails XA5300 without Android SDK
dotnet test tests/HealthCare.UnitTests/HealthCare.UnitTests.csproj --no-build -c Release
dotnet test tests/HealthCare.ArchitectureTests/HealthCare.ArchitectureTests.csproj --no-build -c Release
dotnet test tests/HealthCare.Web.Tests/HealthCare.Web.Tests.csproj --no-build -c Release
dotnet test tests/HealthCare.PatientWeb.Tests/HealthCare.PatientWeb.Tests.csproj --no-build -c Release
dotnet test tests/HealthCare.Mobile.Tests/HealthCare.Mobile.Tests.csproj --no-build -c Release
```

#### Findings

- HealthCare is a completed .NET 10 modular monolith with reusable identity/org/permission/audit/BFF patterns.
- Plans/trials/subscriptions/billing/entitlements are **missing**.
- AntDesign 1.6.2 is staff-Web-only; PatientWeb uses native CSS.
- Nested `HealthCare/.git` and local `.env` files require careful monorepo onboarding.
- Verdict: **controlled platform extraction** after Phase 0 closeout — not wholesale move.

#### Risks

Recorded as R-010…R-014 in [risks-and-issues.md](../risks-and-issues.md).

#### Deferred actions

- Integration + E2E baselines (P0-WP02 / Ubuntu guidance).
- Nested Git disposition and root `.gitignore` (docs/ops decision; no silent delete).
- Android SDK for full solution build.
- Do **not** extract Platform code in this WP.

#### Commit

| Field | Value |
|---|---|
| Hash | `663b5bf3269ee934d107bacc467d253a4bf28a90` |
| Message | `docs(platform): assess healthcare SaaS reuse` |

### P0-WP02 — Baseline Build, Tests and Runtime Map

Status: Not Started

#### Required outcomes

- Implement only the approved scope described by the architecture and product documents.
- Add required tests and documentation evidence.
- Preserve security, tenant isolation and product boundaries.

#### Definition of Done

- [ ] Approved outcomes complete.
- [ ] Applicable tests pass with exact evidence.
- [ ] Dashboard and phase page updated.
- [ ] Completion report created.
- [ ] Focused commit created and hash recorded.
- [ ] Working tree clean.

### P0-WP03 — Ant Design and UI Reuse Review

Status: Not Started

#### Required outcomes

- Implement only the approved scope described by the architecture and product documents.
- Add required tests and documentation evidence.
- Preserve security, tenant isolation and product boundaries.

#### Definition of Done

- [ ] Approved outcomes complete.
- [ ] Applicable tests pass with exact evidence.
- [ ] Dashboard and phase page updated.
- [ ] Completion report created.
- [ ] Focused commit created and hash recorded.
- [ ] Working tree clean.

### P0-WP04 — Assessment Closeout and Recommendation

Status: Not Started

#### Required outcomes

- Implement only the approved scope described by the architecture and product documents.
- Add required tests and documentation evidence.
- Preserve security, tenant isolation and product boundaries.

#### Definition of Done

- [ ] Approved outcomes complete.
- [ ] Applicable tests pass with exact evidence.
- [ ] Dashboard and phase page updated.
- [ ] Completion report created.
- [ ] Focused commit created and hash recorded.
- [ ] Working tree clean.

## Phase exit criteria

- [ ] Every work package is complete or explicitly deferred.
- [ ] Risks and decisions are recorded.
- [ ] Required regression/security tests pass.
- [ ] Next phase is explicitly approved.

**Phase 0 is not complete** — only P0-WP01 is ready for review.
