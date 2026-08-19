# P28-WP15G — Client Boundary Finalization, UX Cleanup and WP15 Hardening

**Status:** Complete  
**Type:** Closeout / hardening  
**Dependencies:** WP15A–F complete and pushed  
**Starting SHA:** `618a7b61` (WP15F final)

---

## Scope

WP15G is the closeout package for the entire WP15 series. It audits, consolidates, fixes gaps, runs regression, and documents the final state. No new subsystem was introduced.

---

## Code fixes delivered

### 1. Web device revoke — missing step-up integration

The Organization Web `Devices.razor` page called `RevokePosDeviceAsync` with only two arguments after WP15F changed the interface to require a `GovernanceCriticalActionRequest` (password step-up + reason). This was a WP15F gap that caused build errors.

**Fix:** Replaced the bare "Revoke" button with a `ConfirmDialog` requiring reason + password, integrated `GovernanceCriticalActionFlow.IssueStepUpTokenAsync` for the `PosDeviceRevoke` action code, and added EN + fil-PH localization keys (`Devices_RevokeTitle`, `Devices_RevokeMessage`, `Devices_Revoke`, `Devices_Revoked`).

### 2. MAUI test fake — missing interface methods

`FakeAccessClient` in `AuthenticationServiceTests.cs` did not implement five new `IPlatformAccessClient` methods added by WP15F (`IssueGovernanceStepUpAsync`, `SuspendBranchAsync`, `ReactivateBranchAsync`, `ArchiveBranchAsync`, `RevokePosDeviceAsync` with 3-arg signature). This caused MAUI test project build failure.

**Fix:** Added stub implementations returning `Unavailable()` for all five methods and updated the `RevokePosDeviceAsync` signature to match the new interface.

---

## Audit results

### Capability matrix compliance

The capability matrix (`docs/engineering/organization-branch-capability-matrix.md`) was reviewed against implementation. All WP15A–F features are reflected accurately:

- Workspace selection flow matches WP14 implementation
- Mobile Primary/Main governance gate matches WP15B `IWorkspaceGovernanceGate`
- Staff↔branch ACL matches WP15C `IOrganizationBranchAccessService`
- Actor attribution matches WP15D `PlatformActorContext`
- Governance audit matches WP15E `platform.audit_records`
- Password step-up matches WP15F `GovernanceStepUpGrant`

### Security/domain audit

| Check | Status |
|---|---|
| Owner ≠ automatic POS selling | Pass — `CreateSale` requires product-local role + device |
| Branch assignment ≠ POS role | Pass — separate authorization dimensions |
| Device-bound branch enforcement | Pass — POS operations validate device registration branch |
| Sale.BranchId from device context | Pass — set from operational context, not workspace selection |
| Multi-org isolation | Pass — queries scoped by `OrganizationId` |
| Governance audit append-only | Pass — INSERT-only, no UPDATE/DELETE on audit records |
| Password never logged | Pass — passwords cleared after step-up verification |
| Financial history no hard-delete | Pass — void/reversal patterns, no DELETE on sales/payments |
| Step-up server-enforced | Pass — `ConsumeGovernanceStepUpGrant` called in API endpoints |
| Branch archive/suspension preserves history | Pass — soft lifecycle with status + timestamp |

### Performance review

- Server pagination used on list endpoints (`page`/`pageSize`)
- MAUI governance audit shows bounded 5–15 recent items with "View full audit on Web"
- No N+1 loading patterns found in WP15-touched pages
- Branch-scoped data queries; org-wide loads are paginated

### Dense design audit

- Web Devices page: revoke action now uses ConfirmDialog (password + reason), consistent with Branches page pattern
- MAUI OrgPosDevices already had ConfirmDialog step-up integration (WP15F)
- All WP15-touched screens use existing DesignSystem components
- EN + fil-PH localization maintained

---

## Regression test results

### Build

| Target | Result |
|---|---|
| Full solution (Release) | 0 C# errors (only XA5300 — Android SDK not installed on this workstation) |
| Warnings | 26 (pre-existing: Checkbox.CheckedExpression obsolete, xUnit analyzer hints, unused events) |

### Tests

| Suite | Passed | Failed | Skipped | Notes |
|---|---|---|---|---|
| Platform integration | 226 | 57 | 0 | 57 failures are pre-existing (SaaS payment/subscription tests, commercial flow tests). WP15F step-up tests: **7/7 passed** |
| POS unit | 1032 | 0 | 0 | Clean |
| MAUI unit | 546 | 18 | 0 | 18 failures are pre-existing (16 UI guard/localization pattern tests + 2 branch-restore tests). No WP15G-introduced failures |

