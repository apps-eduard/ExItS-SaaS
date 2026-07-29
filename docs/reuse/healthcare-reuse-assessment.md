# HealthCare SaaS Reuse Assessment

[Home](../index.md) | [Dashboard](../portfolio-progress.md) | [Reuse Matrix](reuse-classification-matrix.md) | [Completion Report](../reports/P0-WP01-completion.md)

**Work package:** P0-WP01 — Repository and Reuse Inventory  
**Assessment date:** 2026-07-29  
**Status:** Ready for Review  
**Verdict:** Suitable for **controlled platform extraction** of identity, organization, permission, audit, and BFF patterns — **not** a wholesale lift of the HealthCare solution into ExITS Platform.

---

## 1. Executive summary

The repository contains a completed HealthCare MVP at `HealthCare/` (nested Git history from `https://github.com/apps-eduard/HealthCare.git`, HEAD `ed987d5`). It is a .NET 10 modular monolith: API + staff Blazor Server (Ant Design 1.6.2) + patient Blazor Server + MAUI Hybrid, PostgreSQL/EF Core, JWT + refresh tokens, Hangfire, and strong service-layer tenant checks.

**Reusable now (with rename/namespace and generalization):** Identity/JWT/refresh, permission authorization infrastructure, Organization entity + soft usage limits, SecurityEvents / OrganizationAuditEvents, BFF cookie session pattern, ProblemDetails/correlation, pagination contracts, Ant Design notification/modal wrappers, test/architecture patterns, Docker deploy patterns.

**Must stay in HealthCare:** Clinics, Staff clinical roles, Patients, Patient self-scope, Appointments, Medical notes, PatientWeb, MAUI clinical flows, Hangfire reminder/summary jobs.

**Missing for ExITS Platform:** Products, Plans, Trials, Subscriptions, Billing/Payments, Product Entitlements, dedicated Platform Admin product catalog UI, EF global tenant filters, MFA, production email, multi-organization membership, localization (`en`/`fil`), light/dark/system themes for reusable UI, root CI workflows.

**Do not** rename Patient→Customer, Clinic→Store, or copy medical-note / patient self-scope rules into PinoyBusinessPOS.

---

## 2. Actual repository structure

### ExITS SaaS root (verified 2026-07-29)

```text
ExItS-SaaS/
├── .git/                         # parent repo; no commits before this assessment
├── HealthCare/                   # copied MVP (contains its own nested .git — see risks)
├── docs/
├── FILE-MANIFEST.md
└── README.md
```

No `Platform/`, `Products/`, or `Shared/` implementation folders exist yet (target direction only).

No `docs/platform/`, `docs/healthcare/`, or `docs/pinoy-business-pos/` folders exist; product/platform intent lives under `docs/product/`, `docs/engineering/`, `docs/reuse/`, and `docs/phases/`.

### HealthCare actual tree (evidence-based)

```text
HealthCare/
├── HealthCare.sln
├── Directory.Build.props          # net10.0 (except MAUI host)
├── NuGet.config
├── docker-compose.yml             # legacy postgres helper
├── .env.example
├── .gitignore                     # ignores .env, bin/, obj/
├── .git/                          # NESTED repository (do not delete without approval)
├── api-stdout.log / api-stderr.log
├── create-healthcare-db.csx
├── Docs/                          # HealthCare product docs
├── deploy/
│   ├── docker/dev/                # persistent Dev PostgreSQL (+ local .env)
│   ├── docker/e2e/                # disposable E2E stack (+ local .env)
│   ├── docker/Dockerfile.{api,web,patientweb,dbmigrate,e2e-tests}
│   └── k8s/e2e/
├── scripts/dev/, scripts/e2e/
├── src/
│   ├── HealthCare.Api/
│   ├── HealthCare.Web/            # Staff Blazor Server + AntDesign
│   ├── HealthCare.PatientWeb/     # Patient Blazor Server (no AntDesign)
│   ├── HealthCare.Mobile/         # MAUI Blazor Hybrid (Android TFM)
│   ├── HealthCare.Mobile.Core/
│   ├── HealthCare.Domain/
│   ├── HealthCare.Application/
│   ├── HealthCare.Infrastructure/ # EF + Identity + services; Migrations/
│   └── HealthCare.Contracts/
├── tests/
│   ├── HealthCare.UnitTests/
│   ├── HealthCare.IntegrationTests/   # Testcontainers PostgreSQL
│   ├── HealthCare.ArchitectureTests/
│   ├── HealthCare.Web.Tests/
│   ├── HealthCare.PatientWeb.Tests/
│   ├── HealthCare.Mobile.Tests/
│   └── HealthCare.EndToEndTests/      # Playwright
└── tools/
    ├── HealthCare.DbMigrate/
    └── DbBootstrap/
```

