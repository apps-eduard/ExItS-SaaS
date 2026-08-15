# Organization Web — UI & responsive standard

**Status:** Phase 25 OPEN — Owner Validation Pending  
**Audience:** Organization Web (management host only)

## Shared components

| Component | Purpose |
|-----------|---------|
| `OrgAlert` | Sanitized error/success + optional retry |
| `OrgLoading` | Skeleton loading block |
| `OrgEmpty` | Empty guidance + optional actions |
| `OrgStatusBadge` | Status text + tone (not color-only) |
| `OrgSection` | Section title/subtitle grouping |
| `OrgMetricCard` | Dashboard metric with unavailable state |

Use with `ExitsPageHeader`, Ant Design, and `org-page` / `org-page--form` / `org-page--narrow` wrappers.

## Authentication (Development Test User)

| Step | Behavior |
|------|----------|
| Development Test User selector | Development/Testing **only** |
| Selecting a test user | Fills **username / login identifier** only |
| Password | Remains **empty** — operator types Local Validation shared password manually |
| Sign in | Normal credentials → Platform login → session → membership → workspace routing |
| Not allowed | Auto-auth, password in browser/JS/API DTOs, fake development actors |

## Post-login routing (server-authoritative)

1. **Platform** → Platform Admin  
2. **One** qualifying Organization Web membership (Owner / Organization Administrator) → Organization Web  
3. **Multiple** qualifying orgs → workspace chooser (Cashier / Member-only excluded)  
4. **Personal-only** → Personal Web  
5. **Cashier** → never Organization Web  

## Page anatomy

1. Page header (title + short description + primary action)  
2. Optional metric sections  
3. Optional filters  
4. Main table/list/form  
5. Empty / loading / error states  

## Responsive targets

| Breakpoint | Behavior |
|------------|----------|
| ≥1440 | Wide content; tables expand |
| 1024–1439 | Collapsible sider |
| 768–1023 | Collapsible / drawer |
| 480–767 | Drawer nav; stacked forms; contained table scroll |
| &lt;480 | Full-width controls; no page overflow |

Owner checklist: [organization-web-responsive-owner-checklist.md](../validation/organization-web-responsive-owner-checklist.md).

## Error copy

| Condition | Message |
|-----------|---------|
| 403 | You don't have permission to view this section. |
| Session | Your session has expired. Please sign in again. |
| Generic | We couldn't load this information. Try again. |

## Privacy

Test User may show display name, username, and role/scope hint only. Phase 21 remains OPEN.
