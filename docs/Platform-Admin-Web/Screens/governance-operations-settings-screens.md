# Platform Admin Web — Governance, Operations + Settings Screen Specifications

**Status:** Documentation Only — implementation not authorized  
**Source:** PLATFORM-WEB-DOC-08  
**Branch:** `docs/platform-admin-web-v2`

---

## 0. Security UX principles

1. High-risk actions require explicit confirmation dialogs (default focus on Cancel).
2. Step-up authentication (password re-entry or future MFA challenge) is a hook for policy-defined sensitive operations. `PlatformLifecycleStepUp` exists for select Platform lifecycle actions; generalization is future work.
3. Server authorization remains authoritative — UI confirmation never replaces it.
4. Forbidden states reveal minimum necessary information (generic 403 page per DOC-05/06 template; no permission enumeration).
5. No secret, token, credential, or password display in any screen.
6. No silent permission failure — denied operations surface an explicit error.
7. Audit evidence is append-only; no edit/delete through the UI.
8. Last-owner/admin safety: the final active Platform Administrator and the final active Organization Owner are protected server-side from suspension, deactivation, removal, or demotion.
9. No impersonation or support-login behavior is introduced. `platform.support-session.start` exists in the authorization matrix; its UX surface (if any) will be defined only when an approved contract authorizes it.

---

## 1. Capability requirement IDs (DOC-08)

| ID | Description |
|---|---|
| `PWEB-CAP-AUDIT-LIST` | Query Platform audit records with filters |
| `PWEB-CAP-AUDIT-GET` | Get single audit record detail |
| `PWEB-CAP-AUDIT-EXPORT` | Export filtered audit records |
| `PWEB-CAP-AUTH-SESSION-LIST` | List active browser sessions for a user |
| `PWEB-CAP-AUTH-SESSION-REVOKE` | Revoke a browser session |
| `PWEB-CAP-AUTH-TOKEN-LIST` | List active access tokens for a user |
| `PWEB-CAP-AUTH-TOKEN-REVOKE` | Revoke an access token |
| `PWEB-CAP-AUTH-CREDENTIAL-RESET` | Initiate credential reset for a user |
| `PWEB-CAP-AUTH-LOCKOUT-CLEAR` | Clear account lockout |
| `PWEB-CAP-AUTH-MFA-STATUS` | View MFA readiness status for a user |
| `PWEB-CAP-AUTH-EXTERNAL-LIST` | List external login providers linked to a user |
| `PWEB-CAP-GOVERNANCE-ROLE-LIST` | List Platform role assignments |
| `PWEB-CAP-GOVERNANCE-ROLE-ASSIGN` | Assign Platform role to user |
| `PWEB-CAP-GOVERNANCE-ROLE-REVOKE` | Revoke Platform role from user |
| `PWEB-CAP-GOVERNANCE-PERMISSION-VIEW` | View effective permissions for a user/role |
| `PWEB-CAP-OPS-HEALTH-STATUS` | View Platform health/status (future; requires backend) |
| `PWEB-CAP-OPS-EVENT-DELIVERY` | View event delivery status (future; requires backend) |
| `PWEB-CAP-SETTINGS-PLATFORM-LIST` | List Platform global settings |
| `PWEB-CAP-SETTINGS-PLATFORM-MANAGE` | Update Platform global settings |
| `PWEB-CAP-SETTINGS-ORG-LIST` | List organization-scoped settings (in org context) |
| `PWEB-CAP-SETTINGS-ORG-MANAGE` | Update organization-scoped settings (in org context) |

Backend existence is **not claimed**. DOC-09 will verify.

---

## 2. A) Audit Explorer

### Purpose
Provide Platform administrators with visibility into Platform governance audit records: who did what, when, to which resource, in which organization context, and with what outcome. Uses the existing `platform.audit_records` infrastructure.

### Route concept
"Audit" page in the Governance / Audit navigation group. Also accessible as a tab within the Organization workspace (org-scoped view).

### Primary personas
- Platform Administrator
- Platform Support (view only)
- Future Platform Auditor role

### Access / authorization expectation
- `platform.audit.view` required. Organization-scoped audit additionally requires org Owner/Manager or Platform `ViewAuditRecords`.

### Data displayed
- Audit record list: timestamp (UTC), actor (identifier + type), action code, target type/id, organization (if applicable), product code (if applicable), outcome (success/denied), correlation id.
- Detail view: all fields above plus reason, safe summary, related records by correlation.

