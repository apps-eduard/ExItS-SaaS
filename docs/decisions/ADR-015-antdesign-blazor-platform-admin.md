# ADR-015 — Ant Design Blazor for Platform Admin

[Decisions](README.md) | [Phase 15](../phases/phase-15-ant-design-platform-admin.md) | [ADR-010](ADR-010-separate-ui-implementations-platform-and-pos.md)

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-08-01 |
| Related | ADR-010 (amended), Phase 15 |

## Decision summary

```text
ExItS Platform Admin
→ Ant Design Blazor (package AntDesign, pinned stable)
→ Ant Design Pro Blazor is a design/reference only (https://pro.antblazor.com/)

PinoyBusinessPOS
→ Native / DesignSystem (no Ant Design requirement)

HealthCare Staff Web
→ Retains its historical Ant Design usage (separate product)

Fluent UI for Platform Admin
→ Cancelled / superseded (never shipped)
```

## Context

Phase 11 delivered a native-CSS Admin design system. A brief Fluent UI Phase 15 attempt was started then **cancelled before any push**. The authorized Admin direction is now enterprise Ant Design Blazor with a compact, information-dense Pro-style console.

## Decision

1. Platform Admin adopts **`AntDesign` NuGet**, pinned (no floating versions). Initial pin: **1.6.2** (net10.0 compatible).
2. Register with `builder.Services.AddAntDesign()`; host `<AntContainer />`; ensure CSS/JS load for SSR login and Interactive Server; keep `MapStaticAssets().AllowAnonymous()` and Staging Live Preview `UseStaticWebAssets()`.
3. Use Ant Design Pro Blazor as **visual/structural reference only** — do not vendor its repo or unrelated demos.
4. Prefer Ant components directly (Layout, Menu, Table, Form, Modal, etc.). Custom wrappers only for ExItS branding, density, or business-specific composition.
5. **No Tailwind. No Fluent UI. No dual visible design systems** in Admin.
6. POS remains native/DesignSystem unless a later ADR says otherwise.
7. Theme: Light / Dark / System via restrained tokens + minimal scoped CSS — do not fight Ant Design with broad overrides.
8. Upgrade strategy: pin exact versions in `Directory.Packages.props`; bump deliberately with Admin smoke + full Release tests.

## Custom CSS / JS policy

- Minimal `app.css` for branding, density, and login SSR panel only.
- Prefer Ant Design APIs for dialogs, drawers, notifications, and focus management.
- Presentation-only JS only when Ant Design does not cover a justified need (e.g. pre-paint theme boot).

## Consequences

- ADR-010 Admin “native CSS only / no Ant” clause is **superseded for Platform Admin**.
- Architecture guards must allow `AntDesign` in Admin and continue to forbid Tailwind/FluentUI and Ant in POS.

## Rejected alternatives

- Fluent UI Blazor Admin (cancelled P15 attempt)
- Continuing native-only Admin indefinitely
- Importing Ant Design Pro source tree wholesale
