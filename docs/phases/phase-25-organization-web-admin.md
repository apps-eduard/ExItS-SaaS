# Phase 25 — Organization Web Admin, AntDesign hosts, and unified web auth

[Phases](README.md) | [Portfolio](../portfolio-progress.md) | [P25-WP01](../reports/P25-WP01-organization-web-admin-management-center.md) | [P25-WP02](../reports/P25-WP02-antdesign-web-standardization-and-host-separation.md) | [P25-WP03](../reports/P25-WP03-unified-web-authentication-sso-and-workspace-routing.md) | [P25-WP04](../reports/P25-WP04-web-host-legacy-cleanup-and-local-validation-identity-determinism.md) | [P25-WP05](../reports/P25-WP05-cash-count-policy-simplification-and-denomination-assisted-reconciliation.md) | [P25-WP06](../reports/P25-WP06-personal-organization-identity-isolation.md) | [P25-WP07](../reports/P25-WP07-sales-buyer-party-isolation.md) | [P25-WP08](../reports/P25-WP08-organization-profile-independence.md) | [P25-WP09](../reports/P25-WP09-organization-ownership-transfer.md) | [ADR-022](../decisions/ADR-022-separated-antdesign-web-hosts-and-unified-auth.md)

| Field | Value |
|---|---|
| Status | **Open** — WP01–WP09 Code Complete / Owner Validation Pending; Connected ExItS supplier request lifecycle visibility Code Complete ([report](../reports/connected-supplier-connection-request-lifecycle.md)) |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |
| Boundary | **Organization Web Admin is not a POS checkout client.** |
| Closeout | **Not started** — do **not** create P25-WP10 / Phase 25 closeout until the owner completes device/browser validation |

## Purpose

Deliver a professional Organization Web Admin for **management, control, and reporting**, plus identity/organization management hardening (typed QR, buyer-party isolation, independent org profiles, ownership transfer). Operational selling stays in the POS/MAUI experience.

## Web vs POS

| Surface | Allowed |
|---|---|
| Organization Web | Profile, branches, staff/roles, catalog, inventory management, customers, devices/registers, shift inspection, reports, settings, subscription (read), notifications, **ownership transfer** |
| Organization Web sales | **Read-only** history, receipt detail, aggregates |
| POS / MAUI | Checkout, cart, barcode selling, payment-taking, cashier sale creation, open/close shift as cashier work; Personal accept/decline for ownership transfer |

## Navigation

Overview · Business (profile / branches / devices) · People (staff / customers) · Catalog · Inventory · Sales (history / reports / business credit) · Operations (shifts / reports) · Settings (operational / sales documents / ownership / subscription)

Unauthorized sections are hidden in navigation. Cashier POS role is denied the host (`CanAccessOrganizationWeb`). Server APIs remain authoritative.

Role/workflow matrix: [organization-web-role-and-workflow-matrix.md](../engineering/organization-web-role-and-workflow-matrix.md).  
UI / responsive / Development Test User: [organization-web-ui-responsive-standard.md](../engineering/organization-web-ui-responsive-standard.md).  
Full responsive UX pass: [P25-org-web-full-responsive-ux-completion.md](../reports/P25-org-web-full-responsive-ux-completion.md).  
Owner checklist (unchecked): [organization-web-responsive-owner-checklist.md](../validation/organization-web-responsive-owner-checklist.md).

### Authentication & routing (remediation)

- Development **Test User** fills username only; password is manual; normal credentials login.
- Post-login: Platform → Org Web (Owner / Organization Administrator) → Personal; multi-org → chooser; Cashier org workspaces excluded.
- Organization Owner has **full Organization management authority** for the selected Organization (Platform membership + ownership). POS management APIs use `OrganizationManagementAuthority` from membership (commercial entitlement is feature-level); **CreateSale / EnterPos remain denied** unless a separate product-local selling role is assigned. Owners do **not** need `view_portfolio`.
- Owner API authorization fix reports: [P25-owner-organization-management-authority-fix.md](../reports/P25-owner-organization-management-authority-fix.md), [P25-org-web-runtime-owner-auth-and-icon-nav-remediation.md](../reports/P25-org-web-runtime-owner-auth-and-icon-nav-remediation.md).
- Management pages share `OrgAlert` / `OrgLoading` / `OrgEmpty` / localized headers; mobile drawer covers all sections.
- Phase 25 remains **OPEN** (no closeout).