No `.github` workflows under `HealthCare/` or the ExITS root.

---

## 3. Actual stack

| Area | Evidence |
|---|---|
| SDK | `dotnet --version` → **10.0.302** |
| TFM | `Directory.Build.props` → **net10.0**; Mobile uses `net10.0-android` |
| ASP.NET Core / EF | Package refs **10.0.4** |
| PostgreSQL provider | `Npgsql.EntityFrameworkCore.PostgreSQL` **10.0.3** |
| Identity | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.0.4; `ApplicationUser : IdentityUser<Guid>` |
| Auth | JWT Bearer + hashed refresh tokens; staff/patient BFF HttpOnly cookies |
| Validation | FluentValidation 11.11.0 + AspNetCore 11.3.1 |
| Logging | Serilog.AspNetCore 9.0.0 |
| Jobs | Hangfire.AspNetCore 1.8.21 + Hangfire.PostgreSql 1.20.12 |
| Staff UI | AntDesign **1.6.2**; Blazor Interactive Server |
| Patient UI | Blazor Server; custom CSS; **no** AntDesign |
| Mobile | MAUI + `Microsoft.AspNetCore.Components.WebView.Maui` |
| Tests | xUnit, FluentAssertions, bUnit (Web), Playwright, Testcontainers, NetArchTest |
| Package mgmt | Per-csproj `PackageReference` (no Central Package Management) |
| Frontend npm | **None** |
| SignalR app hubs | **Not present** (Blazor circuit only) |

---

## 4. Reuse classification (summary)

| Class | Examples |
|---|---|
| Reuse with modification | JWT/refresh Identity, permission handlers, Organization + limits, SecurityEvents, org audit, BFF pattern, ProblemDetails, Hangfire hosting, Ant wrappers |
| Pattern only | Tenant access service (no EF filters), platform tenant banner, staff membership (single-org) |
| Keep in HealthCare | Clinic, Patient, Appointment, MedicalNote, clinical roles, PatientWeb, Mobile clinical UX |
| Do not reuse | Dev seed credentials, `/auth/dev/*` token peek, DevelopmentAccountEmailSender as production email, AntDesign into POS |
| Missing | Plans, Trials, Subscriptions, Billing, Entitlements, MFA, localization, theme service, multi-org membership |

Full row-level matrix: [reuse-classification-matrix.md](reuse-classification-matrix.md).

---

## 5. Platform candidates

Verified existing capabilities that can become ExITS Platform (after extraction work packages):

1. **Global users** — `ApplicationUser`, Identity tables, `IsActive`
2. **Authentication** — login, refresh rotation, logout/revoke, JWT claims
3. **Organizations** — `Organization` (slug, status, profile, MaxClinics/MaxStaff)
4. **Membership pattern** — `StaffMember` as *org membership inspiration* (must generalize; today Clinic-bound and unique UserId)
5. **Roles/permissions infrastructure** — `AuthorizePermission`, `RolePermissionMatrix`, unknown-deny
6. **Platform admin bypass + org picker** — `PLATFORM_ADMIN` + `PlatformTenantBanner` / organization directory API
7. **Security audit** — `SecurityEvent`
8. **Operational audit** — `OrganizationAuditEvent` (strip HC event types later)
9. **API cross-cutting** — ProblemDetails, correlation ID, FluentValidation, health checks
10. **Soft usage limits** — `OrganizationLimitService` / usage APIs (precursor to entitlements, **not** billing)

---

## 6. HealthCare-only modules

| Module | Location |
|---|---|
| Clinics | `Domain/Clinics`, clinic settings/reports/audit UI |
| Staff clinical roles | DOCTOR, NURSE, RECEPTIONIST, CLINIC_ADMIN |
| Patients + ClinicPatient | `Domain/Patients`, enrollment, patient numbers |
| Patient self-scope | patient APIs conceal cross-patient data; staff directory blocked for patients |
| Appointments + availability + reminders | `Domain/Appointments`, Hangfire reminder/summary queues |
| Medical notes + amendments | `Domain/MedicalNotes`, clinical audit |
| Patient portal | `HealthCare.PatientWeb` |
| Patient/Doctor mobile | `HealthCare.Mobile`, `HealthCare.Mobile.Core` |
| HC permission strings | `patients.*`, `appointments.*`, `medical_notes.*`, `availability.*`, etc. |

