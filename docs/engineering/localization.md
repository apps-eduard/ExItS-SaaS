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

## Translation quality

Use natural store language, not literal technical translation. Maintain a glossary for Utang, balance, payment, due date, stock, sale, refund and cashier terms.
