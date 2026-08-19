# Platform Admin Web — Migration, Testing, and Implementation Gates

**Status:** Documentation Only — implementation not authorized  
**Source:** PLATFORM-WEB-DOC-10  
**Branch:** `docs/platform-admin-web-v2`

---

## 1. Migration / Coexistence Plan

### 1.1 Staged replacement strategy

The Platform Admin modernization follows a staged replacement approach. There is no big-bang rewrite.

| Stage | Name | Description |
|---|---|---|
| 0 | Current state | Existing Blazor Admin (`src/Platform/ExItS.Platform.Admin`) remains the authoritative, working Platform administration UI. No changes to the existing Admin are made during planning. |
| 1 | React scaffold | New React Admin application is scaffolded at `src/Platform/ExItS.Platform.Admin.Web/` as a separate frontend. The existing Admin remains fully operational and accessible. Requires explicit Product Owner authorization (Gate C). |
| 2 | Feature-by-feature API integration | Vertical feature slices are implemented in the React Admin, each consuming Platform API endpoints. Features are developed incrementally — not as a monolithic UI rewrite. Each slice must demonstrate correct authorization, loading states, error handling, and audit behavior. |
| 3 | Feature parity validation | Each implemented React feature is compared against the corresponding existing Admin capability using the Feature Parity Model (§2). Parity is validated per-feature, not as a single pass. |
| 4 | Controlled internal acceptance | The React Admin is deployed alongside the existing Admin for internal acceptance testing. Both applications remain accessible. Acceptance includes security validation, accessibility verification, and browser compatibility testing. |
| 5 | Cutover | Cutover to the React Admin as the primary Platform administration UI occurs only after explicit Product Owner authorization (Gate F). The existing Admin remains available as a fallback during the cutover period. |
| 6 | Old Admin retirement | Deprecation and removal of the existing Blazor Admin occurs in a separate, explicitly authorized work package (Gate G). Retirement requires confirmation that no operational dependency on the old Admin exists. |

### 1.2 Rollback strategy

If the React Admin fails acceptance at any stage:

- The existing Blazor Admin remains available and operational.
- No data migration or backend schema change is required for rollback (the React Admin consumes the same Platform APIs).
- Rollback is a routing/deployment decision, not a code revert.
- The React Admin can be taken offline without affecting the existing Admin.

### 1.3 Coexistence rules

- Both applications may run concurrently during Stages 1–5.
- Both consume the same Platform API endpoints with the same authentication/session model.
- No backend changes are made solely to support the new Admin during the documentation phase.
- The existing Admin's Ant Design Blazor stack and the new Admin's React stack do not share runtime dependencies.

---

## 2. Feature Parity Model

### 2.1 Purpose

Before the existing Admin can be retired, every Platform administration capability must be verified as present and functional in the React Admin. The old Admin may not be removed merely because new routes exist.

### 2.2 Parity matrix structure (future)

When implementation begins, a parity matrix will compare:

| Column | Description |
|---|---|
| Existing Admin feature | The specific capability in the current Blazor Admin |
| New Admin screen | The corresponding React Admin screen/component |
| API capability | The `PWEB-CAP-*` ID from the API capability matrix (DOC-09) |
| Authorization | Verified server-side permission enforcement |
| Audit | Verified audit record generation for mutations |
| Loading / error states | Correct loading, empty, error, and forbidden states |
| Accessibility | WCAG 2.2 AA compliance verification |
| Browser test | Playwright E2E test coverage |
| Status | Not started / In progress / Parity achieved / Regression |

### 2.3 Parity rules

- Parity is measured per feature, not per page or per route.
- A feature achieves parity when it satisfies all columns in the matrix row.
- Missing or degraded capability in any column blocks parity status for that feature.
- The parity matrix is maintained throughout the implementation phase and reviewed before cutover (Gate E).

---

## 3. Frontend Testing Strategy

### 3.1 Purpose

Define the expected testing layers for the future React Admin implementation. Tools are documented without version pinning; versions are determined at implementation time.

### 3.2 Testing layers

| Layer | Scope | Intended tooling |
|---|---|---|
| TypeScript compile / typecheck | Type safety across the entire codebase | TypeScript compiler (`tsc --noEmit`) |
| Lint / format checks | Code style, import ordering, accessibility lint rules | ESLint, Prettier |
| Unit tests | Pure functions, utilities, data transformations, form validation schemas | Vitest |
| React component tests | Component rendering, user interaction, state management, conditional display | Vitest + Testing Library |
| API client tests | Request construction, response parsing, error normalization, retry behavior | Vitest (with MSW or equivalent for HTTP mocking) |
| Integration tests | Feature-level flows combining components, hooks, and mocked API responses | Vitest + Testing Library |
| Playwright browser / E2E tests | Full browser flows: navigation, authentication, data loading, form submission, destructive confirmations | Playwright |
| Accessibility checks | Automated WCAG 2.2 AA validation, focus management, screen reader compatibility | axe-core (via Testing Library or Playwright), manual screen reader testing |
| Responsive viewport checks | Layout correctness at defined breakpoints (desktop, laptop, tablet, narrow) | Playwright viewport configurations |

