# Cursor Work-Package Prompt Template

## Assignment

Implement only `<PHASE / WORK PACKAGE>`.

## Required reading

Read the dashboard, active phase, relevant product and engineering documents, and the previous completion report.

## Inspection

Run Git status, inspect existing implementation/tests and preserve unrelated changes.

## Approved scope

`<INSERT SCOPE>`

## Out of scope

`<INSERT EXCLUSIONS>`

## Mandatory rules

- Preserve Platform and product regression safety.
- Enforce platform/product/database boundaries.
- Enforce tenant isolation and server authorization.
- Keep UI framework choices aligned with ADR-015: Ant Design for Platform Admin and native DesignSystem for POS.
- Use native CSS/CSS isolation for POS UI.
- Localize all new user-facing POS strings in English and Filipino.
- Support light/dark/system themes for new POS UI.
- Build only reusable components required by the active phase.

## Validation and reporting

Run applicable tests, update dashboard/phase/report, create one focused commit, record hash and report final Git status.
