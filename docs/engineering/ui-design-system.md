# UI Design System and Reusable Components

[Home](../index.md) | [Localization](localization.md) | [Themes](theme-system.md)

## Decision

Use a compact, accessible design system with separate framework implementations:

```text
Shared semantics/models
├── design-token names
├── localization keys
├── validation and formatting
├── table/query/pagination models
└── status and option models

Platform/HealthCare UI
└── Ant Design Blazor wrappers and existing components

PinoyBusinessPOS UI
└── Native Razor components + CSS isolation
```

Do not create a single component that conditionally switches between Ant Design and native CSS.

## Compact design requirements

- Desktop tables and forms use compact spacing options.
- Touch controls remain at least approximately 44×44 CSS pixels where interaction requires it.
- Dense tables may use smaller row heights on Windows/web but switch to cards or horizontal-safe layouts on phones.
- Typography, spacing and icons remain readable at 100–200% zoom.
- Compact mode must never hide validation, status or critical financial information.

## Reusable MVP components

### Inputs

- `ExTextField`
- `ExPasswordField`
- `ExNumberField`
- `ExMoneyField`
- `ExSelectField<T>`
- `ExCheckboxField`
- `ExDateField`
- `ExDateRangeField` when required

### Data and navigation

- `ExDataTable<T>`
- `ExSearchBox`
- `ExPagination`
- `ExTabs`
- `ExPageHeader`

### Feedback and overlay

- `ExAlert`
- `ExToast`
- `ExLoading`
- `ExEmptyState`
- `ExModal`
- `ExConfirmDialog`
- `ExStatusBadge`

## Calendar/date strategy

MVP uses a styled wrapper around the platform-native date input/picker for reliability, mobile localization and accessibility. The wrapper owns label, validation, min/max, disabled state, clear behavior and culture-aware display.

A custom calendar popover is added only when date range, disabled dates, presets or uniform visual behavior becomes a verified requirement. It must support keyboard navigation, month/year navigation, leap years, English/Filipino labels and both themes.

## Table model example

Shared table/query models may be reused, but Platform Admin renders them through Ant Design wrappers while POS renders native components.

## Component review rule

Before creating a component, Cursor must search HealthCare and classify the existing implementation:

- reusable unchanged
- reusable wrapper/model
- pattern only
- product-specific
- unsuitable