### Primary actions
- View audit record detail.
- Filter/search audit records.

### Secondary actions
- Export filtered results (if `PWEB-CAP-AUDIT-EXPORT` available).
- Navigate to related entity (actor, organization, target resource) where links are meaningful.

### Search
- Full-text search on action code, actor identifier, target identifier.

### Filtering
- Date range (from/to UTC).
- Actor filter.
- Action code filter (multi-select from known action codes).
- Organization filter.
- Product filter.
- Outcome filter (success/denied).
- Branch filter (for org-scoped governance audit).

### Sorting
- Default: newest first. Sort by timestamp, action, actor.

### Pagination
- Server pagination with page/pageSize.

### Table / card behavior
- Desktop: dense table with monospace timestamps and correlation ids. Mobile: card list with expandable detail.

### Loading / empty / zero-result / error / forbidden
- Standard per DOC-06 template.
- Empty state: "No audit records match your filters."
- Forbidden: generic 403 per DOC-05; no enumeration of what the user cannot see.

### Destructive actions
- None. Audit records are append-only; no edit or delete is possible through the UI.

### Audit implications
- Audit export actions are themselves audited.

### Security constraints
- Audit summaries must never contain passwords, tokens, PINs, card numbers, PHI, or raw exception dumps. This is enforced at the backend; the UI simply displays the sanitized summary.
- Cross-organization audit access is denied server-side; the UI must not attempt to display records from unauthorized organizations.

### Required backend capabilities
- `PWEB-CAP-AUDIT-LIST`
- `PWEB-CAP-AUDIT-GET`
- `PWEB-CAP-AUDIT-EXPORT`

### Explicit non-goals
- No POS operational audit (sales, stock, orders). POS uses actor-on-record fields, not `platform.audit_records`.
- No audit record edit or deletion.
- No password step-up on audit read (explicitly excluded by WP15E).

---

## 3. B) Identity / Authentication Administration

### Purpose
Platform-owned administrative visibility and management of user authentication state: sessions, access tokens, credential lifecycle, lockout, MFA readiness, and external login providers. This is administrative oversight of identity security, not user self-service.

### Route concept
Within the Platform Users / Identity section, as a detail panel or tab on the user detail page (cross-reference DOC-06 Platform Users screen).

### Primary personas
- Platform Administrator
- Platform Support (limited by `platform.accounts.security-manage`)

### Access / authorization expectation
- `platform.accounts.view` for viewing authentication state.
- `platform.accounts.security-manage` for security mutations (session revoke, lockout clear, credential reset).

### Data displayed
- **Sessions tab**: active browser sessions — session id (truncated), created UTC, last activity, IP/user-agent (if available), status.
- **Access tokens tab**: active access tokens — token id (truncated), created, bound organization/product, status, last used.
- **Credential status**: credential exists (boolean), last changed date, lockout state (locked/unlocked, lockout end), failed attempt count.
- **MFA status**: readiness state (not enrolled / readiness-only). MFA enforcement is deferred; display current readiness signals only.
- **External logins**: linked providers (Google, Facebook) with link date. No provider secrets displayed.
- **Recovery email**: verified/unverified status only. The actual email address is visible only if `platform.accounts.view` permits it.

### Primary actions
- Revoke a browser session (if `PWEB-CAP-AUTH-SESSION-REVOKE`).
- Revoke an access token (if `PWEB-CAP-AUTH-TOKEN-REVOKE`).

### Secondary actions
- Clear account lockout (if `PWEB-CAP-AUTH-LOCKOUT-CLEAR`). Confirmation required.
- Initiate credential reset (if `PWEB-CAP-AUTH-CREDENTIAL-RESET`). Confirmation required.

### High-risk action UX
- **Session revoke**: confirmation dialog naming the session. Default focus on Cancel.
- **Token revoke**: confirmation dialog showing token id (truncated) and bound context.
- **Lockout clear**: confirmation dialog. Step-up auth hook available for policy.
- **Credential reset**: confirmation dialog with explicit warning that this invalidates the user's current password. Step-up auth hook available.

### Destructive actions
- Session revoke, token revoke, lockout clear, credential reset are destructive. All require confirmation.

### Audit implications
- All security mutations are audited (session revoke, token revoke, lockout clear, credential reset).

