# Platform Admin Web Modernization — Decisions

Accepted decision identifiers for this planning track (documentation-only).

| ID | Decision | Status |
|---|---|---|
| PWEB-D-001 | Existing `src/Platform/ExItS.Platform.Admin` remains untouched during replacement | Accepted |
| PWEB-D-002 | Future Platform Admin frontend is a separate application | Accepted |
| PWEB-D-003 | Platform Admin remains a Platform control-plane surface, not a POS or PLM operational console | Accepted |
| PWEB-D-004 | New frontend must consume server-authoritative Platform APIs/contracts; must never directly access Platform persistence | Accepted |
| PWEB-D-005 | Documentation completion does not authorize implementation | Accepted |
| PWEB-D-006 | The future application is the ExItS Platform SaaS Control Center; its scope is shared SaaS control-plane administration | Accepted |
| PWEB-D-007 | UX personas are design artifacts, not authorization roles; authorization is governed by existing Platform roles and permissions | Accepted |
| PWEB-D-008 | Product-operational workflows (POS, PLM) are explicitly excluded from the SaaS Control Center navigation | Accepted |
| PWEB-D-009 | Navigation visibility is a UX convenience, not a security boundary; all authorization is server-side | Accepted |
| PWEB-D-010 | DOC-03 approved target stack: React + TypeScript + Vite; Tailwind CSS + shadcn/ui + Lucide; React Router; TanStack Query/Table; React Hook Form + Zod; Motion; backend is existing .NET Platform API | Accepted |
| PWEB-D-011 | State management mapping: TanStack Query for server state; React Hook Form + Zod for forms/schema; local React state for transient UI; minimal shared UI context only when justified | Accepted |
| PWEB-D-012 | API client boundary: React page -> feature service/hook -> typed API client -> `ExItS.Platform.Api`, with server-authoritative authz and normalized problem+json error handling | Accepted |
| PWEB-D-013 | Auth security posture for React is evidence-based: cookie/session-first when compatible; explicit CSRF + OpenAPI/typed-client gaps recorded if evidence is incomplete | Accepted |
| PWEB-D-014 | Dependency policy governance: review cadence expectations, no auto-merge dependency PRs, and lock file required when implementation begins | Accepted |
| PWEB-D-015 | React replacement uses canonical ExItS green brand tokens from DesignSystem, not the Ant Design blue overrides from the current Admin | Accepted |
| PWEB-D-016 | Compact density is the default for administrative data views; comfortable density for form-heavy flows | Accepted |
| PWEB-D-017 | WCAG 2.2 AA is the accessibility design target (not a compliance claim for the current application) | Accepted |
| PWEB-D-018 | Motion is restrained and functional; `prefers-reduced-motion` fully honored with 0ms token overrides | Accepted |
| PWEB-D-019 | DOC-05 application shell: persistent primary sidebar + top bar with context switcher, environment indicator, search entry, and account menu; breadcrumbs under header; responsive drawer navigation for tablet/narrow | Accepted |
| PWEB-D-020 | DOC-05 global interactions: global search for supported entities (server-side, permission-safe; capability requirement for DOC-09) is distinct from command palette (safe navigation/commands only; no destructive one-keystroke mutations) | Accepted |
| PWEB-D-021 | DOC-05 entity context rules: organization/product/user/commercial contexts are UX/navigation context only; server authorization must validate access and must not rely on client-supplied OrganizationId | Accepted |
| PWEB-D-022 | DOC-05 canonical page templates + cross-page UX behavior are standardized (breadcrumbs, deep links, browser back/forward, session expiry, forbidden/not-found, stale-data indication, retry rules, success toasts, destructive confirmation) | Accepted |
| PWEB-D-023 | DOC-05 keyboard model: predictable Tab order; Escape closes overlays only when safe; `Ctrl+K` opens command palette and `Alt+/` focuses global search; shortcuts must never bypass confirmation requirements | Accepted |
| PWEB-D-024 | DOC-06 core screen specifications standardize required UI surfaces and introduce stable capability requirement IDs (`PWEB-CAP-*`); these must not be claimed as implemented until DOC-09 verification | Accepted |
| PWEB-D-025 | DOC-07 commercial screen specifications enforce money ownership boundaries: Platform SaaS billing screens must never display POS operational money or PLM operational money; usage/metering screens reference PLM LOAN_DISBURSED as a future product contract concept (D-P12-03 unresolved) without inventing transport | Accepted |
| PWEB-D-026 | DOC-07 high-risk commercial actions (subscription state change, plan change, entitlement override, manual payment, payment void) require explicit confirmation dialogs; UI confirmation never replaces server authorization/audit | Accepted |
| PWEB-D-027 | DOC-08 security UX: step-up auth is a hook for policy-defined sensitive operations (`PlatformLifecycleStepUp` exists; generalization is future); no impersonation/support-login invented; forbidden states use minimum-disclosure 403; no secret/token/credential display | Accepted |
| PWEB-D-028 | DOC-08 audit explorer surfaces existing `platform.audit_records` only; no POS operational audit; no audit record edit/delete; export is itself audited | Accepted |
| PWEB-D-029 | DOC-08 Platform Operations screens are evidence-gated: only display what the backend can actually report; no fabricated health dashboards; capabilities marked for DOC-09 verification | Accepted |
| PWEB-D-030 | DOC-08 Platform Settings strictly separates Platform-global, organization-scoped, and product-local (read-only reference) settings; no POS/PLM operational configuration editing from Platform | Accepted |

| PWEB-D-031 | DOC-10 staged replacement strategy: six stages (current state → scaffold → feature slices → parity → acceptance → cutover → retirement); no big-bang rewrite; existing Admin retained as fallback at every stage | Accepted |
| PWEB-D-032 | DOC-10 feature parity model: old Admin cannot be removed merely because new routes exist; parity is measured per feature across authorization, audit, states, accessibility, and browser tests | Accepted |
| PWEB-D-033 | DOC-10 implementation gates: seven sequential gates (A–G); documentation completion (Gate A) does not authorize React scaffold (Gate C); each gate requires explicit authorization | Accepted |
| PWEB-D-034 | DOC-10 cross-document audit: all 20 planning documents verified consistent; no contradictions found across vision, boundaries, auth, design, screens, API matrix, and status | Accepted |

Library decisions are recorded by DOC-03; design system decisions by DOC-04.