### 3.3 Testing principles

- Every data-loading surface must have tests for loading, success, empty, error, and forbidden states.
- Destructive actions must have tests verifying confirmation dialogs prevent accidental execution.
- Authorization-gated UI elements must have tests verifying correct show/hide behavior based on permission state.
- Tests must not depend on production databases or external services.
- Test data must use factories or fixtures, not hardcoded production values.

---

## 4. Security Testing

### 4.1 Purpose

Define the security testing expectations for the future React Admin. These are test categories, not test implementations.

### 4.2 Security test categories

| Category | What to verify |
|---|---|
| Unauthenticated behavior | Unauthenticated users are redirected to login; no data leakage in unauthenticated state |
| Expired session | Expired sessions trigger redirect to login; cached server state is cleared; no stale data displayed |
| Forbidden permissions | Users without required permissions see the forbidden state, not a blank page or generic error; navigation items for forbidden areas are hidden |
| Organization boundary | Users cannot access organization data they are not authorized for; client-supplied OrganizationId is not trusted for authorization |
| Branch scope | Branch-scoped operations respect branch access boundaries |
| Product access | Product-scoped views enforce product access authorization |
| CSRF protection | Cookie-authenticated mutations are protected against CSRF (pending backend CSRF posture confirmation per DOC-03 gap) |
| Sensitive error handling | Error responses do not leak stack traces, connection strings, internal paths, or user data beyond what the API problem+json contract defines |
| No token leakage | Auth tokens, session tokens, and bearer values are never displayed in the UI, logged to the console, or stored in localStorage/sessionStorage |
| Destructive confirmation | All destructive operations require explicit confirmation; default focus is on Cancel; no single-keystroke destructive execution |
| Server-authoritative rejection | When the server rejects a mutation (permission denied, last-admin protection, validation failure), the UI displays the server's error and does not retry silently |
| Audit evidence | Security-sensitive mutations (role changes, credential resets, session revocations) generate audit records that are visible in the audit explorer |

---

## 5. Performance / UX Quality Gates

### 5.1 Purpose

Define measurable performance and UX expectations for the future React Admin. These are quality gates, not SLO commitments.

### 5.2 Quality gate expectations

| Area | Expectation |
|---|---|
| Route-level loading | Each route loads its own data independently; navigating to a route shows a loading state for that route's data without blocking the shell |
| No app-blocking fetches | Local page data fetches must not block the entire application; the shell (sidebar, header) remains interactive during page loads |
| Large table strategy | Tables with potentially large datasets use server-side pagination; client does not attempt to load unbounded result sets |
| Pagination | All list/table views use server pagination with explicit page/pageSize parameters; no infinite scroll unless explicitly backed |
| Query caching | TanStack Query cache boundaries are used to avoid redundant fetches during navigation; stale data is indicated where appropriate |
| Cancellation | In-flight requests are cancelled when the user navigates away before completion; search/filter requests are debounced |
| Perceived responsiveness | Navigation transitions are instant (route change renders immediately with loading state); skeleton placeholders appear within one frame of data fetch initiation |
| Lazy loading | Route-level code splitting where valuable; large feature bundles are loaded on demand, not in the initial bundle |
| No uncontrolled animation | All motion follows the design system motion tokens; `prefers-reduced-motion` is fully honored; no decorative animations that delay interaction |
| Responsive layout validation | Layout is validated at all defined breakpoints (desktop ≥1024px, laptop 768–1023px, tablet 480–767px, narrow <480px); no uncontrolled horizontal overflow |

---

## 6. Implementation Gates

### 6.1 Purpose

Define the authorization gates that control progression from documentation to implementation to cutover. Documentation completion alone does not authorize implementation.

### 6.2 Gate definitions