---

## 7. UI and Ant Design assessment

| Finding | Evidence |
|---|---|
| AntDesign used **only** in staff Web | `HealthCare.Web.csproj` PackageReference AntDesign 1.6.2; `AddAntDesign()` in `Program.cs` |
| Direct component use in pages | `_Imports.razor` `@using AntDesign`; DatePicker, Select, Layout, Menu, Button, Drawer, etc. |
| Thin wrappers exist | `IUserNotificationService`/`AntUserNotificationService`; `IUiModalService`/`AntUiModalService` |
| No shared table wrapper library | Mix of Ant tables and custom HTML; custom Prev/Next paging common |
| PatientWeb is independent | Custom CSS (`hc-portal.css`); suitable pattern for POS “native CSS” direction |
| Theme | `healthcare-ant-enterprise.css` tokens; dark sider only; **no** Light/Dark/System user preference |
| Localization | **Missing** — no `.resx` / `IStringLocalizer` found |
| Accessibility | Partial (AriaLabel on many controls; E2E responsive smoke) |

**POS implication:** Do not depend on AntDesign for PinoyBusinessPOS. Reuse **behavior/models** (pagination DTOs, status presentation patterns, modal/confirm *interfaces*), implement native Razor + CSS isolation per `docs/engineering/ui-design-system.md`.

---

## 8. Localization and theme assessment

| Requirement | HealthCare status |
|---|---|
| `en` / `fil` resources | **Missing** |
| Persistent language preference | **Missing** |
| Culture-aware dates/numbers | Partial (clinic timezone display); not Filipino localization |
| Light / Dark / System themes | **Missing** as product feature |
| Semantic design tokens | Staff CSS variables exist (Ant enterprise); not portable to POS |

PinoyBusinessPOS must introduce localization and theme services greenfield (or Shared contracts only), informed by PatientWeb’s non-Ant CSS approach.

---

## 9. Reusable-component assessment (vs POS library)

| POS need | HealthCare today | Classification |
|---|---|---|
| Design tokens / theme service | Ant enterprise CSS | Pattern only for Platform Admin; **missing** for POS |
| Button / form fields | Direct AntDesign | Should not reuse into POS |
| DateField | Ant `DatePicker` in staff pages | Pattern only; POS uses native date input wrapper |
| Tables | Ad hoc | Inform compact table design; rebuild native |
| Modal / Confirm | `IUiModalService` abstraction | **Reusable behavior/model**; Ant impl stays in HC/Platform Admin |
| Toast | `IUserNotificationService` | Same as modal |
| App shell / nav | Ant Layout/Menu | HC-specific |
| Localization integration | Missing | Required for POS |

---

## 10. Database ownership assessment

**Current:** Single PostgreSQL database via `HealthCareDbContext` (`IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`), schema `public`, migrations under `HealthCare.Infrastructure/Persistence/Migrations/` (18 migrations + snapshot).

| Likely Platform-owned later | Must remain HealthCare-owned |
|---|---|
| AspNet* Identity, RefreshTokens | Clinics, StaffMembers (or HC projection), Patients, ClinicPatients |
| Organizations (+ profile/limits columns) | Appointments*, DoctorAvailability*, Reminders, SummaryRuns |
| SecurityEvents (platform subset) | MedicalNotes*, MedicalNoteAuditEvents |
| OrganizationAuditEvents (generalized) | Clinic-specific audit views |

**Recommended future DBs (not created in this WP):** `ExItS_Platform`, `ExItS_HealthCare`, `ExItS_PinoyBusinessPOS`.

**Risks of moving identity/orgs:** PK/FK breakage across HC tables; JWT issuer/audience changes invalidate sessions; migration history cannot be copied into POS; rollback requires baseline tag before extraction.

**Seed/dev credentials:** Development seeders and `appsettings.Development.json` / Compose `.env` lab passwords — must never ship to production Platform.

---

## 11. Deployment assessment

| Host | Typical local port | Notes |
|---|---|---|
| API | 5080 / 7080 | `/health`, `/health/ready`; Hangfire dashboard Dev |
| Staff Web | 5018 | BFF to API |
| Patient Web | 5020 | BFF to API |
| PostgreSQL | 5432 (dev compose) | Separate e2e DB in `deploy/docker/e2e` |

Dockerfiles and Compose exist under `deploy/docker/`. No GitHub Actions in-tree. Platform and HealthCare **can** deploy independently **after** identity/org contracts are extracted; today they are one deployable product with shared DB.

---

