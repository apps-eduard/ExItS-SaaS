# ADR-022 — Separated AntDesign browser hosts and unified authentication

[Decisions](README.md) | [ADR-015](ADR-015-antdesign-blazor-platform-admin.md) | [ADR-010](ADR-010-separate-ui-implementations-platform-and-pos.md) | [ADR-017](ADR-017-scope-bound-sessions.md) | [Phase 25](../phases/phase-25-organization-web-admin.md)

| Field | Value |
|---|---|
| Status | **Accepted** |
| Date | 2026-08-13 |
| Related | ADR-010, ADR-015, ADR-016, ADR-017, P25-WP02, P25-WP03 |

## Decision summary

```text
Browser web applications are separated by responsibility:
  Platform Admin     → internal :8090  (production HTTPS :443 / platform host)
  Organization Web   → internal :8093  (production HTTPS :443 / org host)
  Personal Web       → internal :8094  (production HTTPS :443 / personal host)

All three use AntDesign Blazor 1.6.2 via Directory.Packages.props
  and shared ExItS.Web.UI conventions (shell, theme, culture, page header).

Authenticate once at canonical /admin/login.
Authorize independently per app/account scope (ADR-016 / ADR-017).
Do not share one unrestricted cookie across hosts.
POS / MAUI remains DesignSystem (no AntDesign).
```

## Context

P25-WP01 delivered Organization Web as a DesignSystem management host. Platform Admin already used AntDesign and also hosted Personal product pages. The owner decision is that **all ExItS browser applications** use AntDesign Blazor, while remaining **separate processes** so Platform operators, organization managers, and Personal users do not share one giant Admin surface.

Public production entry is HTTPS :443. Local Validation uses distinct internal ports. Reverse proxy maps public hostnames to private app ports. 8090/8093/8094 are not public production UX.

## Decision

1. **Separate hosts by responsibility.** Platform.Admin is the long-term Platform operator console. Organization Web owns organization management/reporting. Personal Web owns Personal product UI. Business logic stays in existing APIs/use cases.
2. **AntDesign Blazor is the browser UI standard.** Pin at **1.6.2** in `Directory.Packages.props`. Do not run unrelated UI stacks (Tailwind, Fluent UI, MudBlazor) in these hosts.
3. **Shared conventions, not wrappers around every Ant component.** `ExItS.Web.UI` owns app-level patterns: theme (Light/Dark/System), culture set, page header, empty/access-denied/pager, host options, safe return paths, handoff URL helpers.
4. **Public production uses HTTPS :443.** Internal Kestrel ports stay private.
5. **Unified authentication, app-specific authorization.** Canonical sign-in is Platform Admin `/admin/login`. Cross-host SSO uses a **60-second SHA-256 hashed one-time ticket** in process memory (`MemoryWebHandoffTicketStore`). Each host then sets its own cookie (`.ExItS.Admin.*`, `.ExItS.OrgWeb.*`, `.ExItS.PersonalWeb.*`).
6. **Never trust client OrganizationId.** Membership is validated server-side before handoff and again by product APIs.
7. **Migration preserves behavior.** Do not delete Organization/Personal pages until a replacement host exists. Compatibility redirects from `/admin/personal/*` to Personal Web are allowed.
8. **No database migration for UI separation.** Handoff tickets are in-memory; multi-instance production would need a later shared store (not Redis in this WP).

## Why the architecture is split

- Account scopes must not collapse (Personal ≠ Organization staff ≠ Platform admin).
- Organization Web must never become a checkout client.
- Platform Admin must not remain the long-term product UI for Org/Personal.
- Separate cookies prevent one unrestricted session from authorizing every host.

## Rejected alternatives

| Alternative | Why rejected |
|---|---|
| Single giant Platform.Admin host | Mixes operator and product UX; weakens account-scope boundaries |
| Unrelated UI frameworks per host | Incoherent browser experience; duplicate a11y/theme work |
| Shared unrestricted cookie across apps | Authorization leakage across Platform / Org / Personal |
| Delete/rewrite migration | Destroys working P25-WP01 behavior |
| Invent OAuth/OIDC for this WP | Existing Platform sessions + one-time tickets are sufficient |
| Redis / extra microservice for routing | Out of scope; in-memory tickets are enough for single-instance LV |

## Consequences

- ADR-015 remains the Admin AntDesign pin; this ADR **extends** AntDesign to Organization Web and Personal Web.
- ADR-010 POS/MAUI DesignSystem clause is **unchanged**.
- Architecture tests now **require** AntDesign in Org/Personal Web and **forbid** DesignSystem as the Org Web production UI.
- Local Validation starts five app processes: Admin 8090, Platform API 8091, POS API 8092, Org Web 8093, Personal Web 8094.
