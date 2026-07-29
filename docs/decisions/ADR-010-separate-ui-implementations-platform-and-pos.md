# ADR-010 — Separate UI Implementations for Platform and POS

[Decisions](README.md) | [UI design system](../engineering/ui-design-system.md) | [UI reuse assessment](../reuse/healthcare-ui-reuse-assessment.md)

| Field | Value |
|---|---|
| Status | **Accepted** (validated P0-WP03) |
| Date | 2026-07-29 |
| Related | ADR-004, ADR-005, ADR-006, ADR-007, ADR-008 |

## Context

HealthCare Staff Web is built on **Ant Design Blazor 1.6.2** with substantial direct page usage and thin modal/toast wrappers. PatientWeb and Mobile use **native CSS** without Ant Design. PinoyBusinessPOS requires compact bilingual (English/Filipino) cashier workflows, Light/Dark/System themes, and MAUI Blazor Hybrid on Android/Windows (future iOS). Forcing one component framework across all products would either rewrite a completed HealthCare MVP or pull Ant Design / Tailwind into POS contrary to product requirements.

## Decision

1. **Existing HealthCare Staff Web and interim ExITS Platform Admin retain Ant Design Blazor.**
2. **Do not rewrite HealthCare UI** during Phase 0 or early platform extraction solely for visual unification.
3. **PinoyBusinessPOS uses native CSS, Blazor CSS isolation, CSS custom properties, and a product Razor component library.**
4. **No Ant Design dependency** and **no Tailwind** in PinoyBusinessPOS.
5. **Shared across products:** semantic design principles, token *names*, localization conventions (`en`/`fil`), validation/formatting helpers, and UI-independent models (for example pagination DTOs, status tone enums).
6. **Framework-specific components are not shared** (no Ant↔native switcher component).
7. **MVP date control** is a native `DateField` wrapper; rich custom calendars only by approved need (ADR-008).
8. New Platform Admin features initially follow the existing Ant Design language; optional Ant wrappers for new controls; modernization is a future approved activity.

## Consequences

### Positive

- Preserves HealthCare MVP investment and reduces extraction risk.
- Lets POS optimize for touch density, offline MAUI, and Filipino localization without Ant constraints.
- Clear ownership: Platform Admin Ant, POS native.

### Negative / risks

- Two visual implementations to maintain (mitigated by shared tokens/principles).
- Risk of Ant patterns leaking into POS (mitigated by catalog + review rule).
- Platform Admin may later need a modernization WP if Ant coupling blocks portfolio features.

## Validation evidence (P0-WP03)

- AntDesign referenced only from `HealthCare.Web`.
- No shared UI RCL across Web/PatientWeb/Mobile.
- Localization and Light/Dark/System product themes are missing in HealthCare.
- PatientWeb/Mobile demonstrate viable native CSS approaches.

## Rejected alternatives

- Rewrite HealthCare to native CSS immediately — high regression cost.
- Adopt Ant Design in POS — conflicts with POS requirements.
- Adopt Tailwind in POS — explicitly prohibited.
- Single polymorphic component library switching renderers — complexity and coupling.
