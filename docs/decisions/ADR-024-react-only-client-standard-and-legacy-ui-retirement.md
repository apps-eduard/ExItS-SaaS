# ADR-024 — React-only client standard and legacy UI retirement

[Decisions](README.md) | [ADR-015](ADR-015-antdesign-blazor-platform-admin.md) | [ADR-022](ADR-022-separated-antdesign-web-hosts-and-unified-auth.md)

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-10-04 |
| Supersedes | UI technology portions of ADR-015 and ADR-022. Account-scope rules in ADR-022 remain. |

## Decision

```text
Browser UI:     React + TypeScript
Mobile:         React PWA, plus Capacitor when native packaging is required
Backend:        ASP.NET Core 10 APIs and services remain
Retired:        .NET MAUI, MAUI Blazor Hybrid, Blazor Web UI, Ant Design Blazor
Unchanged:      Product, domain, and database boundaries
```

UI technology does not own business rules. Those stay in the product and platform application layers.

## What this change removed

- `ExItS.PinoyBusinessPOS.Maui` and `ExItS.PinoyBusinessPOS.Maui.Tests`
- The isolated MAUI Local Validation compose, env example, and launchers
- The MAUI emulator and physical-device install guides
- `ExItS.PinoyLoanManager.Web`, which was a placeholder shell with no lending screens

Native Android packaging is temporarily unavailable. MAUI is not kept as a fallback. The canonical client is the React application.

`ExItS.PinoyBusinessPOS.LocalStore` stays because POS unit tests still reference it. `ExItS.PinoyBusinessPOS.ApiClient` stays because architecture scope tests and `ExItS.PinoyBusinessPOS.ApiClient.Tests` still read that C# client. No retained runtime host references it. Pinoy Loan Manager's ApiClient stays as that product's C# client boundary. The React client calls HTTP itself.

## Organization Web Blazor removed

`ExItS.PinoyBusinessPOS.Web` and `ExItS.PinoyBusinessPOS.Web.Tests` are removed. Git history is the archive. The React client (`src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React`) is the canonical Organization experience, including profile, branches, staff, subscription, tax compliance (`/org/tax-compliance`), audit (`/org/audit`), and sales documents (`/org/sales-documents`). Local Validation serves it on port 5177 (`react-pos`). Public hostname `app.exitsapps.com` targets that React service. `ExItS.DesignSystem` is removed with it because no retained runtime consumed it.

## Personal Web, shared Razor UI, and Ant Design Blazor removed

`ExItS.Personal.Web`, `ExItS.Personal.Web.Tests`, and `deploy/docker/Dockerfile.personal-web` are removed. Git history is the archive. The same React client is the canonical Personal, Organization, and POS frontend. Local Validation origin is `http://127.0.0.1:5177`. Preview hostnames `app.exitsapps.com`, `my.exitsapps.com`, and `pos.exitsapps.com` target `http://react-pos:80`.

`ExItS.Web.UI` is removed. No retained runtime used its Razor components, theme helpers, handoff HTTP client, or forwarded-headers helper. Ant Design Blazor has no active package consumer. Port 8094 is retired.

## Retirement status

| Host | Status |
|---|---|
| MAUI | REMOVED |
| PLM Blazor Web | REMOVED |
| Platform Admin Blazor | REMOVED |
| Organization Web Blazor | REMOVED |
| Personal Web Blazor | REMOVED |
| DesignSystem | REMOVED |
| ExItS.Web.UI | REMOVED |
| AntDesign Blazor | REMOVED |

`ExItS.Platform.Admin` is removed. React Platform Admin (`src/Platform/ExItS.Platform.Admin.Web`, Local Validation service `admin-web` on port 8095) is the canonical Platform Administration frontend. Do not add new Blazor or MAUI projects.