| Gate | Name | Requirements |
|---|---|---|
| **A** | Documentation approved | DOC-01 through DOC-10 complete. Final review complete. Corrections resolved. Explicit Product Owner authorization to proceed to Gate B. |
| **B** | Backend gap plan | DOC-09 API capability matrix reviewed. Required Platform API gaps prioritized and scheduled. Auth/browser integration gaps (CSRF posture, OpenAPI/typed-client) confirmed or planned. Backend readiness confirmed for initial feature slices. |
| **C** | React scaffold | Explicit Product Owner authorization required. React application scaffolded at `src/Platform/ExItS.Platform.Admin.Web/`. Development environment, build pipeline, and test infrastructure established. No feature implementation until scaffold is validated. |
| **D** | Feature implementation | Vertical feature slices implemented incrementally. Each slice satisfies the testing strategy (§3) and security testing (§4). Parity matrix updated per feature. No giant UI rewrite — features are delivered and validated individually. |
| **E** | Feature parity | Feature parity matrix (§2) reviewed for all required capabilities. All parity columns satisfied for each feature. Accessibility, security, and browser compatibility validated across the full surface. |
| **F** | Cutover | Explicit Product Owner approval required. Internal acceptance testing complete. Security/accessibility/browser validation passed. Rollback plan confirmed. Cutover deployed with existing Admin retained as fallback. |
| **G** | Old Admin retirement | Separate authorization required after proven cutover period. Confirmation that no operational dependency on the old Admin exists. Retirement executed as a distinct work package. |

### 6.3 Gate rules

- Gates are sequential. No gate may be skipped.
- Documentation completion (Gate A) does not authorize Gate C (React scaffold).
- Each gate requires explicit authorization before proceeding to the next.
- Gate authorization is recorded in the decisions log with the authorizing party and date.
- If a gate fails validation, work returns to the previous gate for remediation.
---

## 7. Visual Definition of Done

### 7.1 Purpose

A frontend package is not complete merely because it compiles, API calls work, and tests pass. Every significant UI package must satisfy this visual Definition of Done.

### 7.2 Verification checklist

| Category | Verification items |
|---|---|
| **Structure** | Documented screen/component structure matched |
| **Design tokens** | ExItS design tokens used; no arbitrary one-off styling |
| **Typography** | Typography hierarchy correct (page title, section, body, label, caption) |
| **Spacing** | Spacing consistency using the 4px base scale |
| **Desktop** | Correct layout at desktop viewport (>=1024px) |
| **Laptop** | Correct layout at laptop viewport (768-1023px) |
| **Tablet** | Correct layout at tablet viewport (480-767px) |
| **Narrow** | Correct layout at narrow viewport (<480px) where applicable |
| **Mobile auth** | Authentication screens polished for phone-sized screens |
| **Light theme** | Correct appearance in Light theme |
| **Dark theme** | Correct appearance in Dark theme |
| **System theme** | Correct theme switching with system preference |
| **English** | All text renders correctly in English |
| **Filipino** | All text renders correctly in Filipino; no clipping or overflow with longer labels |
| **Density** | Correct appearance in Comfortable, Balanced, and Compact modes (where relevant) |
| **Loading state** | Loading skeleton/indicator present |
| **Empty state** | Correct empty state message and optional action |
| **Zero-result** | Correct zero-result state reflecting search/filter terms |
| **Error state** | Correct partial/error state with retry |
| **Forbidden state** | Correct forbidden/unauthorized state |
| **Focus** | Visible focus ring on all interactive elements |
| **Hover** | Correct hover states |
| **Keyboard** | Full keyboard operability; logical tab order |
| **Reduced motion** | All animations suppressed when `prefers-reduced-motion: reduce` is active |
| **No clipping** | No text, icon, or control clipping |
| **No overflow** | No uncontrolled horizontal page overflow |
| **No overlap** | No overlapping controls or elements |
| **Long translations** | No broken layout with long Filipino translations |
| **Console** | Console free of unexpected runtime errors |
| **TypeScript** | TypeScript typecheck passes |
| **Lint** | ESLint/Prettier passes |
| **Tests** | Relevant unit/component/integration tests pass |
| **Accessibility** | Automated accessibility checks pass (axe-core or equivalent) |

---

## 8. Playwright Visual QA / Screenshots

### 8.1 Purpose

Document automated screenshot QA for visual consistency verification. Exact implementation tooling and versions remain future work.

### 8.2 Representative screenshot matrix

#### Platform shell / Dashboard

| Viewport | Language | Theme | Description |
|---|---|---|---|
| 1440x900 | English | Light | Primary desktop reference |
| 1440x900 | English | Dark | Desktop dark theme |
| 1280x800 | English | Light | Laptop viewport |
| 768x1024 | English | Light | Tablet responsive |

#### Authentication

| Viewport | Language | Theme | Description |
|---|---|---|---|
| 1440x900 | English | Light | Desktop sign-in Light |
| 1440x900 | English | Dark | Desktop sign-in Dark |
| 375x812 | English | Light | Phone sign-in Light |
| 375x812 | English | Dark | Phone sign-in Dark |
| 375x812 | Filipino | Light | Phone sign-in Filipino (label length stress) |
| 320x568 | English | Light | Narrow-width stress case |

