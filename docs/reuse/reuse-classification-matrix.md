# Reuse Classification Matrix

[Reuse classification matrix](reuse-classification-matrix.md) | [Extraction Rules](extraction-rules.md)

Filled from repository evidence during **P0-WP01** and extended for UI in **P0-WP03** (2026-07-29). Classifications:

- **Reuse** — reusable largely as-is after namespace move
- **Adapt** — reusable with generalization
- **Keep in legacy product** — product-bound
- **Do not reuse** — unsafe or wrong abstraction
- **Missing** — not implemented

| Capability | Actual Location | Classification | Required Changes | Risks | Evidence |
|---|---|---|---|---|---|
| Identity | `legacy product.Domain/Identity/ApplicationUser.cs`; Identity EF tables | Adapt | Extract to Platform; keep Guid PK; strip legacy product seed coupling | Issuer/PK migration breaks sessions | `ApplicationUser`, `IdentityDbContext` |
| Authentication | `AuthController`, `AuthService`, JwtBearer | Adapt | Platform-owned login/refresh; product clients as OIDC/JWT consumers | Token format/issuer drift | `legacy product.Api/Controllers/AuthController.cs` |
| Refresh tokens | `Domain/Identity/RefreshToken.cs`; hasher/generator | Reuse | Move with Identity; preserve rotation/family revoke | Weak hashing if changed carelessly | `RefreshTokens` table; migrations `AddRefreshTokenAuthentication` |
| Users | AspNetUsers + `IsActive` | Adapt | Platform user directory; product projections | Dev seed users leaking | `ApplicationUser.IsActive` |
| Organizations | `Domain/Organizations/Organization.cs` | Adapt | Become PlatformOrganization; remove legacy product-only assumptions | FK from Clinics/Staff | Organization entity + settings APIs |
| Memberships | `Domain/Staff/StaffMember.cs` | Adapt | Multi-org; separate clinical assignment from platform membership | Unique UserId today | StaffMember comments + unique constraint |
| Tenant context | `ICurrentUser` / `ICurrentStaff` / `ICurrentPatient`; Web `PlatformTenantContext` | Adapt | Server-owned org context; product-specific current-* | Client ID trust if weakened | TenantAccessService; platform banner |
| Roles | `AppRoles` | Adapt | Keep PLATFORM_ADMIN / ORGANIZATION_ADMIN; legacy product clinical roles stay in product | Role name collisions across products | `AppRoles.cs` |
| Permissions | `Application/Authorization/Permissions.cs`, handlers | Adapt | Split catalog: platform vs legacy product; keep handler infra | Over-broad platform perms | `PermissionAuthorization.cs`, `RolePermissionMatrix.cs` |
| Patient self-scope | Patient APIs + staff UI gates | Keep in legacy product | Do not promote to Platform | Wrongly applied to POS | Patient scope tests; `EnsureCanAccessPatient` pattern |
| Plans (billing) | — | Missing | Design in Phase 3 | Confusing SOAP “Plan” field | Grep: no Subscription/Billing entities; medical note Plan ≠ billing |
| Trials | — | Missing | Phase 3 | — | Docs `mvp-*-scope` out of billing |
| Subscriptions | — | Missing | Phase 3 | — | No entities/APIs |
| Product entitlements | Soft limits only (`MaxClinics`/`MaxStaff`) | Adapt (limits) / Missing (entitlements) | Generalize limits; add signed snapshots | Using limits as fake billing | `OrganizationLimitService`, `OrganizationLimits` config |
| Billing | — | Missing | Phase 3 payments | — | Explicitly out of legacy product MVP scope |
| Platform Admin | Staff Web pages + org directory API (today) | Adapt workflows; **new** admin is native UI | Rebuild org/user/subscription admin natively; no Ant | Mixing legacy product clinical UI into Platform | Routes `/organization/*`, …; ADR-010 |
| Audit logging | `SecurityEvent`, `OrganizationAuditEvent`, `MedicalNoteAuditEvent` | Adapt / Keep | Platform: security+org; legacy product: medical | PHI in wrong store | DbSets + recorders |
| Validation | FluentValidation in Application/Api | Reuse | Shared package or pattern | Version drift | FluentValidation 11.11.0 |
| API errors | `GlobalExceptionHandler` ProblemDetails + correlation | Reuse | Shared contract shape | Leaking internals | Api middleware |
| Pagination | `PagedResponse<T>` in Contracts | Reuse | Shared contracts | — | Contracts + API clients |
| Notifications | `IAccountEmailSender` (dev capture); UI toasts | Adapt / Do not reuse impl | Production email new; keep UI abstraction | Dev sender in prod | Development email sender; `IUserNotificationService` |
| Localization | — | Missing | Platform Admin & POS `en`/`fil` resources | Hard-coded English | No resx/IStringLocalizer |
| Themes | `legacy product-ant-enterprise.css` | Keep in legacy product Staff / Missing for new apps | Shared `--exits-*` Light/Dark/System for Platform Admin & POS | Contrast failures | CSS file; no theme service |
| Tables | Staff pages ad hoc Ant/HTML | Pattern only | Native `ExDataTable` for Platform Admin & POS | Ant coupling | Web Components/Pages |
| Dropdowns | Ant `Select`; `ClinicPicker`/`OrganizationPicker`/`PatientPicker` | Adapt pickers as pattern | Native `SelectField` for Platform Admin & POS | Free-text ID bypass; Ant Select | Picker components |
| Calendars | Ant `DatePicker`; appointments calendar pages | Keep / Pattern | POS `DateField` native wrapper; no custom engine | Building full calendar early | `AppointmentsCalendar.razor`, DatePicker usage |
| Dialogs | `IUiModalService` / `AntUiModalService` | Adapt | Interface reusable; Ant impl **legacy product only**; native for Platform Admin & POS | — | `Services/IUiModalService.cs` |
| CSS / design tokens | `legacy product-ant-enterprise.css`, `hc-portal.css`; Mobile hard-coded | Adapt token *names* / Keep legacy product CSS | Shared `--exits-*` Light/Dark/System for Platform Admin & POS | Copying Ant CSS into new apps | P0-WP03 UI assessment |
| Animations / motion | Staff `--hc-motion`, `hc-rise-in`, reduced-motion; Mobile spinner | Pattern only | Native motion table + `prefers-reduced-motion` for Platform Admin & POS | Blocking cashier UX | `legacy product-ant-enterprise.css` |
| Accessibility | Aria labels, pickers listbox; weak `h1:focus` | Adapt strengths / fix gaps in native apps | Platform Admin & POS a11y checklist mandatory | Assuming Ant equals a11y | UI assessment §6 |
| Responsive layouts | Staff sider breakpoints; PatientWeb weak media; Mobile scroll nav | Pattern only | Phone cards / tablet split / desktop dense | Wide tables on phones | UI assessment §5 |
| Design tokens system | Partial `--hc-*` | Missing full system | Density + theme + motion tokens for Platform Admin & POS | Treating legacy product CSS as final | UI assessment §4 |
| Ant Design Blazor | `legacy product web project` only, v1.6.2 | Keep in **legacy product Staff Web only** | Do not add to Platform Admin or POS | Framework leak | Web csproj; ADR-010 |
| Tailwind | — | Missing / Do not introduce | Explicitly prohibited for Platform Admin & POS | Accidental add | ADR-010 |
| Test infrastructure | Unit/Integration/Architecture/Web/E2E fixtures | Reuse pattern | Baseline before extraction | Flaky E2E env | `tests/*` |
| CI/CD | Scripts + Docker only | Missing (Actions) | Add portfolio CI later | No automated gate in monorepo | No `.github` workflows |
| Docker/deployment | `deploy/docker/*`, Dockerfiles | Adapt | Split Platform vs legacy product compose later | Shared DB today | compose.yaml files |

## Capability notes

- **Clinic** is legacy product location semantics — do not rename to Store for POS.
- **Organization usage limits** are the only commercial-adjacent controls found; they are **not** subscriptions.
- **Hangfire** hosting is reusable infrastructure; reminder/summary jobs are legacy product-specific.
- **UI (P0-WP03, corrected):** Historical legacy product Staff Web kept Ant; **Platform Admin** uses Ant Design Blazor (ADR-015); **POS** uses native CSS/DesignSystem — see [ADR-010](../decisions/ADR-010-separate-ui-implementations-platform-and-pos.md).
