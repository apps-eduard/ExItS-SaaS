# Localization — English and Filipino/Tagalog

[UI Design System](ui-design-system.md) | [Themes](theme-system.md)

## Supported cultures

- `en` / `en-PH` — English
- `fil` / `fil-PH` — Filipino/Tagalog

Use Filipino (`fil-PH`) as the technical culture identifier while product copy may say “Tagalog” for user familiarity.

## Requirements

- All user-facing text comes from localization resources.
- No hard-coded English in reusable components or business pages.
- Dates, numbers and Philippine peso amounts use culture-aware formatting.
- User language preference is stored per global user, with product override only if justified.
- Language can be changed without losing form data.
- Validation, empty states, confirmations and error messages are localized.
- Database codes/statuses remain language-neutral; only display labels are translated.
- Fallback is English when a Filipino resource is missing, and missing keys are detected in tests.

## Suggested resource structure

```text
Localization/
├── SharedResources.resx
├── SharedResources.fil-PH.resx
├── PosResources.resx
└── PosResources.fil-PH.resx
```

### Platform Admin resources (P4-WP04)

```text
src/Platform/ExItS.Platform.Admin/Localization/
├── AdminResources.resx          # English (default)
├── AdminResources.fil-PH.resx   # Filipino/Tagalog
└── AdminResources.cs
```

ASP.NET Core request localization + cookie/localStorage language preference. Shell, navigation, and shared components use `IStringLocalizer<AdminResources>`. Business page copy may remain English in P4-WP04. Glossary: [admin-terminology-guide.md](admin-terminology-guide.md).

### PinoyBusinessPOS resources (P5-WP01–P6-WP04)

```text
src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/Localization/
├── PosResources.resx            # English (default)
├── PosResources.fil-PH.resx     # Filipino/Tagalog
└── PosResources.cs
```

Includes shell/home/settings/auth keys plus **Customers_***, **Credit_***, **Payment_***, **Ledger_***, **DueDate_***, **Overdue_***, and remaining **Utang_*** deferred-feature strings (statements/receipts).

src/Shared/ExItS.DesignSystem/Localization/
├── DesignSystemResources.resx (+ .fil-PH.resx)
├── ValidationResources.resx (+ .fil-PH.resx)
├── ErrorResources.resx (+ .fil-PH.resx)
└── marker classes for IStringLocalizer<T>
```

MAUI registers `en` + `fil-PH`; culture preference persisted. Shell/Home/Settings/deferred/NotFound/Dev showcase use `IStringLocalizer<PosResources>`. Shared chrome and MVP components use DesignSystem/Validation/Error resources (including Data_*, Money_Unavailable, Confirm_ReasonLabel). UI label for `fil-PH` is **Tagalog**. Formatting: `CultureFormatting`. Glossary: [pos-terminology-guide.md](pos-terminology-guide.md).

## Translation quality

Use natural store language, not literal technical translation. Maintain a glossary for Utang, balance, payment, due date, stock, sale, refund and cashier terms.
