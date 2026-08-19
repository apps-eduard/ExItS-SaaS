# Pinoy Loan Manager — Web / MAUI Component Sharing Policy

**Status:** Accepted architecture policy (PLM-DOC-09); **PLM-D-00-09 Closed**
**Implementation present:** No
**Last updated:** 2026-08-19

Approved sharing and isolation strategy between Organization Web and MAUI Hybrid. Closes **PLM-D-00-09**. Complements [application-surface-model.md](application-surface-model.md), [source-and-project-layout.md](source-and-project-layout.md), and [../architecture.md](../architecture.md).

No client project is authorized solely because this policy is closed. Scaffold remains gated by **PLM-D-00-03** and explicit owner authorization.

---

## Principle

Organization Web and MAUI Hybrid are **separate presentation targets** over the **same server-authoritative** Application and API layers. Sharing is intentional and bounded. MAUI must not inherit full Web scope by default.

---

## Shared (approved)

The following may be shared across Web and MAUI when implementation is authorized:

| Layer | Share |
|---|---|
| **Domain** | Single product domain model |
| **Application** | Use cases, authorization guards, workflow rules |
| **Api / ApiClient** | Contracts and DTOs consumed by both clients |
| **Validation messages** | Shared business rule text where identical |
| **Formatting helpers** | Money, dates, schedule labels (non-UI-framework-specific) |
| **Authorization context** | Scope and grant evaluation results from server |

Presentation must not reimplement business rules that belong in Application.

---

## Not shared (by default)

The following remain **Web-only** unless a future ADR explicitly extends MAUI:

| Area | Reason |
|---|---|
| Full Organization dashboard and KPI workspace | MAUI is field-subset |
| Staff / role / grant administration | High-risk configuration |
| Loan Product configuration | Admin complexity |
| Traditional origination back-office review consoles | Full-screen ops |
| Deep operational and accounting reports | Screen real estate and scope |
| Branch treasury administration | Cash oversight belongs on Web |
| Bulk imports / exports | Admin operations |
| Audit search and compliance exports | Admin operations |
| Platform-adjacent settings | Not field scope |

MAUI receives **purpose-built field views** that call the same APIs, not a responsive reflow of entire Web pages.

---

## UI component sharing rules

### MVP scaffold direction

1. **Separate UI projects**: `ExItS.PinoyLoanManager.Web` and `ExItS.PinoyLoanManager.Maui`.
2. **No mandatory shared Razor Class Library (RCL) in MVP scaffold.** Initial duplication of simple field components is acceptable to preserve clear boundaries.
3. **Do not reference** PinoyBusinessPOS UI, Infrastructure, EF Core, or Npgsql from either client.
4. **Native MAUI concerns stay in Maui** — connectivity, secure storage hooks, camera, biometrics, platform permissions.
5. **Web-specific layout stays in Web** — multi-column admin layouts, dense tables, print-oriented report chrome.

### Future shared UI library (conditional)

Create `ExItS.PinoyLoanManager.Ui.Shared` (or equivalent RCL) **only when all** conditions are met:

1. Web and Maui projects exist on mainline (PLM-D-00-03 closed for scaffold).
2. At least **three** non-trivial components require identical behavior in both clients (example: money entry control, borrower summary card, payment confirmation panel).
3. Shared components contain **no** EF, Infrastructure, or direct HTTP calls — parameters and callbacks only.
4. Shared components use styling compatible with both targets (native CSS / design tokens agreed for PLM; **no Tailwind**, **no copying POS DesignSystem** without explicit ADR).
5. MAUI-specific rendering variants remain in Maui when platform behavior diverges.

Until those conditions are met, prefer duplication over premature abstraction.

---

## Personal and Platform Admin

- **ExItS Personal** is out of scope for Web/MAUI sharing; it consumes PLM APIs under Personal contracts only.
- **Platform Admin** is never part of PLM Web/MAUI sharing.

---

## Testing boundary

Shared Application behavior is tested in Application/API tests. UI sharing does not require identical UI test suites; each client validates its own critical flows.

---

## Explicit non-goals

- One Blazor UI project for Web and MAUI
- MAUI hosting full Organization Web inside a WebView shell
- Shared UI library in documentation-only phase
- Closing PLM-D-00-03 or authorizing scaffold by this document alone

---

## Decision closure

**PLM-D-00-09 — Web/MAUI component-sharing strategy: Closed.**

Evidence: this policy, [../Decisions/ADR-018-branch-treasury-float-and-ui-sharing-policy.md](../Decisions/ADR-018-branch-treasury-float-and-ui-sharing-policy.md), [../Reports/PLM-DOC-09-mobile-field-treasury-and-ui-boundaries.md](../Reports/PLM-DOC-09-mobile-field-treasury-and-ui-boundaries.md).