### Security constraints
- **No password display.** Never show current or historical passwords.
- **No token value display.** Token ids are truncated; full bearer values are never surfaced.
- **No credential creation from this screen.** Credential creation is part of user onboarding, not administrative override.
- **No impersonation.** The `platform.support-session.start` permission exists in the authorization matrix but its UX is not defined here. Do not invent support-login behavior.

### Required backend capabilities
- `PWEB-CAP-AUTH-SESSION-LIST`
- `PWEB-CAP-AUTH-SESSION-REVOKE`
- `PWEB-CAP-AUTH-TOKEN-LIST`
- `PWEB-CAP-AUTH-TOKEN-REVOKE`
- `PWEB-CAP-AUTH-CREDENTIAL-RESET`
- `PWEB-CAP-AUTH-LOCKOUT-CLEAR`
- `PWEB-CAP-AUTH-MFA-STATUS`
- `PWEB-CAP-AUTH-EXTERNAL-LIST`

### Explicit non-goals
- No password visibility or password field.
- No token value display.
- No MFA enrollment or enforcement (readiness only; enforcement deferred).
- No impersonation/support-login.
- No credential storage or creation from the admin surface.

---

## 4. C) Access / Governance

### Purpose
Visibility into Platform role assignments and effective permissions. Allows Platform administrators to assign and revoke Platform roles. This does not create a second authorization model — it surfaces the existing `PlatformAuthz` role assignment system.

### Route concept
"Roles & Permissions" page in the Governance / Audit navigation group.

### Primary personas
- Platform Administrator

### Access / authorization expectation
- `platform.platform-staff.manage` for role assignment/revocation.
- `platform.accounts.view` for viewing role assignments.

### Data displayed
- **Role assignments list**: user identifier, assigned role(s), scope (platform-wide or organization-scoped), assigned by, assigned date.
- **Role detail**: role name, permissions included, current assignees.
- **Effective permissions view**: for a selected user, show the resolved permission set from their role assignments.

### Primary actions
- Assign role to user (if `PWEB-CAP-GOVERNANCE-ROLE-ASSIGN`).
- Revoke role from user (if `PWEB-CAP-GOVERNANCE-ROLE-REVOKE`).

### Secondary actions
- View effective permissions for a user.
- Filter assignments by role, user, scope.

### Search
- Search by user identifier or name.

### Filtering
- Filter by role (Platform Administrator, Platform Support, future roles).
- Filter by scope (platform-wide / organization-scoped).

### Sorting
- Sort by user, role, assigned date.

### Pagination
- Server pagination.

### High-risk action UX
- **Role assignment**: confirmation dialog showing user, role, and scope.
- **Role revocation**: confirmation dialog with warning. If the user is the last Platform Administrator, server-side protection prevents the revocation and the UI displays the server error.
- Step-up auth hook available for role mutations.

### Destructive actions
- Role revocation is destructive. Confirmation required with default focus on Cancel.

### Audit implications
- Role assignment and revocation are audited.

### Security constraints
- The UI does not define permissions — it displays the server-defined permission set. There is no "custom permission" creation.
- Last-admin protection is server-enforced; the UI surfaces the server's rejection.

### Required backend capabilities
- `PWEB-CAP-GOVERNANCE-ROLE-LIST`
- `PWEB-CAP-GOVERNANCE-ROLE-ASSIGN`
- `PWEB-CAP-GOVERNANCE-ROLE-REVOKE`
- `PWEB-CAP-GOVERNANCE-PERMISSION-VIEW`

### Explicit non-goals
- No creation of new roles or permissions through the UI (roles are defined in domain code).
- No product-local role management (POS Cashier, PLM Collector, etc.) — those belong to product administration.
- No duplicate authorization model.

---

## 5. D) Platform Operations

### Purpose
Provide visibility into Platform operational health where backend signals exist or are planned. This screen surfaces what the backend can report; it does not fabricate health data.

### Route concept
"Operations" page in the Operations navigation group (or a sub-section if the group is small).

### Primary personas
- Platform Administrator

### Access / authorization expectation
- Platform Administrator role or future Platform Operations role.

### Data displayed (where backend supports it)
- **Application health**: basic health check status (healthy/degraded/unhealthy) if a health endpoint exists.
- **Event delivery status**: if async event delivery infrastructure exists (per Product Foundation async-events guidance), display delivery queue depth, failed deliveries, and retry status.
- **Background job status**: if background jobs exist, display job name, last run, status, next scheduled.

