# ADR-010 — Separate UI Implementations for HealthCare vs Platform Admin and POS

[Decisions](README.md) | [UI design system](../engineering/ui-design-system.md) | [UI reuse assessment](../reuse/healthcare-ui-reuse-assessment.md)

| Field | Value |
|---|---|
| Status | **Accepted** (validated P0-WP03; corrected 2026-07-29) |
| Date | 2026-07-29 |
| Related | ADR-004, ADR-005, ADR-006, ADR-007, ADR-008 |

## Decision summary

```text
Existing HealthCare Staff Web
→ Retains Ant Design Blazor

Existing HealthCare Patient Web / MAUI
→ Retain their current native implementations (no rewrite)

New ExItS Platform Admin
→ Native Razor components and native CSS

PinoyBusinessPOS
→ Native Razor components and native CSS
```

## Context

HealthCare Staff Web is built on **Ant Design Blazor 1.6.2**. PatientWeb and Mobile use native CSS without Ant Design. The new ExITS Platform Admin and PinoyBusinessPOS need compact bilingual workflows, Light/Dark/System themes, and a maintainable shared native foundation. Requiring the **new** Platform Admin to use Ant Design would couple the portfolio to HealthCare’s UI framework and block a shared native stack with POS.

## Decision

1. **Existing HealthCare Staff Web retains Ant Design Blazor.** No HealthCare UI rewrite, modernization, framework migration, or restyling is in the current ExITS MVP work.
2. **Existing HealthCare Patient Web and MAUI retain their current implementations.**
3. **New ExITS Platform Admin must not use Ant Design Blazor.** It uses Blazor Web App, native CSS, CSS isolation, CSS custom properties, semantic tokens, reusable Razor components, Compact/Comfortable density, Light/Dark/System themes, English and Filipino localization, purposeful motion with reduced-motion, responsive layouts, and accessibility requirements. **No Tailwind.** No third-party UI component framework unless separately approved.
4. **PinoyBusinessPOS** uses the same **new native UI foundation** (MAUI Blazor Hybrid + native CSS + reusable Razor components). **No Ant Design. No Tailwind.**
5. **Shared consistency** comes from semantic design tokens, typography, spacing, theme conventions, localization conventions, accessibility standards, motion standards, and shared UI-independent models — **not** from sharing Ant Design components.
6. Platform Admin may reuse HealthCare **framework-independent** patterns (authz, org/user workflows, pagination/search models, status semantics, modal/notification *contracts*, page-state patterns, UX and test lessons). It must **not** depend on Ant components, Ant CSS/layouts/services, or HealthCare clinical/navigation/permission presentation.
7. **MVP date control** remains a native `DateField` wrapper; rich calendars only by approved need (ADR-008).

## Consequences

### Positive

- Preserves HealthCare MVP UI investment (no forced rewrite).
- One native stack for **new** Platform Admin and POS.
- Clear separation: legacy Ant only inside existing HealthCare Staff Web.

### Negative / risks (controlled technical separation)

- Temporary dual stacks: HealthCare Ant vs new native Platform Admin + POS.
- Brand drift, duplicated visual implementation, separate a11y/theme maintenance, future HealthCare modernization cost.

### Mitigations

- Shared semantic design principles, branding guidance, terminology, and UI-independent contracts.
- Separate framework-specific implementations.
- No forced HealthCare rewrite during current MVP work.

## Validation evidence (P0-WP03)

- AntDesign referenced only from `HealthCare.Web`.
- No shared UI RCL across Web/PatientWeb/Mobile.
- Localization and product Light/Dark/System themes are missing in HealthCare.

## Rejected alternatives

- New Platform Admin on Ant Design — couples portfolio to HC UI framework; blocks shared native foundation with POS.
- Rewrite HealthCare to native CSS immediately — high regression cost; out of current scope.
- Tailwind in Platform Admin or POS — prohibited.
- Sharing Ant components across products — rejected.

## Correction note

An earlier P0-WP03 draft incorrectly stated that interim Platform Admin would retain Ant Design. That statement is **superseded** by this ADR.
