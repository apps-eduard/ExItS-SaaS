# PLM-D3-PRE — Product registration + current-session product access

**Package:** PLM-D3-PRE  
**Date:** 2026-08-20  
**Branch:** `feat/plm-react-client`  
**Starting SHA:** `37f2882e5279f29a8f96da86a39f0286102eee4b`

Resolves the verified blocker that prevented PLM-CLIENT-GATE-D3. Registers `pinoy-loan-manager` as the final Platform product code, seeds a Local Validation-only commercial fixture, and exposes a server-authoritative current-session product-access check. Does **not** implement Gate D3 React (organization selector / product-access UI). Does **not** start Gate E.

---

## Status

| Item | Status |
|---|---|
| PLM-D-00-01 | **CLOSED / PRODUCT OWNER APPROVED** (2026-08-20) |
| Final product code | `pinoy-loan-manager` |
| Local Validation catalog | Idempotent ACTIVE product `pinoy-loan-manager` |
| Local Validation PLM commercial fixture | Independent of POS; ABC allowed / XYZ denied |
| Current-session access API | Implemented |
| Privileged `GET /api/v1/platform/access/evaluate` | **UNCHANGED** |
| D-P12-03 | **OPEN** |
| Gate D3 React | **NOT STARTED** |
| Gate E | **NOT STARTED** |
| Loan features | **ABSENT** |
| Capacitor | **ABSENT** |
| PLM DB / migrations | **NONE** |
| PinoyBusinessPOS source | **UNCHANGED** |

---

## Product Owner decision recorded

**PLM-D-00-01 FINAL PRODUCT CODE / SLUG**

- Decision: `pinoy-loan-manager`
- Status: CLOSED / PRODUCT OWNER APPROVED
- Approval date: 2026-08-20

This closes **only** the product-code decision. It does **not** close D-P12-03, R-091, D-P12-05, PLM-D-00-02, PLM-D-00-04 through PLM-D-00-08, or PLM-D-00-11 through PLM-D-00-13. No production pricing, plans, grants, loan rules, persistence, or legal/compliance policy is approved by this package.

---

## D-P12-03 remains OPEN

The new API is a **browser / current-session Platform access check**.

It is **not** the final Platform → PLM product-server commercial-state transport.

D-P12-03 remains required before PLM backend services consume commercial state in Production. This package does **not** invent a D-P12-03 workaround (no product-server headers, no Platform EF/SQL from PLM, no production commercial-state contract).

---

## Delivered

### Product identity

- `ProductCode.PinoyLoanManager = "pinoy-loan-manager"`
- Independent of `ProductCode.PinoyBusinessPos`
- `ProductCode.Create(...)` generic behavior unchanged

### Platform catalog (existing `CreateProduct` / `IProductRepository`)

- Local Validation registers an ACTIVE catalog entry:
  - code: `pinoy-loan-manager`
  - display name: Pinoy Loan Manager
- Idempotent; Production does not silently auto-create commercial data (`LocalValidation:Enabled` only)

### Local Validation commercial fixture (test-only)

Unmistakably named **PLM Local Validation** (`plm-local-validation`).

Not production commercial policy. Plan name, CreatePlan default zero price, 14-day trial duration (framework-required positive duration matching CreatePlan `defaultTrialDays`), and empty entitlement grants are schema values required by existing Platform primitives. They do **not** close pricing, trial, or PLM-D-00-06 grant-identifier decisions.

Empty product-local grants are used because `ProductAccessEligibility.CanEnterProduct` for non-POS products requires only Trialing/Active subscription status. No Owner/Manager/Cashier/Collector grant codes were invented.

**ALLOWED fixture:** ABC Sari-Sari Store members (`maria-santos`, `carlo-reyes`) receive independent PLM product-access assignment + PLM trial subscription + current PLM entitlement snapshot.

**DENIED fixture:** XYZ Mini Grocery members retain org membership and POS access but receive **no** PLM assignment/commercial eligibility.

