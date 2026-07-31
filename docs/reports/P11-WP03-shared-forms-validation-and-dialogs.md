# P11-WP03 — Shared Forms, Validation, and Dialogs

Package: **P11-WP03 — Shared Forms, Validation, and Dialogs**  
Prior tip (P11-WP02 feature): `7ce7df139a9494c9aab7d189900e96d5e43fdc1d`  
Prior tip (P11-WP02 docs): `2db60f5e65556259d7ab724c84568bfb78a69de5`  
Feature tip (this WP): `6825b8eb423e73cd5d3dc24e393e7201b04232bc`  
Docs tip: _(recorded after docs commit)_

## Status

**Complete.** Platform Admin has one authoritative shared form/validation/dialog foundation (native CSS/Razor). Representative create/edit flows and confirmation patterns use it. Business rules, API contracts, and P11-WP02 shell/routing/theme behavior are unchanged.

## Discovery (P11-WP01 + inspection)

| Finding | Action |
|---|---|
| Admin `ConfirmDialog` vs DesignSystem overlay duplicate | Consolidated **Admin** dialog API (Kind, focus, Escape, backdrop); DesignSystem left for MAUI/hybrid reuse — Admin does not reference DesignSystem |
| Ad-hoc labels, panels, inline `_toast` | Shared `FormSection` / `FormField` / `FormActions` + `ToastService` on migrated pages |
| Inconsistent busy / double-submit | `AdminFormErrorMapper.TryBeginSubmit` |
| Conflict/server errors | Mapped via `MapPageError` + `IsConflict` into `FormValidationSummary` |
| `Func<Task>` confirm/cancel | Replaced with `EventCallback` so owning pages re-render when dialogs close |
| Searchable select / money-specific control | Not introduced; `AdminInput` `type=number` used where needed; formatting stays culture/API boundary |
| Browser-only dependencies | Theme/focus remain web Admin concerns; primitives stay Razor/CSS for later Hybrid evaluation |

## Components added or consolidated

| Component / type | Role |
|---|---|
| `Forms/AdminFormErrorMapper.cs` + `AdminDialogKind` | Submit gate, conflict detection, page error mapping |
| `FormSection.razor` | Section panel + title |
| `FormField.razor` + `FormFieldContext` | Label, required, help, error, ARIA described-by cascade |
| `FormValidationSummary.razor` | Page/conflict alert |
| `FormActions.razor` | Primary/secondary save bar + busy text |
| `AdminInput` / `AdminSelect` / `AdminTextArea` / `AdminCheck` | Controls consuming field context |
| `ConfirmDialog.razor` | Confirm / Destructive / Conflict / UnsavedChanges; focus into confirm; Escape; intentional backdrop cancel |

CSS: `.form-field*`, `.form-validation-summary`, `.dialog--*` variants in `wwwroot/app.css`.  
Localization: `Form_*`, `Dialog_*` keys in EN + `fil-PH` `AdminResources`.

**Duplicates removed:** none as separate files; feature markup replaced on migrated pages. DesignSystem dialog retained (not deleted) for non-Admin surfaces.

## Shared API conventions

```razor
<FormSection Title="...">
  <FormValidationSummary Message="@_formError" IsConflict="_formConflict" />
  <FormField Label="..." Required="true" Error="@_fieldX" FieldId="...">
    <AdminInput @bind-Value="_x" />
  </FormField>
  <FormActions PrimaryText="..." Busy="_busy" OnPrimary="SaveAsync" SecondaryText="..." OnSecondary="Cancel" />
</FormSection>

<ConfirmDialog Visible="true" Kind="AdminDialogKind.Destructive"
               OnConfirm="RunConfirmAsync" OnCancel="ClearConfirm" Busy="_busy" />
```

- Labels associate via `for` / control `id`.
- Errors use `aria-invalid` + `aria-describedby`.
- No third-party form framework; no client-authoritative business calculations.
- Money/quantity/date: use `AdminInput` types (`number`, `date`, etc.); values remain server-authoritative.

## Pages migrated

| Page | Coverage |
|---|---|
| `Users.razor` | Create + profile edit fields; field validation; TryBeginSubmit; ToastService; destructive confirms; **UnsavedChanges** on create cancel with draft |
| `OrganizationMembers.razor` | Add-member form; destructive suspend/revoke confirms; conflict summary; ToastService |
| `Payments.razor` | Create payment (numeric amount); confirm/reject/void dialog kinds; conflict summary; ToastService |

**Also updated for EventCallback dialog contract:** `Subscriptions.razor`, `OrganizationProductAccess.razor` (confirm wiring only).

## Validation and dialog behavior

- Required/optional fields: field-level messages + summary for server/page errors.
- Duplicate submit: `TryBeginSubmit` + disabled busy buttons on forms and dialogs.
- Conflict: HTTP 409 → `IsConflict` styling on summary; no silent overwrite.
- Dialogs: semantic `dialog` / `alertdialog`, title/description ids, distinct destructive primary, focus to confirm on open, Escape/backdrop → cancel when safe.
- Unsaved: Keep editing (secondary) vs Discard (danger).

## Runtime / browser evidence

Host: `http://127.0.0.1:5289`  
Scripts: `artifacts/p11-wp03-forms.mjs`, `artifacts/p11-wp02-nav-matrix.mjs`

| Check | Result |
|---|---|
| Users create: empty submit → field errors | Pass |
| Unsaved dialog on cancel with draft; Keep editing closes dialog | Pass |
| Dark theme + Payments form section present | Pass |
| Mobile drawer closes after Organizations nav | Pass |
| WP02 nav matrix (routes, refresh, Back/Forward, theme, no `data-permanent`, no Hello world) | Pass |

Desktop/tablet/mobile widths exercised via Playwright viewports (1280 + 390). Formal a11y certification not claimed.

## Tests

Full `ExItS.slnx` Release: **1164 passed / 0 failed / 0 skipped** (baseline 1161 + 3 Admin form foundation tests).

Admin unit tests: **44 passed** (`AdminFormErrorMapperTests` — submit gate, conflict mapping, foundation file/migration guards).

## Remaining migration debt

- Remaining Admin forms still on ad-hoc markup (`Subscriptions` trial forms, product access grant, Organizations create, etc.)
- Broad table/list/card consolidation → **P11-WP04**
- DesignSystem ↔ Admin token convergence deferred
- Focus return to trigger control (after close) not fully automated
- Searchable select / dedicated money control not built
- Formal a11y certification not claimed
- R-091 production auth remains open

## Explicit exclusions

No business-rule changes; no P11-WP04 table work; Phase 12 / `docs/Product-Foundation/` untouched.

## Exact next

**P11-WP04 — Shared Tables, Lists, Cards, and Status Components** when explicitly authorized.
