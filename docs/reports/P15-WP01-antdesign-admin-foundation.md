# P15-WP01 — Ant Design Admin Foundation (completion)

[Phase 15](../phases/phase-15-ant-design-platform-admin.md) | [ADR-015](../decisions/ADR-015-antdesign-blazor-platform-admin.md) | [Portfolio](../portfolio-progress.md)

## Status

**Complete** at tip `0ee125487cba83747f36fd260c404249700ae858`. Fluent UI Phase 15 direction cancelled/superseded before any push.

## Cancellation / revert

| Item | Result |
|---|---|
| Starting commit | `e6d0185c7f9659888ca26c557ffdef0128f1169d` (= `origin/main`) |
| Fluent UI commits on remote | **None** — no revert commits required |
| Uncommitted Fluent work | Discarded cleanly (`git checkout` / `git clean`) before Ant foundation |
| History rewrite / force-push | **Not used** |

## Package

- `AntDesign` **1.6.2** pinned in `Directory.Packages.props` + `ExItS.Platform.Admin.csproj`
- No floating versions; no FluentUI; no Tailwind

## Foundation delivered

- `AddAntDesign()`, `<AntContainer />`, Ant CSS/JS root-absolute assets, Staging Live Preview `UseStaticWebAssets()`
- Ant Design Pro–inspired shell: Layout / Sider / Header / Content / Menu / Breadcrumb / Dropdown / Avatar
- Theme Light / Dark / System; compact density; restrained ExItS branding CSS
- SSR credential login + Live Preview quick login + antiforgery + cookie issuance preserved
- `MapStaticAssets().AllowAnonymous()`; Production rejects Live Preview

## Pages migrated (WP01)

| Page | Notes |
|---|---|
| Login | Ant chrome; native SSR POST inputs |
| Admin shell + nav | Fully Ant Design |
| Dashboard | Ant Statistic / Result / Spin |
| Platform Users list + detail | Ant Table remote pagination, search/filter, tags, Popconfirm/Confirm |
| Organizations list + detail | Ant Table + Descriptions |

Residual report/native controls remain on Payments/Subscriptions/Audit/etc. until later WPs — not a dual chrome system on migrated surfaces.

## Tests

`dotnet test ExItS.slnx -c Release` → **1268 passed / 0 failed / 0 skipped**

## Startup

```powershell
.\tools\Start-LivePreviewLocal.ps1
```

Admin: http://localhost:8090
