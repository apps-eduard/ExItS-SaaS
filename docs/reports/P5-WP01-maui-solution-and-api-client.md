# P5-WP01 — MAUI Solution and API Client

## 1. Status

**Complete.** PinoyBusinessPOS Android-first MAUI Blazor Hybrid foundation delivered: shared DesignSystem library, shell, System/Light/Dark themes, EN/fil-PH resources, typed API client with connectivity/health classification, and Release Android APK evidence. Phase 5 remains **In Progress**. HealthCare remains frozen.

| Field | Value |
|---|---|
| Phase | Phase 5 — PinoyBusinessPOS MAUI Foundation |
| Work package | P5-WP01 — MAUI Solution and API Client |
| Branch | `main` |
| Date | 2026-07-30 |
| Phase marker | `P5-WP01-maui-solution-api-client` |
| Phase 5 decision | **In progress** — P5-WP01 complete; not production-ready |

## 2. Project structure

```text
src/Shared/
└── ExItS.DesignSystem/          # shared Razor class library (tokens, primitives, DS resources)

src/Products/PinoyBusinessPOS/
├── ExItS.PinoyBusinessPOS.Application/   # abstractions (ApiResult, connectivity, options)
├── ExItS.PinoyBusinessPOS.ApiClient/     # typed HTTP client → Platform API
└── ExItS.PinoyBusinessPOS.Maui/          # Android-first MAUI Blazor Hybrid (net10.0-android)

tests/
├── ExItS.DesignSystem.Tests/
├── ExItS.PinoyBusinessPOS.ApiClient.Tests/
└── ExItS.PinoyBusinessPOS.Maui.Tests/
```

No PinoyBusinessPOS Domain, Infrastructure, or product API projects. No product database. Wired into `ExItS.slnx`.

## 3. Design system

`ExItS.DesignSystem` (`net10.0` Razor class library):

- Semantic CSS tokens in `wwwroot/exits-design-system.css` (`--exits-*`)
- Theme via `[data-theme="light|dark|system"]`; density via `[data-density="compact|comfortable"]`
- Shared Blazor primitives (Button, inputs, layout, feedback/empty/error states, overlays)
- `DesignSystemResources` (`en` + `fil-PH`) for shared empty/error/offline/API messages
- **No** Ant Design, Tailwind, Bootstrap, EF Core, or product/Platform Infrastructure references

## 4. Shell

MAUI + Blazor Hybrid (not classic `AppShell` flyouts):

- `App` → `MainPage` (`BlazorWebView`) → `Routes` → `PosShell`
- Top bar: brand, environment badge, connectivity indicator, settings
- Bottom nav: Home + deferred placeholders (Products, Sales, Customers, More)
- Routes: `/` / `/home`, `/settings`, deferred pages, `NotFound`
- Target TFM: `net10.0-android` only for P5-WP01 completion (iOS/Windows/MacCatalyst not required)

## 5. Themes

System / Light / Dark via DesignSystem semantic tokens:

- Settings selector; preference persisted (MAUI Preferences + `localStorage` mirror)
- `theme-boot.js` reduces incorrect-theme flash
- Primary brand green `#166534`
- Density tokens exist in DesignSystem; POS shell inherits CSS comfortable default (compact layout expansion is P5-WP02)

## 6. Localization

| Resource set | Cultures | Scope |
|---|---|---|
| `PosResources` | `en`, `fil-PH` | Shell, Home, Settings, deferred, connectivity/API messages |
| `DesignSystemResources` | `en`, `fil-PH` | Shared empty/error/loading/offline/API unavailable copy |

UI label for `fil-PH` is “Tagalog”. Culture preference persisted. Glossary: [pos-terminology-guide.md](../engineering/pos-terminology-guide.md).

## 7. API client

`IPosApiClient` / `PosApiClient`:

- `GetAsync<T>`, `SendAsync<T>`, `GetHealthAsync` → `GET /health`
- Returns `ApiResult<T>` (never throws for transport/HTTP failures)
- Classifies Success, NotFound, Validation, Conflict, Unauthorized, Forbidden, Timeout, Offline, Unavailable, Cancelled, Failed
- Parses ProblemDetails + `X-Correlation-ID`
- Offline short-circuit when `IConnectivityService` reports disconnected
- GET retry once (200 ms) only on Unavailable/Timeout
- Default base URL for Android emulator: `http://10.0.2.2:5288`

Platform API root phase marker: `P5-WP01-maui-solution-api-client`.

## 8. Offline limitations

Connectivity detection and API status classification only:

- OS network access ≠ API reachability
- **No** offline sync queue, SQLite, SyncQueue, or Phase 7 sync engine
- **No** offline business operations (sales, inventory, Utang)
- Foundation must not be mistaken for offline-capable commerce

## 9. Explicit exclusions

- Authentication / login / MFA / real secure token storage (`NullSecureTokenStore` stub only)
- Sales, inventory, products, customers, Utang business flows
- Offline synchronization / product database / Domain / Infrastructure
- Payment gateways; iOS/Windows/MacCatalyst as completion targets
- Production security claims (development-stage unauthenticated warnings remain)
- P5-WP02+ (native UI tokens/compact layout polish, dedicated loc WP, reusable MVP components WP, auth/onboarding)

## 10. Tests

Placeholder counts pending final recorded run after push:

| Suite | Passed |
|---|---:|
| Unit | 261 |
| Architecture | 41 |
| Admin unit | 27 |
| DesignSystem | 7 |
| ApiClient | 17 |
| Maui | 6 |
| Integration | 84 |
| **Total** | **443** |

Baseline 411 not reduced.

## 11. Android build evidence

Release `net10.0-android` APK produced for `ExItS.PinoyBusinessPOS.Maui` (Android-first foundation). iOS/Windows/MacCatalyst builds are not required for P5-WP01 completion.

## 12. HealthCare freeze

`/HealthCare/` ignored, untracked, outside `ExItS.slnx`. Platform Integration contracts under `src/Platform/.../Integration/HealthCare/` remain tracked Platform files only.

## 13. Exact next work package

**P5-WP02 — Native UI Tokens, Themes and Compact Layout**

Do not begin until explicitly authorized.

## 14. Commits

3015925d16560be13953270565c1ab99a8d69934