POS subscription, POS entitlement snapshot, and POS product-access assignment are **not** reused.

### Current-session access contract

`GET /api/v1/platform/auth/product-access/effective?productCode=pinoy-loan-manager`

- Browser may supply **only** `productCode`
- Endpoint does **not** bind `userId` or `organizationId`
- Cookie/session transport identical to other AuthEndpoints (`ExtractSessionToken`)
- No bearer token for browser D3
- No ManageProductAccess / ManageMemberships / Platform Administrator / org-owner role required
- Session-derived user + `SelectedOrganizationId`
- Organization account required; Personal → `application.auth.account_scope_denied`; Platform → same (not treated as PLM org staff)
- Missing selected org → `application.auth.organization_context_required`
- Delegates commercial evaluation to existing `EvaluateEffectiveProductAccess`
- Normal commercial denial remains `Allowed = false` + existing `EffectiveAccessReasonCodes`

### Audit

Read-only self evaluation writes **no** mutation-style audit stream. The privileged `/access/evaluate` endpoint audits because of its authorization gate. The evaluator itself has no audit writes; this package follows that precedent.

---

## Explicit non-goals

- Gate D3 React organization selector / product-access gate / workspace
- Gate E / F / G / H / I / J
- PLM-02
- Loan / borrower UI or domain
- Capacitor
- PLM database / migrations
- D-P12-03 production commercial-state transport
- Production pricing / plans / grant identifiers

---

## Validation

Recorded after execution in this package:

| Suite | Result |
|---|---|
| Platform unit tests | **1022 passed / 0 failed / 0 skipped** (full `ExItS.Platform.UnitTests`) |
| Focused unit (product code / current-session / PLM fixture / identity catalog / access) | **40 passed** |
| Platform integration tests | Focused `ApiCurrentSessionProductAccessTests`: **7 passed**. Full suite on this worktree: **222 passed / 62 failed / 0 skipped** — failures are pre-existing (Testcontainers/start-business/catalog/RBAC); not caused by POS source changes. `ApiIdentityAccessTests` invite-search and platform-user-as-org-member failures predate this package (P19 staff identity). |
| Architecture tests | `PinoyLoanManagerArchitectureTests`: **5 passed**. Full `ExItS.ArchitectureTests` on this worktree: **162 passed / 10 failed** — failures are pre-existing `FindRepositoryRoot` path issues for this worktree name (`ExItS-SaaS-PLM-01A`), unrelated POS/Admin tests. |
| PLM client typecheck/lint/format/test/build/PWA/e2e | typecheck/lint/format:check/build **PASS**; vitest **42 passed**; Playwright **29 passed**; PWA validation **PASS**. No D3 client tests added. |
| Real Local Validation API | Host-run from this branch against preserved `exits_local_validation_platform_db` volume (no `-v`). Catalog: `pinoy-loan-manager` **Active** independent of `pinoy-business-pos` **Active**. Unauthenticated → 401 `session_invalid`. Platform Olivia session → 403 `account_scope_denied`. Organization session with selected org: Allowed **true** (independent PLM trial + assignment); Denied **true** as Allowed **false** (`product_assignment_missing`). Extra `userId`/`organizationId` query params ignored. Privileged `/access/evaluate` still requires non-Organization scope / ManageProductAccess. ABC/XYZ catalog orgs on this volume were already **Closed** by prior PlatformAdministratorsOnly seed; Full-seed commercial attach skips Closed orgs rather than crashing. Allowed/denied proof used Start a Business Active orgs plus the PLM Local Validation plan `plm-local-validation`. |
| Screenshots | **none** (no UI); D1/D2 screenshots unmodified |

---

## Exact next package

**STOPPED AFTER PLM-D3-PRE.** Do not automatically resume Gate D3 React. Do not start Gate E.

Queue: **STOPPED AFTER PLM-D3-PRE**
