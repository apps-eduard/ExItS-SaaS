# ADR-010 — Separate UI Implementations for HealthCare vs Platform Admin and POS

[Decisions](README.md) | [UI design system](../engineering/ui-design-system.md) | [ADR-015](ADR-015-antdesign-blazor-platform-admin.md)

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
→ Ant Design Blazor (ADR-015; Pro Blazor as design reference only)
  No Tailwind. No Fluent UI.

PinoyBusinessPOS
→ Native Razor components and native CSS / DesignSystem
```

## Context

HealthCare Staff Web is built on **Ant Design Blazor**. PatientWeb and Mobile use native CSS. Early ExITS decisions kept **new** Platform Admin on native CSS to avoid coupling Admin to HealthCare’s UI stack and to share a native foundation with POS. **P15-WP01 (2026-08-01)** authorizes Ant Design Blazor for Platform Admin specifically (see ADR-015). A brief Fluent UI Admin direction was cancelled before push and is superseded.

## Decision

1. **Existing HealthCare Staff Web retains Ant Design Blazor.** No HealthCare UI rewrite, modernization, framework migration, or restyling is in the current ExITS MVP work.
2. **Existing HealthCare Patient Web and MAUI retain their current implementations.**
3. **Platform Admin uses Ant Design Blazor** per **ADR-015** (pinned `AntDesign` package; Ant Design Pro Blazor as visual/structural reference only). **No Tailwind. No Fluent UI.** Compact/Comfortable density, Light/Dark/System themes, English and Filipino localization, and accessibility requirements remain in force.
4. **PinoyBusinessPOS** retains the **native UI foundation** (MAUI Blazor Hybrid + native CSS / DesignSystem). **No Ant Design requirement. No Tailwind.**
5. **Shared consistency** across products comes from semantic design principles, terminology, and UI-independent contracts — **not** from forcing one component library into POS.
6. Platform Admin may reuse HealthCare **framework-independent** patterns (authz, org/user workflows, pagination/search models, status semantics, modal/notification *contracts*, page-state patterns). It must **not** depend on HealthCare clinical/navigation/permission presentation or HealthCare project references.
7. **MVP date control** remains a controlled wrapper; rich calendars only by approved need (ADR-008).

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
- Fluent UI for Platform Admin — cancelled before push; superseded by ADR-015.
- Sharing Ant Design Pro source / HealthCare Staff projects into ExItS Admin — rejected.

## Correction note

An earlier P0-WP03 draft incorrectly stated that interim Platform Admin would retain Ant Design. That statement was superseded by the native-Admin rule.

### P15-WP01 amendment (2026-08-01)

Platform Admin is authorized to use **Ant Design Blazor** per **ADR-015**. The native-only Admin requirement and any Fluent UI Admin direction are **superseded for Platform Admin**. POS remains native/DesignSystem.
