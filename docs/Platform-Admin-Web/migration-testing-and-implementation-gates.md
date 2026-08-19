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
