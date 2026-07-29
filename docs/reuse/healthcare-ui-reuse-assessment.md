# HealthCare UI Reuse Assessment

[Dashboard](../portfolio-progress.md) | [UI design system](../engineering/ui-design-system.md) | [Component catalog](../engineering/reusable-component-catalog.md) | [ADR-010](../decisions/ADR-010-separate-ui-implementations-platform-and-pos.md)

**Work package:** P0-WP03  
**Date:** 2026-07-29  
**HealthCare freeze:** Read-only inspection only; no UI code modified.

---

## 1. Actual UI inventory

| Application | Framework | Component Library | Styling | Primary Actors | Reuse Value |
|---|---|---|---|---|---|
| `HealthCare.Web` | Blazor Interactive Server | **AntDesign 1.6.2** | Ant CSS + `healthcare-ant-enterprise.css` (`--hc-*`) + `app.css` | Platform Admin, Org/Clinic Admin, Doctor, Nurse, Receptionist | High for **Platform Admin interim** (Ant stays); pattern source for POS models only |
| `HealthCare.PatientWeb` | Blazor Interactive Server | **None** (plain HTML) | `hc-portal.css` tokens + `app.css` | Patients | High as **native CSS pattern** for POS; not a shared library |
| `HealthCare.Mobile` | MAUI Blazor Hybrid (`net10.0-android`) | **None** | `wwwroot/app.css` + `MainLayout.razor.css` (hard-coded colors) | Patient, Doctor | Medium — state components (`LoadingState`, `EmptyState`, …) as **pattern**; no Ant |
| Shared Razor UI RCL | — | — | — | — | **Missing** — no cross-app UI library |

Evidence: `HealthCare.Web.csproj` (`AntDesign` 1.6.2); `Program.cs` `AddAntDesign()`; PatientWeb/Mobile csproj lack Ant packages; no UI RCL projects in solution.

### Supporting UI assets

| Asset | Path | Notes |
|---|---|---|
| Staff shell JS | `HealthCare.Web/wwwroot/js/healthcare-shell.js` | Narrow viewport helper |
| Ant static | `_content/AntDesign/css|js` | Loaded from `App.razor` |
| Font | IBM Plex Sans (CDN) | Staff Web only |
| UI tests | `HealthCare.Web.Tests`, `PatientWeb.Tests`, `Mobile.Tests`, Playwright E2E | Staff Ant smoke + portal/mobile unit |

---

## 2. Ant Design usage

| Item | Finding |
|---|---|
| Version | **1.6.2** — staff Web only |
| Registration | `AddAntDesign()` — **no `ConfigProvider`**, no compact algorithm |
| Heavy direct use | `Button`, `Alert`, `Input`, `Select`, `DatePicker`, `Icon`, `Card`, `Space`, `Breadcrumb`, `Row`/`Col`, `Drawer`, `Tabs`, `Tag`, `Empty`, `Menu`, `Layout`/`Sider` |
| Not used as markup | Ant `Table`, `Pagination`, `Modal`, `Form`/`FormItem`, `Badge`, `RangePicker`, `Checkbox`, `Popconfirm` |
| Tables | Custom HTML `.hc-table` + Ant chrome |
| Pagination | Custom Prev/Next `.hc-pager` + page-size `Select` |
| Modals | Via `IUiModalService` → Ant `ModalService` / `ConfirmService` (dialogs inherit `FeedbackComponent`) |
| Toasts | `IUserNotificationService` → Ant `IMessageService` |
| Theme | CSS `--hc-*` overlay; sider uses Ant dark sider theme only — **not** app Light/Dark/System |
| Density | No Ant compact mode / density provider |

### Classification of Ant usage

| Usage | Classification |
|---|---|
| Staff shell Layout/Sider/Menu | Keep in HealthCare / Suitable for Platform Admin |
| Button/Input/Select/DatePicker/Alert/Card | Keep in HealthCare / Suitable for Platform Admin |
| `AntUiModalService` / `AntUserNotificationService` | Ant-dependent wrappers — keep for HC/Platform Admin; **behavior contract** reusable |
| Custom `.hc-table` / `.hc-pager` | Reusable **behavior/model** only for POS |
| Clinical pages (patients, appointments, notes UX) | HealthCare-specific |
| Copying Ant into POS | **Do not reuse** / too tightly coupled |

---

## 3. Existing wrappers and reusable behavior

| Abstraction | Path | Framework Dependency | Reuse Recommendation | Required Changes |
|---|---|---|---|---|
| `IUiModalService` | `HealthCare.Web/Services/IUiModalService.cs` | Contract (Ant impl) | Reusable **behavior contract**; native POS impl later | Split contract to shared models; Ant impl stays HC/Platform |
| `AntUiModalService` | `…/AntUiModalService.cs` | Ant Design | Keep in HC / Platform Admin | Do not use in POS |
| `IUserNotificationService` | `…/IUserNotificationService.cs` | Contract | Same as modal | Native toast for POS |
| `AntUserNotificationService` | `…/AntUserNotificationService.cs` | Ant Design | Keep in HC / Platform Admin | — |
| `PagedResponse<T>` | `HealthCare.Contracts/Common/PagedResponse.cs` | None | **Reuse** as shared model | Move/copy to Shared UI models later |
| `StatusTone` + `StatusBadge` | `Design/StatusTone.cs`, `StatusBadge.razor` | Ant `Tag` in badge | Tone enum reusable; badge Ant-bound | Native badge for POS |
| `ClinicPicker` / `OrganizationPicker` / `PatientPicker` | `Components/*/` | Ant Select + HC APIs | **Pattern only** (searchable picker UX) | Native `SelectField`; no free-text IDs |
| `PlatformTenantBanner` | `Components/Organizations/` | Ant + HC | Platform Admin pattern | Adapt for multi-product later |
| `PermissionState` / `WebPermissions` | `Auth/` | None (strings) | Pattern for UI gating | Product-specific permission catalogs |
| `HcPageHeader`, `EmptyState`, `ErrorState`, `PageLoading` | Web Shared | Mixed Ant | Pattern for chrome/states | Native equivalents for POS |
| Mobile `LoadingState` / `EmptyState` / `ErrorState` / `OfflineState` | Mobile Components | None | Strong **native CSS** pattern | Localize; theme tokens |
| Localization abstractions | — | — | **Missing** | Build for POS (`en`/`fil`) |
| Theme / density providers | — | — | **Missing** | Build for POS |