### 8.3 Screenshot workflow

- Cursor generates screenshots during implementation rather than requiring the Product Owner to manually capture every routine state.
- Before Product Owner visual approval, screenshots are review/QA artifacts.
- After Product Owner visual approval, approved screenshots may become visual-regression baselines.
- Cursor must NOT automatically declare its own first screenshot as an approved baseline.
- Visual-regression baselines require explicit Product Owner sign-off.

---

## 9. Visual Reference Screens

### 9.1 Initial visual reference surfaces

The following screens are designated as the initial visual references that establish the SaaS Control Center's visual language:

| Order | Screen | Purpose |
|---|---|---|
| 1 | Authentication / Sign In | Establishes brand identity, auth visual language, mobile-first auth UX |
| 2 | Platform Dashboard | Establishes shell layout, data card patterns, summary presentation |
| 3 | Organizations List | Establishes data table/list patterns, search/filter toolbar, pagination |
| 4 | Organization Workspace | Establishes entity detail, workspace navigation, tab patterns |

Once Product Owner-approved implementations exist for these screens, later screens inherit their visual patterns rather than inventing new visual languages.

### 9.2 Design quality target

The SaaS Control Center targets the quality level of professional administrative products such as Linear, Stripe Dashboard, Vercel, and GitHub administration — but must NOT copy their visual identity. ExItS has its own brand tokens, color palette, and typography.

Target: a professional SaaS control center an administrator can comfortably use for an entire work day.

---

## 10. First Visual Implementation Checkpoint

### 10.1 Purpose

After a future authorized and validated React scaffold (Gate C), the first meaningful visual implementation milestone establishes only the visual foundation.

### 10.2 PWEB Visual Foundation checkpoint scope

The visual foundation milestone covers:

1. Authentication / sign-in experience
2. Core design tokens and primitives
3. Application shell (sidebar, top bar, content region)
4. Navigation (sidebar sections, active/hover/focus states)
5. Breadcrumbs and page-header pattern
6. Theme, language, and density preference switching
7. Dashboard (with evidence-backed widgets only)

### 10.3 Verification sequence

After implementing the visual foundation:

1. TypeScript typecheck
2. Lint
3. Tests
4. Accessibility checks
5. Playwright automated visual screenshots
6. Fix obvious visual failures
7. Git diff review
8. Focused commit
9. Push
10. Report
11. **STOP**

### 10.4 Mandatory stop

After the visual foundation milestone: **STOP**.

Do NOT automatically continue into Organizations, Users, Products, Plans, Subscriptions, Entitlements, Billing, Governance, or Settings implementation — even if later commands were queued.

Product Owner + ChatGPT review the visual foundation first. Later implementation resumes only after explicit approval.

---

## 11. Frontend Release / Cache / Version Policy

### 11.1 Purpose

Define requirements so that normal end users are never instructed to manually clear their browser cache after routine ExItS deployments.

### 11.2 Caching strategy

| Asset type | Caching behavior |
|---|---|
| Entry HTML / app bootstrap | Must not use long-lived immutable caching. Browser/proxy should revalidate on every load. |
| Content-hashed JS/CSS/static assets | May use long-lived immutable caching. Changed builds receive changed filenames (content hash in filename). |
| Fonts / images | May use long-lived caching with content-hash or versioned paths. |

### 11.3 Restrictions

- **No service worker / PWA by default.** Service workers require separate explicit authorization and an update strategy.
- **CDN / reverse proxy** caching rules must preserve entry-point revalidation. Must not pin stale application HTML.

### 11.4 Deployment requirements

- Versioned/atomic or equivalent safe release behavior.
- Avoid mixed old/new asset sets during rollout.
- Preserve previous hashed assets long enough (or use equivalent safe strategy) so existing sessions do not immediately fail on lazy chunk requests.

### 11.5 Version awareness

- Frontend build/release identifier available for diagnostics (accessible in UI or dev tools).
- Long-running Admin sessions can detect that a newer frontend release is available.
- Show a safe "New version available — Refresh" experience where useful.
- Never silently destroy unsaved administrator work.
- Refresh should occur safely.
- Manual cache clearing is NOT the normal update procedure.

### 11.6 API compatibility

- Frontend/backend rolling deployment must avoid an incompatible old-frontend/new-API or new-frontend/old-API window.
- Document compatible rollout/version strategy as a release requirement.

### 11.7 Validation

- Production/deployment validation must verify expected cache headers and update behavior where practical.