## 12. Test evidence

See [§ Tests](../reports/P0-WP01-completion.md#7-tests-and-validation) and portfolio dashboard.

**Windows baseline (this assessment):** Unit + Architecture + Web + PatientWeb + Mobile.Core tests — **1102 passed / 0 failed / 0 skipped**.

**Not run (per HealthCare README Windows guidance):** IntegrationTests (Testcontainers), EndToEndTests (Playwright/Compose).

**Full solution build:** Fails on `HealthCare.Mobile` without Android SDK (`XA5300`). All non-MAUI projects build Release successfully.

---

## 13. Risks

| ID | Risk | Priority |
|---|---|---|
| P0-R01 | Nested `HealthCare/.git` complicates monorepo history; parent has no commits yet | High |
| P0-R02 | Local `deploy/docker/*/.env` secrets present; must stay gitignored | Critical |
| P0-R03 | `bin/`/`obj/` and log files present under HealthCare copy | Medium |
| P0-R04 | No EF global query filters — isolation is service-only | Critical |
| P0-R05 | Single StaffMember per user — insufficient for multi-product orgs | High |
| P0-R06 | Billing/entitlements entirely missing | High |
| P0-R07 | AntDesign coupling in staff UI — must not leak into POS | Medium |
| P0-R08 | Android SDK missing on assessment machine — Mobile host not buildable | Medium |
| P0-R09 | Pre-existing dirty files inside nested HealthCare git (PatientWeb) | Medium |
| P0-R10 | Parent repo lacks root `.gitignore` — risk of committing secrets/bin if HealthCare is added naively | High |

---

## 14. Recommended extraction sequence

1. **P0-WP02** — Baseline build/runtime map; decide how to handle nested git + root ignore rules (docs/ops only unless approved).
2. **P0-WP03** — Deep Ant Design / UI reuse review for Platform Admin vs POS.
3. **P0-WP04** — Assessment closeout and formal recommendation gate.
4. **Phase 1** — Approve Platform/product boundaries and contracts.
5. **Phase 2** — Extract Platform identity/orgs/permissions with HC regression; **do not** move clinical schema.
6. **Phase 3** — Add Products/Plans/Trials/Subscriptions/Entitlements greenfield on Platform.
7. Keep AntDesign in HealthCare + future Platform Admin; build native POS component library separately.

---

## 15. Explicit “do not reuse” list

- Patient, ClinicPatient, medical notes, appointments, doctor availability as POS domain
- Patient self-scope as a generic platform tenant rule
- AntDesign components inside PinoyBusinessPOS
- Development seed passwords / quick-login / `/auth/dev/*` endpoints
- `DevelopmentAccountEmailSender` as production mailer
- Copying HealthCare EF migrations into POS or Platform
- Renaming HC entities into POS names
- Assuming “Plan” in medical notes means product billing plans
- Trusting client-supplied OrganizationId/ClinicId (current code correctly avoids this — preserve the rule)

---

## 16. Final recommendation

**Proceed with controlled platform extraction** after Phase 0 closeout. HealthCare is a strong reference implementation for SaaS identity, org tenancy, permissions, and audit — but billing/entitlements and POS UI must be designed anew. Treat extraction as **adapt + contract**, not **move the folder**.

---

## Recommended target boundaries (no code moved)

```text
Platform-owned
  Users, auth sessions/refresh, Organizations (global), Products/Plans/Subscriptions (new),
  platform audit, platform admin APIs/UI (AntDesign OK)

HealthCare-owned
  Clinics, staff clinical membership projection, patients, appointments, notes,
  PatientWeb, Mobile, HC migrations, HC Hangfire jobs

PinoyBusinessPOS-owned
  Stores, customers, Utang, inventory, MAUI app, native CSS component library

Shared contracts/models
  Pagination DTOs, ProblemDetails shapes, entitlement snapshot contracts (new),
  design-token *names*, localization *keys* — prefer Platform API contract or shared source later

Shared engineering infrastructure
  CI patterns, Docker patterns, test host patterns — copied pattern first; NuGet only after stability
```

| Shared item | Sharing mode |
|---|---|
| JWT/refresh design | Platform API contract + HC adapter |
| Permission handler infra | Shared source later / adapt into Platform |
| `PagedResponse<T>` | Shared source or contract package later |
| `IUiModalService` | Copied pattern; Ant vs native implementations per product |
| TenantAccessService | Copied pattern + product enforcement |
| AntDesign wrappers | Product-specific (HC / Platform Admin only) |
| Localization resources | Product-specific with shared key conventions |