## Performance / bandwidth

- Dashboard uses one POS SQL aggregate (`GET /api/v1/pos/management/overview`) plus a few bounded Platform counts.
- Lists are paged (default 20).
- Reports use server-side aggregation, not browser-side full-history loads.
- No Redis added for this phase.

## Owner acceptance

Owner device/browser/real-world validation is **required** before Phase 25 can close. Until then Phase 25 remains **OPEN**.

## Work packages

| WP | Scope | Status |
|---|---|---|
| [P25-WP01](../reports/P25-WP01-organization-web-admin-management-center.md) | Organization Web management center (original DesignSystem host) | Code Complete / Owner Validation Pending |
| [P25-WP02](../reports/P25-WP02-antdesign-web-standardization-and-host-separation.md) | AntDesign standardization; Org/Personal/Admin host split | Code Complete / Owner Validation Pending |
| [P25-WP03](../reports/P25-WP03-unified-web-authentication-sso-and-workspace-routing.md) | Canonical sign-in, SSO handoff, workspace routing | Code Complete / Owner Validation Pending |
| [P25-WP04](../reports/P25-WP04-web-host-legacy-cleanup-and-local-validation-identity-determinism.md) | Web host legacy cleanup and Local Validation identity determinism | Code Complete / Owner Validation Pending |
| [P25-WP05](../reports/P25-WP05-cash-count-policy-simplification-and-denomination-assisted-reconciliation.md) | Cash count policy simplification and denomination-assisted reconciliation | Code Complete / Owner Validation Pending |
| [P25-WP06](../reports/P25-WP06-personal-organization-identity-isolation.md) | Personal / Organization identity isolation + typed QR | Code Complete / Owner Validation Pending |
| [P25-WP07](../reports/P25-WP07-sales-buyer-party-isolation.md) | POS sales buyer-party / QR purpose isolation | Code Complete / Owner Validation Pending |
| [P25-WP08](../reports/P25-WP08-organization-profile-independence.md) | Organization profile independence + multi-org ownership | Code Complete / Owner Validation Pending |
| [P25-WP09](../reports/P25-WP09-organization-ownership-transfer.md) | Organization ownership transfer | Code Complete / Owner Validation Pending |

**No P25-WP10 closeout in this phase until the owner explicitly closes Phase 25.**

Privacy inventory for identity/QR/ownership/buyer-party processing: [P21-WP11](../reports/P21-WP11-post-phase21-privacy-impact-refresh.md) / [post-phase21-privacy-impact-refresh.md](../compliance/post-phase21-privacy-impact-refresh.md) (Phase 21 remains **OPEN**; NPC compliance **not claimed**).

Local ports: **8090** Admin, **8091** Platform API, **8092** POS API, **8093** Org Web, **8094** Personal Web. Production public entry is HTTPS :443 via reverse proxy.

Owner browser checklist is in [P25-WP04](../reports/P25-WP04-web-host-legacy-cleanup-and-local-validation-identity-determinism.md) (SSO items also in [P25-WP03](../reports/P25-WP03-unified-web-authentication-sso-and-workspace-routing.md)). Cash count owner checklist is in [pos-cashier-cash-count.md](../engineering/pos-cashier-cash-count.md). Ownership transfer engineering note: [organization-ownership-transfer.md](../engineering/organization-ownership-transfer.md). Do not mark Device Verified or Production Ready until the owner validates.

## Future roadmap note (moved to Phase 26)

Sales Documents & Compliance Readiness is now **Phase 26** ([phase](phase-26-sales-documents-compliance-readiness.md); [roadmap](../compliance/bir-compliance-activation-roadmap.md)). Phase 25 remains **OPEN** for owner validation and is independent of Phase 26. **No BIR compliance claim.** Do not implement Phase 26 work inside P25 packages.

## Recorded hashes (selected)

| WP | Kind | SHA |
|---|---|---|
| P25-WP05 | Denomination default refinement | `a50413bc` |
| P25-WP06 | Feat / docs | `3e1515b9` · `8f3875f5` · `66a972f8` |
| P25-WP07 | Feat / docs | `95f5744b` · `710426a9` |
| P25-WP08 | Feat / docs | `a3dfda28` · `5fd997e0` |
| P25-WP09 | Feat / docs | `67bd59bd` · `f20b6dc3` · `5f51e35a` |