**UI-independent models:** `PagedResponse<T>`, `StatusTone`, permission string catalogs, filter/query request shapes.  
**Ant-dependent wrappers:** modal/toast services, `StatusBadge` Tag mapping, staff shell.  
**Product-specific:** clinical pickers, appointment calendar pages, medical-note dialogs, patient portal flows.

---

## 4. CSS and tokens

| App | Tokens | Density | Dark mode | Notes |
|---|---|---|---|---|
| Staff Web | `--hc-*` in `healthcare-ant-enterprise.css` | No density modes | No app theme switch | Partial design-token system over Ant |
| PatientWeb | Subset `--hc-*` in `hc-portal.css` | Fixed | No | Closest POS native CSS lesson |
| Mobile | Hard-coded colors | Fixed | No | Needs token migration for POS |

Staff also defines `--hc-ease`, `--hc-motion`, breakpoints 992/768/576, and `prefers-reduced-motion`.

**Verdict:** HealthCare has a **partial token layer** on staff/portal, not a full Light/Dark/System + density design system. Many page-specific Ant class overrides remain.

---

## 5. Responsive behavior

| Surface | Behavior |
|---|---|
| Staff Web | Ant Sider collapse (`BreakpointType.Lg`), CSS media queries, `healthcare-shell.js` narrow flag; calendar grids collapse |
| PatientWeb | Flex-wrap / max-width; **no `@media` queries** in portal CSS |
| Mobile | Horizontal scroll primary nav; flex-wrap; no media-query density system |

Wide tables remain desktop-oriented; phones rely on scroll more than card transformation.

---

## 6. Accessibility

**Strengths:** widespread `aria-label` on tables/actions; pickers `role="listbox"`; Mobile tablist/roles; alerts; some focus rings; staff `prefers-reduced-motion`.

**Gaps:** `h1:focus { outline: none }` weakens focus; no systematic focus-trap documentation for Ant modals beyond Ant defaults; English-only; no guaranteed 44×44 touch targets in compact staff tables; PatientWeb media-query gaps; no reduced-motion on Mobile.

---

## 7. Localization

**Missing** across all three UIs: no `.resx`, no `IStringLocalizer`, hardcoded English, `lang="en"`. Culture used only for invariant formatting in clients.

---

## 8. Themes

**Missing** product Light/Dark/System preference. Staff sider “dark” is chrome-only. Tokens are light-canvas oriented.

---

## 9. Motion

Staff: `hc-rise-in`, `--hc-motion` (~200ms), reduced-motion disable. Mobile: spinner keyframes only. PatientWeb: minimal. No shared motion language for POS yet — define in design system (this WP).

---

## 10. Keep / adapt / do-not-reuse

| Keep in HealthCare (+ Platform Admin Ant) | Adapt as models/patterns | Do not reuse into POS |
|---|---|---|
| AntDesign 1.6.2 staff UI | `PagedResponse`, filter/paging UX | Ant components / Ant CSS |
| Modal/toast Ant implementations | Picker search UX (no free-text IDs) | Clinical appointment calendar as POS calendar |
| Clinical pages & PatientWeb product flows | Status tone semantics | Staff Ant layouts |
| `--hc-*` for HC branding | PatientWeb/Mobile state components | Hard-coded Mobile colors as final POS tokens |

---

## 11. Evidence paths (selected)

- `HealthCare/src/HealthCare.Web/HealthCare.Web.csproj`
- `HealthCare/src/HealthCare.Web/Program.cs`
- `HealthCare/src/HealthCare.Web/Services/IUiModalService.cs`
- `HealthCare/src/HealthCare.Web/wwwroot/css/healthcare-ant-enterprise.css`
- `HealthCare/src/HealthCare.PatientWeb/wwwroot/css/hc-portal.css`
- `HealthCare/src/HealthCare.Mobile/Components/` state components
- `HealthCare/src/HealthCare.Contracts/Common/PagedResponse.cs`

---

## 12. Final recommendation

1. **Retain Ant Design Blazor** for existing HealthCare Staff Web and interim ExITS Platform Admin.
2. **Do not introduce Ant Design or Tailwind** into PinoyBusinessPOS.
3. Build a **native CSS + CSS isolation** POS component library with shared **models/tokens/localization conventions**.
4. Treat PatientWeb/Mobile state patterns and staff picker/paging contracts as UX lessons, not copy-paste Ant.
5. MVP date control = native `DateField` wrapper; defer rich calendar.
6. Implement Compact + Comfortable density, `en`/`fil`, Light/Dark/System, purposeful motion with reduced-motion — in POS (and later Platform mapping), **not** by rewriting HealthCare now.
