# PinoyBusinessPOS Terminology Guide — English ↔ Tagalog (fil-PH)

[UI design system](ui-design-system.md) | [Localization](localization.md) | [Theme system](theme-system.md) | [Admin terminology](admin-terminology-guide.md)

Short glossary for PinoyBusinessPOS shell/shared copy (`PosResources` / `PosResources.fil-PH.resx`,
P5-WP01) and shared DesignSystem feedback strings (`DesignSystemResources`). Business flows
(sales, inventory, Utang) are not localized yet — deferred to later Phase 5 / Phase 6 work packages.

## Principles

- Keep product brand **PinoyBusinessPOS** / **ExITS** and status codes language-neutral.
- Prefer natural Tagalog for everyday shell verbs; keep technical terms (API, Offline sync) clear.
- UI culture label for `fil-PH` may say **Tagalog** for familiarity.
- Never claim this foundation is offline business-capable.

## Glossary (foundation)

| English | Tagalog (fil-PH) | Notes |
|---|---|---|
| Home | Home | Shell nav; may remain English if widely understood. |
| Settings | Mga Setting | Appearance, language, connection diagnostics. |
| Theme | Tema | System / Light / Dark. |
| Language | Wika | |
| Light | Maliwanag | |
| Dark | Madilim | |
| System | System | Follows OS preference. |
| Online | Online | Connectivity indicator. |
| Offline | Offline | Network disconnected — not sync-complete commerce. |
| Network | Network | OS connectivity label. |
| API connection | Koneksyon sa API | Development diagnostics only. |
| Development-stage | Development-stage | Defined delivery-status term. |

## Adding a new key

1. Add English to `PosResources.resx` (or `DesignSystemResources.resx` for shared feedback).
2. Add Tagalog to the matching `.fil-PH.resx` in the same change.
3. Update this glossary when introducing new store-facing terms (Utang, bayad, stock, etc.).
