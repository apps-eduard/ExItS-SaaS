# ADR-010 — Separate UI Implementations for Platform Admin and POS

[Decisions](README.md) | [UI design system](../engineering/ui-design-system.md) | [ADR-015](ADR-015-antdesign-blazor-platform-admin.md)

| Field | Value |
|---|---|
| Status | **Accepted** (validated P0-WP03; corrected 2026-07-29) |
| Date | 2026-07-29 |
| Related | ADR-004, ADR-005, ADR-006, ADR-007, ADR-008 |

## Decision summary

```text
ExItS Platform Admin
→ Ant Design Blazor (ADR-015; Pro Blazor as design reference only)
  No Tailwind. No Fluent UI.

PinoyBusinessPOS MAUI
→ Native Razor components and native CSS / DesignSystem

Organization Web / Personal Web (P25 / ADR-022)
→ Ant Design Blazor (same pin as Platform Admin)
```

## Context

Platform Admin and PinoyBusinessPOS serve different interaction models and do not need one framework-specific component implementation. Early ExITS decisions kept Platform Admin on native CSS. **P15-WP01 (2026-08-01)** later authorized Ant Design Blazor for Platform Admin specifically (see ADR-015). A brief Fluent UI Admin direction was cancelled before push and is superseded.

## Decision

1. **Platform Admin uses Ant Design Blazor** per **ADR-015** (pinned `AntDesign` package; Ant Design Pro Blazor as visual/structural reference only). **No Tailwind. No Fluent UI.** Compact/Comfortable density, Light/Dark/System themes, English and Filipino localization, and accessibility requirements remain in force.
2. **PinoyBusinessPOS MAUI** retains the **native UI foundation** (MAUI Blazor Hybrid + native CSS / DesignSystem). **No Ant Design requirement. No Tailwind.** Organization Web and Personal Web use Ant Design Blazor per **ADR-022**.
3. **Shared consistency** comes from semantic design principles, terminology, and UI-independent contracts, not from forcing one component library into POS.
4. Platform Admin may reuse domain-neutral, framework-independent patterns such as authorization, organization and user workflows, pagination, search, status semantics, modal contracts, notification contracts, and page-state patterns.
5. No UI project may depend on a foreign product project or presentation model.
6. **MVP date control** remains a controlled wrapper; rich calendars require an approved need (ADR-008).

## Consequences

### Positive

- Keeps framework ownership explicit for each active client.
- Allows Platform Admin to use an enterprise component library without coupling POS to it.

### Negative / risks (controlled technical separation)

- Separate stacks can create brand drift and duplicate accessibility or theme work.

### Mitigations

- Shared semantic design principles, branding guidance, terminology, and UI-independent contracts.
- Separate framework-specific implementations.

## Validation evidence (P0-WP03)

- Framework-specific components were not suitable as a cross-product shared UI layer.
- Reusable value resides in domain-neutral contracts and interaction patterns.

## Rejected alternatives

- Tailwind in Platform Admin or POS — prohibited.
- Fluent UI for Platform Admin — cancelled before push; superseded by ADR-015.
- Sharing Ant Design Pro source or foreign product projects into ExItS Admin — rejected.

## Correction note

An earlier P0-WP03 draft incorrectly stated that interim Platform Admin would retain Ant Design. That statement was superseded by the native-Admin rule.

### P15-WP01 amendment (2026-08-01)

Platform Admin is authorized to use **Ant Design Blazor** per **ADR-015**. The native-only Admin requirement and any Fluent UI Admin direction are **superseded for Platform Admin**. POS remains native/DesignSystem.
