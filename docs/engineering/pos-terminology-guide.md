# PinoyBusinessPOS Terminology Guide — English ↔ Tagalog (fil-PH)

[UI design system](ui-design-system.md) | [Localization](localization.md) | [Theme system](theme-system.md) | [Admin terminology](admin-terminology-guide.md)

Glossary for PinoyBusinessPOS shell and shared DesignSystem feedback (`PosResources`,
`DesignSystemResources`, `ValidationResources`, `ErrorResources`). Technical culture name is
always **`fil-PH`**. User-facing language label is consistently **Tagalog** (not “Filipino”) for
familiarity in the store UI.

Business flows for sales entry and inventory remain deferred. Utang customers/credit/payments/due dates are in progress through Phase 6 — do not invent sales/inventory commercial copy yet.

## Principles

- Prefer clear, familiar Tagalog over literal translation.
- Keep brand names (**ExItS**, **PinoyBusinessPOS**) and technical tokens (**API**, **POS**, **UTC**,
  product codes, IDs, emails) in English when that improves clarity.
- Do not mix English and Tagalog on the same control label without reason.
- Never claim this foundation is offline-business capable.
- Do not localize identifiers, user-entered data, routes, or CSS class names.

## Shell and navigation

| English | Tagalog (fil-PH) | Notes |
|---|---|---|
| Home | Home | Keep English; widely understood. |
| Products | Mga Produkto | Deferred route label. |
| Sales | Benta | Deferred route label. |
| Customers | Mga Kustomer | Deferred route label. |
| More | Higit pa | Deferred route label. |
| Settings | Mga Setting | |
| Primary navigation | Pangunahing navigasyon | `aria-label` for bottom nav. |

## Appearance

| English | Tagalog (fil-PH) | Notes |
|---|---|---|
| Theme | Tema | |
| System | System | Follows OS preference. |
| Light | Maliwanag | |
| Dark | Madilim | |
| Density | Densidad | |
| Compact | Compact | Technical UI term; keep English. |
| Comfortable | Komportable | |
| Language | Wika | |
| English | English | |
| Tagalog | Tagalog | UI label for `fil-PH`. |

## Connectivity and API

| English | Tagalog (fil-PH) | Notes |
|---|---|---|
| Online | Online | |
| Offline | Offline | Network only — not sync. |
| Connected | Nakakonekta | Connection-test success phrasing. |
| Unavailable | Hindi available | |
| Network | Network | |
| API | API | Keep English. |
| API status / environment | API… | Keep “API” English. |
| Connection test | Suriin ang koneksyon | |
| Retry | Subukan ulit / Subukan muli | Prefer “Subukan muli” in DesignSystem. |
| Loading… | Naglo-load… | |

## Common actions and states

| English | Tagalog (fil-PH) | Notes |
|---|---|---|
| Save | I-save | |
| Cancel | Kanselahin | |
| Confirm | Kumpirmahin | |
| Close | Isara | |
| Search | Maghanap | |
| Success | Tagumpay | |
| Warning | Babala | |
| Error | Error | Keep English for severity. |
| No records found | Walang nakitang record | |
| Coming in a later work package | Darating sa susunod na work package | Deferred pages. |
| Development | Development | Environment badge. |
| Application version | Bersyon ng app | |
| Page not found | Hindi nahanap ang page | |

## Adding a new key

1. Add English to the owning `.resx` (`PosResources`, `DesignSystemResources`, `ValidationResources`, or `ErrorResources`).
2. Add Tagalog to the matching `.fil-PH.resx` in the same change.
3. Prefer DesignSystem for generic UI chrome; keep POS product wording in `PosResources`.
4. Update this glossary when introducing new store-facing terms (Utang, bayad, stock, etc.).
5. Add or extend resource-completeness tests for critical keys.
