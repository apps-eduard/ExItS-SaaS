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

Library decisions are recorded by DOC-03; design system decisions by DOC-04.