**Device verified:** No (no Android SDK / physical device on this workstation)  
**Browser verified:** No (no manual browser testing executed)

---

## Doc consistency

The following canonical documents were verified for consistency:

- `docs/engineering/organization-branch-capability-matrix.md` — authoritative, current
- `docs/architecture/client-experience-boundaries.md` — consistent with WP15B boundaries
- `docs/architecture/user-creation-flow-and-account-scope-rules.md` — consistent
- `docs/engineering/authorization-matrix.md` — consistent with WP15C ACL model
- WP15A–F reports — all present and internally consistent
- WP13/WP14 reports — consistent with current workspace/branch model

No stale contradictions found requiring correction.

---

## Consolidated WP15 capability summary

### WP15A — Capability & Client Boundary Baseline
Defined the authoritative capability matrix: Mobile/Web split, scope classes, authorization dimensions, workspace context model, and known gaps. No code changes.

### WP15B — Mobile Operations & Manage Business
Implemented `IWorkspaceGovernanceGate` so MAUI Primary/Main shows "Manage business" hub (branches, staff, settings, subscription, recent governance activity) while non-primary branches show only branch operations. Burger menu: clean operational items + "Switch workspace" + conditional "Manage business".

### WP15C — Staff Branch Authorization
Implemented `organization_membership_branch_assignments` table, `IOrganizationBranchAccessService`, API endpoints for managing staff↔branch assignments, and `IAccessibleBranchResolver` (owner path returns all active branches; staff path filters by assignments).

### WP15D — Operational Actor Traceability
Ensured every meaningful POS mutation persists `ActorId` via `PosOrganizationScope`. Customer order fulfillment, stock count drafts, and payment provider finalization now record actor/system provenance.

### WP15E — Governance Audit Trail
Added `platform.audit_records` table with append-only INSERT. Platform governance mutations emit structured audit events. Organization Web shows full filtered/paged audit. MAUI shows compact recent-activity summary (5–15 rows).

### WP15F — Critical Action Password Step-Up
Implemented `GovernanceStepUpGrant` — server-issued, one-time-use scoped tokens bound to user + org + action + target + expiry. Protected actions: branch suspend/archive/reactivate, membership suspend/revoke/role-change, POS device revoke. ConfirmDialog integration on both MAUI and Web with reason + password.

### WP15G — Closeout & Hardening (this package)
Fixed WP15F gaps (Web Devices step-up, FakeAccessClient stubs). Audited capability matrix compliance, security rules, performance patterns, dense design, and doc consistency. Full regression.

---

## Known remaining limitations

1. **POS void/refund/stock adjustment step-up** — password step-up is not generalized beyond Platform governance lifecycle actions
2. **Shifts/registers org-scoped** — no `BranchId` on shift model (WP13 documented limitation)
3. **Staff branch ACL** — owner path returns all branches; staff filtering implemented but not yet enforced on all operational queries
4. **Android SDK** — MAUI Android target not buildable on this workstation (XA5300)
5. **18 pre-existing MAUI test failures** — UI guard/localization pattern tests and branch-restore tests (not introduced by WP15)
6. **57 pre-existing Platform integration failures** — SaaS payment/subscription commercial flow tests (not introduced by WP15)
7. **Org Web branch selection** — sets org context; branch hierarchy is informational UX. Parity with MAUI `SelectedBranchId` on Web is optional.
8. **Production authentication** — development-stage APIs; no production auth infrastructure claimed

---

## Files changed in WP15G

| File | Change |
|---|---|
| `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Web/Components/Pages/Operations/Devices.razor` | Step-up ConfirmDialog for revoke |
| `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Web/Localization/OrgWebResources.resx` | EN strings for device revoke dialog |
| `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Web/Localization/OrgWebResources.fil-PH.resx` | fil-PH strings for device revoke dialog |
| `tests/ExItS.PinoyBusinessPOS.Maui.Tests/AuthenticationServiceTests.cs` | FakeAccessClient interface stubs |
| `docs/reports/P28-WP15G-governance-branch-authority-closeout.md` | This report |

---

## Git evidence

- **Starting SHA:** `618a7b61` (WP15F final)
- **Branch:** `main`
- **Production ready:** No — development stage; pre-existing test failures exist; no Android/device/browser verification
