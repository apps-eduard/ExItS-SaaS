# P5-WP04 — Reusable MVP Components

[Phase 5](../phases/phase-05-pos-maui-foundation.md) | [Portfolio](../portfolio-progress.md) | [Previous: P5-WP03](P5-WP03-english-and-filipino-localization.md)

## 1. Status

**Complete.** Shared DesignSystem reusable MVP components for forms, validation, feedback, confirmation, responsive data display, and money display — without POS product business logic. Development/Testing-only showcase at `/dev/components`. Phase marker: `P5-WP04-reusable-mvp-components`.

## 2. Component inventory and ownership

| Area | Components | Owner |
|---|---|---|
| Forms | `FormField`, `FormGroup`, `FormActions`, `TextArea`, `Checkbox`, `RadioGroup`, `NumberInput`, `CurrencyInput`, `DateInput`, `TimeInput`, `FormValidationSummary`, `FieldValidationMessage` | `ExItS.DesignSystem` |
| Feedback | `ConfirmDialog`, `ToastHost` (existing), `InlineMessage`, `Progress`, `LoadingOverlay` (existing), `EmptyState`/`ErrorState` (existing) | `ExItS.DesignSystem` |
| Data | `ResponsiveDataList`, `DataTable` (alias), `DataColumn`/`DataColumnDefinition`, `MobileRowCard`, `SearchToolbar`, `FilterBar`, `SortControl`, `Pagination`, `PaginationSummary`, `StatusCell`, `MoneyDisplay` | `ExItS.DesignSystem` |
| Layout | `SectionHeader`, `ActionBar`, `Accordion`, `Dropdown` | `ExItS.DesignSystem` |
| Showcase | `/dev/components` (gated) | `ExItS.PinoyBusinessPOS.Maui` |

**Boundaries:** DesignSystem uses `--exits-*` tokens and `DesignSystemResources` only. No Preferences/SecureStorage/API/EF/Npgsql/DbContext. No POS product resources or business rules. Breadcrumb and BottomSheet deferred (not justified for foundation). PasswordBox, cart, pickers, Utang, and payment selectors deferred.

**Naming note:** `FormValidationSummary` / `FieldValidationMessage` avoid clashing with `Microsoft.AspNetCore.Components.Forms.ValidationSummary` / `ValidationMessage`.

## 3. Responsive data pattern

One pattern (`ResponsiveDataList` / `DataTable`):

- ≥768px: compact table (`.exds-data-table-wrap`)
- &lt;768px: mobile row cards (`.exds-data-cards` / `MobileRowCard`)
- Loading, empty, and error states render inside the data container
- Search / filter / sort / pagination compose outside the list and stay usable on narrow phones
- No viewport-level horizontal overflow; labels wrap via existing overflow-wrap rules

## 4. Forms and validation

- Labels, required indicators, hints, and field errors via shared field patterns
- `FormValidationSummary` for form-level messages; `FieldValidationMessage` for field association
- `NumberInput` / `CurrencyInput` use `decimal?` only (no float/double)
- Formatting is display/input behavior only — no tax, pricing, discount, or payment rules
- `FormActions` keep primary actions in a wrapping row for narrow layouts

## 5. Confirmation and feedback

`ConfirmDialog`: title, message, optional reason, confirm/cancel labels, danger/standard variants, loading, Escape cancel, `alertdialog` role. Does not decide authorization or business validity.

`InlineMessage`, `Progress`, plus existing Toast/Alert/Empty/Error/Loading overlay cover feedback states.

## 6. Money display

`MoneyDisplay`: `decimal?` amount, retained currency code, culture-aware `N2` formatting, negative/zero/unavailable states. No conversion, no price calculations, not editable.

## 7. Showcase

Route `/dev/components` available only when `IAppInfoService.EnvironmentName` is Development or Testing (DEBUG → Development; Release → Production → unavailable). Neutral sample rows only. Not in production bottom nav.

## 8. Explicit exclusions

- Sales, inventory, customers, Utang, repayments, authentication, offline sync, gateways, QR, cards
- PasswordBox, checkout, cart, product/customer pickers, payment selectors
- Interactive Android emulator validation (no device attached — R-109 remains open)
- P5-WP05 authentication/onboarding/closeout

## 9. Tests

| Suite | Passed |
|---|---:|
| Unit | 261 |
| Architecture | 41 |
| Admin unit | 27 |
| DesignSystem | 28 |
| ApiClient | 17 |
| Maui | 16 |
| Integration | 84 |
| **Total** | **474** |

Baseline 462 not reduced (net +12).

## 10. Android evidence

Release `net10.0-android` build/publish succeeded (`com.exits.pinoybusinesspos-Signed.apk`). `adb devices` empty — interactive validation not claimed; R-109 remains open.

## 11. portfolio independence verification

Root a nested foreign product tree must remain absent/untracked and outside `ExItS.slnx`.

## 12. Exact next work package

**P5-WP05 — Authentication, Onboarding and Closeout**

Do not begin until explicitly authorized.

## 13. Commits

| Kind | Message | Hash |
|---|---|---|
| Feature | `feat(pos): reusable mvp design-system components and showcase` | `763b0dc7cd73ab21ada2d101d115423c23d90cfa` |
| Docs hash record | `docs(pos): record P5-WP04 commit hashes` | `1eb776d276f400591cf6f21416422e15d4250b38` |