### Current backend evidence
- Health endpoints: ASP.NET Core health checks may exist in deployed configuration. Verify in DOC-09.
- Async events: Product Foundation documents async event patterns but implementation status varies. Verify in DOC-09.
- Background jobs: product catalog import jobs exist (Phase 20). Platform-level background jobs require verification.

### Primary actions
- Refresh health status.
- Retry failed event delivery (if backend supports it).

### Secondary actions
- Filter events by status, type, date range.

### Pagination
- Server pagination for event/job lists.

### Loading / empty / zero-result / error / forbidden
- Standard per DOC-06 template.
- If no backend capability exists, the section displays "Operational monitoring is not yet available" with a reference to the planned capability.

### Destructive actions
- Event retry is non-destructive but significant; confirmation dialog.

### Audit implications
- Manual event retries are audited.

### Security constraints
- Health data must not expose internal infrastructure details (connection strings, server names, stack traces).
- Event payloads displayed must follow the same sanitization rules as audit summaries.

### Required backend capabilities
- `PWEB-CAP-OPS-HEALTH-STATUS` (future; requires verification)
- `PWEB-CAP-OPS-EVENT-DELIVERY` (future; requires verification)

### Explicit non-goals
- No fabricated health dashboards. Only display what the backend can actually report.
- No POS/PLM operational monitoring (sync queue depth, offline device status, etc.).
- No infrastructure-level monitoring (CPU, memory, disk). That belongs to deployment/infrastructure tooling.
- No log viewer. Logs belong in dedicated logging infrastructure.

---

## 6. E) Platform Settings

### Purpose
Administer Platform-owned configuration settings. Strictly separated into Platform global settings, organization-scoped settings, and awareness of product-local settings (which are not editable from Platform).

### Route concept
"Settings" page in the Platform Settings navigation group.

### Primary personas
- Platform Administrator

### Access / authorization expectation
- Platform global settings: `platform.platform-staff.manage` or equivalent.
- Organization-scoped settings: requires organization context + appropriate permission.

### Data displayed

#### Platform global settings
Settings genuinely owned by Platform (not product-local):
- Default trial duration.
- Default grace period for subscription expiry.
- Platform branding configuration (if applicable).
- Feature flag overrides (Platform-level, not product-local).
- Email/notification configuration visibility (without secrets).

#### Organization-scoped settings (in org context)
Settings the Platform administers per organization:
- Organization profile metadata.
- Organization branding (logo, theme overrides within design system constraints).
- These overlap with the Organization workspace defined in DOC-06; this screen provides a Platform-admin perspective when no org context is selected.

#### Product-local settings (read-only reference)
- Display a summary of which products an organization has and link to the product's own administration surface.
- Platform does not own or edit POS register configuration, PLM loan product settings, or other product-operational configuration.

### Primary actions
- Edit Platform global setting (if `PWEB-CAP-SETTINGS-PLATFORM-MANAGE`).

### Secondary actions
- View organization-scoped settings (in org context).
- Navigate to product administration for product-local settings.

### High-risk action UX
- **Changing global settings** (trial duration, grace period): confirmation dialog showing current value, new value, and impact scope ("affects all new subscriptions").
- Step-up auth hook available for settings mutations.

### Destructive actions
- Settings changes are generally non-destructive but significant. Confirmation required for changes with broad impact.

### Audit implications
- All settings changes are audited with actor, old value, new value, and timestamp.

### Security constraints
- Email/notification provider secrets (SMTP passwords, API keys) must never be displayed. Show configuration status (configured/not configured) only.
- Do not expose database connection strings or infrastructure secrets.

### Required backend capabilities
- `PWEB-CAP-SETTINGS-PLATFORM-LIST`
- `PWEB-CAP-SETTINGS-PLATFORM-MANAGE`
- `PWEB-CAP-SETTINGS-ORG-LIST`
- `PWEB-CAP-SETTINGS-ORG-MANAGE`

### Explicit non-goals
- No POS operational configuration (register settings, receipt templates, tax rules).
- No PLM operational configuration (loan product settings, interest rules, collection policies).
- No infrastructure configuration (database, deployment, networking).
- No secret/credential editing through the UI.
